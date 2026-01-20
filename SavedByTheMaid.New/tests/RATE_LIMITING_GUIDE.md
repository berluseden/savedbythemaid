# Manejo de Rate Limiting en Playwright API Tests

## 📋 Resumen del Problema

Tu API .NET devuelve `429 Too Many Requests` cuando hay demasiadas solicitudes consecutivas.
Los tests de Playwright hacen múltiples llamadas rápidamente, causando fallos intermitentes.

## 🎯 Soluciones Implementadas

### 1. Cliente de API con Retry Automático

Archivo: [utils/rate-limit-handler.ts](utils/rate-limit-handler.ts)

```typescript
import { RateLimitedApiClient } from '../utils/rate-limit-handler';

// El cliente reintenta automáticamente en 429
const apiClient = new RateLimitedApiClient(request, {
  maxRetries: 3,
  initialDelay: 1000,      // Empezar con 1 segundo
  backoffMultiplier: 2,    // Duplicar en cada retry (1s, 2s, 4s)
  maxDelay: 10000,         // Máximo 10 segundos
  retryableStatuses: [429, 503],
  verbose: true,           // Ver logs de retry
});

const response = await apiClient.get('/api/bookings');
```

**Características:**
- ✅ Backoff exponencial con jitter (evita thundering herd)
- ✅ Respeta el header `Retry-After` del servidor
- ✅ Configurable por instancia
- ✅ Logging opcional para debugging

### 2. Fixture de Rate Limiting

Archivo: [fixtures/rate-limit.fixture.ts](fixtures/rate-limit.fixture.ts)

```typescript
import { test, expect, AGGRESSIVE_RETRY_CONFIG } from '../fixtures/rate-limit.fixture';

// Usar configuración agresiva para este archivo
test.use({ retryConfig: AGGRESSIVE_RETRY_CONFIG });

test('mi test con rate limiting', async ({ apiClient }) => {
  // apiClient ya tiene retry automático configurado
  const response = await apiClient.post('/api/bookings', {
    data: { ... }
  });
  expect(response.status()).toBe(201);
});
```

### 3. Uso de `expect().toPass()` (Recomendado por Playwright)

La función más elegante para manejar retries en aserciones:

```typescript
import { test, expect } from '@playwright/test';

test('API con retry en aserción', async ({ request }) => {
  await expect(async () => {
    const response = await request.get('/api/flaky-endpoint');
    
    // Si es 429, lanzar error para que toPass() reintente
    if (response.status() === 429) {
      throw new Error('Rate limited, retrying...');
    }
    
    expect(response.status()).toBe(200);
    const data = await response.json();
    expect(data.items).toHaveLength(5);
  }).toPass({
    // Intervalos de retry: 500ms, 1s, 2s, 5s
    intervals: [500, 1_000, 2_000, 5_000],
    timeout: 30_000,
  });
});
```

## ⚙️ Configuración Óptima de playwright.config.ts

```typescript
import { defineConfig } from '@playwright/test';

export default defineConfig({
  // Para tests de API con rate limiting, usar 1 worker
  workers: process.env.CI ? 1 : 1,
  
  // NO ejecutar en paralelo (evita rate limiting)
  fullyParallel: false,
  
  // Reintentar tests fallidos
  retries: process.env.CI ? 3 : 1,
  
  // Timeout más largo para tests con retry
  timeout: 60_000,
  
  // Configuración de expect
  expect: {
    timeout: 10_000,
  },
  
  // Por proyecto
  projects: [
    {
      name: 'api-tests',
      testDir: './api',
      // Tests de API son más lentos con rate limiting
      timeout: 90_000,
      // Forzar ejecución serial
      fullyParallel: false,
    },
    {
      name: 'e2e-tests',
      testDir: './e2e',
      // E2E pueden correr en paralelo (cada browser es independiente)
      fullyParallel: true,
    },
  ],
});
```

## 📝 Ejemplos de Migración

### Antes (delay manual):

```typescript
const delay = (ms: number) => new Promise(r => setTimeout(r, ms));

test.beforeEach(async () => {
  await delay(300); // ❌ Delay fijo, no elegante
});

test('login test', async ({ request }) => {
  const response = await request.post('/api/auth/login', {...});
  expect(response.status()).toBe(200);
});
```

### Después (con fixture):

```typescript
import { test, expect } from '../fixtures/rate-limit.fixture';

// Sin necesidad de delay manual
test('login test', async ({ apiClient }) => {
  const response = await apiClient.post('/api/auth/login', {...});
  expect(response.status()).toBe(200);
});
```

### Después (con toPass):

```typescript
test('login test', async ({ request }) => {
  await expect(async () => {
    const response = await request.post('/api/auth/login', {...});
    if (response.status() === 429) throw new Error('Rate limited');
    expect(response.status()).toBe(200);
  }).toPass({ intervals: [500, 1000, 2000] });
});
```

## 🔧 Estrategias Adicionales

### 1. Agrupar Tests con `test.describe.configure`

```typescript
test.describe('API Security Tests', () => {
  // Ejecutar en serie (no paralelo)
  test.describe.configure({ mode: 'serial' });
  
  // Configurar retries solo para este grupo
  test.describe.configure({ retries: 3 });
  
  test('test 1', ...);
  test('test 2', ...);
});
```

### 2. Marcar Tests como Lentos

```typescript
test('operación pesada', async ({ request }) => {
  test.slow(); // Triple de timeout
  // ... test code
});
```

### 3. Timeout Personalizado por Test

```typescript
test('test con timeout largo', async ({ request }) => {
  test.setTimeout(120_000); // 2 minutos
  // ... test code
});
```

### 4. Reutilizar Tokens (Menos Requests de Login)

```typescript
// En fixtures/auth.fixture.ts
let cachedToken: string | null = null;

export const test = base.extend({
  authToken: async ({ request }, use) => {
    if (!cachedToken) {
      const response = await request.post('/api/auth/login', {...});
      cachedToken = (await response.json()).accessToken;
    }
    await use(cachedToken);
  },
});
```

### 5. Storage State para Sesiones

```typescript
// Guardar estado después del setup
await request.storageState({ path: 'auth.json' });

// Reutilizar en playwright.config.ts
projects: [
  {
    name: 'setup',
    testMatch: /auth\.setup\.ts/,
  },
  {
    name: 'tests',
    dependencies: ['setup'],
    use: {
      storageState: 'auth.json',
    },
  },
]
```

## 📊 Comparación de Estrategias

| Estrategia | Complejidad | Elegancia | Flexibilidad |
|------------|-------------|-----------|--------------|
| `delay()` manual | Baja | ❌ Baja | Baja |
| `test.slow()` | Baja | ⚠️ Media | Baja |
| `expect().toPass()` | Media | ✅ Alta | Alta |
| Cliente con retry | Media | ✅ Alta | Muy alta |
| Fixture global | Alta | ✅ Muy alta | Muy alta |

## 🚀 Recomendación Final

1. **Para casos simples**: Usa `expect().toPass()` con intervalos personalizados
2. **Para suites grandes**: Implementa el fixture `rate-limit.fixture.ts`
3. **Siempre**:
   - Configura `workers: 1` para tests de API
   - Usa `fullyParallel: false` en describe blocks
   - Reutiliza tokens de autenticación
   - Configura reintentos a nivel de config

## 📚 Referencias

- [Playwright Retries Documentation](https://playwright.dev/docs/test-retries)
- [expect.toPass() API](https://playwright.dev/docs/test-assertions#expecttopass)
- [API Testing Best Practices](https://playwright.dev/docs/api-testing)
- [Test Configuration](https://playwright.dev/docs/test-configuration)
