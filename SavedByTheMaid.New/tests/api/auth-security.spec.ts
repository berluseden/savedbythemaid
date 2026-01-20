/**
 * API Security Tests - Authentication and Authorization
 * 
 * Tests that protected endpoints require proper authentication
 * and that authorization is correctly enforced.
 * 
 * Uses RateLimitedApiClient to handle 429 responses automatically.
 * 
 * API Routes:
 * - /api/admin/service-types (protected)
 * - /api/admin/orders (protected)
 * - /api/admin/employees (protected)
 * - /api/admin/AdditionalServices (protected)
 * - /api/Employees (public)
 * - /api/ServiceAreas (public)
 * - /api/Booking/estimate (public)
 * - /api/Auth/login (public)
 */

import { test, expect, expectUnauthorizedOrRateLimited, API_CREDENTIALS } from '../fixtures/api.fixture';

test.describe('Authentication Security', () => {
  
  test.describe('Login Endpoint', () => {
    
    test('should accept valid credentials', async ({ apiClient }) => {
      const response = await apiClient.post('/api/Auth/login', {
        data: {
          email: API_CREDENTIALS.email,
          password: API_CREDENTIALS.password
        }
      });
      
      // Should be 200 or rate limited
      if (response.status() === 429) {
        console.log('Rate limited - skipping validation');
        return;
      }
      
      expect(response.status()).toBe(200);
      const body = await response.json();
      expect(body).toHaveProperty('accessToken');
      expect(body).toHaveProperty('refreshToken');
      expect(body).toHaveProperty('user');
    });
    
    test('should reject invalid password', async ({ apiClient }) => {
      const response = await apiClient.post('/api/Auth/login', {
        data: {
          email: API_CREDENTIALS.email,
          password: 'WrongPassword123!'
        }
      });
      
      // Should be 400/401 or rate limited
      if (response.status() === 429) {
        console.log('Rate limited - test inconclusive');
        return;
      }
      
      expect([400, 401]).toContain(response.status());
    });
    
    test('should reject non-existent user', async ({ apiClient }) => {
      const response = await apiClient.post('/api/Auth/login', {
        data: {
          email: 'nonexistent@test.com',
          password: 'SomePassword123!'
        }
      });
      
      if (response.status() === 429) {
        console.log('Rate limited - test inconclusive');
        return;
      }
      
      expect([400, 401]).toContain(response.status());
    });
    
    test('should reject empty credentials', async ({ apiClient }) => {
      const response = await apiClient.post('/api/Auth/login', {
        data: {
          email: '',
          password: ''
        }
      });
      
      if (response.status() === 429) {
        console.log('Rate limited - test inconclusive');
        return;
      }
      
      expect([400, 401]).toContain(response.status());
    });
  });

  test.describe('Protected Admin Endpoints - Without Token', () => {
    
    test('should reject /api/admin/orders without token', async ({ apiClient }) => {
      const response = await apiClient.get('/api/admin/orders');
      expectUnauthorizedOrRateLimited(response.status());
    });
    
    test('should reject /api/admin/service-types without token', async ({ apiClient }) => {
      const response = await apiClient.get('/api/admin/service-types');
      expectUnauthorizedOrRateLimited(response.status());
    });
    
    test('should reject /api/admin/AdditionalServices without token', async ({ apiClient }) => {
      const response = await apiClient.get('/api/admin/AdditionalServices');
      expectUnauthorizedOrRateLimited(response.status());
    });
    
    test('should reject /api/admin/employees without token', async ({ apiClient }) => {
      const response = await apiClient.get('/api/admin/employees');
      expectUnauthorizedOrRateLimited(response.status());
    });
  });

  test.describe('Protected Admin Endpoints - With Invalid Token', () => {
    
    test('should reject request with malformed token', async ({ apiClient }) => {
      const response = await apiClient.get('/api/admin/orders', {
        headers: {
          'Authorization': 'Bearer invalid-token-format'
        }
      });
      expectUnauthorizedOrRateLimited(response.status());
    });
    
    test('should reject request with expired token format', async ({ apiClient }) => {
      // This is a structurally valid JWT but with wrong signature
      const fakeToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c';
      
      const response = await apiClient.get('/api/admin/service-types', {
        headers: {
          'Authorization': `Bearer ${fakeToken}`
        }
      });
      expectUnauthorizedOrRateLimited(response.status());
    });
    
    test('should reject request with empty bearer token', async ({ apiClient }) => {
      const response = await apiClient.get('/api/admin/orders', {
        headers: {
          'Authorization': 'Bearer '
        }
      });
      expectUnauthorizedOrRateLimited(response.status());
    });
  });

  test.describe('Protected Admin Endpoints - With Valid Token', () => {
    
    test('should accept /api/admin/orders with valid admin token', async ({ adminAuthHeaders, apiClient }) => {
      const response = await apiClient.get('/api/admin/orders', {
        headers: adminAuthHeaders
      });
      
      if (response.status() === 429) {
        console.log('Rate limited - test inconclusive');
        return;
      }
      
      // Should be 200 for admin user
      expect(response.status()).toBe(200);
    });
    
    test('should accept /api/admin/service-types with valid admin token', async ({ adminAuthHeaders, apiClient }) => {
      const response = await apiClient.get('/api/admin/service-types', {
        headers: adminAuthHeaders
      });
      
      if (response.status() === 429) {
        console.log('Rate limited - test inconclusive');
        return;
      }
      
      expect(response.status()).toBe(200);
      const body = await response.json();
      expect(Array.isArray(body)).toBe(true);
    });
    
    test('should accept /api/admin/AdditionalServices with valid admin token', async ({ adminAuthHeaders, apiClient }) => {
      const response = await apiClient.get('/api/admin/AdditionalServices', {
        headers: adminAuthHeaders
      });
      
      if (response.status() === 429) {
        console.log('Rate limited - test inconclusive');
        return;
      }
      
      expect(response.status()).toBe(200);
      const body = await response.json();
      expect(Array.isArray(body)).toBe(true);
    });
    
    test('should accept /api/admin/employees with valid admin token', async ({ adminAuthHeaders, apiClient }) => {
      const response = await apiClient.get('/api/admin/employees', {
        headers: adminAuthHeaders
      });
      
      if (response.status() === 429) {
        console.log('Rate limited - test inconclusive');
        return;
      }
      
      expect(response.status()).toBe(200);
    });
  });

  test.describe('Public Endpoints', () => {
    
    test('should allow /api/ServiceAreas without token', async ({ apiClient }) => {
      const response = await apiClient.get('/api/ServiceAreas');
      
      if (response.status() === 429) {
        console.log('Rate limited - test inconclusive');
        return;
      }
      
      // Public endpoint should be accessible
      expect(response.status()).toBe(200);
    });
    
    test('should allow /api/Employees without token', async ({ apiClient }) => {
      const response = await apiClient.get('/api/Employees');
      
      if (response.status() === 429) {
        console.log('Rate limited - test inconclusive');
        return;
      }
      
      expect(response.status()).toBe(200);
    });
  });

  test.describe('Booking Endpoints', () => {
    
    test('should allow /api/Booking/estimate without token (public)', async ({ apiClient }) => {
      const response = await apiClient.post('/api/Booking/estimate', {
        data: {
          serviceTypeId: 1,
          squareMeters: 100,
          frequency: 1
        }
      });
      
      if (response.status() === 429) {
        console.log('Rate limited - test inconclusive');
        return;
      }
      
      // Estimate is public - should work or return validation error
      expect([200, 400]).toContain(response.status());
    });
    
    test('should validate booking estimate parameters', async ({ apiClient }) => {
      const response = await apiClient.post('/api/Booking/estimate', {
        data: {
          // Invalid/missing parameters
          serviceTypeId: -1,
          squareMeters: -50
        }
      });
      
      if (response.status() === 429) {
        console.log('Rate limited - test inconclusive');
        return;
      }
      
      // Should reject invalid parameters
      expect([400, 422]).toContain(response.status());
    });
  });

  test.describe('Security Headers', () => {
    
    test('should not expose sensitive headers in error responses', async ({ apiClient }) => {
      const response = await apiClient.get('/api/admin/orders');
      
      // Should not expose stack traces or internal errors
      const headers = response.headers();
      expect(headers['x-powered-by']).toBeUndefined();
      
      // Body should not contain stack traces
      const text = await response.text();
      expect(text).not.toContain('System.');
      expect(text).not.toContain('at Microsoft.');
      expect(text).not.toContain('StackTrace');
    });
  });
});
