# 🚀 GUÍA DE DEPLOYMENT - SavedByTheMaid

## 📋 PRE-REQUISITOS

### Software Requerido:
- .NET 10 SDK
- MySQL 8.0+
- Node.js 20+ y npm
- Azure CLI (para Azure deployment) O AWS CLI (para AWS deployment)

### Servicios de Terceros:
- [ ] Application Insights (Azure) o CloudWatch (AWS) - Opcional
- [ ] Azure Key Vault o AWS Secrets Manager - Recomendado
- [ ] SendGrid/Mailgun para emails - Futuro

---

## 🗄️ PASO 1: PREPARAR BASE DE DATOS

### Opción A: Aplicar Migración Manual (RECOMENDADO)

```bash
# 1. Conectar a MySQL
mysql -h localhost -u root -p

# 2. Seleccionar la base de datos
USE SavedByTheMaidNew;

# 3. Verificar columna PaymentStatus existe
DESCRIBE ServiceOrders;

# 4. Ejecutar migración manualmente
ALTER TABLE ServiceOrders DROP COLUMN IF EXISTS PaymentStatus;

CREATE INDEX IX_ServiceOrders_CreatedAt 
ON ServiceOrders(CreatedAt DESC);

CREATE INDEX IX_ServiceOrders_OrderStatus_CreatedAt 
ON ServiceOrders(OrderStatus, CreatedAt DESC);

# 5. Verificar cambios
SHOW INDEX FROM ServiceOrders;
```

### Opción B: Actualizar EF Tools y Aplicar Migración

```bash
# 1. Actualizar dotnet-ef a versión compatible con .NET 10
dotnet tool update --global dotnet-ef

# 2. Verificar versión
dotnet ef --version
# Debe ser >= 10.0

# 3. Aplicar migración
cd /path/to/SavedByTheMaid.New/src/SavedByTheMaid.Infrastructure
dotnet ef database update --startup-project ../SavedByTheMaid.Api
```

### Verificación:
```sql
-- Debe retornar 0 rows (columna no existe)
SELECT COLUMN_NAME 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'ServiceOrders' 
  AND COLUMN_NAME = 'PaymentStatus';

-- Debe retornar 2 rows (índices creados)
SELECT INDEX_NAME 
FROM INFORMATION_SCHEMA.STATISTICS 
WHERE TABLE_NAME = 'ServiceOrders' 
  AND INDEX_NAME LIKE 'IX_ServiceOrders_%';
```

---

## 🔐 PASO 2: CONFIGURAR SECRETS

### Desarrollo Local (appsettings.Development.json):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=SavedByTheMaidNew;User=root;Password=TU_PASSWORD;"
  },
  "Jwt": {
    "Secret": "LocalDevelopmentSecretKey_AtLeast32Characters!",
    "Issuer": "SavedByTheMaid",
    "Audience": "SavedByTheMaidApp",
    "ExpirationMinutes": 60,
    "RefreshExpirationDays": 7
  },
  "ApplicationInsights": {
    "ConnectionString": ""
  }
}
```

### Producción - Variables de Entorno:

**Linux/Mac:**
```bash
export DATABASE_CONNECTION_STRING="Server=prod-mysql.server.com;Port=3306;Database=SavedByTheMaidProd;User=appuser;Password=SECURE_PASSWORD;Pooling=true;Min Pool Size=5;Max Pool Size=100;"
export JWT_SECRET="SUPER_SECURE_RANDOM_KEY_256_BITS_MINIMUM_LENGTH_REQUIRED_FOR_PRODUCTION!"
export APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=xxx;IngestionEndpoint=https://xxx"
```

**Windows (PowerShell):**
```powershell
$env:DATABASE_CONNECTION_STRING="Server=..."
$env:JWT_SECRET="..."
$env:APPLICATIONINSIGHTS_CONNECTION_STRING="..."
```

**Docker/Kubernetes:**
```yaml
# docker-compose.yml
version: '3.8'
services:
  api:
    image: savedbythemaid-api:latest
    environment:
      - ConnectionStrings__DefaultConnection=${DATABASE_CONNECTION_STRING}
      - Jwt__Secret=${JWT_SECRET}
      - ApplicationInsights__ConnectionString=${APPLICATIONINSIGHTS_CONNECTION_STRING}
      - ASPNETCORE_ENVIRONMENT=Production
    ports:
      - "5000:8080"
