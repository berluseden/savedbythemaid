# �️ ESTRATEGIA DE CALIDAD DE SOFTWARE (QA Master Plan)
**Proyecto:** SavedByTheMaid Platform  
**QA Lead:** GitHub Copilot (Senior QA Engineer)  
**Fecha:** 7 de Enero 2026  
**Versión:** v1.0  
**Enfoque:** Customer-Centric Testing & Business-Critical Validation

---

## 📊 RESUMEN EJECUTIVO

### Visión General del Proyecto
SavedByTheMaid es una plataforma de marketplace de servicios de limpieza que permite a clientes reservar servicios mediante un wizard multi-paso, gestionar disponibilidad de empleadas por zonas geográficas, y administrar órdenes. **Sistema crítico transaccional** donde la integridad de reservas, cálculo de precios y disponibilidad son el núcleo del negocio.

### Estado Actual
- **Backend:** ASP.NET Core .NET 10 con MySQL
- **Frontend:** React 18 + TypeScript + Vite
- **Arquitectura:** Clean Architecture (Domain, Application, Infrastructure, API)
- **Sistema de Pagos:** ELIMINADO (decisión de negocio)

### Flujo de Negocio Crítico (Core User Journey)
```
Cliente → ZIP Code → Service Selection → Details → Schedule → 
Soft Reserve (15 min TTL) → Contact Info → Confirm → Order Created (Draft)
```

Soft Reserve (15 min TTL) → Contact Info → Confirm → Order Created (Draft)
```

---

## 🎯 1. OBJETIVOS DE PRUEBAS (TEST OBJECTIVES)

### Objetivos Alineados al Negocio
1. **Garantizar la integridad de reservas**: Prevenir double-booking (dos clientes en el mismo slot)
2. **Validar precisión de precios**: Backend debe rechazar precios manipulados desde el cliente
3. **Asegurar disponibilidad de agenda**: Sistema de Soft Reserve debe funcionar bajo concurrencia
4. **Optimizar experiencia de usuario**: Completar reserva en < 2 minutos sin errores

### Objetivos Alineados al Usuario Final
- **Como cliente**, necesito confiar en que mi reserva es real y no será cancelada
- **Como cliente**, necesito ver precios transparentes y consistentes
- **Como admin**, necesito gestionar órdenes sin conflictos de estado
- **Como empleada**, necesito recibir solo asignaciones válidas (sin conflictos de horario)

---

## 📋 2. ESTRATEGIA DE TESTING

### 2.1 Pirámide de Pruebas (Testing Pyramid)

```
           /\
          /  \  E2E (30%)         ← Flujos críticos de usuario
         /____\
        /      \ Integration (20%) ← Servicios + BD + Background Jobs
       /________\
      /          \ API (40%)       ← Endpoints públicos + Reglas de negocio
     /____________\
    /              \ Unit (10%)    ← Lógica de dominio aislada
```

**Justificación de la distribución:**
- **API Testing (40%)**: Mayor ROI, más rápido, menos frágil que E2E
- **E2E (30%)**: Crítico para validar el wizard completo desde la perspectiva del usuario
- **Integration (20%)**: Validar SoftReserveCleanupService, migraciones automáticas, etc.
- **Unit (10%)**: El equipo ya implementó validaciones inline, priorizamos integración

### 2.2 Tipos de Pruebas

| Tipo | Alcance | Herramienta | Ejecutor | Frecuencia |
|------|---------|-------------|----------|------------|
| **Unit Tests** | Lógica de dominio, cálculos | xUnit/NUnit | Backend devs | Por commit |
| **API Tests** | Endpoints REST, validaciones de negocio | Playwright API + Postman | QA Automation | Por PR |
| **Integration Tests** | DB + Services + Background Jobs | .NET TestServer | Backend devs | Por PR |
| **E2E Tests** | Wizard completo, Admin Panel | Playwright | QA Automation | Nightly + Pre-release |
| **Smoke Tests** | Funcionalidad básica (Login, Home) | Playwright | CI/CD | Por deploy |
| **Regression Suite** | Casos críticos históricos | Playwright | QA Manual + Auto | Por release |
| **Exploratory Testing** | UX, edge cases no automatizables | Manual | QA Lead | Por sprint |
| **Security Testing** | Inyección SQL, XSS, CSRF | OWASP ZAP + Manual | Security QA | Por release mayor |

### 2.3 Selección de Herramienta de Automatización

**Herramienta Elegida: PLAYWRIGHT** ✅

**Justificación vs Cypress:**

| Criterio | Playwright | Cypress | Ganador |
|----------|-----------|---------|---------|
| Velocidad | Paralelo nativo, headless por defecto | Secuencial, más lento | 🏆 Playwright |
| Estabilidad | Auto-wait inteligente | Requiere custom waits | 🏆 Playwright |
| API Testing | Soporte nativo | Requiere plugins | 🏆 Playwright |
| Multi-browser | Chrome, Firefox, Safari, Edge | Chrome, Firefox, Edge (beta) | 🏆 Playwright |
| Debugging | Trace Viewer (video + red + DOM) | Time-travel debugging | Empate |
| TypeScript | First-class support | Soporte básico | 🏆 Playwright |
| Comunidad | Creciente, Microsoft | Más madura | Cypress |

**Decisión:** Playwright por velocidad y soporte API/UI en el mismo framework.

### 2.4 Alcance de Pruebas

#### ✅ In Scope
- **Booking Wizard (6 pasos)**: ZipCode → Service → Details → Schedule → Contact → Confirm
- **Admin Panel**: Gestión de órdenes (view, update status, assign employee)
- **Customer Dashboard**: Ver mis reservas, cancelar
- **Background Services**: SoftReserveCleanupService
- **Database Migrations**: Auto-apply al startup

#### ❌ Out of Scope (Esta Fase)
- Procesamiento de pagos (eliminado del sistema)
- Load testing / Stress testing (Fase 2)
- Penetration testing completo (solo básico OWASP Top 10)
- Testing en dispositivos móviles físicos (solo emulación)

### 2.5 Ambientes de Prueba

| Ambiente | Propósito | URL | Datos | CI/CD |
|----------|-----------|-----|-------|-------|
| **Local** | Desarrollo y debugging | localhost:5000 | Seed data | No |
| **Staging** | Testing completo pre-release | staging.savedbythemaid.com | Datos realistas anonimizados | Auto-deploy por PR |
| **Production** | Smoke tests post-deploy | savedbythemaid.com | Datos reales | Monitoreo continuo |

---

## 📝 3. CASOS DE PRUEBA DETALLADOS (TEST CASES)

### 3.1 Flujo Crítico: Booking Wizard (Happy Path)

#### TC-E2E-001: Reserva Exitosa (Happy Path)
- **ID**: TC-E2E-001
- **Prioridad**: 🔴 CRÍTICA
- **Tipo**: E2E
- **Descripción**: Como cliente, quiero completar una reserva de Deep Cleaning para verificar que el sistema funciona end-to-end.

**Precondiciones:**
- Sistema en estado limpio (sin reservas previas en el slot de prueba)
- Empleadas disponibles en el área de prueba (ZIP: 10001)
- Servicios activos en catálogo

**Pasos:**
1. Navegar a `/booking`
2. Ingresar ZIP Code: `10001`
3. Click "Check Coverage"
4. Verificar mensaje: "¡Excelente! Damos servicio en tu zona"
5. Click "Next"
6. Seleccionar servicio: "Deep Clean"
7. Click "Next"
8. Ingresar:
   - Bedrooms: `2`
   - Bathrooms: `2`
   - Square Feet: `1500`
9. Click "Get Estimate"
10. Verificar estimado mostrado (ej. `$120.00`)
11. Click "Next"
12. Seleccionar fecha: `Mañana`
13. Seleccionar slot: `09:00 AM`
14. Click "Reserve Slot" (crea SoftReserve)
15. Verificar mensaje: "Slot reserved for 15 minutes"
16. Click "Next"
17. Ingresar:
    - First Name: `Juan`
    - Last Name: `Pérez`
    - Email: `juan@test.com`
    - Phone: `555-1234`
    - Address: `123 Main St`
18. Click "Next"
19. Revisar resumen (precio, fecha, hora)
20. Click "Confirm Booking"

**Resultado Esperado:**
- ✅ Pantalla de éxito mostrada: "Your booking has been confirmed!"
- ✅ Número de orden mostrado (ej. `#12345`)
- ✅ Email de confirmación enviado (verificar en logs si no hay SMTP real)
- ✅ Base de datos:
  - `ServiceOrders` tiene nueva fila con `OrderStatus = Draft`
  - `ServiceMeets` tiene nueva fila con `Status = Scheduled`
  - `SoftReserves` tiene estado `Confirmed`

