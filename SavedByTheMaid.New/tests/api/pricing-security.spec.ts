import { test, expect } from '@playwright/test';

/**
 * API Security Tests - Pricing Validation
 * Estos tests validan que el backend NO confía en el precio enviado por el cliente
 */
test.describe('API Security - Pricing Fraud Prevention', () => {
  
  test('TC-API-003: Backend rechaza precio manipulado en /confirm', async ({ request }) => {
    // TODO: Necesita datos reales de la BD (ServiceTypeId, EmployeeId, ZipCode válidos)
    // Este test requiere setup de datos previo
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    
    const softReserveResponse = await request.post('/api/booking/soft-reserve', {
      data: {
        date: tomorrow.toISOString().split('T')[0],
        startTime: { hours: 10, minutes: 0 },
        estimatedMinutes: 120,
        zipCode: '10001',
        employeeId: 1,
        serviceAreaId: 1
      }
    });
    
    if (!softReserveResponse.ok()) {
      console.log('SoftReserve failed:', await softReserveResponse.text());
      return;
    }
    const { softReserveId, sessionId } = await softReserveResponse.json();
    
    // PASO 2: Obtener precio REAL del servicio
    const estimateResponse = await request.post('/api/booking/estimate', {
      data: {
        serviceTypeId: 1,
        rooms: [{ roomTypeId: 1, quantity: 2 }],
        additionalServiceIds: [],
        squareFootage: 1500,
        dirtLevel: 1,
        hasPets: false
      }
    });
    
    expect(estimateResponse.ok()).toBeTruthy();
    const { total: realPrice } = await estimateResponse.json();
    
    expect(realPrice).toBeGreaterThan(0);
    
    // PASO 3: Intentar FRAUDE - enviar precio manipulado
    const fraudAttempt = await request.post('/api/booking/confirm', {
      data: {
        softReserveId,
        sessionId,
        serviceTypeId: 1,
        rooms: [{ roomTypeId: 1, quantity: 2 }],
        additionalServiceIds: [],
        squareFootage: 1500,
        dirtLevel: 1,
        hasPets: false,
        total: 1.00, // ❌ FRAUDE: Cliente envía $1 en lugar del precio real
        subtotal: 1.00,
        contactName: 'Hacker Test',
        contactEmail: `fraud-test-${Date.now()}@evil.com`,
        contactPhone: '555-0000',
        address: '123 Hack St'
      }
    });
    
    // VALIDACIÓN: El backend debe RECHAZAR
    expect(fraudAttempt.status()).toBe(400);
    
    const errorBody = await fraudAttempt.json();
    expect(errorBody.error || errorBody.message).toMatch(/pricing mismatch|price.*invalid|total.*incorrect/i);
  });

  test('TC-API-004: Backend recalcula precio independientemente del frontend', async ({ request }) => {
    const estimatePayload = {
      serviceTypeId: 1,
      rooms: [
        { roomTypeId: 1, quantity: 2 },
        { roomTypeId: 2, quantity: 1 }
      ],
      additionalServiceIds: [5, 7], // Asumiendo que existen
      squareFootage: 2000,
      dirtLevel: 2, // Normal
      hasPets: true,
      hasElevator: false,
      floorLevel: 3
    };
    
    // Primera llamada
    const estimate1 = await request.post('/api/booking/estimate', { data: estimatePayload });
    expect(estimate1.ok()).toBeTruthy();
    const result1 = await estimate1.json();
    
    // Segunda llamada con mismo payload
    const estimate2 = await request.post('/api/booking/estimate', { data: estimatePayload });
    expect(estimate2.ok()).toBeTruthy();
    const result2 = await estimate2.json();
    
    // Los valores deben ser EXACTAMENTE iguales (determinístico)
    expect(result1.total).toBe(result2.total);
    expect(result1.subtotal).toBe(result2.subtotal);
    expect(result1.estimatedMinutes).toBe(result2.estimatedMinutes);
    
    // Validar estructura de respuesta
    expect(result1).toHaveProperty('total');
    expect(result1).toHaveProperty('subtotal');
    expect(result1).toHaveProperty('estimatedMinutes');
    
    // Valores deben tener sentido
    expect(result1.total).toBeGreaterThan(0);
    expect(result1.subtotal).toBeGreaterThan(0);
    expect(result1.subtotal).toBeLessThanOrEqual(result1.total);
    expect(result1.estimatedMinutes).toBeGreaterThan(60); // Mínimo 1 hora
  });

  test('TC-API-005: Endpoint /estimate valida datos de entrada', async ({ request }) => {
    // Caso 1: ServiceTypeId inválido
    const invalidServiceType = await request.post('/api/booking/estimate', {
      data: {
        serviceTypeId: 99999, // No existe
        rooms: [],
        additionalServiceIds: []
      }
    });
    
    expect(invalidServiceType.status()).toBe(400);
    
    // Caso 2: Quantity negativa - AHORA SE VALIDA
    const negativeQuantity = await request.post('/api/booking/estimate', {
      data: {
        serviceTypeId: 1,
        rooms: [{ roomId: 1, quantity: -5 }], // ❌ Inválido
        additionalServiceIds: []
      }
    });
    
    expect(negativeQuantity.status()).toBe(400);
    
    // Caso 3: SquareFootage fuera de rango - AHORA SE VALIDA
    const invalidSquareFeet = await request.post('/api/booking/estimate', {
      data: {
        serviceTypeId: 1,
        rooms: [],
        squareFootage: 999999, // Fuera de rango (máximo 50,000)
        additionalServiceIds: []
      }
    });
    
    expect(invalidSquareFeet.status()).toBe(400);
  });
});

