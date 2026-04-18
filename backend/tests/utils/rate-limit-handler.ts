import { APIRequestContext, APIResponse, expect } from '@playwright/test';

/**
 * Rate Limit Handler para Playwright API Tests
 * =============================================
 * 
 * Implementa estrategias para manejar HTTP 429 (Too Many Requests)
 * con backoff exponencial y retry automático.
 * 
 * Características:
 * - Retry automático en 429
 * - Backoff exponencial configurable
 * - Respeto del header Retry-After
 * - Logging para debugging
 * 
 * @example
 * ```typescript
 * const client = new RateLimitedApiClient(request);
 * const response = await client.get('/api/bookings');
 * ```
 */

export interface RetryConfig {
  /** Número máximo de reintentos (default: 3) */
  maxRetries: number;
  /** Delay inicial en ms antes del primer retry (default: 1000) */
  initialDelay: number;
  /** Factor de multiplicación para backoff exponencial (default: 2) */
  backoffMultiplier: number;
  /** Delay máximo entre reintentos en ms (default: 10000) */
  maxDelay: number;
  /** Códigos de estado que disparan retry (default: [429, 503]) */
  retryableStatuses: number[];
  /** Mostrar logs de retry (default: false) */
  verbose: boolean;
}

const DEFAULT_RETRY_CONFIG: RetryConfig = {
  maxRetries: 3,
  initialDelay: 1000,
  backoffMultiplier: 2,
  maxDelay: 10000,
  retryableStatuses: [429, 503],
  verbose: false,
};

/**
 * Calcula el delay para el siguiente retry usando backoff exponencial
 */
function calculateBackoff(
  attempt: number,
  config: RetryConfig,
  retryAfterHeader?: string | null
): number {
  // Si el servidor especifica Retry-After, respetarlo
  if (retryAfterHeader) {
    const retryAfterSeconds = parseInt(retryAfterHeader, 10);
    if (!isNaN(retryAfterSeconds)) {
      return Math.min(retryAfterSeconds * 1000, config.maxDelay);
    }
    // Podría ser una fecha HTTP
    const retryAfterDate = Date.parse(retryAfterHeader);
    if (!isNaN(retryAfterDate)) {
      const delayMs = retryAfterDate - Date.now();
      return Math.min(Math.max(delayMs, 0), config.maxDelay);
    }
  }

  // Backoff exponencial: initialDelay * (backoffMultiplier ^ attempt)
  const exponentialDelay = config.initialDelay * Math.pow(config.backoffMultiplier, attempt);
  
  // Añadir jitter aleatorio (±10%) para evitar thundering herd
  const jitter = exponentialDelay * 0.1 * (Math.random() * 2 - 1);
  
  return Math.min(exponentialDelay + jitter, config.maxDelay);
}

/**
 * Espera un tiempo determinado
 */
const sleep = (ms: number): Promise<void> => 
  new Promise(resolve => setTimeout(resolve, ms));

/**
 * Cliente de API con manejo automático de rate limiting
 */
export class RateLimitedApiClient {
  private request: APIRequestContext;
  private config: RetryConfig;

  constructor(request: APIRequestContext, config: Partial<RetryConfig> = {}) {
    this.request = request;
    this.config = { ...DEFAULT_RETRY_CONFIG, ...config };
  }

  /**
   * Ejecuta una request con retry automático en rate limiting
   */
  private async executeWithRetry(
    method: 'get' | 'post' | 'put' | 'patch' | 'delete',
    url: string,
    options?: Parameters<APIRequestContext['post']>[1]
  ): Promise<APIResponse> {
    let lastError: Error | null = null;
    let lastResponse: APIResponse | null = null;

    for (let attempt = 0; attempt <= this.config.maxRetries; attempt++) {
      try {
        const response = await this.request[method](url, options);
        
        // Si es un código retryable, intentar de nuevo
        if (this.config.retryableStatuses.includes(response.status())) {
          lastResponse = response;
          
          if (attempt < this.config.maxRetries) {
            const retryAfter = response.headers()['retry-after'];
            const delay = calculateBackoff(attempt, this.config, retryAfter);
            
            if (this.config.verbose) {
              console.log(
                `[RateLimitedApiClient] ${response.status()} en ${url}, ` +
                `reintentando en ${delay}ms (intento ${attempt + 1}/${this.config.maxRetries})`
              );
            }
            
            await sleep(delay);
            continue;
          }
        }
        
        return response;
      } catch (error) {
        lastError = error as Error;
        
        if (attempt < this.config.maxRetries) {
          const delay = calculateBackoff(attempt, this.config);
          
          if (this.config.verbose) {
            console.log(
              `[RateLimitedApiClient] Error en ${url}: ${lastError.message}, ` +
              `reintentando en ${delay}ms (intento ${attempt + 1}/${this.config.maxRetries})`
            );
          }
          
          await sleep(delay);
        }
      }
    }

    // Si tenemos una respuesta (aunque sea 429), devolverla
    if (lastResponse) {
      return lastResponse;
    }

    // Si no, lanzar el último error
    throw lastError || new Error(`Failed after ${this.config.maxRetries} retries`);
  }