**Datos de Entrada:**
```json
{
  "zipCode": "10001",
  "serviceTypeId": 1,
  "bedrooms": 2,
  "bathrooms": 2,
  "squareFeet": 1500,
  "date": "tomorrow",
  "timeSlot": "09:00",
  "contact": {
    "firstName": "Juan",
    "lastName": "Pérez",
    "email": "juan@test.com",
    "phone": "555-1234"
  }
}
```

---

#### TC-E2E-002: Cancelación de Reserva Soft
- **ID**: TC-E2E-002
- **Prioridad**: 🟡 ALTA
- **Tipo**: E2E
- **Descripción**: Validar que al hacer "Back" desde el paso de Contact, se cancela el SoftReserve y se libera el slot.

**Pasos:**
1. Completar hasta el paso 5 (Schedule) y crear SoftReserve
2. Click "Next" para ir a Contact
3. Click "Back" para regresar a Schedule
4. Verificar que el slot muestra "Available" nuevamente

**Resultado Esperado:**
- SoftReserve marcado como `Cancelled` en BD
- Slot disponible para otro usuario

---

### 3.2 Validaciones de Seguridad y Negocio

#### TC-API-003: Fraude de Precio (Manipulación de Total)
- **ID**: TC-API-003
- **Prioridad**: 🔴 CRÍTICA
- **Tipo**: API Security
- **Descripción**: Intentar enviar un `Total` manipulado al endpoint `/api/booking/confirm`.

**Precondiciones:**
- SoftReserve válido creado con estimado real de `$150.00`

**Pasos:**
1. Interceptar request POST `/api/booking/confirm`
2. Modificar payload:
   ```json
   {
     "softReserveId": 123,
     "total": 10.00,  // MANIPULADO (real: 150.00)
     ...
   }
   ```
3. Enviar request

**Resultado Esperado:**
- ✅ HTTP `400 Bad Request`
- ✅ Mensaje: `"Pricing mismatch detected"`
- ✅ Orden NO creada en BD
- ✅ Log de seguridad generado con nivel WARNING

---

#### TC-API-004: Validación de Recalculo de Precio
- **ID**: TC-API-004
- **Prioridad**: 🔴 CRÍTICA
- **Tipo**: API
- **Descripción**: Verificar que el backend recalcula el precio independientemente del valor enviado por el frontend.

**Pasos:**
1. Enviar request POST `/api/booking/estimate`:
   ```json
   {
     "serviceTypeId": 1,
     "rooms": [{"roomTypeId": 1, "quantity": 2}],
     "additionalServiceIds": [5]
   }
   ```
2. Backend retorna: `{ "total": 150.00 }`
3. Enviar request POST `/api/booking/confirm` con el mismo payload + `total: 150.00`

**Resultado Esperado:**
- ✅ HTTP `200 OK`
- ✅ Orden creada con `Total = 150.00`
- ✅ Si cambiamos `total: 151.00`, debe rechazarse

---

### 3.3 Casos Edge y Negativos

#### TC-NEG-005: ZIP Code sin Cobertura
- **ID**: TC-NEG-005
- **Prioridad**: 🟡 ALTA
- **Tipo**: E2E Negative
- **Descripción**: Validar mensaje de error amigable cuando no hay cobertura.

**Pasos:**
1. Ingresar ZIP Code: `00000` (no existe en BD)
2. Click "Check Coverage"

**Resultado Esperado:**
- ✅ Mensaje: "Lo sentimos, aún no damos servicio en esta zona."
- ✅ NO mostrar error 500 o spinner infinito
- ✅ Botón "Next" debe estar deshabilitado

---

#### TC-NEG-006: Cantidad de Habitaciones Negativa
- **ID**: TC-NEG-006
- **Prioridad**: 🟢 MEDIA
- **Tipo**: Validation
- **Descripción**: Sistema debe prevenir valores inválidos en inputs numéricos.

**Pasos:**
1. En paso "Details", intentar ingresar:
   - Bedrooms: `-5`
   - Bathrooms: `0`
   - Square Feet: `999999`

**Resultado Esperado:**
- ✅ Input no permite negativos (validación HTML5 `min="1"`)
- ✅ O muestra mensaje de error: "Debe ser mayor a 0"
- ✅ Botón "Next" deshabilitado si valores inválidos

---

#### TC-EDGE-007: Concurrencia (Race Condition)
- **ID**: TC-EDGE-007
- **Prioridad**: 🔴 CRÍTICA
- **Tipo**: Integration
- **Descripción**: Validar que MySQL GET_LOCK previene double-booking.

**Pasos:**
1. Ejecutar 2 requests simultáneos POST `/api/booking/soft-reserve`:
   ```json
   {
     "date": "2026-01-10",
     "timeSlot": "10:00",
     "employeeId": 1
   }
   ```
2. Enviar ambos requests en paralelo (ej. con `Promise.all()`)

**Resultado Esperado:**
- ✅ Solo 1 request debe retornar `200 OK`
- ✅ El segundo debe retornar `400 Bad Request` o `503 Service Unavailable`
- ✅ Mensaje: "Slot no longer available"
- ✅ BD debe tener solo 1 SoftReserve activo para ese slot

---

#### TC-EDGE-008: Expiración de Soft Reserve
- **ID**: TC-EDGE-008
- **Prioridad**: 🟡 ALTA
- **Tipo**: Integration
- **Descripción**: SoftReserveCleanupService debe marcar reservas expiradas.

**Pasos:**
1. Crear SoftReserve con `ExpiresAt = DateTime.UtcNow.AddMinutes(-1)` (ya expirado)
2. Esperar a que `SoftReserveCleanupService` ejecute (corre cada 5 min)
3. O ejecutar manualmente el servicio

**Resultado Esperado:**
- ✅ SoftReserve marcado como `Expired`
- ✅ Log: "Marked 1 soft reserves as expired"

---

### 3.4 Validaciones de UX y Mensajes

#### TC-UX-009: Mensajes de Error Claros
- **ID**: TC-UX-009
- **Prioridad**: 🟢 MEDIA
- **Tipo**: UX
- **Descripción**: Todos los errores deben mostrar mensajes en español claro (no stack traces).

**Escenarios a Probar:**
- Error 400: "Datos inválidos, verifica tu información"
- Error 404: "Recurso no encontrado"
- Error 500: "Ocurrió un error, intenta de nuevo"
- Timeout de red: "Conexión lenta, verifica tu internet"

**Resultado Esperado:**
- ✅ Ningún mensaje técnico visible para el usuario
- ✅ Stack traces solo en consola (desarrollo)

---

#### TC-UX-010: Tiempos de Carga
- **ID**: TC-UX-010
- **Prioridad**: 🟢 MEDIA
- **Tipo**: Performance
- **Descripción**: Endpoint crítico `/api/booking/estimate` debe responder en < 500ms.

**Pasos:**
1. Ejecutar 10 requests POST `/estimate` con payload estándar
2. Medir tiempo de respuesta (p95)

**Resultado Esperado:**
- ✅ p95 < 500ms
- ✅ p99 < 1000ms

---

## 🤖 4. SCRIPTS DE AUTOMATIZACIÓN (PLAYWRIGHT)

### 4.1 Estructura de Proyecto

```
/SavedByTheMaid.New/tests/
  playwright.config.ts
  /e2e
    booking-wizard.spec.ts
    admin-panel.spec.ts
    customer-dashboard.spec.ts
  /api
    pricing-security.spec.ts
    availability.spec.ts
  /pages
    BookingPage.ts
    AdminPage.ts
    BasePage.ts
  /fixtures
    test-users.json
    test-data.json
  /utils
    db-helpers.ts
    auth-helpers.ts
```

### 4.2 Configuración Base (playwright.config.ts)

```typescript
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 4 : undefined,
  reporter: [
    ['html'],
    ['json', { outputFile: 'test-results/results.json' }],
    ['junit', { outputFile: 'test-results/junit.xml' }]
  ],
  use: {
    baseURL: process.env.BASE_URL || 'http://localhost:5000',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },
    {
      name: 'mobile-chrome',
      use: { ...devices['Pixel 5'] },
    },
  ],

  webServer: {
    command: 'cd SavedByTheMaid.Api && dotnet run',
    url: 'http://localhost:5000/health',
    reuseExistingServer: !process.env.CI,
    timeout: 120000,
  },
});
```