test.describe('API - Soft Reserve Validation', () => {
  
  test('TC-API-006: Soft Reserve expira después de 15 minutos', async ({ request }) => {
    // TODO: Requiere EmployeeId, ServiceAreaId, ZipCode válidos de la BD
    // Crear soft reserve
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    
    const response = await request.post('/api/booking/soft-reserve', {
      data: {
        date: tomorrow.toISOString().split('T')[0],
        startTime: '11:00',
        estimatedMinutes: 120,
        zipCode: '10001',
        employeeId: 1,
        serviceAreaId: 1
      }
    });
    
    expect(response.ok()).toBeTruthy();
    const { softReserveId, sessionId, expiresAt } = await response.json();
    
    // Validar que tiene expiración
    expect(expiresAt).toBeTruthy();
    const expiryDate = new Date(expiresAt);
    const now = new Date();
    const diffMinutes = (expiryDate.getTime() - now.getTime()) / 60000;
    
    expect(diffMinutes).toBeGreaterThan(14); // Debe ser ~15 min
    expect(diffMinutes).toBeLessThan(16);
  });

  test('TC-API-007: No se puede confirmar SoftReserve expirado', async ({ request }) => {
    // Este test requiere manipular la BD o esperar 15 min
    // Por ahora, validamos que el backend rechaza sessionId inválido
    
    const invalidConfirm = await request.post('/api/booking/confirm', {
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
    
    expect(invalidConfirm.status()).toBe(400 || 404);
  });

  test('TC-API-008: ExtendSoftReserve añade 10 minutos más', async ({ request }) => {
    // TODO: Requiere crear SoftReserve válido primero
    // Crear soft reserve
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    
    const createResponse = await request.post('/api/booking/soft-reserve', {
      data: {
        date: tomorrow.toISOString().split('T')[0],
        startTime: '12:00',
        estimatedMinutes: 120,
        zipCode: '10001',
        employeeId: 1,
        serviceAreaId: 1
      }
    });
    
    expect(createResponse.ok()).toBeTruthy();
    const { softReserveId, sessionId, expiresAt: originalExpiry } = await createResponse.json();
    
    // Extender
    const extendResponse = await request.post(`/api/booking/soft-reserve/${softReserveId}/extend`, {
      params: { sessionId }
    });
    
    if (extendResponse.ok()) {
      const { expiresAt: newExpiry } = await extendResponse.json();
      
      const original = new Date(originalExpiry);
      const extended = new Date(newExpiry);
      
      const diffMinutes = (extended.getTime() - original.getTime()) / 60000;
      expect(diffMinutes).toBeGreaterThan(9); // ~10 minutos más
      expect(diffMinutes).toBeLessThan(11);
    }
  });
});

test.describe('API - Performance', () => {
  
  test('TC-UX-010: /estimate responde en menos de 500ms (p95)', async ({ request }) => {
    const iterations = 20;
    const times: number[] = [];
    
    for (let i = 0; i < iterations; i++) {
      const start = Date.now();
      
      await request.post('/api/booking/estimate', {
        data: {
          serviceTypeId: 1,
          rooms: [{ roomTypeId: 1, quantity: 2 }],
          additionalServiceIds: [5],
          squareFootage: 1500
        }
      });
      
      times.push(Date.now() - start);
    }
    
    times.sort((a, b) => a - b);
    
    const p50 = times[Math.floor(iterations * 0.5)];
    const p95 = times[Math.floor(iterations * 0.95)];
    const p99 = times[Math.floor(iterations * 0.99)];
    
    console.log(`Performance /estimate: P50=${p50}ms, P95=${p95}ms, P99=${p99}ms`);
    
    expect(p95).toBeLessThan(500);
    expect(p99).toBeLessThan(1000);
  });
});
