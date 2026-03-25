#!/bin/bash
# ============================================================================
# run-e2e-gcp.sh -- Run Playwright E2E tests against a GCP-hosted instance
#
# Determines the target URL automatically from the GCP VM's external IP,
# installs Playwright dependencies if needed, and runs the E2E test suite.
#
# Usage:
#   ./run-e2e-gcp.sh
#
# Environment variables (override defaults):
#   GCP_URL         -- Explicit target URL (skips auto-detection)
#   BASE_URL        -- Alternative to GCP_URL
#   INSTANCE_NAME   -- GCP VM name for auto-detection (default: instancia-gratis-ubuntu)
#   ZONE            -- GCP zone for auto-detection (default: us-central1-a)
#   FRONTEND_PORT   -- Frontend port (default: 3000)
# ============================================================================

set -euo pipefail

echo "Running E2E tests against GCP..."

# ── Configurable variables ────────────────────────────────────────────────
FRONTEND_PORT="${FRONTEND_PORT:-3000}"

# ── Determine target URL ─────────────────────────────────────────────────
# Priority: GCP_URL > BASE_URL > auto-detect from gcloud
GCP_URL="${GCP_URL:-${BASE_URL:-}}"

if [ -z "$GCP_URL" ]; then
    INSTANCE_NAME="${INSTANCE_NAME:-instancia-gratis-ubuntu}"
    ZONE="${ZONE:-us-central1-a}"

    if command -v gcloud >/dev/null 2>&1; then
        EXTERNAL_IP=$(gcloud compute instances describe "$INSTANCE_NAME" \
            --zone="$ZONE" \
            --format='get(networkInterfaces[0].accessConfigs[0].natIP)' 2>/dev/null || true)
        if [ -n "$EXTERNAL_IP" ]; then
            GCP_URL="http://${EXTERNAL_IP}:${FRONTEND_PORT}"
        fi
    fi
fi

if [ -z "$GCP_URL" ]; then
    echo "ERROR: Could not determine the target URL automatically."
    echo "   Set GCP_URL or BASE_URL, or configure gcloud with the correct instance."
    exit 1
fi

echo "Target URL: $GCP_URL"

# ── Navigate to test directory ────────────────────────────────────────────
cd SavedByTheMaid.New/tests

# ── Install npm dependencies if missing ───────────────────────────────────
if [ ! -d "node_modules" ]; then
    echo "Installing Playwright dependencies..."
    npm install
fi

# ── Install Playwright browsers if missing ────────────────────────────────
if [ ! -d "node_modules/@playwright/test" ]; then
    echo "Installing Playwright browsers..."
    npx playwright install --with-deps chromium
fi

# ── Run E2E tests ─────────────────────────────────────────────────────────
echo "Executing tests against $GCP_URL..."
SKIP_WEBSERVER=true BASE_URL="$GCP_URL" npx playwright test e2e/gcp-booking-flow.spec.ts --reporter=list,html

# ── Summary ──────────────────────────────────────────────────────────────
echo ""
echo "========================================
Tests complete!
========================================

View HTML report:
   npx playwright show-report

Screenshots: test-results/
Logs:        playwright-report/
"