### 4.3 Page Object Model (POM)

#### BasePage.ts
```typescript
import { type Page, type Locator } from '@playwright/test';

export class BasePage {
  readonly page: Page;

  constructor(page: Page) {
    this.page = page;
  }

  async goto(path: string) {
    await this.page.goto(path);
    await this.page.waitForLoadState('networkidle');
  }

  async fillInput(label: string | RegExp, value: string) {
    await this.page.getByLabel(label).fill(value);
  }

  async clickButton(text: string | RegExp) {
    await this.page.getByRole('button', { name: text }).click();
  }

  async waitForToast(message: string | RegExp) {
    await this.page.getByText(message).waitFor({ state: 'visible' });
  }
}
```

#### BookingPage.ts
```typescript
import { type Page, type Locator, expect } from '@playwright/test';
import { BasePage } from './BasePage';

export class BookingPage extends BasePage {
  // Locators
  readonly zipInput: Locator;
  readonly checkCoverageBtn: Locator;
  readonly nextBtn: Locator;
  readonly estimateDisplay: Locator;

  constructor(page: Page) {
    super(page);
    this.zipInput = page.getByPlaceholder(/zip code/i);
    this.checkCoverageBtn = page.getByRole('button', { name: /check coverage/i });
    this.nextBtn = page.getByRole('button', { name: /next|continue/i });
    this.estimateDisplay = page.locator('[data-testid="price-estimate"]');
  }

  async gotoBooking() {
    await this.goto('/booking');
  }

  async checkCoverage(zip: string) {
    await this.zipInput.fill(zip);
    await this.checkCoverageBtn.click();
    
    // Esperar respuesta del servidor
    await this.page.waitForResponse(
      resp => resp.url().includes('/api/booking/coverage') && resp.status() === 200
    );
    
    // Validar mensaje de éxito
    await expect(
      this.page.getByText(/excelente|we serve your area/i)
    ).toBeVisible();
  }

  async selectService(serviceName: string) {
    await this.page.getByText(serviceName, { exact: true }).click();
    await this.nextBtn.click();
  }

  async fillPropertyDetails(details: {
    bedrooms: number;
    bathrooms: number;
    squareFeet: number;
  }) {
    await this.fillInput(/bedrooms?/i, details.bedrooms.toString());
    await this.fillInput(/bathrooms?/i, details.bathrooms.toString());
    
    // Square feet es un slider
    const slider = this.page.getByLabel(/square feet/i);
    await slider.fill(details.squareFeet.toString());
    
    // Click "Get Estimate"
    await this.page.getByRole('button', { name: /get estimate/i }).click();
    
    // Esperar cálculo
    await this.page.waitForResponse(
      resp => resp.url().includes('/api/booking/estimate') && resp.status() === 200
    );
  }

  async selectTimeSlot(dayOffset: number, time: string) {
    // Seleccionar fecha (dayOffset días desde hoy)
    const targetDate = new Date();
    targetDate.setDate(targetDate.getDate() + dayOffset);
    
    await this.page.locator('.calendar-day').nth(dayOffset).click();
    
    // Seleccionar hora
    await this.page.getByRole('button', { name: new RegExp(time) }).click();
    
    // Click "Reserve Slot" - esto crea el SoftReserve
    const softReservePromise = this.page.waitForResponse(
      resp => resp.url().includes('/api/booking/soft-reserve') && resp.ok
    );
    
    await this.page.getByRole('button', { name: /reserve slot/i }).click();
    
    const response = await softReservePromise;
    const body = await response.json();
    
    return body.softReserveId;
  }

  async fillContactInfo(contact: {
    firstName: string;
    lastName: string;
    email: string;
    phone: string;
    address: string;
  }) {
    await this.fillInput(/first name/i, contact.firstName);
    await this.fillInput(/last name/i, contact.lastName);
    await this.fillInput(/email/i, contact.email);
    await this.fillInput(/phone/i, contact.phone);
    await this.fillInput(/address/i, contact.address);
    
    await this.nextBtn.click();
  }

  async confirmBooking() {
    const confirmPromise = this.page.waitForResponse(
      resp => resp.url().includes('/api/booking/confirm') && resp.ok
    );
    
    await this.page.getByRole('button', { name: /confirm booking/i }).click();
    
    const response = await confirmPromise;
    return await response.json();
  }

  async getEstimatedTotal(): Promise<string> {
    return await this.estimateDisplay.textContent() || '$0.00';
  }
}
```

### 4.4 Test Spec Completo (booking-wizard.spec.ts)

```typescript
import { test, expect } from '@playwright/test';
import { BookingPage } from '../pages/BookingPage';

test.describe('Booking Wizard - Critical Flow', () => {
  let bookingPage: BookingPage;

  test.beforeEach(async ({ page }) => {
    bookingPage = new BookingPage(page);
    await bookingPage.gotoBooking();
  });

  test('TC-E2E-001: Complete booking flow (Happy Path)', async ({ page }) => {
    // Step 1: Coverage
    await test.step('Check coverage for valid ZIP', async () => {
      await bookingPage.checkCoverage('10001');
    });

    // Step 2: Service Selection
    await test.step('Select Deep Clean service', async () => {
      await bookingPage.selectService('Deep Clean');
    });

    // Step 3: Property Details & Estimate
    await test.step('Fill property details and get estimate', async () => {
      await bookingPage.fillPropertyDetails({
        bedrooms: 2,
        bathrooms: 2,
        squareFeet: 1500
      });
      
      // Validar que el estimado se muestra
      const estimate = await bookingPage.getEstimatedTotal();
      expect(parseFloat(estimate.replace(/[^0-9.]/g, ''))).toBeGreaterThan(0);
    });

    // Step 4: Schedule & Soft Reserve
    await test.step('Select time slot and create soft reserve', async () => {
      const softReserveId = await bookingPage.selectTimeSlot(1, '09:00 AM');
      expect(softReserveId).toBeGreaterThan(0);
      
      // Validar mensaje de confirmación
      await expect(page.getByText(/reserved for 15 minutes/i)).toBeVisible();
    });

    // Step 5: Contact Information
    await test.step('Fill contact information', async () => {
      await bookingPage.fillContactInfo({
        firstName: 'Juan',
        lastName: 'Pérez',
        email: `test+${Date.now()}@qa.com`, // Email único
        phone: '555-1234',
        address: '123 Main St'
      });
    });

    // Step 6: Confirm Booking
    await test.step('Confirm booking and validate response', async () => {
      const confirmation = await bookingPage.confirmBooking();
      
      // Validaciones de respuesta
      expect(confirmation.serviceOrderId).toBeGreaterThan(0);
      expect(confirmation.message).toContain('confirmed');
      
      // Validar UI de éxito
      await expect(page.getByText(/booking confirmed|success/i)).toBeVisible();
      await expect(page.getByText(/#\d+/)).toBeVisible(); // Order number
    });
  });

  test('TC-NEG-005: Invalid ZIP code shows error message', async ({ page }) => {
    await test.step('Enter invalid ZIP and validate error', async () => {
      await bookingPage.zipInput.fill('00000');
      await bookingPage.checkCoverageBtn.click();
      
      await expect(
        page.getByText(/no damos servicio|not serve/i)
      ).toBeVisible();
      
      // Next button should be disabled
      await expect(bookingPage.nextBtn).toBeDisabled();
    });
  });

  test('TC-EDGE-007: Concurrent booking attempts (Race Condition)', async ({ browser }) => {
    // Crear 2 contextos (2 usuarios simultáneos)
    const context1 = await browser.newContext();
    const context2 = await browser.newContext();
    
    const page1 = await context1.newPage();
    const page2 = await context2.newPage();
    
    const booking1 = new BookingPage(page1);
    const booking2 = new BookingPage(page2);
    
    // Ambos completan hasta el paso de Schedule
    await Promise.all([
      booking1.gotoBooking().then(() => booking1.checkCoverage('10001')),
      booking2.gotoBooking().then(() => booking2.checkCoverage('10001'))
    ]);
    
    await Promise.all([
      booking1.selectService('Deep Clean'),
      booking2.selectService('Deep Clean')
    ]);
    
    await Promise.all([
      booking1.fillPropertyDetails({ bedrooms: 2, bathrooms: 2, squareFeet: 1500 }),
      booking2.fillPropertyDetails({ bedrooms: 2, bathrooms: 2, squareFeet: 1500 })
    ]);
    
    // Intentar reservar el MISMO slot simultáneamente
    const results = await Promise.allSettled([
      booking1.selectTimeSlot(1, '10:00 AM'),
      booking2.selectTimeSlot(1, '10:00 AM')
    ]);
    
    // Validar que solo UNO tuvo éxito
    const successCount = results.filter(r => r.status === 'fulfilled').length;
    expect(successCount).toBe(1);
    
    // El que falló debe ver mensaje de error
    const failedPage = results[0].status === 'rejected' ? page1 : page2;
    await expect(
      failedPage.getByText(/no longer available|ya no disponible/i)
    ).toBeVisible();
    
    await context1.close();
    await context2.close();
  });
});
```

