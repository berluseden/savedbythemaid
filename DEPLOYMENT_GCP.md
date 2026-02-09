# 🚀 Guía de Despliegue en Google Cloud VM

## 📋 Pre-requisitos en la VM

✅ Docker instalado y funcionando
✅ Git instalado
✅ Puerto SSH (22) abierto
✅ Usuario con permisos sudo

## 🔧 Paso 1: Conectar a la VM

```bash
# Desde tu máquina local, conecta vía SSH
gcloud compute ssh instancia-gratis-ubuntu --zone=<tu-zona>

# O si ya configuraste gcloud
gcloud compute ssh instancia-gratis-ubuntu
```

## 📦 Paso 2: Configuración Inicial (Solo Primera Vez)

### Instalar Docker Compose Plugin

```bash
sudo apt-get update
sudo apt-get install docker-compose-plugin -y
```

### Verificar instalación

```bash
docker compose version
```

### Configurar Git (si vas a clonar)

```bash
git config --global user.name "Tu Nombre"
git config --global user.email "tu@email.com"
```

## 🚀 Paso 3: Opción A - Despliegue Automático (Recomendado)

### Opción A0 (Desde tu máquina local): crear VM si no existe + desplegar

Este repo incluye un helper que ejecutas desde tu máquina local (requiere `gcloud`).

```bash
chmod +x gcp-deploy.sh

# Defaults: instancia-gratis-ubuntu + us-central1-a
./gcp-deploy.sh

# O con parámetros
./gcp-deploy.sh <INSTANCE_NAME> <ZONE>

# O con env vars
INSTANCE_NAME=instancia-gratis-ubuntu ZONE=us-central1-a ./gcp-deploy.sh
```

### Ejecutar script de despliegue

```bash
# Descargar el script
curl -o deploy.sh https://raw.githubusercontent.com/berluseden/savedbythemaid/main/deploy.sh

# Dar permisos de ejecución
chmod +x deploy.sh

# Ejecutar
./deploy.sh
```

> Nota: `deploy.sh` está pensado para ejecutarse dentro de la VM.

## 🔨 Paso 3: Opción B - Despliegue Manual

### 1. Clonar repositorio

```bash
# Crear directorio para la aplicación
sudo mkdir -p /opt/savedbythemaid
sudo chown -R $USER:$USER /opt/savedbythemaid

# Clonar
cd /opt/savedbythemaid
git clone https://github.com/berluseden/savedbythemaid.git .
```

### 2. Configurar Firewall

```bash
# Permitir puertos necesarios
sudo ufw allow 22/tcp   # SSH
sudo ufw allow 80/tcp   # HTTP
sudo ufw allow 443/tcp  # HTTPS (futuro)
sudo ufw allow 3000/tcp # Frontend
sudo ufw allow 5000/tcp # API

# Habilitar firewall
sudo ufw enable
sudo ufw status
```

### 3. Levantar contenedores

```bash
cd /opt/savedbythemaid

# Construir y levantar
docker compose up -d --build

# Ver logs
docker compose logs -f
```

## 🔍 Paso 4: Verificar Despliegue

### Comprobar contenedores

```bash
docker compose ps
```

Deberías ver 3 contenedores corriendo:
- `savedbythemaid-mysql`
- `savedbythemaid-api`
- `savedbythemaid-frontend`

### Obtener IP externa

```bash
curl ifconfig.me
```

### Probar endpoints

```bash
# Obtener IP
EXTERNAL_IP=$(curl -s ifconfig.me)

# Probar health check de la API
curl http://${EXTERNAL_IP}:5000/health

# Probar frontend (desde navegador)
# http://<EXTERNAL_IP>:3000
```

## 🌐 Paso 5: Configurar Reglas de Firewall en Google Cloud

### Desde la consola de Google Cloud:

```bash
# Crear regla para HTTP/Frontend
gcloud compute firewall-rules create allow-frontend \
    --allow tcp:3000 \
    --source-ranges 0.0.0.0/0 \
    --description "Allow frontend access"

# Crear regla para API
gcloud compute firewall-rules create allow-api \
    --allow tcp:5000 \
    --source-ranges 0.0.0.0/0 \
    --description "Allow API access"

# Verificar reglas
gcloud compute firewall-rules list
```

### O desde la consola web:

1. Ve a **VPC Network > Firewall**
2. Crea reglas para:
   - `tcp:3000` (Frontend)
   - `tcp:5000` (API)
   - Source: `0.0.0.0/0`

## 📊 Paso 6: Monitoreo y Mantenimiento

### Ver logs en tiempo real

```bash
# Todos los servicios
docker compose logs -f

# Solo API
docker compose logs -f api

# Solo Frontend
docker compose logs -f frontend

# Solo MySQL
docker compose logs -f mysql
```

### Reiniciar servicios

```bash
# Reiniciar todo
docker compose restart

# Reiniciar solo la API
docker compose restart api
```

