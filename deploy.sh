#!/bin/bash
# ============================================================================
# deploy.sh -- Full deployment script for SavedByTheMaid on a GCP VM
#
# This script is designed to run ON the VM itself (not locally).
# It clones/updates the repo, builds and starts all Docker containers,
# then waits for the API to become healthy.
#
# Usage:
#   sudo ./deploy.sh
#
# Environment variables (all optional, with sensible defaults):
#   REPO_URL      -- Git repository URL
#   APP_DIR       -- Installation directory on the VM
#   BRANCH        -- Git branch to deploy
#   API_PORT      -- Port the API listens on (default: 5000)
#   FRONTEND_PORT -- Port the frontend listens on (default: 3000)
# ============================================================================

set -euo pipefail

echo "Iniciando despliegue de SavedByTheMaid..."

# ── Color helpers ─────────────────────────────────────────────────────────
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# ── Configurable variables ────────────────────────────────────────────────
REPO_URL="${REPO_URL:-https://github.com/berluseden/savedbythemaid.git}"
APP_DIR="${APP_DIR:-/opt/savedbythemaid}"
BRANCH="${BRANCH:-main}"
API_PORT="${API_PORT:-5000}"
FRONTEND_PORT="${FRONTEND_PORT:-3000}"

# When invoked via sudo, $USER is root. We want the real caller.
DEPLOY_USER="${SUDO_USER:-$USER}"

# ── Helper functions ──────────────────────────────────────────────────────

# Run a command as the deploy user (not root), preserving login environment.
run_as_deploy_user() {
    if [ "$DEPLOY_USER" = "$USER" ]; then
        bash -lc "$1"
    else
        sudo -u "$DEPLOY_USER" -H bash -lc "$1"
    fi
}

# Retrieve the VM's external IP via GCP metadata server or a public service.
get_external_ip() {
    local ip
    # Prefer the GCP metadata server when available
    ip=$(curl -fsS -H "Metadata-Flavor: Google" \
        "http://metadata.google.internal/computeMetadata/v1/instance/network-interfaces/0/access-configs/0/external-ip" 2>/dev/null || true)
    if [ -n "$ip" ]; then
        echo "$ip"
        return 0
    fi
    # Fallback: public IP service
    curl -fsS ifconfig.me 2>/dev/null || true
}

# ── 1. Install required system packages ───────────────────────────────────
ensure_packages() {
    echo -e "${GREEN}Checking required packages...${NC}"
    sudo apt-get update -y

    if ! command -v git &> /dev/null; then
        echo -e "${GREEN}Installing Git...${NC}"
        sudo apt-get install -y git
    fi

    if ! command -v curl &> /dev/null; then
        echo -e "${GREEN}Installing curl...${NC}"
        sudo apt-get install -y curl
    fi

    if ! command -v docker &> /dev/null; then
        echo -e "${GREEN}Installing Docker...${NC}"
        sudo apt-get install -y docker.io
        sudo systemctl enable --now docker
    fi

    if ! docker compose version &> /dev/null; then
        echo -e "${GREEN}Installing Docker Compose (v2)...${NC}"
        if ! sudo apt-get install -y docker-compose-v2; then
            echo -e "${YELLOW}docker-compose-v2 not available; trying docker-compose-plugin...${NC}"
            sudo apt-get install -y docker-compose-plugin
        fi
    fi
}

# Determine whether we need 'sudo docker' or plain 'docker'.
select_docker_cmd() {
    if docker info &> /dev/null; then
        echo "docker"
    else
        echo "sudo docker"
    fi
}

ensure_packages
DOCKER_CMD=$(select_docker_cmd)

# ── 2. Clone or update the repository ─────────────────────────────────────
if [ -d "$APP_DIR" ]; then
    echo -e "${GREEN}Updating repository...${NC}"
    sudo chown -R "$DEPLOY_USER":"$DEPLOY_USER" "$APP_DIR" || true
    # Mark as safe directory for Git (avoids "dubious ownership" errors)
    (git config --global --add safe.directory "$APP_DIR" >/dev/null 2>&1) || true
    run_as_deploy_user "git config --global --add safe.directory '$APP_DIR' >/dev/null 2>&1 || true"
    cd "$APP_DIR"
    run_as_deploy_user "cd '$APP_DIR' && git fetch origin"
    run_as_deploy_user "cd '$APP_DIR' && git reset --hard origin/$BRANCH"
    run_as_deploy_user "cd '$APP_DIR' && git pull origin $BRANCH"
else
    echo -e "${GREEN}Cloning repository...${NC}"
    sudo mkdir -p "$APP_DIR"
    sudo chown -R "$DEPLOY_USER":"$DEPLOY_USER" "$APP_DIR"
    run_as_deploy_user "git clone -b $BRANCH '$REPO_URL' '$APP_DIR'"
    cd "$APP_DIR"
fi

# ── 3. Stop existing containers ───────────────────────────────────────────
echo -e "${GREEN}Stopping existing containers...${NC}"
${DOCKER_CMD} compose down || true

# ── 4. Clean up old Docker images ─────────────────────────────────────────
echo -e "${YELLOW}Pruning unused Docker resources...${NC}"
${DOCKER_CMD} system prune -f

# ── 5. Build and start all containers ─────────────────────────────────────
echo -e "${GREEN}Building and starting containers...${NC}"
${DOCKER_CMD} compose up -d --build

# ── 6. Wait for MySQL to be ready ─────────────────────────────────────────
echo -e "${GREEN}Waiting for MySQL to start...${NC}"
sleep 15

# ── 7. Wait for the API health endpoint ───────────────────────────────────
echo -e "${GREEN}Waiting for API health check...${NC}"
API_READY=false
for i in {1..30}; do
    if curl -sf "http://localhost:${API_PORT}/health" > /dev/null; then
        echo -e "${GREEN}API is ready!${NC}"
        API_READY=true
        break
    fi
    echo "Waiting for API... ($i/30)"
    sleep 2
done

if [ "$API_READY" = false ]; then
    echo -e "${RED}API did not become healthy within 60 seconds.${NC}"
    echo -e "${RED}Check the logs: ${DOCKER_CMD} compose logs api${NC}"
fi

# ── 8. Show container status ──────────────────────────────────────────────
echo -e "${GREEN}Container status:${NC}"
${DOCKER_CMD} compose ps

# ── 9. Show recent logs ──────────────────────────────────────────────────
echo -e "${GREEN}Recent API logs:${NC}"
${DOCKER_CMD} compose logs api --tail=30

echo -e "${GREEN}Recent Frontend logs:${NC}"
${DOCKER_CMD} compose logs frontend --tail=20

# ── 10. Print deployment summary ─────────────────────────────────────────
EXTERNAL_IP=$(get_external_ip)

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Deployment complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "Frontend:   http://${EXTERNAL_IP}:${FRONTEND_PORT}"
echo -e "API:        http://${EXTERNAL_IP}:${API_PORT}"
echo -e "API Health: http://${EXTERNAL_IP}:${API_PORT}/health"
echo -e "Swagger:    http://${EXTERNAL_IP}:${API_PORT}/swagger"
echo ""
echo -e "${YELLOW}Useful commands:${NC}"
echo -e "  View logs:     cd ${APP_DIR} && docker compose logs -f"
echo -e "  Restart:       cd ${APP_DIR} && docker compose restart"
echo -e "  Stop:          cd ${APP_DIR} && docker compose down"
echo -e "  Status:        cd ${APP_DIR} && docker compose ps"
echo ""
