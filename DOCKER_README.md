# 🐳 Docker Quick Start - SavedByTheMaid

## 📦 Requisitos Previos

- Docker (20.10+)
- Docker Compose (2.0+)

## 🚀 Inicio Rápido

### 1. Clonar y configurar variables de entorno

```bash
# Copiar archivo de ejemplo
cp .env.example .env

# Editar .env con tus configuraciones (opcional, los valores por defecto funcionan)
nano .env
```

### 2. Levantar toda la aplicación

```bash
# Construir y levantar todos los servicios
docker compose up -d

# Ver logs en tiempo real
docker compose logs -f

# Ver logs de un servicio específico
docker compose logs -f api
docker compose logs -f frontend
```

### 3. Acceder a la aplicación

Una vez que todos los contenedores estén corriendo:

- **Frontend**: http://localhost:3000
- **API**: http://localhost:5000
- **API Swagger**: http://localhost:5000/swagger
- **PHPMyAdmin**: http://localhost:8080
  - Usuario: `appuser`
  - Password: `App@123456`

## 🔧 Comandos Útiles

### Gestión de contenedores

```bash
# Detener todos los servicios
docker compose down

# Detener y eliminar volúmenes (CUIDADO: elimina la base de datos)
docker compose down -v

# Reconstruir imágenes
docker compose build

# Reconstruir y levantar
docker compose up -d --build

# Ver estado de los servicios
docker compose ps

# Ver uso de recursos
docker stats
```

### Gestión de la base de datos

```bash
# Ejecutar comandos SQL directamente
docker exec -it savedbythemaid-mysql mysql -u appuser -pApp@123456 SavedByTheMaidNew

# Hacer backup de la base de datos
docker exec savedbythemaid-mysql mysqldump -u appuser -pApp@123456 SavedByTheMaidNew > backup.sql

# Restaurar backup
docker exec -i savedbythemaid-mysql mysql -u appuser -pApp@123456 SavedByTheMaidNew < backup.sql

# Ver logs de MySQL
docker compose logs -f mysql
```

### Debugging

```bash
# Ejecutar bash en el contenedor de la API
docker exec -it savedbythemaid-api bash

# Ejecutar bash en el contenedor de MySQL
docker exec -it savedbythemaid-mysql bash

# Ver logs de la API
docker exec savedbythemaid-api cat /app/logs/savedbythemaid-$(date +%Y%m%d).log

# Reiniciar un servicio específico
docker compose restart api
docker compose restart frontend
```

## 📁 Estructura de Servicios

```
┌─────────────────────────────────────────┐
│         Docker Compose Stack            │
├─────────────────────────────────────────┤
│                                         │
│  ┌──────────┐    ┌──────────┐          │
│  │ Frontend │◄───┤   Nginx  │          │
│  │  React   │    │   :80    │          │
│  └────┬─────┘    └──────────┘          │
│       │                                 │
│       │ HTTP Requests                   │
│       ▼                                 │
│  ┌──────────┐                           │
│  │   API    │                           │
│  │  .NET 10 │                           │
│  │  :5000   │                           │
│  └────┬─────┘                           │
│       │                                 │
│       │ Entity Framework                │
│       ▼                                 │
│  ┌──────────┐    ┌────────────┐        │
│  │  MySQL   │◄───┤ PHPMyAdmin │        │
│  │  :3306   │    │   :8080    │        │
│  └──────────┘    └────────────┘        │
│                                         │
└─────────────────────────────────────────┘
```

## 🔐 Configuración de Seguridad

### Para Desarrollo

Los valores por defecto en `.env.example` son adecuados.

### Para Producción

**⚠️ IMPORTANTE**: Antes de desplegar a producción, debes cambiar:

1. **Contraseñas de MySQL**:
   ```env
   MYSQL_ROOT_PASSWORD=<password-seguro-aleatorio>
   MYSQL_PASSWORD=<password-seguro-aleatorio>
   ```

2. **JWT Secret Key** (mínimo 32 caracteres):
   ```env
   JWT_SECRET_KEY=<genera-una-clave-aleatoria-de-al-menos-32-caracteres>
   ```

3. **Configurar HTTPS** en el frontend (usar reverse proxy como Nginx o Traefik)

## 🧪 Aplicar Migraciones de Base de Datos

Las migraciones de Entity Framework se aplicarán automáticamente al iniciar la API por primera vez.

Si necesitas aplicarlas manualmente:

```bash
# Desde el directorio del proyecto
docker exec -it savedbythemaid-api dotnet ef database update
```

## 🐛 Troubleshooting

### La API no se conecta a MySQL

```bash
# Verificar que MySQL esté saludable
docker compose ps

# Ver logs de MySQL
docker compose logs mysql

# Reiniciar servicios
docker compose restart mysql api
```

### El frontend no carga

```bash
# Verificar logs del contenedor
docker compose logs frontend

# Reconstruir la imagen del frontend
docker compose build frontend
docker compose up -d frontend
```

### Error "Cannot connect to Docker daemon"

```bash
# Iniciar Docker daemon
sudo systemctl start docker

# O en Mac/Windows, abrir Docker Desktop
```

### Puertos ya en uso

Si algún puerto está ocupado (3000, 5000, 3306, 8080), puedes cambiarlos en el `docker-compose.yml`:

```yaml
ports:
  - "NUEVO_PUERTO:PUERTO_INTERNO"
```

### Limpiar todo y empezar de nuevo

```bash
# Detener y eliminar todo (incluye volúmenes)
docker compose down -v

# Eliminar imágenes locales
docker rmi savedbythemaid-api savedbythemaid-frontend

# Reconstruir todo
docker compose up -d --build
```

## 📊 Monitoreo

### Health Checks

Todos los servicios tienen health checks configurados:

```bash
# Ver estado de salud
docker compose ps

# Formato: servicio (healthy|unhealthy|starting)
```

### Logs Centralizados

```bash
# Ver logs de todos los servicios
docker compose logs -f --tail=100

# Filtrar por nivel de log (si está configurado)
docker compose logs | grep "ERROR"
docker compose logs | grep "WARNING"
```

## 🔄 Actualizar la Aplicación

```bash
# 1. Obtener últimos cambios
git pull

# 2. Reconstruir imágenes
docker compose build

# 3. Recrear contenedores (sin perder datos)
docker compose up -d

# 4. Verificar que todo está corriendo
docker compose ps
docker compose logs -f
```

## 💾 Backups Automatizados

Script de ejemplo para backup diario:

```bash
#!/bin/bash
# backup.sh

DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="./backups"
mkdir -p $BACKUP_DIR

# Backup de MySQL
docker exec savedbythemaid-mysql mysqldump -u appuser -pApp@123456 SavedByTheMaidNew > $BACKUP_DIR/db_$DATE.sql

# Comprimir
gzip $BACKUP_DIR/db_$DATE.sql

# Mantener solo últimos 7 días
find $BACKUP_DIR -name "db_*.sql.gz" -mtime +7 -delete

echo "Backup completado: $BACKUP_DIR/db_$DATE.sql.gz"
```

Agregar a crontab para ejecución diaria:
```bash
# Ejecutar todos los días a las 2 AM
0 2 * * * /path/to/backup.sh
```

## 📚 Más Información

- [Docker Compose Docs](https://docs.docker.com/compose/)
- [.NET Docker Guide](https://docs.microsoft.com/en-us/dotnet/core/docker/)
- [React Deployment](https://create-react-app.dev/docs/deployment/)
