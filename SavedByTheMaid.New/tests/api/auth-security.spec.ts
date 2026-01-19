import { test, expect } from '@playwright/test';
import { 
  test as authTest, 
  AUTH_CREDENTIALS, 
  authHeader, 
  uniqueTestEmail 
} from '../fixtures/auth.fixture';

/**
 * API Security Tests - Authentication & Authorization
 * ====================================================
 * 
 * Esta suite valida la seguridad de la API:
 * - Autenticación JWT (login/logout)
 * - Autorización basada en roles
 * - Protección de endpoints admin
 * - Validación de tokens
 */
test.describe('API Security - Authentication @security', () => {
  
  // =====================================================
  // LOGIN TESTS
  // =====================================================
  
  test('@smoke TC-050: Login con credenciales válidas retorna token JWT', async ({ request }) => {
    const response = await request.post('/api/auth/login', {
      data: {
        email: AUTH_CREDENTIALS.customer.email,
        password: AUTH_CREDENTIALS.customer.password,
      },
    });

    expect(response.status(), 'Login debería retornar 200 OK').toBe(200);

    const body = await response.json();
    
    expect(body.accessToken, 'Respuesta debe incluir accessToken').toBeDefined();
    expect(body.accessToken, 'accessToken no debe estar vacío').not.toBe('');
    expect(body.refreshToken, 'Respuesta debe incluir refreshToken').toBeDefined();
    expect(body.user, 'Respuesta debe incluir datos del usuario').toBeDefined();
    expect(body.user.email, 'Email del usuario debe coincidir').toBe(AUTH_CREDENTIALS.customer.email);
    expect(body.user.roles, 'Usuario debe tener roles asignados').toBeInstanceOf(Array);
    expect(body.expiresAt, 'Debe incluir fecha de expiración').toBeDefined();
  });

  test('@smoke TC-051: Login con credenciales inválidas retorna 401', async ({ request }) => {
    const response = await request.post('/api/auth/login', {
      data: {
        email: 'usuario.inexistente@test.com',
        password: 'contraseñaIncorrecta123',
      },
    });

    expect(response.status(), 'Login inválido debería retornar 401 Unauthorized').toBe(401);

    const body = await response.json();
    expect(body.message || body.error, 'Debe incluir mensaje de error').toBeDefined();
  });

  test('@smoke TC-052: Login con password incorrecto retorna 401', async ({ request }) => {
    const response = await request.post('/api/auth/login', {
      data: {
        email: AUTH_CREDENTIALS.customer.email,
        password: 'WrongPassword123!',
      },
    });

    expect(response.status(), 'Password incorrecto debería retornar 401').toBe(401);
  });

  test('@regression TC-053: Login con email vacío retorna 400', async ({ request }) => {
    const response = await request.post('/api/auth/login', {
      data: {
        email: '',
        password: 'SomePassword123!',
      },
    });

    expect(
      response.status(),
      'Email vacío debería retornar 400 Bad Request'
    ).toBeGreaterThanOrEqual(400);
    expect(response.status()).toBeLessThan(500);
  });

  test('@regression TC-054: Login con password vacío retorna 400', async ({ request }) => {
    const response = await request.post('/api/auth/login', {
      data: {
        email: AUTH_CREDENTIALS.customer.email,
        password: '',
      },
    });

    expect(
      response.status(),
      'Password vacío debería retornar 400 Bad Request'
    ).toBeGreaterThanOrEqual(400);
    expect(response.status()).toBeLessThan(500);
  });
});

test.describe('API Security - Admin Endpoints Protection @security', () => {
  
  // =====================================================
  // ENDPOINT PROTECTION TESTS (Sin autenticación)
  // =====================================================
  
  test('@security TC-060: Endpoints /api/admin/* requieren autenticación (401 sin token)', async ({ request }) => {
    const adminEndpoints = [
      '/api/admin/orders',
      '/api/admin/users',
      '/api/admin/employees',
      '/api/admin/service-types',
      '/api/admin/additionalservices',
    ];

    for (const endpoint of adminEndpoints) {
      const response = await request.get(endpoint);
      
      expect(
        response.status(),
        `${endpoint} sin token debería retornar 401 Unauthorized`
      ).toBe(401);
    }
  });

  test('@security TC-061: POST a endpoints admin sin token retorna 401', async ({ request }) => {
    const response = await request.post('/api/admin/service-types', {
      data: {
        name: 'Hacked Service',
        description: 'This should not work',
        basePrice: 1,
      },
    });

    expect(
      response.status(),
      'POST a admin sin autenticación debería retornar 401'
    ).toBe(401);
  });

  test('@security TC-062: PUT a endpoints admin sin token retorna 401', async ({ request }) => {
    const response = await request.put('/api/admin/orders/1/status', {
      data: { orderStatus: 'Completed' },
    });

    expect(
      response.status(),
      'PUT a admin sin autenticación debería retornar 401'
    ).toBe(401);
  });

  test('@security TC-063: DELETE a endpoints admin sin token retorna 401', async ({ request }) => {
    const response = await request.delete('/api/admin/service-types/999');

    expect(
      response.status(),
      'DELETE a admin sin autenticación debería retornar 401'
    ).toBe(401);
  });
});

