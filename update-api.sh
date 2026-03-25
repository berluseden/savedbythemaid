#!/bin/bash
# ============================================================================
# update-api.sh -- Update only the API container on a GCP VM
#
# Connects to the remote VM via SSH, pulls the latest code, and rebuilds
# only the API container without touching the frontend or database.
#
# Usage:
#   ./update-api.sh [INSTANCE_NAME] [ZONE]
#
# Environment variables (override defaults):
#   INSTANCE_NAME -- VM instance name (default: instancia-gratis-ubuntu)
#   ZONE          -- GCP zone (default: us-central1-a)
#   APP_DIR       -- Application directory on the VM (default: /opt/savedbythemaid)
#   BRANCH        -- Git branch to pull (default: main)
#   API_PORT      -- API port for summary output (default: 5000)
# ============================================================================

set -euo pipefail

echo "Updating API on GCP..."

# ── Configurable variables ────────────────────────────────────────────────
INSTANCE_NAME="${INSTANCE_NAME:-${1:-instancia-gratis-ubuntu}}"
ZONE="${ZONE:-${2:-us-central1-a}}"
APP_DIR="${APP_DIR:-/opt/savedbythemaid}"
BRANCH="${BRANCH:-main}"
API_PORT="${API_PORT:-5000}"

# ── Retrieve external IP ─────────────────────────────────────────────────
get_instance_ip() {
	gcloud compute instances describe "$INSTANCE_NAME" \
		--zone="$ZONE" \
		--format='get(networkInterfaces[0].accessConfigs[0].natIP)'
}

EXTERNAL_IP="$(get_instance_ip || true)"
if [ -z "$EXTERNAL_IP" ]; then
	echo "WARNING: Could not retrieve external IP for the instance."
	echo "   Verify it exists and has an external IP: INSTANCE_NAME=$INSTANCE_NAME ZONE=$ZONE"
else
	echo "VM: $INSTANCE_NAME ($ZONE) IP: $EXTERNAL_IP"
fi

# ── Execute remote commands ──────────────────────────────────────────────
# Pull latest code, rebuild, and restart only the API container.
gcloud compute ssh "$INSTANCE_NAME" --zone="$ZONE" --command="
set -euo pipefail
cd '${APP_DIR}'
git pull origin '${BRANCH}'
docker compose build api
docker compose up -d api
echo 'API updated successfully'
docker compose ps
"

# ── Summary ──────────────────────────────────────────────────────────────
echo "========================================
API update complete!
========================================
API:        http://${EXTERNAL_IP:-<VM_EXTERNAL_IP>}:${API_PORT}
API Health: http://${EXTERNAL_IP:-<VM_EXTERNAL_IP>}:${API_PORT}/health
"