### 4.5 API Security Tests (pricing-security.spec.ts)

```typescript
import { test, expect } from '@playwright/test';

test.describe('API Security - Pricing Validation', () => {
  
  test('TC-API-003: Reject manipulated price in confirm endpoint', async ({ request }) => {
    // Primero, crear un soft reserve válido
    const softReserve = await request.post('/api/booking/soft-reserve', {
      data: {
        date: new Date(Date.now() + 86400000), // Mañana
        startTime: '10:00',
        estimatedMinutes: 120,
        zipCode: '10001',
        employeeId: 1
      }
    });
    
    expect(softReserve.ok()).toBeTruthy();
    const { softReserveId, sessionId } = await softReserve.json();
    
    // Obtener precio real
    const estimate = await request.post('/api/booking/estimate', {
      data: {
        serviceTypeId: 1,
        rooms: [{ roomTypeId: 1, quantity: 2 }],
        additionalServiceIds: []
      }
    });
    
    const { total: realPrice } = await estimate.json();
    
    // Intentar confirmar con precio MANIPULADO
    const fraudAttempt = await request.post('/api/booking/confirm', {
      data: {
        softReserveId,
        sessionId,
        total: 1.00, // FRAUDE: precio real es > $100
        serviceTypeId: 1,
        rooms: [{ roomTypeId: 1, quantity: 2 }],
        contactName: 'Hacker',
        contactEmail: 'hack@evil.com',
        contactPhone: '555-0000'
      }
    });
    
    // Validaciones
    expect(fraudAttempt.status()).toBe(400);
    const body = await fraudAttempt.json();
    expect(body.error).toMatch(/pricing mismatch|price.*invalid/i);
  });

  test('TC-API-004: Backend recalculates price independently', async ({ request }) => {
    const estimate = await request.post('/api/booking/estimate', {
      data: {
        serviceTypeId: 1,
        rooms: [
          { roomTypeId: 1, quantity: 2 },
          { roomTypeId: 2, quantity: 1 }
        ],
        additionalServiceIds: [5, 7],
        squareFootage: 2000,
        dirtLevel: 2, // Normal
        hasPets: true
      }
    });
    
    const { total, subtotal, estimatedMinutes } = await estimate.json();
    
    // Validar que los valores tienen sentido
    expect(total).toBeGreaterThan(0);
    expect(subtotal).toBeLessThanOrEqual(total);
    expect(estimatedMinutes).toBeGreaterThan(60);
    
    // El total debe ser consistente si llamamos de nuevo
    const estimate2 = await request.post('/api/booking/estimate', {
      data: {
        serviceTypeId: 1,
        rooms: [
          { roomTypeId: 1, quantity: 2 },
          { roomTypeId: 2, quantity: 1 }
        ],
        additionalServiceIds: [5, 7],
        squareFootage: 2000,
        dirtLevel: 2,
        hasPets: true
      }
    });
    
    const { total: total2 } = await estimate2.json();
    expect(total).toBe(total2); // Debe ser EXACTAMENTE igual
  });
});
```

### 4.6 Helpers y Utilidades

#### db-helpers.ts
```typescript
import mysql from 'mysql2/promise';

export class DbHelpers {
  private connection: mysql.Connection | null = null;

  async connect() {
    this.connection = await mysql.createConnection({
      host: process.env.DB_HOST || 'localhost',
      user: process.env.DB_USER || 'root',
      password: process.env.DB_PASSWORD || 'Root@123456',
      database: process.env.DB_NAME || 'SavedByTheMaidNew'
    });
  }

  async cleanupTestData(email: string) {
    if (!this.connection) await this.connect();
    
    // Eliminar órdenes de prueba
    await this.connection!.execute(
      'DELETE FROM ServiceOrders WHERE ContactEmail = ?',
      [email]
    );
  }

  async getOrderByEmail(email: string) {
    if (!this.connection) await this.connect();
    
    const [rows] = await this.connection!.execute(
      'SELECT * FROM ServiceOrders WHERE ContactEmail = ? ORDER BY CreatedAt DESC LIMIT 1',
      [email]
    );
    
    return (rows as any[])[0];
  }

  async close() {
    if (this.connection) {
      await this.connection.end();
      this.connection = null;
    }
  }
}
```

---

## ⚠️ 5. RIESGOS CRÍTICOS Y MITIGACIÓN

### 5.1 Matriz de Riesgos

| ID | Riesgo | Impacto | Probabilidad | Severidad | Estrategia de Mitigación |
|----|--------|---------|--------------|-----------|--------------------------|
| **R-001** | **Double-Booking** (2 clientes en el mismo slot) | Muy Alto | Media | 🔴 CRÍTICA | ✅ Implementado GET_LOCK en MySQL<br>✅ Test TC-EDGE-007 obligatorio<br>✅ Monitoreo en producción |
| **R-002** | **Fraude de Precio** (cliente manipula Total) | Alto | Media | 🔴 CRÍTICA | ✅ Backend recalcula precio<br>✅ Test TC-API-003 obligatorio<br>✅ Logs de seguridad |
| **R-003** | **Soft Reserves no limpiados** (agenda bloqueada falsamente) | Alto | Alta | 🟡 ALTA | ✅ Background service cada 5 min<br>✅ Test TC-EDGE-008<br>✅ Alertas si servicio falla |
| **R-004** | **Servicios recurrentes con fechas inválidas** | Alto | Media | 🟡 ALTA | ⚠️ PENDIENTE: Validar CADA fecha generada<br>⚠️ Limitar a 52 ocurrencias |
| **R-005** | **Regresión por Refactoring** | Medio | Alta | 🟡 ALTA | ✅ Suite E2E en CI/CD<br>⚠️ Coverage > 80% pendiente |
| **R-006** | **Performance Degradation** | Medio | Media | 🟢 MEDIA | ✅ Índices en BD<br>✅ Test TC-UX-010<br>⚠️ Caching pendiente |

### 5.2 Plan de Mitigación Detallado

#### R-001: Double-Booking
**Descripción:** Dos clientes reservan el mismo slot de empleada simultáneamente, causando conflicto en la agenda real.

**Impacto en el Negocio:**
- Pérdida de confianza del cliente
- Cancelación forzada de una reserva
- Daño reputacional

**Controles Implementados:**
1. MySQL `GET_LOCK` en `CreateSoftReserve`
2. Doble verificación dentro del lock
3. Timeout de lock: 10 segundos

**Controles Faltantes:**
- Implementar retry logic en Frontend si falla el lock
- Alertar a admin si > 10 conflictos/día

**Testing:**
- TC-EDGE-007 debe ejecutarse en CADA release
- Load testing con 100 usuarios concurrentes (Fase 2)

---

#### R-002: Fraude de Precio
**Descripción:** Usuario malicioso intercepta request y modifica el campo `Total` para pagar menos.

**Impacto en el Negocio:**
- Pérdida financiera directa
- Explotación sistemática por bots

**Controles Implementados:**
1. Backend recalcula precio desde cero
2. Comparación con threshold de 0.01
3. Rechazo con HTTP 400

**Controles Faltantes:**
- Rate limiting por IP (parcialmente implementado)
- Banear IPs con intentos de fraude repetidos
- Alerta a equipo de seguridad si > 5 intentos/hora

**Testing:**
- TC-API-003 debe estar en CI/CD
- Penetration testing manual trimestral

---

#### R-004: Servicios Recurrentes con Fechas Inválidas
**Descripción:** Sistema genera 12 citas recurrentes pero no valida que la empleada tenga vacaciones en la semana 5.

**Impacto en el Negocio:**
- Citas imposibles de cumplir
- Cancelaciones masivas
- Logística caótica

**Controles Faltantes:**
1. Validar CADA fecha generada contra:
   - TimeOff de empleada
   - Horario laboral
   - Conflictos existentes
2. Limitar a máximo 52 ocurrencias
3. Preview de fechas antes de confirmar