```

### Producción - Azure Key Vault (RECOMENDADO):

```bash
# 1. Crear Key Vault
az keyvault create \
  --name savedbythemaid-vault \
  --resource-group SavedByTheMaid-RG \
  --location eastus

# 2. Agregar secrets
az keyvault secret set \
  --vault-name savedbythemaid-vault \
  --name DbConnectionString \
  --value "Server=..."

az keyvault secret set \
  --vault-name savedbythemaid-vault \
  --name JwtSecret \
  --value "..."

# 3. Dar acceso a la App Service
az webapp identity assign \
  --name savedbythemaid-api \
  --resource-group SavedByTheMaid-RG

# 4. Configurar permisos
az keyvault set-policy \
  --name savedbythemaid-vault \
  --object-id <OBJECT_ID_FROM_STEP_3> \
  --secret-permissions get list
```

**En Program.cs (ya configurado si usas Key Vault):**
```csharp
// Descomentar para Azure Key Vault:
// builder.Configuration.AddAzureKeyVault(
//     new Uri("https://savedbythemaid-vault.vault.azure.net/"),
//     new DefaultAzureCredential());
```

---

## 📦 PASO 3: BUILD DEL PROYECTO

### Backend (.NET):
```bash
cd /path/to/SavedByTheMaid.New/src/SavedByTheMaid.Api

# Restaurar dependencias
dotnet restore

# Build en modo Release
dotnet build -c Release

# Publicar (genera archivos optimizados)
dotnet publish -c Release -o ./publish

# Verificar build
ls ./publish
# Debe contener: SavedByTheMaid.Api.dll, appsettings.json, wwwroot/, etc.
```

### Frontend (React + Vite):
```bash
cd /path/to/SavedByTheMaid.New/src/SavedByTheMaid.Web

# Instalar dependencias
npm install

# Build para producción
npm run build
# Genera: dist/ folder

# Verificar build
ls dist/
# Debe contener: index.html, assets/
```

---

## ☁️ PASO 4: DEPLOYMENT

### Opción A: Azure App Service

**Backend:**
```bash
# 1. Crear App Service Plan
az appservice plan create \
  --name SavedByTheMaid-Plan \
  --resource-group SavedByTheMaid-RG \
  --sku B1 \
  --is-linux

# 2. Crear Web App
az webapp create \
  --name savedbythemaid-api \
  --resource-group SavedByTheMaid-RG \
  --plan SavedByTheMaid-Plan \
  --runtime "DOTNET|10.0"

# 3. Configurar variables de entorno
az webapp config appsettings set \
  --name savedbythemaid-api \
  --resource-group SavedByTheMaid-RG \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    Cors__AllowedOrigins__0=https://yourdomain.com \
    Cors__AllowedOrigins__1=https://www.yourdomain.com

# 4. Deploy desde local
cd src/SavedByTheMaid.Api
az webapp deploy \
  --resource-group SavedByTheMaid-RG \
  --name savedbythemaid-api \
  --src-path ./publish.zip \
  --type zip
```

**Frontend:**
```bash
# 1. Crear Static Web App
az staticwebapp create \
  --name savedbythemaid-web \
  --resource-group SavedByTheMaid-RG \
  --source https://github.com/yourorg/savedbythemaid \
  --location eastus2 \
  --branch main \
  --app-location "/src/SavedByTheMaid.Web" \
  --output-location "dist"

# 2. Configurar API location (si usas Azure Functions)
# En staticwebapp.config.json:
{
  "routes": [
    {
      "route": "/api/*",
      "rewrite": "https://savedbythemaid-api.azurewebsites.net/api/*"
    }
  ]
}
```

### Opción B: AWS (EC2 + RDS)

**Backend en EC2:**
```bash
# 1. SSH a instancia EC2
ssh -i your-key.pem ubuntu@ec2-xx-xx-xx-xx.compute.amazonaws.com

# 2. Instalar .NET 10
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 10.0

# 3. Copiar archivos
scp -i your-key.pem -r ./publish ubuntu@ec2-xx-xx-xx-xx:/home/ubuntu/api

# 4. Configurar systemd service
sudo nano /etc/systemd/system/savedbythemaid.service

# Contenido:
[Unit]
Description=SavedByTheMaid API

[Service]
WorkingDirectory=/home/ubuntu/api
ExecStart=/home/ubuntu/.dotnet/dotnet SavedByTheMaid.Api.dll
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DATABASE_CONNECTION_STRING=...
Environment=JWT_SECRET=...

