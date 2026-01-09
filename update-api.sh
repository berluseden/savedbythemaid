#!/bin/bash
set -e

echo "🚀 Actualizando solo el API en GCP..."

# Detectar IP externa automáticamente
EXTERNAL_IP=$(curl -s ifconfig.me)
echo "📍 Conectando a VM con IP: $EXTERNAL_IP"

# Ejecutar comandos en la VM
gcloud compute ssh instancia-gratis-ubuntu --zone=us-central1-a --command="
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
🔌 API: http://$EXTERNAL_IP:5000
📊 API Health: http://$EXTERNAL_IP:5000/health
"
