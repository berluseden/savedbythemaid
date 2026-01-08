#!/bin/bash
# Script para actualizar solo el frontend en GCP

set -e

echo "🔄 Actualizando frontend en GCP..."

# Variables
APP_DIR="/opt/savedbythemaid"

cd "$APP_DIR"

# Detener solo el frontend
echo "🛑 Deteniendo frontend..."
docker compose stop frontend

# Eliminar la imagen anterior del frontend
echo "🗑️  Eliminando imagen anterior..."
docker rmi savedbythemaid-frontend || true

# Reconstruir solo el frontend
echo "🏗️  Reconstruyendo frontend..."
docker compose build --no-cache frontend

# Levantar el frontend
echo "🚀 Iniciando frontend..."
docker compose up -d frontend

# Esperar a que esté saludable
echo "⏳ Esperando a que el frontend esté saludable..."
sleep 15

# Verificar estado
echo "✅ Estado del frontend:"
docker compose ps frontend

echo ""
echo "✅ Actualización completada!"
echo "🌐 Frontend: http://35.243.209.204:3000"
