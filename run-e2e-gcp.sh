#!/bin/bash
set -e

echo "🧪 Ejecutando tests E2E contra GCP..."

# Permite pasar URL explícita por env
GCP_URL="${GCP_URL:-${BASE_URL:-}}"

if [ -z "$GCP_URL" ]; then
    INSTANCE_NAME="${INSTANCE_NAME:-instancia-gratis-ubuntu}"
    ZONE="${ZONE:-us-central1-a}"

    if command -v gcloud >/dev/null 2>&1; then
        EXTERNAL_IP=$(gcloud compute instances describe "$INSTANCE_NAME" --zone="$ZONE" --format='get(networkInterfaces[0].accessConfigs[0].natIP)' 2>/dev/null || true)
        if [ -n "$EXTERNAL_IP" ]; then
            GCP_URL="http://${EXTERNAL_IP}:3000"
        fi
    fi
fi

if [ -z "$GCP_URL" ]; then
    echo "❌ No se pudo determinar GCP_URL automáticamente."
    echo "   Setea GCP_URL o BASE_URL, o configura gcloud/instancia."
    exit 1
fi

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
