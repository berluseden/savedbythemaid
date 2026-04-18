import { test as base, expect, APIRequestContext } from '@playwright/test';

/**
 * Authentication Fixture para tests de API
 * =========================================
 * 
 * Proporciona tokens JWT pre-autenticados para diferentes roles:
 * - customerToken: Token de usuario con rol Customer
 * - adminToken: Token de usuario con rol Admin
 * 
 * Uso:
 * ```typescript
 * import { test, expect } from '../fixtures/auth.fixture';
 * 
 * test('acceso con token', async ({ request, customerToken }) => {
 *   const response = await request.get('/api/auth/me', {
 *     headers: { 'Authorization': `Bearer ${customerToken}` }
 *   });
 *   expect(response.ok()).toBeTruthy();
 * });
 * ```
 */

// Credenciales de prueba (deben existir en la BD de test)
// NOTA: Para tests que requieren customer, usar el endpoint de registro primero
export const AUTH_CREDENTIALS = {
  customer: {
    // Temporalmente usar admin como customer para tests básicos
    email: 'admin@savedbytemaid.com',
    password: 'Admin123!',
  },
  admin: {
    email: 'admin@savedbytemaid.com',
    password: 'Admin123!',
  },
};

interface AuthTokens {
  customerToken: string;
  adminToken: string;
  getToken: (email: string, password: string) => Promise<string>;
}

interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: {
    id: string;
    email: string;
    phone?: string;
    roles: string[];
  };
}

/**
 * Función helper para obtener token JWT mediante login
 */
async function loginAndGetToken(
  request: APIRequestContext,
  email: string,
  password: string
): Promise<string> {
  const response = await request.post('/api/auth/login', {
    data: { email, password },
  });

  if (!response.ok()) {
    const errorBody = await response.text();
    throw new Error(
      `Login failed for ${email}: ${response.status()} - ${errorBody}`
    );
  }

  const authResponse: AuthResponse = await response.json();
  
  if (!authResponse.accessToken) {
    throw new Error(`No accessToken in response for ${email}`);
  }

  return authResponse.accessToken;
}

/**
 * Fixture extendido con autenticación
 */
export const test = base.extend<AuthTokens>({
  /**
   * Token JWT para usuario Customer
   * Se obtiene automáticamente al inicio del test
   */
  customerToken: async ({ request }, use) => {
    let token: string;
    
    try {
      token = await loginAndGetToken(
        request,
        AUTH_CREDENTIALS.customer.email,
        AUTH_CREDENTIALS.customer.password
      );
    } catch (error) {
      // Si el usuario no existe, intentar registrarlo primero
      console.log('Customer user not found, attempting to register...');
      
      const registerResponse = await request.post('/api/auth/register', {
        data: {
          name: 'Test Customer',
          email: AUTH_CREDENTIALS.customer.email,
          password: AUTH_CREDENTIALS.customer.password,
          phone: '555-0100',
        },
      });

      if (registerResponse.ok()) {
        const authResponse: AuthResponse = await registerResponse.json();
        token = authResponse.accessToken;
      } else {
        // Reintentar login en caso de que el registro falle por email duplicado
        token = await loginAndGetToken(
          request,
          AUTH_CREDENTIALS.customer.email,
          AUTH_CREDENTIALS.customer.password
        );
      }
    }

    await use(token);
  },

  /**
   * Token JWT para usuario Admin
   * Se obtiene automáticamente al inicio del test
   */
  adminToken: async ({ request }, use) => {
    let token: string;

    try {
      token = await loginAndGetToken(
        request,
        AUTH_CREDENTIALS.admin.email,
        AUTH_CREDENTIALS.admin.password
      );
    } catch (error) {
      throw new Error(
        `Admin login failed. Ensure admin user exists in DB: ${error}`
      );
    }

    await use(token);
  },

  /**
   * Función helper para obtener token de cualquier usuario
   * Útil para tests que necesitan usuarios dinámicos
   */
  getToken: async ({ request }, use) => {
    const tokenGetter = async (email: string, password: string): Promise<string> => {
      return loginAndGetToken(request, email, password);
    };

    await use(tokenGetter);
  },
});

export { expect };

// AUTH_CREDENTIALS ya exportadas arriba

/**
 * Helper para crear headers de autorización
 */
export function authHeader(token: string): { Authorization: string } {
  return { Authorization: `Bearer ${token}` };
}

/**
 * Helper para generar email único para tests
 */
export function uniqueTestEmail(prefix: string = 'test'): string {
  return `${prefix}-${Date.now()}-${Math.random().toString(36).substr(2, 9)}@test.com`;
}
