import { test as base, expect, APIRequestContext } from '@playwright/test';
import { RateLimitedApiClient, RateLimiter, RetryConfig } from '../utils/rate-limit-handler';

/**
 * Rate Limit Fixture para Playwright Tests
 * =========================================
 * 
 * Proporciona fixtures que manejan rate limiting automáticamente:
 * - `apiClient`: Cliente con retry automático en 429
 * - `rateLimiter`: Limitador para controlar la frecuencia de requests
 * - `slowRequest`: Request con delay garantizado entre llamadas
 * 
 * Configuración recomendada de playwright.config.ts:
 * - workers: 1 para tests de API con rate limiting
 * - fullyParallel: false para tests seriales
 * 
 * @example
 * ```typescript
 * import { test, expect } from '../fixtures/rate-limit.fixture';
 * 
 * test('debería manejar rate limiting', async ({ apiClient }) => {
 *   // El cliente reintenta automáticamente en 429
 *   const response = await apiClient.get('/api/bookings');
 *   expect(response.status()).toBe(200);
 * });
 * ```
 */

// ===================================================================
// CONFIGURACIÓN
// ===================================================================

interface RateLimitFixtures {
  /** Cliente de API con retry automático en 429 */
  apiClient: RateLimitedApiClient;
  
  /** Configuración de retry personalizable por test */
  retryConfig: Partial<RetryConfig>;
  
  /** Rate limiter para controlar frecuencia de requests */
  rateLimiter: RateLimiter;
  
  /** Delay mínimo entre requests en ms */
  minRequestInterval: number;
}

// ===================================================================
// FIXTURE DEFINITION
// ===================================================================

export const test = base.extend<RateLimitFixtures>({
  /**
   * Delay mínimo entre requests (configurable por test)
   * Usar con: test.use({ minRequestInterval: 500 })
   */
  minRequestInterval: [200, { option: true }],

  /**
   * Configuración de retry (configurable por test)
   * Usar con: test.use({ retryConfig: { maxRetries: 5 } })
   */
  retryConfig: [{
    maxRetries: 3,
    initialDelay: 1000,
    backoffMultiplier: 2,
    maxDelay: 10000,
    retryableStatuses: [429, 503],
    verbose: true, // Cambiar a false en producción
  }, { option: true }],

  /**
   * Rate limiter que garantiza un mínimo de tiempo entre ejecuciones
   */
  rateLimiter: async ({ minRequestInterval }, use) => {
    const limiter = new RateLimiter(minRequestInterval);
    await use(limiter);
  },

  /**
   * Cliente de API con manejo automático de rate limiting
   */
  apiClient: async ({ request, retryConfig }, use) => {
    const client = new RateLimitedApiClient(request, retryConfig);
    await use(client);
  },
});

export { expect };

// ===================================================================
// HELPERS ADICIONALES
// ===================================================================

/**
 * Decorador para tests que necesitan delays entre requests
 * Usar cuando múltiples requests en un mismo test causan rate limiting
 * 
 * @example
 * ```typescript
 * test('múltiples requests', async ({ request }) => {
 *   await withRateLimit(async () => {
 *     const r1 = await request.get('/api/endpoint1');
 *     const r2 = await request.get('/api/endpoint2');
 *   }, 300);
 * });
 * ```
 */
export async function withRateLimit<T>(
  fn: () => Promise<T>,
  delayMs: number = 200
): Promise<T> {
  await new Promise(r => setTimeout(r, delayMs));
  return fn();
}

/**
 * Ejecuta requests en serie con delay entre cada una
 * Útil para batch de requests que causan rate limiting
 * 
 * @example
 * ```typescript
 * const responses = await serialRequests(request, [
 *   () => request.get('/api/users/1'),
 *   () => request.get('/api/users/2'),
 *   () => request.post('/api/users', { data: {...} }),
 * ], 300);
 * ```
 */
export async function serialRequests<T>(
  requestFns: Array<() => Promise<T>>,
  delayBetween: number = 200
): Promise<T[]> {
  const results: T[] = [];
  
  for (const fn of requestFns) {
    const result = await fn();
    results.push(result);
    
    // No esperar después del último request
    if (requestFns.indexOf(fn) < requestFns.length - 1) {
      await new Promise(r => setTimeout(r, delayBetween));
    }
  }
  
  return results;
}

/**
 * Helper para usar con expect().toPass() para requests que pueden dar 429
 * 
 * @example
 * ```typescript
 * await expect(async () => {
 *   const response = await expectApiSuccess(
 *     request.get('/api/flaky-endpoint')
 *   );
 *   expect(await response.json()).toHaveProperty('data');
 * }).toPass({
 *   intervals: [500, 1000, 2000, 5000],
 *   timeout: 30000
 * });
 * ```
 */
export async function expectApiSuccess(
  responsePromise: Promise<import('@playwright/test').APIResponse>,
  acceptableStatuses: number[] = [200, 201, 204]
): Promise<import('@playwright/test').APIResponse> {
  const response = await responsePromise;
  const status = response.status();
  
  if (status === 429) {
    throw new Error('Rate limited (429) - toPass() will retry');
  }
  
  if (!acceptableStatuses.includes(status)) {
    const body = await response.text();
    throw new Error(`Unexpected status ${status}: ${body.substring(0, 200)}`);
  }
  
  return response;
}

// ===================================================================
// CONFIGURACIONES PREDEFINIDAS PARA test.use()
// ===================================================================

/**
 * Configuración agresiva - muchos reintentos, delays largos
 * Para endpoints especialmente problemáticos con rate limiting
 */
export const AGGRESSIVE_RETRY_CONFIG: Partial<RetryConfig> = {
  maxRetries: 5,
  initialDelay: 2000,
  backoffMultiplier: 2,
  maxDelay: 30000,
  verbose: true,
};

/**
 * Configuración conservadora - pocos reintentos, delays cortos
 * Para tests rápidos donde rate limiting es poco probable
 */
export const CONSERVATIVE_RETRY_CONFIG: Partial<RetryConfig> = {
  maxRetries: 2,
  initialDelay: 500,
  backoffMultiplier: 1.5,
  maxDelay: 5000,
  verbose: false,
};

/**
 * Sin reintentos - falla inmediatamente en 429
 * Para tests que necesitan verificar comportamiento de rate limiting
 */
export const NO_RETRY_CONFIG: Partial<RetryConfig> = {
  maxRetries: 0,
  retryableStatuses: [],
};