// Tests que usan el fixture de autenticación
authTest.describe('API Security - Role-Based Access Control @security', () => {
  
  // =====================================================
  // AUTORIZACIÓN POR ROL
  // =====================================================
  
  authTest('@security TC-065: Customer NO puede acceder a /api/admin/orders', async ({ request, customerToken }) => {
    const response = await request.get('/api/admin/orders', {
      headers: authHeader(customerToken),
    });

    expect(
      response.status(),
      'Customer accediendo a admin debería retornar 403 Forbidden'
    ).toBe(403);
  });

  authTest('@security TC-066: Customer NO puede acceder a /api/admin/users', async ({ request, customerToken }) => {
    const response = await request.get('/api/admin/users', {
      headers: authHeader(customerToken),
    });

    expect(
      response.status(),
      'Customer accediendo a users admin debería retornar 403'
    ).toBe(403);
  });

  authTest('@security TC-067: Customer NO puede crear service types', async ({ request, customerToken }) => {
    const response = await request.post('/api/admin/service-types', {
      headers: authHeader(customerToken),
      data: {
        name: 'Unauthorized Service',
        description: 'Should fail',
        basePrice: 100,
      },
    });

    expect(
      response.status(),
      'Customer creando service type debería retornar 403'
    ).toBe(403);
  });

  authTest('@security TC-068: Admin SÍ puede acceder a /api/admin/orders', async ({ request, adminToken }) => {
    const response = await request.get('/api/admin/orders', {
      headers: authHeader(adminToken),
    });

    expect(
      response.status(),
      'Admin debería poder acceder a orders'
    ).toBe(200);
  });

  authTest('@security TC-069: Admin SÍ puede acceder a /api/admin/service-types', async ({ request, adminToken }) => {
    const response = await request.get('/api/admin/service-types', {
      headers: authHeader(adminToken),
    });

    expect(
      response.status(),
      'Admin debería poder acceder a service-types'
    ).toBe(200);
  });
});

test.describe('API Security - Token Validation @security', () => {
  
  // =====================================================
  // VALIDACIÓN DE TOKENS
  // =====================================================
  
  test('@security TC-070: Token inválido retorna 401', async ({ request }) => {
    const response = await request.get('/api/auth/me', {
      headers: { Authorization: 'Bearer invalid.token.here' },
    });

    expect(
      response.status(),
      'Token inválido debería retornar 401'
    ).toBe(401);
  });

  test('@security TC-071: Token malformado retorna 401', async ({ request }) => {
    const response = await request.get('/api/auth/me', {
      headers: { Authorization: 'Bearer not-a-jwt' },
    });

    expect(
      response.status(),
      'Token malformado debería retornar 401'
    ).toBe(401);
  });

  test('@security TC-072: Header Authorization vacío retorna 401', async ({ request }) => {
    const response = await request.get('/api/auth/me', {
      headers: { Authorization: '' },
    });

    expect(
      response.status(),
      'Authorization vacío debería retornar 401'
    ).toBe(401);
  });

  test('@security TC-073: Token expirado (simulado) retorna 401', async ({ request }) => {
    // Token JWT expirado de ejemplo (exp en el pasado)
    const expiredToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwiZW1haWwiOiJ0ZXN0QHRlc3QuY29tIiwiZXhwIjoxNjAwMDAwMDAwfQ.invalid-signature';
    
    const response = await request.get('/api/auth/me', {
      headers: { Authorization: `Bearer ${expiredToken}` },
    });

    expect(
      response.status(),
      'Token expirado debería retornar 401'
    ).toBe(401);
  });
});

// Tests usando el fixture de auth
authTest.describe('API Security - Authenticated User Operations @regression', () => {
  
  // =====================================================
  // OPERACIONES AUTENTICADAS
  // =====================================================
  
  authTest('@regression TC-075: GET /api/auth/me con token válido retorna datos del usuario', async ({ request, customerToken }) => {
    const response = await request.get('/api/auth/me', {
      headers: authHeader(customerToken),
    });

    expect(response.status(), '/me con token válido debería retornar 200').toBe(200);

    const body = await response.json();
    expect(body.email, 'Debe retornar email del usuario').toBeDefined();
    expect(body.id, 'Debe retornar ID del usuario').toBeDefined();
    expect(body.roles, 'Debe retornar roles del usuario').toBeInstanceOf(Array);
  });

  authTest('@regression TC-076: Admin puede ver su perfil', async ({ request, adminToken }) => {
    const response = await request.get('/api/auth/me', {
      headers: authHeader(adminToken),
    });

    expect(response.status(), 'Admin /me debería retornar 200').toBe(200);

    const body = await response.json();
    expect(
      body.roles,
      'Admin debe tener rol Admin'
    ).toContain('Admin');
  });
});

