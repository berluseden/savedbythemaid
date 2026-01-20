/**
 * API Client with automatic retry on rate limiting (429)
 * Uses exponential backoff strategy
 */

import { APIRequestContext, APIResponse } from '@playwright/test';

interface RequestOptions {
  data?: unknown;
  headers?: Record<string, string>;
}

interface RetryConfig {
  maxRetries: number;
  baseDelay: number;
  maxDelay: number;
}

const DEFAULT_RETRY_CONFIG: RetryConfig = {
  maxRetries: 3,
  baseDelay: 1000,  // Start with 1 second
  maxDelay: 10000,  // Max 10 seconds
};

/**
 * Wraps Playwright's APIRequestContext with automatic 429 retry handling
 */
export class RateLimitedApiClient {
  private requestContext: APIRequestContext;
  private config: RetryConfig;
  private baseUrl: string;

  constructor(
    requestContext: APIRequestContext, 
    baseUrl: string = '',
    config: Partial<RetryConfig> = {}
  ) {
    this.requestContext = requestContext;
    this.baseUrl = baseUrl;
    this.config = { ...DEFAULT_RETRY_CONFIG, ...config };
  }

  private async executeWithRetry(
    method: 'get' | 'post' | 'put' | 'delete' | 'patch',
    url: string,
    options?: RequestOptions
  ): Promise<APIResponse> {
    const fullUrl = this.baseUrl + url;
    let lastError: Error | null = null;
    let lastResponse: APIResponse | null = null;

    for (let attempt = 0; attempt <= this.config.maxRetries; attempt++) {
      try {
        const response = await this.makeRequest(method, fullUrl, options);
        
        if (response.status() === 429) {
          lastResponse = response;
          const delay = this.calculateDelay(attempt);
          console.log(`Rate limited (429). Retry ${attempt + 1}/${this.config.maxRetries} after ${delay}ms`);
          await this.sleep(delay);
          continue;
        }

        return response;
      } catch (error) {
        lastError = error as Error;
        const delay = this.calculateDelay(attempt);
        console.log(`Request failed. Retry ${attempt + 1}/${this.config.maxRetries} after ${delay}ms`);
        await this.sleep(delay);
      }
    }

    // If we got a 429 on all retries, return the last response
    if (lastResponse) {
      return lastResponse;
    }

    throw lastError || new Error('Request failed after all retries');
  }

  private async makeRequest(
    method: 'get' | 'post' | 'put' | 'delete' | 'patch',
    url: string,
    options?: RequestOptions
  ): Promise<APIResponse> {
    const requestOptions: Record<string, unknown> = {};
    
    if (options?.data) {
      requestOptions.data = options.data;
    }
    if (options?.headers) {
      requestOptions.headers = options.headers;
    }

    switch (method) {
      case 'get':
        return this.requestContext.get(url, requestOptions);
      case 'post':
        return this.requestContext.post(url, requestOptions);
      case 'put':
        return this.requestContext.put(url, requestOptions);
      case 'delete':
        return this.requestContext.delete(url, requestOptions);
      case 'patch':
        return this.requestContext.patch(url, requestOptions);
    }
  }

  private calculateDelay(attempt: number): number {
    // Exponential backoff: 1s, 2s, 4s, 8s... capped at maxDelay
    const delay = this.config.baseDelay * Math.pow(2, attempt);
    return Math.min(delay, this.config.maxDelay);
  }

  private sleep(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  // Public API methods
  async get(url: string, options?: RequestOptions): Promise<APIResponse> {
    return this.executeWithRetry('get', url, options);
  }

  async post(url: string, options?: RequestOptions): Promise<APIResponse> {
    return this.executeWithRetry('post', url, options);
  }

  async put(url: string, options?: RequestOptions): Promise<APIResponse> {
    return this.executeWithRetry('put', url, options);
  }

  async delete(url: string, options?: RequestOptions): Promise<APIResponse> {
    return this.executeWithRetry('delete', url, options);
  }

  async patch(url: string, options?: RequestOptions): Promise<APIResponse> {
    return this.executeWithRetry('patch', url, options);
  }
}

/**
 * Helper function to create an authenticated API client
 */
export async function createAuthenticatedClient(
  request: APIRequestContext,
  baseUrl: string,
  credentials: { email: string; password: string }
): Promise<{ client: RateLimitedApiClient; token: string }> {
  const client = new RateLimitedApiClient(request, baseUrl);
  
  const response = await client.post('/api/auth/login', {
    data: credentials,
  });

  if (!response.ok()) {
    throw new Error(`Authentication failed: ${response.status()}`);
  }

  const data = await response.json();
  const token = data.token;

  // Return a new client with auth headers
  const authClient = new RateLimitedApiClient(request, baseUrl);
  
  return { 
    client: authClient, 
    token 
  };
}

/**
 * Create headers with authorization token
 */
export function authHeaders(token: string): Record<string, string> {
  return {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json',
  };
}
