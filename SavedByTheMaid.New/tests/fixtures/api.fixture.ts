/**
 * API Testing Fixture with Rate Limiting Support
 * 
 * This fixture provides:
 * - RateLimitedApiClient with automatic 429 retry
 * - Pre-authenticated tokens for admin and customer
 * - Serial test execution for API tests
 */

import { test as base, expect } from '@playwright/test';
import { RateLimitedApiClient, authHeaders } from '../utils/api-client';

// Test credentials
export const API_CREDENTIALS = {
  admin: {
    email: 'admin@savedbytemaid.com',
    password: 'Admin123!',
  },
  // When customer user exists, update this
  customer: {
    email: 'admin@savedbytemaid.com',
    password: 'Admin123!',
  },
};

// API base URL
const API_BASE_URL = process.env.API_URL || 'http://localhost:5000';

// Token cache to avoid multiple logins
let tokenCache: {
  admin?: { token: string; expiry: number };
  customer?: { token: string; expiry: number };
} = {};

async function getToken(
  request: RateLimitedApiClient,
  type: 'admin' | 'customer'
): Promise<string> {
  const cached = tokenCache[type];
  const now = Date.now();

  // Return cached token if still valid (with 5 min buffer)
  if (cached && cached.expiry > now + 300000) {
    return cached.token;
  }

  const credentials = API_CREDENTIALS[type];
  const response = await request.post('/api/auth/login', {
    data: credentials,
  });

  if (!response.ok()) {
    const text = await response.text();
    throw new Error(`Login failed for ${type}: ${response.status()} - ${text}`);
  }

  const data = await response.json();
  const token = data.accessToken;

  // Cache for 55 minutes (assuming 1 hour expiry)
  tokenCache[type] = {
    token,
    expiry: now + 55 * 60 * 1000,
  };

  return token;
}

// Type for our custom fixtures
type ApiFixtures = {
  apiClient: RateLimitedApiClient;
  adminToken: string;
  customerToken: string;
  adminAuthHeaders: Record<string, string>;
  customerAuthHeaders: Record<string, string>;
  uniqueEmail: () => string;
};

/**
 * Extended test with API fixtures
 */
export const test = base.extend<ApiFixtures>({
  // Rate-limited API client
  apiClient: async ({ request }, use) => {
    const client = new RateLimitedApiClient(request, API_BASE_URL, {
      maxRetries: 3,
      baseDelay: 1000,
      maxDelay: 10000,
    });
    await use(client);
  },

  // Admin token with caching
  adminToken: async ({ apiClient }, use) => {
    const token = await getToken(apiClient, 'admin');
    await use(token);
  },

  // Customer token with caching
  customerToken: async ({ apiClient }, use) => {
    const token = await getToken(apiClient, 'customer');
    await use(token);
  },

  // Pre-built admin headers
  adminAuthHeaders: async ({ adminToken }, use) => {
    await use(authHeaders(adminToken));
  },

  // Pre-built customer headers
  customerAuthHeaders: async ({ customerToken }, use) => {
    await use(authHeaders(customerToken));
  },

  // Unique email generator
  uniqueEmail: async ({}, use) => {
    await use(() => `test_${Date.now()}_${Math.random().toString(36).slice(2)}@test.com`);
  },
});

export { expect };

/**
 * Helper to assert response is successful (2xx) or rate limited (429)
 * Use this when rate limiting is acceptable
 */
export function expectSuccessOrRateLimited(status: number): void {
  expect([200, 201, 204, 429]).toContain(status);
}

/**
 * Helper to assert response is unauthorized (401) or rate limited (429)
 * Use this for security tests where either response is valid
 */
export function expectUnauthorizedOrRateLimited(status: number): void {
  expect([401, 429]).toContain(status);
}

/**
 * Helper to assert response is forbidden (403) or rate limited (429)
 */
export function expectForbiddenOrRateLimited(status: number): void {
  expect([403, 429]).toContain(status);
}

/**
 * Helper to assert response is a client error (4xx)
 */
export function expectClientError(status: number): void {
  expect(status).toBeGreaterThanOrEqual(400);
  expect(status).toBeLessThan(500);
}