  async get(url: string, options?: Parameters<APIRequestContext['get']>[1]): Promise<APIResponse> {
    return this.executeWithRetry('get', url, options);
  }

  async post(url: string, options?: Parameters<APIRequestContext['post']>[1]): Promise<APIResponse> {
    return this.executeWithRetry('post', url, options);
  }

  async put(url: string, options?: Parameters<APIRequestContext['put']>[1]): Promise<APIResponse> {
    return this.executeWithRetry('put', url, options);
  }

  async patch(url: string, options?: Parameters<APIRequestContext['patch']>[1]): Promise<APIResponse> {
    return this.executeWithRetry('patch', url, options);
  }

  async delete(url: string, options?: Parameters<APIRequestContext['delete']>[1]): Promise<APIResponse> {
    return this.executeWithRetry('delete', url, options);
  }
}

/**
 * Wrapper funcional para una sola request con retry
 * Útil cuando no quieres instanciar un cliente completo
 */
export async function fetchWithRetry(
  request: APIRequestContext,
  method: 'get' | 'post' | 'put' | 'patch' | 'delete',
  url: string,
  options?: Parameters<APIRequestContext['post']>[1],
  retryConfig?: Partial<RetryConfig>
): Promise<APIResponse> {
  const client = new RateLimitedApiClient(request, retryConfig);
  return client[method](url, options);
}

/**
 * Espera hasta que una condición sea verdadera con polling inteligente
 * Útil para esperar a que el rate limit se resetee
 */
export async function waitForRateLimitReset(
  request: APIRequestContext,
  healthEndpoint: string = '/health',
  options: {
    timeout?: number;
    pollInterval?: number;
  } = {}
): Promise<void> {
  const { timeout = 30000, pollInterval = 1000 } = options;
  const startTime = Date.now();

  while (Date.now() - startTime < timeout) {
    try {
      const response = await request.get(healthEndpoint);
      if (response.ok()) {
        return;
      }
      if (response.status() !== 429) {
        return; // Si no es 429, el rate limit no es el problema
      }
    } catch {
      // Ignorar errores de red
    }
    await sleep(pollInterval);
  }

  throw new Error(`Rate limit no se reseteó después de ${timeout}ms`);
}

/**
 * Custom matcher para usar con expect().toPass()
 * Permite reintentar aserciones sobre respuestas de API
 * 
 * @example
 * ```typescript
 * await expect(async () => {
 *   const response = await request.get('/api/status');
 *   expectSuccessfulResponse(response);
 * }).toPass({ intervals: [500, 1000, 2000] });
 * ```
 */
export function expectSuccessfulResponse(
  response: APIResponse,
  expectedStatus: number = 200
): void {
  const status = response.status();
  
  // Si es rate limited, lanzar error para que toPass() reintente
  if (status === 429) {
    throw new Error(
      `Rate limited (429) - será reintentado por toPass()`
    );
  }
  
  expect(status).toBe(expectedStatus);
}

/**
 * Helper para ejecutar una función con un rate limit interno
 * Garantiza un mínimo de tiempo entre ejecuciones
 */
export class RateLimiter {
  private lastExecutionTime: number = 0;
  private minInterval: number;

  constructor(minIntervalMs: number = 100) {
    this.minInterval = minIntervalMs;
  }

  async execute<T>(fn: () => Promise<T>): Promise<T> {
    const now = Date.now();
    const timeSinceLastExecution = now - this.lastExecutionTime;
    
    if (timeSinceLastExecution < this.minInterval) {
      await sleep(this.minInterval - timeSinceLastExecution);
    }
    
    this.lastExecutionTime = Date.now();
    return fn();
  }
}
