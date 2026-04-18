# 🧪 SavedByTheMaid - Test Suite

Playwright tests para validación E2E, API y seguridad.

## 📁 Estructura

```
tests/
├── e2e/                    # Tests End-to-End
│   └── booking-wizard.spec.ts
├── api/                    # Tests de API
│   └── pricing-security.spec.ts
├── pages/                  # Page Object Model
│   ├── BasePage.ts
│   └── BookingPage.ts
├── fixtures/               # Datos de prueba
│   └── test-data.json
├── utils/                  # Utilidades
│   └── db-helpers.ts
└── playwright.config.ts    # Configuración
```

## 🚀 Instalación

```bash
cd tests
npm install
npx playwright install --with-deps
```

## ▶️ Ejecución

```bash
# Todos los tests
npm test

# Solo E2E
npm run test:e2e

# Solo API
npm run test:api

# Tests críticos
npm run test:critical

# Con interfaz UI
npm run test:ui

# Con navegador visible
npm run test:headed

# Modo debug
npm run test:debug
```

## 📊 Reportes

```bash
# Ver último reporte
npm run test:report

# Resultados en:
# - HTML: test-results/html/index.html
# - JSON: test-results/results.json
# - JUnit: test-results/junit.xml
```

## 🔧 Variables de Entorno

Crear archivo `.env`:

```env
BASE_URL=http://localhost:5221
DB_HOST=localhost
DB_PORT=3306
DB_USER=root
DB_PASSWORD=Root@123456
DB_NAME=SavedByTheMaidNew
```

## 📝 Casos de Prueba

### E2E Critical
- **TC-E2E-001**: Reserva completa (Happy Path)
- **TC-E2E-002**: Cancelación de SoftReserve
- **TC-EDGE-007**: Race condition / double-booking

### API Security
- **TC-API-003**: Fraude de precio
- **TC-API-004**: Recálculo de precio
- **TC-API-005**: Validación de entrada

### Performance
- **TC-UX-010**: Response time < 500ms

## 🎯 Coverage Objetivo

- E2E: 100% de flujos críticos
- API: 100% de endpoints públicos
- Integration: 80% de servicios background

## 🐛 Debugging

```bash
# Generar código desde UI (backend debe estar en :5221)
npm run test:codegen

# Inspector de Playwright
npx playwright test --debug

# Trace viewer
npx playwright show-trace test-results/trace.zip
```

## 📚 Documentación

- [Playwright Docs](https://playwright.dev)
- [QA Strategy](../QA_ANALYSIS_FINAL.md)
- [Test Plan](../TEST_PLAN.md)
