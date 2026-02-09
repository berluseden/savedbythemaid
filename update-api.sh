#!/bin/bash
set -e

echo "🚀 Actualizando solo el API en GCP..."

# Parámetros (pueden venir por env o args)
INSTANCE_NAME="${INSTANCE_NAME:-${1:-instancia-gratis-ubuntu}}"
ZONE="${ZONE:-${2:-us-central1-a}}"

get_instance_ip() {
	gcloud compute instances describe "$INSTANCE_NAME" \
		--zone="$ZONE" \
		--format='get(networkInterfaces[0].accessConfigs[0].natIP)'
}

EXTERNAL_IP="$(get_instance_ip || true)"
if [ -z "$EXTERNAL_IP" ]; then
	echo "⚠️  No pude obtener la IP externa de la instancia."
	echo "   Verifica que exista y tenga External IP: INSTANCE_NAME=$INSTANCE_NAME ZONE=$ZONE"
else
	echo "📍 VM: $INSTANCE_NAME ($ZONE) IP: $EXTERNAL_IP"
fi

# Ejecutar comandos en la VM
gcloud compute ssh "$INSTANCE_NAME" --zone="$ZONE" --command="
set -e
cd /opt/savedbythemaid
git pull origin main
docker compose build api
docker compose up -d api
echo '✅ API actualizado correctamente'
docker compose ps
"

echo "========================================
✅ Actualización del API completada!
========================================
🔌 API: http://${EXTERNAL_IP:-<IP_EXTERNA_DE_LA_VM>}:5000
📊 API Health: http://${EXTERNAL_IP:-<IP_EXTERNA_DE_LA_VM>}:5000/health
"