**Testing:**
- Crear test TC-LOGIC-011: "Recurring service respects TimeOff"
- Prioridad: ALTA (debe implementarse antes de producción)

---

## 📏 6. CRITERIOS DE ENTRADA Y SALIDA

### 6.1 Entry Criteria (Inicio de Testing)

Para que QA pueda iniciar pruebas formales:

✅ **Ambiente:**
- [ ] Ambiente de Staging desplegado y accesible
- [ ] Base de datos con datos semilla (ServiceTypes, CleaningPlaces, Employees, ServiceAreas)
- [ ] Variables de entorno configuradas correctamente

✅ **Código:**
- [ ] Build exitoso sin errores de compilación
- [ ] Migraciones de BD aplicadas automáticamente
- [ ] Smoke test automatizado pasa (Login, Home, Health Check)

✅ **Documentación:**
- [ ] Criterios de aceptación claros para cada feature
- [ ] API documentation actualizada (Swagger)
- [ ] Cambios de la versión documentados (CHANGELOG)

### 6.2 Exit Criteria (Release a Producción)

Para aprobar el release:

✅ **Calidad:**
- [ ] **Cero defectos** de severidad CRÍTICA o ALTA sin resolver
- [ ] Defectos de severidad MEDIA: < 5 abiertos (documentados como "known issues")
- [ ] 95% de casos de prueba E2E ejecutados y aprobados
- [ ] 100% de casos de prueba de seguridad (API) aprobados

✅ **Cobertura:**
- [ ] Todos los flujos críticos (P1) cubiertos por tests automatizados
- [ ] Test de regresión completo ejecutado
- [ ] Test en al menos 2 navegadores (Chrome, Firefox)

✅ **Performance:**
- [ ] `/api/booking/estimate` responde en < 500ms (p95)
- [ ] `/api/booking/confirm` responde en < 1s (p95)
- [ ] Frontend carga inicial < 3s (3G Fast)

✅ **Seguridad:**
- [ ] Validación de precios funcionando (TC-API-003 pasa)
- [ ] Protección contra double-booking (TC-EDGE-007 pasa)
- [ ] Secrets en variables de entorno (no hardcodeados)

✅ **Operacional:**
- [ ] Logs estructurados funcionando (Serilog)
- [ ] Health checks configurados (`/health`)
- [ ] Rollback plan documentado
- [ ] Post-deployment smoke tests definidos

---

## 📊 7. MÉTRICAS DE CALIDAD (KPIs)

### 7.1 Métricas de Testing

| Métrica | Fórmula | Objetivo | Actual | Estado |
|---------|---------|----------|--------|--------|
| **Test Coverage** | (Casos ejecutados / Casos totales) × 100 | ≥ 95% | TBD | 🟡 |
| **Pass Rate** | (Tests pasados / Tests ejecutados) × 100 | ≥ 98% | TBD | 🟡 |
| **Defect Removal Efficiency** | Bugs QA / (Bugs QA + Bugs Prod) × 100 | ≥ 95% | TBD | 🟡 |
| **Automation Coverage** | (Tests automatizados / Tests totales) × 100 | ≥ 80% | 50% | 🔴 |

### 7.2 Métricas de Defectos

| Métrica | Descripción | Objetivo | Tracking |
|---------|-------------|----------|----------|
| **Defect Density** | Defectos por 1000 líneas de código | < 2 | Por release |
| **Defect Leakage** | Bugs encontrados en Prod vs QA | < 5% | Mensual |
| **Mean Time to Detect (MTTD)** | Tiempo promedio para encontrar un bug | < 24h | Por sprint |
| **Mean Time to Resolve (MTTR)** | Tiempo promedio para resolver un bug | < 48h (P1) | Por sprint |

### 7.3 Métricas de Experiencia de Usuario

| Métrica | Herramienta | Objetivo | Frecuencia |
|---------|-------------|----------|------------|
| **Booking Success Rate** | Analytics + Logs | > 85% | Diario |
| **Wizard Abandonment Rate** | Analytics | < 30% | Semanal |
| **Average Booking Time** | Analytics | < 2 min | Semanal |
| **API Error Rate** | Application Insights | < 1% | Real-time |

### 7.4 Métricas de Performance

```typescript
// Ejemplo de test de performance con Playwright
test('Performance: Estimate endpoint under load', async ({ request }) => {
  const iterations = 50;
  const times: number[] = [];
  
  for (let i = 0; i < iterations; i++) {
    const start = Date.now();
    await request.post('/api/booking/estimate', {
      data: { /* payload */ }
    });
    times.push(Date.now() - start);
  }
  
  times.sort((a, b) => a - b);
  const p50 = times[Math.floor(iterations * 0.5)];
  const p95 = times[Math.floor(iterations * 0.95)];
  const p99 = times[Math.floor(iterations * 0.99)];
  
  console.log(`P50: ${p50}ms, P95: ${p95}ms, P99: ${p99}ms`);
  
  expect(p95).toBeLessThan(500);
  expect(p99).toBeLessThan(1000);
});
```

### 7.5 Dashboard de Calidad

**Herramientas Recomendadas:**
- **Test Results:** Playwright HTML Reporter + Allure
- **Coverage:** Istanbul/NYC
- **Bugs:** GitHub Issues con labels (bug, severity:high, etc.)
- **Monitoring:** Application Insights (ya configurado)

**Dashboard Semanal (Manual hasta Fase 2):**
```
┌─────────────────────────────────────┐
│  SavedByTheMaid - QA Status         │
├─────────────────────────────────────┤
│  Sprint: 1 | Week: 3                │
│                                     │
│  Tests Executed:    45 / 50  (90%) │
│  Tests Passed:      42 / 45  (93%) │
│  Tests Failed:       3 / 45   (7%) │
│                                     │
│  Bugs Critical:      0              │
│  Bugs High:          2 (in progress)│
│  Bugs Medium:        5              │
│                                     │
│  Automation:        35 / 50  (70%) │
│  Coverage:          85%             │
│                                     │
│  Status: 🟡 ON TRACK                │
└─────────────────────────────────────┘
```

---

## 🎯 8. PLAN DE EJECUCIÓN (TEST EXECUTION PLAN)

### 8.1 Ciclo de Testing por Sprint (2 semanas)

| Día | Actividad | Responsable | Entregable |
|-----|-----------|-------------|------------|
| **Lunes** | Planning + Smoke Test | QA Lead | Test Plan actualizado |
| **Martes-Miércoles** | Escritura de casos nuevos | QA Engineer | Test Cases en GitHub Issues |
| **Jueves** | Ejecución de Regression Suite | QA Automation | Report HTML |
| **Viernes** | Exploratory Testing + UX | QA Lead | Bug reports |
| **Lunes (S2)** | API Testing | QA Automation | Postman Collection |
| **Martes-Miércoles** | E2E Testing | QA Automation | Playwright Report |
| **Jueves** | Bug Bash (todo el equipo) | All | Triaged bugs |
| **Viernes** | Sign-off Meeting | QA Lead + PM | Go/No-Go decision |

### 8.2 CI/CD Integration

```yaml
# .github/workflows/qa-pipeline.yml
name: QA Pipeline

on:
  pull_request:
  push:
    branches: [main, staging]

jobs:
  smoke-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
      - name: Install dependencies
        run: npm ci
      - name: Run smoke tests
        run: npx playwright test --grep @smoke

  api-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Start backend
        run: docker-compose up -d
      - name: Run API tests
        run: npx playwright test tests/api/

  e2e-tests:
    runs-on: ubuntu-latest
    strategy:
      matrix:
        browser: [chromium, firefox]
    steps:
      - uses: actions/checkout@v3
      - name: Run E2E tests
        run: npx playwright test --project=${{ matrix.browser }}
      - uses: actions/upload-artifact@v3
        if: failure()
        with:
          name: test-results-${{ matrix.browser }}
          path: test-results/
```

---

## 📚 9. DOCUMENTACIÓN Y ENTREGABLES

### 9.1 Documentos de QA

1. **Test Strategy** (este documento)
2. **Test Plan** (por sprint)
3. **Test Cases Repository** (GitHub Projects)
4. **Bug Reports** (GitHub Issues con template)
5. **Test Execution Reports** (Playwright HTML + PDF export)
6. **Defect Metrics** (Excel/Google Sheets)
7. **Sign-off Document** (Google Docs)

### 9.2 Templates

