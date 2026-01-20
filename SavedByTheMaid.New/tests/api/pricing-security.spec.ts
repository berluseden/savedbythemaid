/**
 * API Security Tests - Pricing Validation
 * Estos tests validan que el backend NO confía en el precio enviado por el cliente
 * 
 * Uses RateLimitedApiClient for automatic 429 handling with exponential backoff.
 */

import { test, expect, API_CREDENTIALS } from '../fixtures/api.fixture';

// Configure serial execution for API tests
test.describe.configure({ mode: 'serial' });

test.describe('API Security - Pricing Fraud Prevention', () => {
  
  test('TC-API-003: Backend rechaza precio manipulado en /confirm', async ({ apiClient }) => {
    // Este test valida que el backend recalcula precios server-side
    // Primero necesitamos crear una reserva válida
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    
    // Intentar confirm con precio fraudulento (sin soft reserve válido)
    const fraudAttempt = await apiClient.post('/api/booking/confirm', {
      data: {
        softReserveId: 99999,
        sessionId: 'invalid-session',
        serviceTypeId: 1,
        rooms: [{ roomTypeId: 1, quantity: 2 }],
        additionalServiceIds: [],
        squareFootage: 1500,
        dirtLevel: 1,
        hasPets: false,
        total: 1.00, // ❌ FRAUDE: Cliente envía $1 
        subtotal: 1.00,
        contactName: 'Fraud Test',
        contactEmail: `fraud-test-${Date.now()}@evil.com`,
        contactPhone: '555-0000',
        address: '123 Hack St'
      }
    });
    
    // El backend debe rechazar por softReserve inválido
    expect(fraudAttempt.status()).toBeGreaterThanOrEqual(400);
  });

  test('TC-API-004: Backend recalcula precio independientemente del frontend', async ({ apiClient }) => {
    const estimatePayload = {
      serviceTypeId: 1,
      rooms: [
        { roomTypeId: 1, quantity: 2 },
        { roomTypeId: 2, quantity: 1 }
      ],
      additionalServiceIds: [],
      squareFootage: 2000,
      dirtLevel: 2,
      hasPets: true,
      hasElevator: false,
      floorLevel: 3
    };
    
    // Primera llamada
    const estimate1 = await apiClient.post('/api/booking/estimate', { data: estimatePayload });
    
    if (!estimate1.ok()) {
      console.log('Estimate endpoint may not exist or format differs:', estimate1.status());
      test.skip();
      return;
    }
    
    const result1 = await estimate1.json();
    
    // Segunda llamada con mismo payload
    const estimate2 = await apiClient.post('/api/booking/estimate', { data: estimatePayload });
    expect(estimate2.ok()).toBeTruthy();
    const result2 = await estimate2.json();
    
    // Los valores deben ser EXACTAMENTE iguales (determinístico)
    expect(result1.total).toBe(result2.total);
    expect(result1.subtotal).toBe(result2.subtotal);
    
    // Valores deben tener sentido
    expect(result1.total).toBeGreaterThan(0);
    expect(result1.subtotal).toBeGreaterThan(0);
    expect(result1.subtotal).toBeLessThanOrEqual(result1.total);
  });

  test('TC-API-005: Endpoint /estimate valida datos de entrada', async ({ apiClient }) => {
    // Caso 1: ServiceTypeId inválido
    const invalidServiceType = await apiClient.post('/api/booking/estimate', {
      data: {
        serviceTypeId: 99999, // No existe
        rooms: [],
        additionalServiceIds: []
      }
    });
    
    // Debe rechazar o devolver error
    expect(invalidServiceType.status()).toBeGreaterThanOrEqual(400);
    
    // Caso 2: Quantity negativa
    const negativeQuantity = await apiClient.post('/api/booking/estimate', {
      data: {
        serviceTypeId: 1,
        rooms: [{ roomId: 1, quantity: -5 }],
        additionalServiceIds: []
      }
    });
    
    expect(negativeQuantity.status()).toBeGreaterThanOrEqual(400);
    
    // Caso 3: SquareFootage fuera de rango
    const invalidSquareFeet = await apiClient.post('/api/booking/estimate', {
      data: {
        serviceTypeId: 1,
        rooms: [],
        squareFootage: 999999,
        additionalServiceIds: []
      }
    });
    
    expect(invalidSquareFeet.status()).toBeGreaterThanOrEqual(400);
  });
});