### Actualizar la aplicación

```bash
cd /opt/savedbythemaid

# Obtener últimos cambios
git pull origin main

# Reconstruir y reiniciar
docker compose up -d --build

# Ver logs
docker compose logs -f
```

Si quieres actualizar solo la API desde tu máquina local:

```bash
chmod +x update-api.sh

# Defaults: instancia-gratis-ubuntu + us-central1-a
./update-api.sh

# O con parámetros
./update-api.sh <INSTANCE_NAME> <ZONE>
```

### Detener todo

```bash
docker compose down
```

### Detener y eliminar volúmenes (⚠️ CUIDADO - Borra la BD)

```bash
docker compose down -v
```

## 🔐 Paso 7: Configurar HTTPS (Opcional pero Recomendado)

### Opción A: Usando Certbot y Nginx

```bash
# Instalar Nginx
sudo apt-get install nginx certbot python3-certbot-nginx -y

# Crear configuración de Nginx
sudo nano /etc/nginx/sites-available/savedbythemaid
```

Contenido del archivo:

```nginx
server {
    listen 80;
    server_name tu-dominio.com;

    # Frontend
    location / {
        proxy_pass http://localhost:3000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }

    # API
    location /api/ {
        proxy_pass http://localhost:5000/;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Activar configuración:

```bash
sudo ln -s /etc/nginx/sites-available/savedbythemaid /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx

# Obtener certificado SSL
sudo certbot --nginx -d tu-dominio.com
```

## 🧹 Paso 8: Limpieza y Optimización

### Limpiar imágenes y contenedores antiguos

```bash
# Limpiar recursos no utilizados
docker system prune -a

# Ver uso de espacio
docker system df
```

### Configurar logs rotativos

```bash
# Editar daemon.json
sudo nano /etc/docker/daemon.json
```

Agregar:

```json
{
  "log-driver": "json-file",
  "log-opts": {
    "max-size": "10m",
    "max-file": "3"
  }
}
```

Reiniciar Docker:

```bash
sudo systemctl restart docker
docker compose up -d
```

## 📈 Paso 9: Backup de Base de Datos

### Script de backup automático

```bash
# Crear script
nano ~/backup-db.sh
```

Contenido:

```bash
#!/bin/bash
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="/opt/backups"
mkdir -p $BACKUP_DIR

docker exec savedbythemaid-mysql mysqldump -u appuser -pApp@123456 SavedByTheMaidNew > $BACKUP_DIR/db_$DATE.sql
gzip $BACKUP_DIR/db_$DATE.sql

# Mantener solo últimos 7 días
find $BACKUP_DIR -name "db_*.sql.gz" -mtime +7 -delete

echo "Backup completado: $BACKUP_DIR/db_$DATE.sql.gz"
```

Dar permisos y configurar cron:

```bash
chmod +x ~/backup-db.sh

# Agregar a crontab (ejecutar diario a las 2 AM)
crontab -e
# Agregar: 0 2 * * * /home/codespace/backup-db.sh
```

## 🔄 Paso 10: Actualización de la Aplicación

### Actualización completa (recomendado)

```bash
cd /opt/savedbythemaid
./deploy.sh
```

Este script automáticamente:
- Actualiza el código desde GitHub
- Detiene los contenedores
- Reconstruye las imágenes
- Reinicia todo

### Actualización solo del frontend

Si solo necesitas actualizar el frontend (por ejemplo, después de corregir el healthcheck):

```bash
cd /opt/savedbythemaid

# Actualizar código
git pull origin main

# Ejecutar script de actualización
./update-frontend.sh
```

### Actualización manual selectiva

```bash
cd /opt/savedbythemaid

# Actualizar repositorio
git pull origin main

# Reconstruir y actualizar contenedores
docker compose up -d --build

# Verificar estado
docker compose ps
```

### Actualización solo de la API

```bash
cd /opt/savedbythemaid
docker compose stop api
docker compose build --no-cache api
docker compose up -d api
docker compose ps api
```

### Actualización sin downtime (rolling update)

Para minimizar el downtime:

```bash
cd /opt/savedbythemaid
git pull origin main

# Actualizar solo un servicio a la vez
docker compose up -d --no-deps --build api
sleep 10
docker compose up -d --no-deps --build frontend
```

## 🔧 Troubleshooting

### Frontend marcado como "unhealthy"

**Síntoma**: `docker ps` muestra el frontend como `(unhealthy)`

**Causa**: El healthcheck del Dockerfile.frontend usa `wget` pero no estaba instalado.

**Solución**: Actualiza a la última versión que incluye la corrección:

```bash
cd /opt/savedbythemaid
git pull origin main
./update-frontend.sh