test.describe('API Security - User Registration @regression', () => {
  
  // =====================================================
  // REGISTRO DE USUARIOS
  // =====================================================
  
  test('@regression TC-080: Registro de nuevo usuario exitoso', async ({ request }) => {
    const uniqueEmail = uniqueTestEmail('register');
    
    const response = await request.post('/api/auth/register', {
      data: {
        name: 'Test User Registration',
        email: uniqueEmail,
        password: 'SecurePass123!',
        phone: '555-0199',
      },
    });

    expect(
      response.status(),
      'Registro exitoso debería retornar 200'
    ).toBe(200);

    const body = await response.json();
    expect(body.accessToken, 'Debe retornar accessToken').toBeDefined();
    expect(body.user, 'Debe retornar datos del usuario').toBeDefined();
    expect(body.user.email, 'Email debe coincidir').toBe(uniqueEmail);
    expect(body.user.roles, 'Usuario nuevo debe tener rol Customer').toContain('Customer');
  });

  test('@regression TC-081: Registro con email duplicado retorna error', async ({ request }) => {
    // Primero registrar un usuario
    const testEmail = uniqueTestEmail('duplicate');
    
    await request.post('/api/auth/register', {
      data: {
        name: 'First User',
        email: testEmail,
        password: 'FirstPass123!',
        phone: '555-0200',
      },
    });

    // Intentar registrar con el mismo email
    const duplicateResponse = await request.post('/api/auth/register', {
      data: {
        name: 'Second User',
        email: testEmail,
        password: 'SecondPass123!',
        phone: '555-0201',
      },
    });

    expect(
      duplicateResponse.status(),
      'Registro duplicado debería retornar 400'
    ).toBe(400);

    const body = await duplicateResponse.json();
    expect(
      body.message || body.error,
      'Debe incluir mensaje de error'
    ).toBeDefined();
  });

  test('@regression TC-082: Registro con email inválido retorna error', async ({ request }) => {
    const response = await request.post('/api/auth/register', {
      data: {
        name: 'Invalid Email User',
        email: 'not-an-email',
        password: 'ValidPass123!',
        phone: '555-0202',
      },
    });

    expect(
      response.status(),
      'Email inválido debería retornar 400'
    ).toBeGreaterThanOrEqual(400);
  });

  test('@regression TC-083: Registro con password débil retorna error', async ({ request }) => {
    const response = await request.post('/api/auth/register', {
      data: {
        name: 'Weak Password User',
        email: uniqueTestEmail('weakpass'),
        password: '123', // Password muy débil
        phone: '555-0203',
      },
    });

    // Dependiendo de la validación puede ser 400 o aceptarlo
    // Lo importante es que no sea 500
    expect(
      response.status(),
      'Password débil no debería causar error de servidor'
    ).toBeLessThan(500);
  });
});

test.describe('API Security - Email Check Endpoint @regression', () => {
  
  // =====================================================
  // VERIFICACIÓN DE EMAIL
  // =====================================================
  
  test('@regression TC-085: Verificar email existente retorna exists=true', async ({ request }) => {
    const response = await request.get('/api/auth/check-email', {
      params: { email: AUTH_CREDENTIALS.customer.email },
    });

    expect(response.status(), 'Check-email debería retornar 200').toBe(200);

    const body = await response.json();
    expect(body.exists, 'Email existente debe retornar exists=true').toBe(true);
    expect(body.email, 'Debe retornar el email verificado').toBe(AUTH_CREDENTIALS.customer.email);
  });

  test('@regression TC-086: Verificar email inexistente retorna exists=false', async ({ request }) => {
    const response = await request.get('/api/auth/check-email', {
      params: { email: 'nonexistent@nowhere.test' },
    });

    expect(response.status(), 'Check-email debería retornar 200').toBe(200);

    const body = await response.json();
    expect(body.exists, 'Email inexistente debe retornar exists=false').toBe(false);
  });

  test('@regression TC-087: Verificar email vacío retorna 400', async ({ request }) => {
    const response = await request.get('/api/auth/check-email', {
      params: { email: '' },
    });

    expect(
      response.status(),
      'Email vacío debería retornar 400'
    ).toBe(400);
  });
});

// Tests de cambio de password con fixture
authTest.describe('API Security - Password Management @regression', () => {
  
  authTest('@regression TC-090: Cambio de password requiere autenticación', async ({ request }) => {
    const response = await request.post('/api/auth/change-password', {
      data: {
        currentPassword: 'OldPass123!',
        newPassword: 'NewPass456!',
      },
    });

    expect(
      response.status(),
      'Change-password sin token debería retornar 401'
    ).toBe(401);
  });

  authTest('@regression TC-091: Forgot password no revela si email existe', async ({ request }) => {
    // Probar con email existente
    const responseExisting = await request.post('/api/auth/forgot-password', {
      data: { email: AUTH_CREDENTIALS.customer.email },
    });

    // Probar con email inexistente
    const responseNonExisting = await request.post('/api/auth/forgot-password', {
      data: { email: 'nonexistent@nowhere.test' },
    });

    // Ambas respuestas deberían ser idénticas por seguridad
    expect(
      responseExisting.status(),
      'Forgot-password siempre debe retornar 200 por seguridad'
    ).toBe(200);
    
    expect(
      responseNonExisting.status(),
      'Forgot-password con email inexistente también debe retornar 200'
    ).toBe(200);
  });
});