[Install]
WantedBy=multi-user.target

# 5. Iniciar servicio
sudo systemctl enable savedbythemaid
sudo systemctl start savedbythemaid
sudo systemctl status savedbythemaid
```

**RDS MySQL:**
```bash
# 1. Crear RDS instance
aws rds create-db-instance \
  --db-instance-identifier savedbythemaid-db \
  --db-instance-class db.t3.micro \
  --engine mysql \
  --engine-version 8.0 \
  --master-username admin \
  --master-user-password SECURE_PASSWORD \
  --allocated-storage 20

# 2. Conectar y aplicar migración
mysql -h savedbythemaid-db.xxx.rds.amazonaws.com -u admin -p < migration.sql
```

### Opción C: Docker + Docker Compose

**Dockerfile (Backend):**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["SavedByTheMaid.Api/SavedByTheMaid.Api.csproj", "SavedByTheMaid.Api/"]
RUN dotnet restore
COPY . .
WORKDIR "/src/SavedByTheMaid.Api"
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SavedByTheMaid.Api.dll"]
```

**docker-compose.yml:**
```yaml
version: '3.8'

services:
  mysql:
    image: mysql:8.0
    environment:
      MYSQL_ROOT_PASSWORD: rootpassword
      MYSQL_DATABASE: SavedByTheMaidProd
    ports:
      - "3306:3306"
    volumes:
      - mysql_data:/var/lib/mysql

  api:
    build:
      context: ./src
      dockerfile: SavedByTheMaid.Api/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Server=mysql;Port=3306;Database=SavedByTheMaidProd;User=root;Password=rootpassword;
      - Jwt__Secret=${JWT_SECRET}
    ports:
      - "5000:8080"
    depends_on:
      - mysql

  web:
    image: nginx:alpine
    volumes:
      - ./src/SavedByTheMaid.Web/dist:/usr/share/nginx/html
      - ./nginx.conf:/etc/nginx/nginx.conf
    ports:
      - "80:80"
    depends_on:
      - api

volumes:
  mysql_data:
```

**Ejecutar:**
```bash
# Build
docker-compose build

# Run
docker-compose up -d

# Logs
docker-compose logs -f api

# Aplicar migración
docker-compose exec mysql mysql -u root -p SavedByTheMaidProd < migration.sql
```

---

## 🔍 PASO 5: VERIFICACIÓN POST-DEPLOYMENT

### Health Checks:
```bash
# 1. API Health
curl https://your-api-domain.com/health
# Esperado: {"status":"healthy","timestamp":"...","version":"1.0.0"}

# 2. Autenticación
curl -X POST https://your-api-domain.com/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@savedbythemaid.com","password":"Admin123!"}'
# Esperado: {"token":"eyJ...","refreshToken":"..."}

# 3. Endpoint protegido
curl https://your-api-domain.com/api/admin/orders \
  -H "Authorization: Bearer <TOKEN>"
# Esperado: Lista de órdenes o 401 Unauthorized

# 4. Frontend
curl https://your-web-domain.com
# Esperado: HTML de la SPA
```

### Verificar Logs:
```bash
# Azure:
az webapp log tail --name savedbythemaid-api --resource-group SavedByTheMaid-RG

# Docker:
docker-compose logs -f api

# Archivos locales:
tail -f logs/savedbythemaid-*.log
```

### Verificar Application Insights (si configurado):
1. Ir a Azure Portal → Application Insights
2. Revisar "Live Metrics" (debe mostrar requests en tiempo real)
3. Revisar "Failures" (debe estar en 0%)
4. Revisar "Performance" (p95 < 2 segundos)

---

## 📊 PASO 6: CONFIGURAR MONITORING

### Application Insights Alerts (Azure):
```bash
# 1. Alerta de error rate
az monitor metrics alert create \
  --name "High Error Rate" \
  --resource-group SavedByTheMaid-RG \
  --scopes /subscriptions/.../resourceGroups/SavedByTheMaid-RG/providers/Microsoft.Insights/components/savedbythemaid-insights \
  --condition "avg requests/failed > 10" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action email your@email.com

# 2. Alerta de response time
az monitor metrics alert create \
  --name "Slow Response Time" \
  --resource-group SavedByTheMaid-RG \
  --scopes ... \
  --condition "avg requests/duration > 2000" \
  --window-size 5m
```

