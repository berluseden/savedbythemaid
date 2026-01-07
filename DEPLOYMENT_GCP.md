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

### Ejecutar script de despliegue

```bash
# Descargar el script
curl -o deploy.sh https://raw.githubusercontent.com/berluseden/savedbythemaid/main/deploy.sh

# Dar permisos de ejecución
chmod +x deploy.sh

# Ejecutar
./deploy.sh
```

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

## 🔧 Troubleshooting

### Error: "Cannot connect to Docker daemon"

```bash
sudo systemctl start docker
sudo systemctl enable docker
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

## 🆘 Soporte

Si encuentras problemas:

1. Revisa los logs: `docker compose logs -f`
2. Verifica el estado: `docker compose ps`
3. Comprueba conectividad: `curl http://localhost:5000/health`
4. Revisa firewall: `sudo ufw status`
5. Verifica recursos: `docker stats`