#### Bug Report Template
```markdown
## Bug Report

**ID:** BUG-XXX
**Severity:** Critical / High / Medium / Low
**Priority:** P1 / P2 / P3
**Status:** Open / In Progress / Fixed / Closed

### Description
[Clear description of the bug]

### Steps to Reproduce
1. 
2. 
3. 

### Expected Result
[What should happen]

### Actual Result
[What actually happens]

### Environment
- Browser: Chrome 120
- OS: Windows 11
- URL: https://staging.savedbythemaid.com/booking

### Attachments
- Screenshot
- Video
- Logs

### Notes
[Additional context]
```

---

## ✅ 10. RECOMENDACIONES FINALES

### Prioridad CRÍTICA (Antes de Producción)
1. ✅ **Implementar tests automatizados P1** (TC-E2E-001, TC-API-003, TC-EDGE-007)
2. ⚠️ **Validación de servicios recurrentes** (R-004)
3. ⚠️ **Aumentar coverage de Unit Tests** (Backend > 80%)
4. ⚠️ **Configurar Application Insights** para monitoreo

### Prioridad ALTA (Sprint 2)
5. Implementar test de carga (100 usuarios concurrentes)
6. Penetration testing OWASP Top 10
7. Accessibility testing (WCAG 2.1)
8. Mobile responsive testing (real devices)

### Mejora Continua
- Agregar tests nuevos por cada bug encontrado en producción
- Revisar métricas semanalmente en retrospectiva
- Mantener suite de regresión actualizada

---

**Firmado:**  
GitHub Copilot - Senior QA Lead  
**Fecha:** 7 de Enero 2026  
**Próxima Revisión:** Post-deployment a Staging

### 1. FLUJO DE RESERVA (BOOKING WIZARD) **7/10**

#### ✅ HAPPY PATH - Funcionamiento Correcto

**Paso 1: CheckCoverage**
```csharp
// BIEN: Validación de cobertura por ZIP
var cleaningPlace = await _context.CleaningPlaces
    .FirstOrDefaultAsync(cp => cp.ZipCode == request.ZipCode && cp.IsActive);

// ✅ Verifica IsActive
// ✅ Verifica ZipCode exacto
```

**Paso 2: GetServiceTypes**
```csharp
// BIEN: Solo servicios activos
var serviceTypes = await _context.ServiceTypes
    .Where(st => st.IsActive)
    .ToListAsync();

// ✅ Query filter automático para IsDeleted
// ✅ Solo retorna activos
```

**Paso 3: CalculateEstimate**
```csharp
// BIEN: Cálculo de precio
decimal total = serviceType.Price;

// Descuento por frecuencia
if (request.ApplyFrequencyDiscount && serviceType.FrequencyDiscountPercentage.HasValue)
    total *= (1 - serviceType.FrequencyDiscountPercentage.Value / 100);

// ✅ Lógica correcta
// ✅ Maneja null safety
```

#### 🔴 PROBLEMAS CRÍTICOS DETECTADOS

**PROBLEMA #1: Sin validación de cantidad mínima/máxima de habitaciones**
```csharp
// ACTUAL: No hay límites
foreach (var room in request.Rooms)
{
    var roomType = await _context.CleaningPlaceRooms
        .FirstOrDefaultAsync(r => r.Id == room.RoomTypeId);
    total += roomType.Price * room.Quantity; // ❌ Sin validar Quantity
}

// CASO EDGE: ¿Qué pasa si Quantity = 0? ¿O -5? ¿O 999?
// CASO EDGE: ¿Qué pasa si roomType es null?

// SOLUCIÓN RECOMENDADA:
if (room.Quantity <= 0 || room.Quantity > 50)
    return BadRequest("Quantity must be between 1 and 50");

if (roomType == null)
    return BadRequest($"Invalid room type: {room.RoomTypeId}");
```
**SEVERIDAD:** ALTA  
**IMPACTO:** Cliente puede enviar cantidad negativa o excesiva, causando cálculos incorrectos

---

**PROBLEMA #2: CalculateEstimate no valida que ServiceType existe**
```csharp
// ACTUAL: Asume que existe
var serviceType = await _context.ServiceTypes.FindAsync(request.ServiceTypeId);
decimal total = serviceType.Price; // ❌ NullReferenceException si no existe

// SOLUCIÓN:
if (serviceType == null)
    return NotFound($"Service type {request.ServiceTypeId} not found");
```
**SEVERIDAD:** ALTA  
**IMPACTO:** Crash de aplicación con input inválido

---

**PROBLEMA #3: GetAvailability - Race condition entre verificación y reserva**
```csharp
// ACTUAL: Verificación sin lock
var existingReserves = await _context.SoftReserves
    .Where(sr => sr.EmployeeId == employeeId && 
                 sr.Status == SoftReserveStatus.Active &&
                 sr.StartDateTime < endDateTime && 
                 sr.EndDateTime > startDateTime)
    .AnyAsync();

// CASO EDGE: 2 clientes verifican al mismo tiempo
// T1: Cliente A verifica disponibilidad → TRUE
// T2: Cliente B verifica disponibilidad → TRUE (aún no hay reserve de A)
// T3: Cliente A crea SoftReserve
// T4: Cliente B crea SoftReserve → ❌ DOBLE RESERVA

// NOTA: Esto se resuelve en CreateSoftReserve con GET_LOCK, pero 
// GetAvailability muestra slots que pueden no estar disponibles al crear reserve
```
**SEVERIDAD:** MEDIA  
**IMPACTO:** UX - Cliente ve slot disponible pero falla al reservar

---

**PROBLEMA #4: Sin validación de rango de fechas razonable**
```csharp
// ACTUAL: Cliente puede reservar para dentro de 10 años
if (request.StartDateTime < DateTime.UtcNow)
    return BadRequest("Cannot book in the past");

// FALTA:
if (request.StartDateTime > DateTime.UtcNow.AddYears(1))
    return BadRequest("Cannot book more than 1 year in advance");

// FALTA:
if (request.StartDateTime < DateTime.UtcNow.AddHours(24))
    return BadRequest("Must book at least 24 hours in advance");
```
**SEVERIDAD:** MEDIA  
**IMPACTO:** Datos inconsistentes, problemas de planificación

---

### 2. CONFIRMACIÓN DE RESERVA (ANTI-FRAUDE) **8/10**

#### ✅ BIEN IMPLEMENTADO

**Re-cálculo de pricing:**
```csharp
// ✅ EXCELENTE: Backend recalcula para evitar manipulación
decimal calculatedSubtotal = serviceType.Price;

if (request.ApplyFrequencyDiscount && serviceType.FrequencyDiscountPercentage.HasValue)
    calculatedSubtotal *= (1 - serviceType.FrequencyDiscountPercentage.Value / 100);

decimal calculatedTotal = calculatedSubtotal;
if (request.Rooms != null)
{
    foreach (var room in request.Rooms)
    {
        var roomType = await _context.CleaningPlaceRooms
            .FirstOrDefaultAsync(r => r.Id == room.RoomTypeId);
        if (roomType != null)
            calculatedTotal += roomType.Price * room.Quantity;
    }
}

if (Math.Abs(request.Total - calculatedTotal) > 0.01m)
    return BadRequest(new { error = "Pricing mismatch detected" });
```

#### 🔴 PROBLEMAS DETECTADOS

**PROBLEMA #5: Pricing mismatch no considera AdditionalServices**
```csharp
// ACTUAL: Solo calcula ServiceType + Rooms
// FALTA: AdditionalServices (oven cleaning, fridge, etc.)

// DATOS EN ServiceOrder:
public class ServiceOrder
{
    public List<ServiceOrderAdditional>? AdditionalServices { get; set; }
}

// ❌ AdditionalServices no se recalculan en backend
// Cliente puede manipular el precio de adicionales
```
**SEVERIDAD:** ALTA  
**IMPACTO:** Fraude - Cliente paga menos de lo que debe

---

**PROBLEMA #6: Sin validación de duración mínima/máxima del servicio**
```csharp
// ACTUAL: No valida duración
var duration = request.EndDateTime - request.StartDateTime;

// FALTA:
if (duration < TimeSpan.FromHours(1))
    return BadRequest("Service must be at least 1 hour");

if (duration > TimeSpan.FromHours(8))
    return BadRequest("Service cannot exceed 8 hours");
```
**SEVERIDAD:** MEDIA  
**IMPACTO:** Servicios irrealistas (5 minutos o 24 horas)

---

### 3. SOFT RESERVES (ANTI-COLLISION) **9/10**

#### ✅ EXCELENTE IMPLEMENTACIÓN