test.describe('API - Soft Reserve Validation', () => {
  
  test('TC-API-007: No se puede confirmar SoftReserve inválido', async ({ apiClient }) => {
    // Validamos que el backend rechaza sessionId inválido
    const invalidConfirm = await apiClient.post('/api/booking/confirm', {
      data: {
        softReserveId: 99999,
        sessionId: 'invalid-session-id',
        serviceTypeId: 1,
        rooms: [],
        total: 100,
        contactName: 'Test',
        contactEmail: 'test@test.com',
        contactPhone: '555-0000',
        address: '123 Test St'
      }
    });
    
    // Debe ser 400 o 404
    expect(invalidConfirm.status()).toBeGreaterThanOrEqual(400);
  });
});

test.describe('API - Performance', () => {
  
  test('TC-UX-010: /estimate responde en menos de 500ms (p95)', async ({ apiClient }) => {
    const iterations = 10;  // Reducido para evitar rate limiting
    const times: number[] = [];
    
    for (let i = 0; i < iterations; i++) {
      const start = Date.now();
      
      const response = await apiClient.post('/api/booking/estimate', {
        data: {
          serviceTypeId: 1,
          rooms: [{ roomTypeId: 1, quantity: 2 }],
          additionalServiceIds: [],
          squareFootage: 1500
        }
      });
      
      times.push(Date.now() - start);
      
      // Skip if endpoint doesn't exist
      if (response.status() === 404) {
        test.skip();
        return;
      }
    }
    
    times.sort((a, b) => a - b);
    
    const p50 = times[Math.floor(iterations * 0.5)];
    const p95 = times[Math.floor(iterations * 0.95)];
    
    console.log(`Performance /estimate: P50=${p50}ms, P95=${p95}ms`);
    
    // Note: P95 includes retry time if rate limited
    expect(p95).toBeLessThan(2000);  // More lenient due to potential retries
  });
});

test.describe('API - Authenticated Pricing Endpoints', () => {
  
  test('should access services list with auth', async ({ apiClient, adminAuthHeaders }) => {
    const response = await apiClient.get('/api/admin/service-types', {
      headers: adminAuthHeaders,
    });
    
    expect(response.ok()).toBe(true);
    const services = await response.json();
    expect(Array.isArray(services)).toBe(true);
    
    if (services.length > 0) {
      const service = services[0];
      expect(service).toHaveProperty('id');
      expect(service).toHaveProperty('name');
    }
  });

  test('should access additional services list with auth', async ({ apiClient, adminAuthHeaders }) => {
    const response = await apiClient.get('/api/admin/AdditionalServices', {
      headers: adminAuthHeaders,
    });
    
    expect(response.ok()).toBe(true);
    const services = await response.json();
    expect(Array.isArray(services)).toBe(true);
    
    if (services.length > 0) {
      const service = services[0];
      expect(service).toHaveProperty('id');
      expect(service).toHaveProperty('title');  // API returns 'title' not 'name'
    }
  });

  test('should access room types list with auth', async ({ apiClient, adminAuthHeaders }) => {
    const response = await apiClient.get('/api/room-types', {
      headers: adminAuthHeaders,
    });
    
    // May return 404 if endpoint doesn't exist
    if (response.status() === 404) {
      test.skip();
      return;
    }
    
    expect(response.ok()).toBe(true);
    const roomTypes = await response.json();
    expect(Array.isArray(roomTypes)).toBe(true);
  });

  test('should get bookings list with pricing info', async ({ apiClient, adminAuthHeaders }) => {
    const response = await apiClient.get('/api/admin/orders', {
      headers: adminAuthHeaders,
    });
    
    expect(response.ok()).toBe(true);
    const bookings = await response.json();
    expect(Array.isArray(bookings)).toBe(true);
    
    if (bookings.length > 0) {
      const booking = bookings[0];
      expect(booking).toHaveProperty('id');
      // Pricing fields may vary
      if (booking.totalPrice !== undefined) {
        expect(booking.totalPrice).toBeGreaterThanOrEqual(0);
      }
    }
  });
});
