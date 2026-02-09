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

get_external_ip() {
    # Preferir metadata server de GCP si está disponible
    local ip
    ip=$(curl -fsS -H "Metadata-Flavor: Google" \
        "http://metadata.google.internal/computeMetadata/v1/instance/network-interfaces/0/access-configs/0/external-ip" 2>/dev/null || true)
    if [ -n "$ip" ]; then
        echo "$ip"
        return 0
    fi

    # Fallback genérico
    curl -fsS ifconfig.me 2>/dev/null || true
}

ensure_packages() {
    sudo apt-get update -y

    if ! command -v git &> /dev/null; then
        echo -e "${GREEN}📦 Instalando Git...${NC}"
        sudo apt-get install -y git
    fi

    if ! command -v curl &> /dev/null; then
        echo -e "${GREEN}📦 Instalando curl...${NC}"
        sudo apt-get install -y curl
    fi

    if ! command -v docker &> /dev/null; then
        echo -e "${GREEN}📦 Instalando Docker...${NC}"
        sudo apt-get install -y docker.io
        sudo systemctl enable --now docker
    fi

    if ! docker compose version &> /dev/null; then
        echo -e "${GREEN}📦 Instalando Docker Compose (v2)...${NC}"
        # En Ubuntu 24.04 suele ser docker-compose-v2 (no docker-compose-plugin)
        if ! sudo apt-get install -y docker-compose-v2; then
            echo -e "${YELLOW}⚠️  No se pudo instalar docker-compose-v2; intentando docker-compose-plugin...${NC}"
            sudo apt-get install -y docker-compose-plugin
        fi
    fi
}

select_docker_cmd() {
    # Si no tenemos permisos sobre el socket de Docker, usar sudo.
    if docker info &> /dev/null; then
        echo "docker"
    else
        echo "sudo docker"
    fi
}

ensure_packages
DOCKER_CMD=$(select_docker_cmd)

# 2. Clonar o actualizar repositorio
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

# 3. Detener contenedores existentes
echo -e "${GREEN}🛑 Deteniendo contenedores existentes...${NC}"
${DOCKER_CMD} compose down || true

# 4. Limpiar imágenes antiguas (opcional)
echo -e "${YELLOW}🧹 Limpiando imágenes antiguas...${NC}"
${DOCKER_CMD} system prune -f

# 5. Construir y levantar contenedores
echo -e "${GREEN}🏗️  Construyendo y levantando contenedores...${NC}"
${DOCKER_CMD} compose up -d --build

# 6. Esperar a que MySQL esté listo
echo -e "${GREEN}⏳ Esperando a que MySQL inicie...${NC}"
sleep 15

# 7. Esperar a que la API esté lista
echo -e "${GREEN}⏳ Esperando a que la API inicie...${NC}"
for i in {1..30}; do
    if curl -sf http://localhost:5000/health > /dev/null; then
        echo -e "${GREEN}✅ API está lista!${NC}"
        break
    fi
    echo "Esperando API... ($i/30)"
    sleep 2
done

# 8. Verificar estado de los contenedores
echo -e "${GREEN}✅ Estado de los contenedores:${NC}"
${DOCKER_CMD} compose ps

# 9. Mostrar logs
echo -e "${GREEN}📋 Logs recientes de la API:${NC}"
${DOCKER_CMD} compose logs api --tail=30

echo -e "${GREEN}📋 Logs recientes del Frontend:${NC}"
${DOCKER_CMD} compose logs frontend --tail=20

# 10. Obtener IP externa
EXTERNAL_IP=$(get_external_ip)

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
echo -e "  Ver logs:      cd ${APP_DIR} && docker compose logs -f"
echo -e "  Reiniciar:     cd ${APP_DIR} && docker compose restart"
echo -e "  Detener:       cd ${APP_DIR} && docker compose down"
echo -e "  Ver estado:    cd ${APP_DIR} && docker compose ps"
echo ""