```csharp
// ✅ Uso de MySQL GET_LOCK para prevenir race conditions
var lockName = $"soft_reserve_{employeeId}_{startDateTime:yyyyMMddHH}";
var lockAcquired = await _context.Database
    .ExecuteSqlAsync($"SELECT GET_LOCK({lockName}, 10)") == 1;

if (!lockAcquired)
    return StatusCode(503, "Unable to acquire lock");

try
{
    // Doble verificación dentro del lock
    var hasConflict = await _context.SoftReserves...
    
    if (hasConflict)
        return BadRequest("Slot no longer available");
    
    // Crear reserve
}
finally
{
    await _context.Database.ExecuteSqlAsync($"SELECT RELEASE_LOCK({lockName})");
}
```

#### 🟡 OBSERVACIONES

**MEJORA #1: Lock timeout de 10 segundos puede ser largo**
```csharp
// ACTUAL: 10 segundos
SELECT GET_LOCK({lockName}, 10)

// RECOMENDACIÓN: 2-3 segundos es suficiente
SELECT GET_LOCK({lockName}, 3)

// Si falla, es porque hay alta concurrencia → responder rápido al usuario
```

---

### 4. VALIDACIÓN DE DISPONIBILIDAD (EMPLOYEE SCHEDULE) **6/10**

#### ✅ VALIDACIÓN DE HORARIOS

```csharp
// ✅ Verifica EmployeeSchedule
var schedule = await _context.EmployeeSchedules
    .FirstOrDefaultAsync(s => 
        s.EmployeeId == employeeId && 
        s.DayOfWeek == dayOfWeek && 
        s.IsAvailable);

if (schedule == null || startTime < schedule.StartTime || startTime >= schedule.EndTime)
    return BadRequest("Employee not available at this time");
```

#### 🔴 PROBLEMAS CRÍTICOS

**PROBLEMA #7: No valida EndTime del servicio**
```csharp
// ACTUAL: Solo valida StartTime
if (startTime < schedule.StartTime || startTime >= schedule.EndTime)
    return BadRequest(...);

// FALTA: Validar que el servicio completo cabe en el horario
var serviceEndTime = startTime.Add(request.Duration);
if (serviceEndTime > schedule.EndTime)
    return BadRequest("Service extends beyond employee work hours");

// CASO EDGE: 
// - Schedule: 9 AM - 5 PM
// - Servicio: 4 PM - 7 PM (3 horas)
// - StartTime (4 PM) está dentro del horario ✅
// - EndTime (7 PM) está FUERA del horario ❌
```
**SEVERIDAD:** ALTA  
**IMPACTO:** Servicios agendados fuera de horario laboral

---

**PROBLEMA #8: No considera tiempo de traslado entre servicios**
```csharp
// ACTUAL: Servicios back-to-back sin gap
// Service A: 10:00 AM - 12:00 PM
// Service B: 12:00 PM - 2:00 PM

// REALIDAD: Empleada necesita tiempo para trasladarse
// SOLUCIÓN: Agregar buffer de 30 min entre servicios

public const int TravelBufferMinutes = 30;

var nextService = await _context.ServiceMeets
    .Where(sm => sm.AssignedEmployeeId == employeeId &&
                 sm.ScheduledDateTime >= request.EndDateTime &&
                 sm.ScheduledDateTime < request.EndDateTime.AddMinutes(TravelBufferMinutes))
    .AnyAsync();

if (nextService)
    return BadRequest("Not enough time for travel to next appointment");
```
**SEVERIDAD:** ALTA  
**IMPACTO:** Logística imposible, empleadas llegan tarde

---

### 5. TIME OFF (VACACIONES/AUSENCIAS) **8/10**

#### ✅ VALIDACIÓN CORRECTA

```csharp
// ✅ Verifica TimeOff aprobados
var hasTimeOff = await _context.EmployeeTimeOffs.AnyAsync(t => 
    t.EmployeeId == employeeId && 
    t.Status == EmployeeTimeOffStatus.Approved && 
    t.StartDateTime <= endDateTime && 
    t.EndDateTime >= startDateTime);

if (hasTimeOff)
    return BadRequest("Employee has approved time off during this period");
```

#### 🟡 OBSERVACIONES

**MEJORA #2: No considera TimeOff pendientes**
```csharp
// ACTUAL: Solo Approved
t.Status == EmployeeTimeOffStatus.Approved

// RECOMENDACIÓN: También considerar Pending para evitar conflictos
t.Status == EmployeeTimeOffStatus.Approved || 
t.Status == EmployeeTimeOffStatus.Pending

// RAZÓN: Si empleada solicitó vacaciones (Pending), no agendar servicios
// que tendrían que cancelarse si se aprueban
```

---

### 6. RECURRING SERVICES (SERVICIOS RECURRENTES) **5/10**

#### 🔴 PROBLEMAS CRÍTICOS

**PROBLEMA #9: CreateRecurringMeeting no valida disponibilidad de TODAS las ocurrencias**
```csharp
// ACTUAL: Crea todas las ocurrencias sin validar cada fecha
for (var date = startDate; date <= endDate; date = date.AddDays(7))
{
    var meet = new ServiceMeet
    {
        // ...
        ScheduledDateTime = date
    };
    order.ServiceMeets.Add(meet);
}

// FALTA: Validar CADA fecha
// - TimeOff de la empleada
// - Conflictos con otros servicios
// - Horario laboral

// CASO EDGE:
// - Servicio recurrente: Todos los lunes 10 AM, 3 meses
// - Empleada tiene vacaciones en lunes de semana 5
// - Sistema crea 12 meets, incluyendo el de vacaciones ❌
```
**SEVERIDAD:** CRÍTICA  
**IMPACTO:** Servicios agendados cuando empleada no está disponible

---

**PROBLEMA #10: Sin límite de ocurrencias para recurrentes**
```csharp
// ACTUAL: Cliente puede crear servicio infinito
// StartDate: 2026-01-01
// EndDate: 2099-12-31 
// RecurrenceType: Weekly
// Resultado: Miles de ServiceMeets en BD

// SOLUCIÓN:
var totalOccurrences = CalculateOccurrences(request.StartDate, request.EndDate, request.RecurrenceType);
if (totalOccurrences > 52) // Máximo 1 año
    return BadRequest("Recurring service cannot exceed 52 occurrences");
```
**SEVERIDAD:** ALTA  
**IMPACTO:** Saturación de BD, performance degradada

---

### 7. TRANSICIONES DE ESTADO (ORDER STATUS) **9/10**

#### ✅ EXCELENTE VALIDACIÓN

```csharp
// ✅ Matriz de transiciones válidas
var validTransitions = new Dictionary<OrderStatus, OrderStatus[]>
{
    [OrderStatus.Draft] = new[] { OrderStatus.Confirmed, OrderStatus.Cancelled },
    [OrderStatus.Confirmed] = new[] { OrderStatus.InProgress, OrderStatus.Cancelled, OrderStatus.NoShow },
    [OrderStatus.InProgress] = new[] { OrderStatus.Completed, OrderStatus.Cancelled },
    [OrderStatus.Completed] = new OrderStatus[0],
    [OrderStatus.Cancelled] = new OrderStatus[0],
    [OrderStatus.NoShow] = new OrderStatus[0]
};

if (!validTransitions[order.OrderStatus].Contains(newStatus))
    return BadRequest("Invalid status transition");
```

#### 🟡 MEJORA RECOMENDADA

**MEJORA #3: Auditar transiciones de estado**
```csharp
// ACTUAL: Solo loggea
_logger.LogInformation("Status changed...");

// RECOMENDACIÓN: Tabla de auditoría
public class OrderStatusHistory
{
    public int Id { get; set; }
    public int ServiceOrderId { get; set; }
    public OrderStatus FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }
    public string ChangedBy { get; set; } // UserId
    public string? Reason { get; set; }
    public DateTime ChangedAt { get; set; }
}

// Permite rastrear quién canceló qué y cuándo
```

---

## 📊 ANÁLISIS DE INTEGRIDAD DE DATOS

### 1. INTEGRIDAD REFERENCIAL **7/10**

#### ✅ BIEN CONFIGURADO

```csharp
// Foreign keys definidas correctamente
builder.Entity<ServiceOrder>()
    .HasOne(so => so.Customer)
    .WithMany()
    .HasForeignKey(so => so.CustomerId)
    .OnDelete(DeleteBehavior.Restrict); // ✅ No permite borrar Customer con Orders

builder.Entity<ServiceMeet>()
    .HasOne(sm => sm.ServiceOrder)
    .WithMany(so => so.ServiceMeets)
    .HasForeignKey(sm => sm.ServiceOrderId)
    .OnDelete(DeleteBehavior.Cascade); // ✅ Borrar Order → Borra Meets
```