### Queries Útiles en Application Insights:
```kusto
// Pricing mismatch events
traces
| where message contains "PRICING MISMATCH"
| project timestamp, message, customDimensions
| order by timestamp desc

// Rate limiting events
traces
| where message contains "RATE LIMIT EXCEEDED"
| summarize count() by bin(timestamp, 1h), tostring(customDimensions.IP)

// SoftReserveCleanupService executions
traces
| where message contains "Marked" and message contains "soft reserves"
| project timestamp, message
| order by timestamp desc

// Error rate by endpoint
requests
| where success == false
| summarize count() by name, resultCode
| order by count_ desc
```

---

## 🔄 PASO 7: BACKUP Y ROLLBACK

### Backup de Base de Datos:
```bash
# Manual backup
mysqldump -h your-mysql-host -u admin -p SavedByTheMaidProd > backup_$(date +%Y%m%d).sql

# Automated backup (cron job)
0 2 * * * mysqldump -h your-mysql-host -u admin -p SavedByTheMaidProd > /backups/backup_$(date +\%Y\%m\%d).sql

# Azure MySQL:
az mysql server backup list \
  --resource-group SavedByTheMaid-RG \
  --server-name savedbythemaid-mysql

# AWS RDS:
aws rds create-db-snapshot \
  --db-instance-identifier savedbythemaid-db \
  --db-snapshot-identifier savedbythemaid-snapshot-$(date +%Y%m%d)
```

### Rollback:
```bash
# Si algo falla, revertir migración
mysql -h your-mysql-host -u admin -p SavedByTheMaidProd

# Ejecutar Down migration:
ALTER TABLE ServiceOrders 
ADD COLUMN PaymentStatus int NOT NULL DEFAULT 0;

DROP INDEX IX_ServiceOrders_CreatedAt ON ServiceOrders;
DROP INDEX IX_ServiceOrders_OrderStatus_CreatedAt ON ServiceOrders;

# Restaurar código anterior
git checkout <previous-commit-hash>
dotnet publish -c Release -o ./publish
# Redeploy
```

---

## ✅ CHECKLIST FINAL

### Pre-Deployment:
- [ ] Migración de BD aplicada y verificada
- [ ] Secrets en Key Vault o variables de entorno
- [ ] Application Insights configurado
- [ ] CORS origins actualizados para producción
- [ ] HTTPS habilitado
- [ ] Backup de BD creado

### Post-Deployment:
- [ ] Health check responde 200
- [ ] Login funciona
- [ ] Crear soft reserve funciona
- [ ] Confirmar booking funciona
- [ ] Admin puede ver órdenes
- [ ] SoftReserveCleanupService está ejecutándose
- [ ] Logs en Application Insights
- [ ] Alertas configuradas

### Monitoring:
- [ ] Dashboard de Application Insights creado
- [ ] Alertas de error rate activas
- [ ] Alertas de pricing mismatch activas
- [ ] Backup automático configurado

---

## 🆘 TROUBLESHOOTING

### Problema: API retorna 500
```bash
# Verificar logs
tail -f logs/savedbythemaid-*.log
# O en Azure:
az webapp log tail --name savedbythemaid-api --resource-group SavedByTheMaid-RG

# Verificar connection string
# Debe tener formato correcto y credenciales válidas
```

### Problema: "PaymentStatus column not found"
```bash
# La migración no se aplicó
# Ejecutar manualmente:
mysql -h ... -u ... -p
ALTER TABLE ServiceOrders DROP COLUMN PaymentStatus;
```

### Problema: JWT tokens no funcionan
```bash
# Verificar secret tiene al menos 32 caracteres
# Verificar Issuer y Audience coinciden con config
# Verificar reloj del servidor (ClockSkew)
```

### Problema: CORS errors
```bash
# Verificar origins en appsettings.Production.json:
"Cors": {
  "AllowedOrigins": [
    "https://yourdomain.com",  // Sin trailing slash
    "https://www.yourdomain.com"
  ]
}
```

---

## 📞 SOPORTE

**Documentación:**
- QA Analysis: `QA_ANALYSIS_FINAL.md`
- Migration Plan: `MIGRATION_PLAN.md`

**Contacto:**
- GitHub Issues: [crear issue](https://github.com/yourorg/savedbythemaid/issues)
- Email: devops@savedbythemaid.com
