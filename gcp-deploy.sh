#!/bin/bash
# ============================================================================
# gcp-deploy.sh -- Deploy SavedByTheMaid to a GCP VM from your local machine
#
# Creates the VM and firewall rules if they don't exist, then uploads and
# runs deploy.sh on the remote instance.
#
# Prerequisites:
#   - gcloud CLI installed and authenticated
#   - A GCP project configured (gcloud config set project <PROJECT_ID>)
#
# Usage:
#   ./gcp-deploy.sh [INSTANCE_NAME] [ZONE] [MACHINE_TYPE]
#
# Environment variables (override defaults):
#   INSTANCE_NAME    -- VM instance name (default: instancia-gratis-ubuntu)
#   ZONE             -- GCP zone (default: us-central1-a)
#   MACHINE_TYPE     -- VM machine type (default: e2-micro)
#   IMAGE_FAMILY     -- OS image family (default: ubuntu-2404-lts-amd64)
#   IMAGE_PROJECT    -- OS image project (default: ubuntu-os-cloud)
#   BOOT_DISK_SIZE   -- Boot disk size (default: 30GB)
#   TAGS             -- Network tags (default: savedbythemaid)
#   RULE_FRONTEND    -- Firewall rule name for frontend port
#   RULE_API         -- Firewall rule name for API port
#   API_PORT         -- API port (default: 5000)
#   FRONTEND_PORT    -- Frontend port (default: 3000)
# ============================================================================

set -euo pipefail

# ── Configurable variables ────────────────────────────────────────────────
INSTANCE_NAME="${INSTANCE_NAME:-${1:-instancia-gratis-ubuntu}}"
ZONE="${ZONE:-${2:-us-central1-a}}"
MACHINE_TYPE="${MACHINE_TYPE:-${3:-e2-micro}}"
IMAGE_FAMILY="${IMAGE_FAMILY:-ubuntu-2404-lts-amd64}"
IMAGE_PROJECT="${IMAGE_PROJECT:-ubuntu-os-cloud}"
TAGS="${TAGS:-savedbythemaid}"
BOOT_DISK_SIZE="${BOOT_DISK_SIZE:-30GB}"
API_PORT="${API_PORT:-5000}"
FRONTEND_PORT="${FRONTEND_PORT:-3000}"

RULE_FRONTEND="${RULE_FRONTEND:-allow-savedbythemaid-frontend}"
RULE_API="${RULE_API:-allow-savedbythemaid-api}"

# ── Helper functions ──────────────────────────────────────────────────────

# Verify gcloud is installed and a project is configured.
require_gcloud() {
  if ! command -v gcloud >/dev/null 2>&1; then
    echo "ERROR: gcloud is not installed on this machine."
    exit 1
  fi

  local project
  project="$(gcloud config get-value project 2>/dev/null || true)"
  if [ -z "$project" ] || [ "$project" = "(unset)" ]; then
    echo "ERROR: No GCP project configured."
    echo "   Run: gcloud config set project <YOUR_PROJECT_ID>"
    exit 1
  fi
}

# Check whether the VM instance already exists.
instance_exists() {
  gcloud compute instances describe "$INSTANCE_NAME" --zone "$ZONE" >/dev/null 2>&1
}

# Create a firewall rule if it doesn't already exist.
ensure_firewall_rule() {
  local rule_name="$1"
  local port="$2"

  if gcloud compute firewall-rules describe "$rule_name" >/dev/null 2>&1; then
    return 0
  fi

  echo "Creating firewall rule $rule_name (tcp:$port) for tag: $TAGS"
  gcloud compute firewall-rules create "$rule_name" \
    --allow "tcp:${port}" \
    --target-tags "$TAGS" \
    --source-ranges "0.0.0.0/0" \
    --description "Allow SavedByTheMaid tcp:${port}"
}

# Create a new VM instance.
create_instance() {
  echo "Creating VM $INSTANCE_NAME in $ZONE ($MACHINE_TYPE)..."
  gcloud compute instances create "$INSTANCE_NAME" \
    --zone "$ZONE" \
    --machine-type "$MACHINE_TYPE" \
    --image-family "$IMAGE_FAMILY" \
    --image-project "$IMAGE_PROJECT" \
    --boot-disk-size "$BOOT_DISK_SIZE" \
    --tags "$TAGS" \
    --metadata "enable-oslogin=FALSE"
}

# Retrieve the external IP of the VM.
get_instance_ip() {
  gcloud compute instances describe "$INSTANCE_NAME" \
    --zone="$ZONE" \
    --format='get(networkInterfaces[0].accessConfigs[0].natIP)'
}

# Upload deploy.sh and execute it on the remote VM.
remote_deploy() {
  echo "Executing remote deployment on VM..."

  # Upload the local deploy.sh (which may include recent fixes)
  gcloud compute scp "$(dirname "$0")/deploy.sh" \
    "$INSTANCE_NAME:/tmp/savedbythemaid-deploy.sh" \
    --zone "$ZONE"

  # Run the deploy script on the VM
  gcloud compute ssh "$INSTANCE_NAME" --zone "$ZONE" --command "
set -e
chmod +x /tmp/savedbythemaid-deploy.sh
sudo mv /tmp/savedbythemaid-deploy.sh /opt/savedbythemaid-deploy.sh
sudo chown \"\$USER\":\"\$USER\" /opt/savedbythemaid-deploy.sh
cd /opt
./savedbythemaid-deploy.sh
"
}

# ── Main ──────────────────────────────────────────────────────────────────

main() {
  require_gcloud

  echo "Config: instance=$INSTANCE_NAME zone=$ZONE machine=$MACHINE_TYPE disk=$BOOT_DISK_SIZE imageFamily=$IMAGE_FAMILY"

  # Check if VM exists; create it if not
  echo "Checking VM: $INSTANCE_NAME ($ZONE)"
  if instance_exists; then
    echo "VM already exists."
  else
    echo "VM does not exist. Creating..."
    create_instance
  fi

  # Ensure firewall rules allow traffic to the frontend and API ports
  ensure_firewall_rule "$RULE_FRONTEND" "$FRONTEND_PORT"
  ensure_firewall_rule "$RULE_API" "$API_PORT"

  # Deploy the application on the remote VM
  remote_deploy

  # Print deployment summary
  local ip
  ip="$(get_instance_ip || true)"

  echo ""
  echo "========================================"
  echo "GCP deployment complete"
  echo "========================================"
  echo "VM:       $INSTANCE_NAME"
  echo "Zone:     $ZONE"
  echo "IP:       ${ip:-<no_external_ip>}"
  echo "Frontend: http://${ip:-<VM_EXTERNAL_IP>}:${FRONTEND_PORT}"
  echo "API:      http://${ip:-<VM_EXTERNAL_IP>}:${API_PORT}"
  echo "Health:   http://${ip:-<VM_EXTERNAL_IP>}:${API_PORT}/health"
}

main "$@"