#### 🔴 PROBLEMAS DETECTADOS

**PROBLEMA #11: AssignedEmployeeId nullable sin validación**
```csharp
// ServiceMeet
public int? AssignedEmployeeId { get; set; }

// ❌ Servicio puede quedar sin empleada asignada indefinidamente
// ❌ No hay validación de que empleada esté asignada antes de InProgress

// SOLUCIÓN: Validar antes de cambiar a InProgress
if (newStatus == OrderStatus.InProgress)
{
    var hasAssignedEmployee = order.ServiceMeets.All(sm => sm.AssignedEmployeeId.HasValue);
    if (!hasAssignedEmployee)
        return BadRequest("Cannot start service without assigned employee");
}
```
**SEVERIDAD:** MEDIA  
**IMPACTO:** Servicios "en progreso" sin empleada asignada

---

### 2. SOFT DELETES **8/10**

#### ✅ IMPLEMENTADO CORRECTAMENTE

```csharp
// ✅ Query filters globales
builder.Entity<ServiceType>().HasQueryFilter(e => !e.IsDeleted);
builder.Entity<ServiceOrder>().HasQueryFilter(e => !e.IsDeleted);

// ✅ Previene N+1 queries de verificación IsDeleted
```

#### 🟡 OBSERVACIÓN

**MEJORA #4: Sin endpoint para restaurar soft deletes**
```csharp
// ACTUAL: Solo marcar IsDeleted = true
// FALTA: Endpoint admin para restaurar

[HttpPut("admin/service-types/{id}/restore")]
public async Task<IActionResult> RestoreServiceType(int id)
{
    var serviceType = await _context.ServiceTypes
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(st => st.Id == id);
    
    if (serviceType == null)
        return NotFound();
    
    serviceType.IsDeleted = false;
    await _context.SaveChangesAsync();
    return Ok();
}
```

---

### 3. CAMPOS CALCULADOS Y AGREGADOS **6/10**

#### 🔴 PROBLEMA CRÍTICO

**PROBLEMA #12: Total almacenado puede desincronizarse**
```csharp
// ServiceOrder guarda Total calculado
public decimal Total { get; set; }

// PROBLEMA: Si se cambian precios de ServiceType o Rooms después,
// Total guardado no refleja precios actuales

// CASO:
// 1. Cliente reserva: ServiceType $100 → Total = $100
// 2. Admin cambia precio: ServiceType $120
// 3. Order.Total sigue siendo $100 ❌

// DEBATE: ¿Es un bug o feature?
// - FEATURE: Precio locked al momento de reserva (como factura)
// - BUG: Si Order está en Draft, debería actualizarse

// RECOMENDACIÓN: Documentar claramente el comportamiento
// O agregar campo:
public decimal LockedTotal { get; set; } // Precio al confirmar
public decimal CurrentTotal { get; set; } // Precio con rates actuales
```
**SEVERIDAD:** MEDIA (depende de requisitos de negocio)

---

## 🧪 CASOS EDGE Y LÍMITES

### Casos Edge Detectados:

| # | Caso | Validado | Severidad |
|---|------|----------|-----------|
| 1 | Quantity de habitaciones negativa | ❌ | ALTA |
| 2 | Quantity de habitaciones = 0 | ❌ | ALTA |
| 3 | Quantity de habitaciones > 999 | ❌ | ALTA |
| 4 | ServiceType no existe en CalculateEstimate | ❌ | ALTA |
| 5 | Reserva para dentro de 10 años | ❌ | MEDIA |
| 6 | Reserva con menos de 1 hora de anticipación | ❌ | MEDIA |
| 7 | Servicio que termina fuera de horario laboral | ❌ | ALTA |
| 8 | Servicios back-to-back sin tiempo de traslado | ❌ | ALTA |
| 9 | Servicio recurrente con 1000+ ocurrencias | ❌ | ALTA |
| 10 | Servicio recurrente con fecha en TimeOff | ❌ | CRÍTICA |
| 11 | Order en InProgress sin empleada asignada | ❌ | MEDIA |
| 12 | AdditionalServices no recalculados en ConfirmBooking | ❌ | ALTA |
| 13 | Duración de servicio < 1 hora | ❌ | MEDIA |
| 14 | Duración de servicio > 8 horas | ❌ | MEDIA |
| 15 | RoomType null en cálculo de precio | ⚠️ | ALTA |

---

## 📋 RECOMENDACIONES PRIORIZADAS

### 🔴 CRÍTICAS (Implementar AHORA)

1. **Validar todas las ocurrencias de servicios recurrentes**
   - Verificar TimeOff, horarios, conflictos para CADA fecha
   - Límite máximo de 52 ocurrencias

2. **Recalcular AdditionalServices en ConfirmBooking**
   - Actualmente solo valida ServiceType + Rooms
   - Falta validar adicionales (fraude potencial)

3. **Validar EndTime de servicios**
   - Verificar que servicio completo cabe en horario laboral
   - No solo StartTime

4. **Validar Quantity de habitaciones**
   - Mínimo: 1
   - Máximo: 50 (o valor de negocio)
   - No permitir negativos

---

### 🟡 IMPORTANTES (Sprint 2)

5. **Tiempo de traslado entre servicios**
   - Buffer de 30 min entre appointments
   - Prevenir logística imposible

6. **Validaciones de rango de fechas**
   - Máximo 1 año en el futuro
   - Mínimo 24 horas de anticipación

7. **Null safety en CalculateEstimate**
   - Verificar ServiceType existe
   - Verificar RoomType existe

8. **Duración mínima/máxima de servicios**
   - Mín: 1 hora
   - Máx: 8 horas

9. **Validar empleada asignada antes de InProgress**
   - No permitir iniciar servicio sin Assignment

---

### 🟢 MEJORAS (Backlog)

10. Considerar TimeOff Pending en validaciones
11. Tabla de auditoría para cambios de estado
12. Endpoint para restaurar soft deletes
13. Documentar comportamiento de Total (locked vs current)
14. Reducir lock timeout de 10s a 3s

---

## ✅ CHECKLIST DE VALIDACIONES FALTANTES

### Booking Flow:
- [ ] Quantity habitaciones: min=1, max=50
- [ ] ServiceType existe (CalculateEstimate)
- [ ] RoomType existe (cálculo de precio)
- [ ] Duración servicio: 1h - 8h
- [ ] Fecha mínima: +24h
- [ ] Fecha máxima: +1 año
- [ ] EndTime dentro de horario laboral
- [ ] Buffer de traslado (30 min)
- [ ] AdditionalServices en re-cálculo

### Recurring Services:
- [ ] Máximo 52 ocurrencias
- [ ] Validar CADA fecha individualmente
- [ ] TimeOff check por ocurrencia
- [ ] Conflictos por ocurrencia

### State Management:
- [ ] Empleada asignada antes de InProgress
- [ ] Auditoría de cambios de estado

---

## 📊 MÉTRICAS FINALES

| Categoría | Score | Comentario |
|-----------|-------|------------|
| Flujos principales | 7/10 | Happy path funciona, casos edge débiles |
| Validaciones de entrada | 6/10 | Falta validar límites y nulls |
| Integridad referencial | 7/10 | FK correctos, falta validar asignaciones |
| Manejo de concurrencia | 9/10 | GET_LOCK implementado correctamente |
| Servicios recurrentes | 5/10 | Lógica básica, falta validar ocurrencias |
| Transiciones de estado | 9/10 | Matriz bien implementada |
| **TOTAL** | **7.2/10** | **Funcional pero requiere hardening** |

---

## 🎯 VEREDICTO FINAL

**ESTADO:** ✅ **APROBADO PARA BETA / SOFT LAUNCH**

### Para Producción Completa:
1. Implementar 4 fixes CRÍTICOS
2. Agregar tests de casos edge
3. Validar servicios recurrentes exhaustivamente

### Riesgos Actuales:
- **ALTO:** Servicios recurrentes pueden crear appointments inválidos
- **ALTO:** Fraude en AdditionalServices (no recalculados)
- **MEDIO:** Servicios agendados fuera de horario
- **MEDIO:** Logística imposible (sin tiempo de traslado)

---

**Firmado:** GitHub Copilot QA  
**Próxima Revisión:** Post-fixes críticos

