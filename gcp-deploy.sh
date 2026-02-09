#!/bin/bash
set -euo pipefail

# Despliegue en GCP (desde tu máquina local) con creación de VM si no existe.
# Requisitos: gcloud instalado + autenticado + proyecto configurado.

INSTANCE_NAME="${INSTANCE_NAME:-${1:-instancia-gratis-ubuntu}}"
ZONE="${ZONE:-${2:-us-central1-a}}"
MACHINE_TYPE="${MACHINE_TYPE:-${3:-e2-micro}}"
IMAGE_FAMILY="${IMAGE_FAMILY:-ubuntu-2404-lts-amd64}"
IMAGE_PROJECT="${IMAGE_PROJECT:-ubuntu-os-cloud}"
TAGS="${TAGS:-savedbythemaid}"

BOOT_DISK_SIZE="${BOOT_DISK_SIZE:-30GB}"

RULE_FRONTEND="${RULE_FRONTEND:-allow-savedbythemaid-frontend}"
RULE_API="${RULE_API:-allow-savedbythemaid-api}"

require_gcloud() {
  if ! command -v gcloud >/dev/null 2>&1; then
    echo "❌ gcloud no está instalado en esta máquina."
    exit 1
  fi

  local project
  project="$(gcloud config get-value project 2>/dev/null || true)"
  if [ -z "$project" ] || [ "$project" = "(unset)" ]; then
    echo "❌ No hay proyecto configurado en gcloud."
    echo "   Ejecuta: gcloud config set project <TU_PROJECT_ID>"
    exit 1
  fi
}

instance_exists() {
  gcloud compute instances describe "$INSTANCE_NAME" --zone "$ZONE" >/dev/null 2>&1
}

ensure_firewall_rule() {
  local rule_name="$1"
  local port="$2"

  if gcloud compute firewall-rules describe "$rule_name" >/dev/null 2>&1; then
    return 0
  fi

  echo "🧱 Creando regla firewall $rule_name (tcp:$port) para tag: $TAGS"
  gcloud compute firewall-rules create "$rule_name" \
    --allow "tcp:${port}" \
    --target-tags "$TAGS" \
    --source-ranges "0.0.0.0/0" \
    --description "Allow SavedByTheMaid tcp:${port}"
}

create_instance() {
  echo "🆕 Creando VM $INSTANCE_NAME en $ZONE ($MACHINE_TYPE)..."
  gcloud compute instances create "$INSTANCE_NAME" \
    --zone "$ZONE" \
    --machine-type "$MACHINE_TYPE" \
    --image-family "$IMAGE_FAMILY" \
    --image-project "$IMAGE_PROJECT" \
    --boot-disk-size "$BOOT_DISK_SIZE" \
    --tags "$TAGS" \
    --metadata "enable-oslogin=FALSE"
}

get_instance_ip() {
  gcloud compute instances describe "$INSTANCE_NAME" \
    --zone="$ZONE" \
    --format='get(networkInterfaces[0].accessConfigs[0].natIP)'
}

remote_deploy() {
  echo "🚀 Ejecutando deploy remoto en la VM..."

  # Subir el deploy.sh local (esta copia incluye fixes para Ubuntu 24.04)
  gcloud compute scp "$(dirname "$0")/deploy.sh" \
    "$INSTANCE_NAME:/tmp/savedbythemaid-deploy.sh" \
    --zone "$ZONE"

  # Ejecutar dentro de la VM
  gcloud compute ssh "$INSTANCE_NAME" --zone "$ZONE" --command "
set -e
chmod +x /tmp/savedbythemaid-deploy.sh
sudo mv /tmp/savedbythemaid-deploy.sh /opt/savedbythemaid-deploy.sh
sudo chown \"$USER\":\"$USER\" /opt/savedbythemaid-deploy.sh
cd /opt
./savedbythemaid-deploy.sh
"
}

main() {
  require_gcloud

  echo "⚙️  Config: instance=$INSTANCE_NAME zone=$ZONE machine=$MACHINE_TYPE disk=$BOOT_DISK_SIZE imageFamily=$IMAGE_FAMILY"

  echo "🔎 Verificando VM: $INSTANCE_NAME ($ZONE)"
  if instance_exists; then
    echo "✅ La VM ya existe."
  else
    echo "⚠️  La VM no existe (o no tienes permisos para verla)."
    create_instance
  fi

  ensure_firewall_rule "$RULE_FRONTEND" 3000
  ensure_firewall_rule "$RULE_API" 5000

  remote_deploy

  local ip
  ip="$(get_instance_ip || true)"

  echo ""
  echo "========================================"
  echo "✅ Despliegue en GCP completado"
  echo "========================================"
  echo "VM: $INSTANCE_NAME"
  echo "Zone: $ZONE"
  echo "IP: ${ip:-<sin_external_ip>}"
  echo "Frontend: http://${ip:-<IP_EXTERNA_DE_LA_VM>}:3000"
  echo "API:      http://${ip:-<IP_EXTERNA_DE_LA_VM>}:5000"
  echo "Health:   http://${ip:-<IP_EXTERNA_DE_LA_VM>}:5000/health"
}

main "$@"
