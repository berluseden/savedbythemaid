#!/bin/bash
# Script de despliegue para VM de Google Cloud

set -e

echo "🚀 Iniciando despliegue de SavedByTheMaid..."

# Colores para output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Variables
REPO_URL="https://github.com/berluseden/savedbythemaid.git"
APP_DIR="/opt/savedbythemaid"
BRANCH="main"

# 1. Actualizar sistema
echo -e "${GREEN}📦 Actualizando sistema...${NC}"
sudo apt-get update
sudo apt-get upgrade -y

# 2. Instalar Docker Compose si no está instalado
if ! command -v docker compose &> /dev/null; then
    echo -e "${GREEN}📦 Instalando Docker Compose...${NC}"
    sudo apt-get install docker-compose-plugin -y
fi

# 3. Configurar firewall (UFW)
echo -e "${GREEN}🔥 Configurando firewall...${NC}"
sudo ufw allow 22/tcp    # SSH
sudo ufw allow 80/tcp    # HTTP
sudo ufw allow 443/tcp   # HTTPS
sudo ufw allow 3000/tcp  # Frontend
sudo ufw allow 5000/tcp  # API
echo "y" | sudo ufw enable || true

# 4. Clonar o actualizar repositorio
if [ -d "$APP_DIR" ]; then
    echo -e "${GREEN}🔄 Actualizando repositorio...${NC}"
    cd "$APP_DIR"
    git fetch origin
    git reset --hard origin/$BRANCH
    git pull origin $BRANCH
else
    echo -e "${GREEN}📥 Clonando repositorio...${NC}"
    sudo mkdir -p "$APP_DIR"
    sudo chown -R $USER:$USER "$APP_DIR"
    git clone -b $BRANCH "$REPO_URL" "$APP_DIR"
    cd "$APP_DIR"
fi

# 5. Detener contenedores existentes
echo -e "${GREEN}🛑 Deteniendo contenedores existentes...${NC}"
docker compose down || true

# 6. Limpiar imágenes antiguas (opcional)
echo -e "${YELLOW}🧹 Limpiando imágenes antiguas...${NC}"
docker system prune -f

# 7. Construir y levantar contenedores
echo -e "${GREEN}🏗️  Construyendo y levantando contenedores...${NC}"
docker compose up -d --build

# 8. Esperar a que los servicios estén listos
echo -e "${GREEN}⏳ Esperando a que los servicios inicien...${NC}"
sleep 10

# 9. Verificar estado de los contenedores
echo -e "${GREEN}✅ Estado de los contenedores:${NC}"
docker compose ps

# 10. Mostrar logs
echo -e "${GREEN}📋 Logs recientes:${NC}"
docker compose logs --tail=50

# 11. Obtener IP externa
EXTERNAL_IP=$(curl -s ifconfig.me)

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}✅ Despliegue completado!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "🌐 Frontend: http://${EXTERNAL_IP}:3000"
echo -e "🔌 API: http://${EXTERNAL_IP}:5000"
echo -e "📊 API Health: http://${EXTERNAL_IP}:5000/health"
echo -e "📚 Swagger: http://${EXTERNAL_IP}:5000/swagger"
echo ""
echo -e "${YELLOW}Comandos útiles:${NC}"
echo -e "  Ver logs:      cd $APP_DIR && docker compose logs -f"
echo -e "  Reiniciar:     cd $APP_DIR && docker compose restart"
echo -e "  Detener:       cd $APP_DIR && docker compose down"
echo -e "  Ver estado:    cd $APP_DIR && docker compose ps"
echo ""