# Verificar
docker compose ps frontend
```

Deberías ver el status cambiar de `(unhealthy)` a `(healthy)` después de ~30 segundos.

### Error: "Cannot connect to Docker daemon"

```bash
sudo systemctl start docker
sudo systemctl enable docker
```
### Error: "Request failed with status code 500" en Login

**Causa:** La base de datos no está inicializada (tablas no creadas).

```bash
# Ver logs de la API
docker compose logs api --tail=50 | grep -i "error\|exception"

# Buscar: "Table 'SavedByTheMaidNew.AspNetUsers' doesn't exist"
```

**Solución:**

La aplicación ahora utiliza `EnsureCreated()` automáticamente si no hay migraciones. Solo reinicia la API:

```bash
docker compose restart api

# Esperar 30 segundos y verificar logs
docker compose logs api --tail=20
```

Si ves "Esquema de base de datos creado exitosamente", el problema está resuelto.

### Error: Timeout al acceder desde navegador + UFW bloqueando

**Síntoma:** `curl` falla con timeout, logs muestran `[UFW BLOCK]`

```bash
# Ver logs del sistema
gcloud compute instances get-serial-port-output instancia-gratis-ubuntu --zone=us-central1-a | grep UFW
```

**Solución:**

```bash
# Configurar UFW para permitir los puertos necesarios
sudo ufw allow 22/tcp
sudo ufw allow 3000/tcp
sudo ufw allow 5000/tcp
sudo ufw --force enable

# Verificar reglas
sudo ufw status numbered
```

**Configuración permanente** (usar startup-script de GCP):

```bash
gcloud compute instances add-metadata instancia-gratis-ubuntu \
  --zone=us-central1-a \
  --metadata=startup-script='#!/bin/bash
sudo ufw allow 22/tcp
sudo ufw allow 3000/tcp
sudo ufw allow 5000/tcp
sudo ufw --force enable
'
```

Luego reinicia la VM para que aplique:

```bash
gcloud compute instances stop instancia-gratis-ubuntu --zone=us-central1-a
gcloud compute instances start instancia-gratis-ubuntu --zone=us-central1-a
```
### Error: "Port already in use"

```bash
# Ver qué está usando el puerto
sudo lsof -i :3000
sudo lsof -i :5000

# Detener proceso o cambiar puertos en docker-compose.yml
```

### Error: "No space left on device"

```bash
# Limpiar Docker
docker system prune -a --volumes

# Ver uso de disco
df -h
du -sh /var/lib/docker
```

### Logs de errores de la API

```bash
# Ver logs completos
docker compose logs api --tail=100

# Entrar al contenedor
docker exec -it savedbythemaid-api bash
cat /app/logs/savedbythemaid-$(date +%Y%m%d).log
```

## 📝 Checklist Final

- [ ] Docker y Docker Compose instalados
- [ ] Repositorio clonado en `/opt/savedbythemaid`
- [ ] Firewall configurado (UFW)
- [ ] Reglas de firewall en Google Cloud creadas
- [ ] Contenedores corriendo (`docker compose ps`)
- [ ] Frontend accesible en `http://<IP>:3000`
- [ ] API accesible en `http://<IP>:5000`
- [ ] Health check responde: `http://<IP>:5000/health`
- [ ] Base de datos inicializada correctamente
- [ ] Backup automático configurado (opcional)
- [ ] HTTPS configurado (opcional)

## 📘 Arquitectura de Red Frontend-Backend

### Cómo funciona el proxy en producción:

```
Usuario → Frontend (nginx:80) → /api/* → Backend API (api:5000)
   ↓                  ↓                           ↓
Navegador      Contenedor nginx          Contenedor .NET
```

**Flujo de una petición:**
1. Usuario navega a `http://<IP-GCP>:3000`
2. Nginx sirve el SPA de React (archivos estáticos)
3. React hace fetch a `/api/auth/login` (ruta relativa)
4. Nginx intercepta `/api/*` y hace proxy a `http://api:5000/api/auth/login`
5. Dentro de Docker network, `api` resuelve al contenedor de la API
6. La API procesa y responde

**Por qué funciona:**
- ✅ Frontend usa `baseURL: '/api'` (ruta relativa)
- ✅ Nginx proxy sin barra final preserva la ruta: `/api/auth/login` → `http://api:5000/api/auth/login`
- ✅ La API tiene controladores con `[Route("api/[controller]")]`
- ✅ No necesita CORS porque desde la perspectiva del navegador, todo es el mismo origen (puerto 3000)

### Variables de entorno:

**VITE_API_URL no se usa en producción** porque:
- Solo afecta al proxy de desarrollo de Vite
- En producción, nginx usa configuración estática
- El frontend buildeado no contiene referencias a variables de entorno de API

## 🆘 Soporte

Si encuentras problemas:

1. Revisa los logs: `docker compose logs -f`
2. Verifica el estado: `docker compose ps`
3. Comprueba conectividad: `curl http://localhost:5000/health`
4. Revisa firewall: `sudo ufw status`
5. Verifica recursos: `docker stats`
6. Prueba el proxy de nginx: `curl http://localhost:3000/api/health`
