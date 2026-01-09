#!/bin/bash
set -e

echo "🧪 Ejecutando tests E2E contra GCP..."

# Detectar IP de GCP
EXTERNAL_IP=$(curl -s ifconfig.me)
GCP_URL="http://136.119.103.67:3000"

echo "🌐 Target URL: $GCP_URL"

cd SavedByTheMaid.New/tests

# Instalar dependencias si no existen
if [ ! -d "node_modules" ]; then
    echo "📦 Instalando dependencias de Playwright..."
    npm install
fi

# Instalar navegadores de Playwright si no existen
if [ ! -d "node_modules/@playwright/test" ]; then
    echo "🎭 Instalando navegadores de Playwright..."
    npx playwright install --with-deps chromium
fi

echo "▶️  Ejecutando tests contra $GCP_URL..."

# Ejecutar tests E2E con la URL de GCP (sin webServer local)
SKIP_WEBSERVER=true BASE_URL="$GCP_URL" npx playwright test e2e/gcp-booking-flow.spec.ts --reporter=list,html

echo ""
echo "========================================
✅ Tests completados!
========================================

📊 Ver reporte HTML:
   npx playwright show-report

📸 Screenshots en: test-results/
📝 Logs en: playwright-report/
"
