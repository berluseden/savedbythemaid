#!/bin/bash
# ============================================================================
# update-frontend.sh -- Update only the frontend container on a GCP VM
#
# This script is designed to run ON the VM itself. It stops the frontend
# container, rebuilds it from scratch (no cache), and restarts it.
#
# Usage:
#   sudo ./update-frontend.sh
#
# Environment variables (override defaults):
#   APP_DIR        -- Application directory (default: /opt/savedbythemaid)
#   FRONTEND_PORT  -- Frontend port for summary output (default: 3000)
# ============================================================================

set -euo pipefail

echo "Updating frontend..."

# ── Configurable variables ────────────────────────────────────────────────
APP_DIR="${APP_DIR:-/opt/savedbythemaid}"
FRONTEND_PORT="${FRONTEND_PORT:-3000}"

cd "$APP_DIR"

# ── Stop the running frontend container ───────────────────────────────────
echo "Stopping frontend..."
docker compose stop frontend

# ── Remove the old frontend image to force a clean rebuild ────────────────
echo "Removing old frontend image..."
docker rmi savedbythemaid-frontend || true

# ── Rebuild frontend from scratch (no cache) ──────────────────────────────
echo "Rebuilding frontend..."
docker compose build --no-cache frontend

# ── Start the new frontend container ──────────────────────────────────────
echo "Starting frontend..."
docker compose up -d frontend

# ── Wait for the frontend to become ready ─────────────────────────────────
echo "Waiting for frontend to become healthy..."
sleep 15

# ── Verify container status ──────────────────────────────────────────────
echo "Frontend status:"
docker compose ps frontend

# ── Retrieve external IP for summary ─────────────────────────────────────
EXTERNAL_IP=$(curl -fsS ifconfig.me 2>/dev/null || echo "<unknown>")

echo ""
echo "Frontend update complete!"
echo "Frontend: http://${EXTERNAL_IP}:${FRONTEND_PORT}"
