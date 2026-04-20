# Infrastructure Guide

## Production Server — Hostinger VPS

| | |
|---|---|
| **IP** | `2.24.205.36` |
| **OS** | Ubuntu 25.10 |
| **CPU** | 2 vCPU |
| **RAM** | 7.8 GB |
| **Disk** | 96 GB |
| **Repo path** | `/opt/savedbythemaid` |

### SSH Access

```bash
ssh -i ~/.ssh/id_ed25519 root@2.24.205.36
```

---

## Services (Docker Compose)

All services are defined in `docker-compose.prod.yml` — single standalone file, no `.env` required.

| Container | Image | Port | Description |
|---|---|---|---|
| `savedbythemaid-mysql-1` | `mysql:8.4` | internal only | Database |
| `savedbythemaid-api-1` | custom build | internal only | .NET 10 API |
| `savedbythemaid-frontend-1` | custom build | `80` → public | React + nginx |
| `savedbythemaid-mysql-backup-1` | `fradelg/mysql-cron-backup` | — | Daily backup at 2 AM |

MySQL and API are **not** exposed publicly — only the frontend on port 80 is accessible from outside.

---

## Deploy

```bash
# From local machine — push changes then deploy
git push
ssh -i ~/.ssh/id_ed25519 root@2.24.205.36 \
  "cd /opt/savedbythemaid && git pull && docker compose -f docker-compose.prod.yml up -d --build"

# Or SSH into the server first
ssh -i ~/.ssh/id_ed25519 root@2.24.205.36
cd /opt/savedbythemaid
git pull
docker compose -f docker-compose.prod.yml up -d --build
```

---

## Useful Commands

### Check status
```bash
docker ps --format 'table {{.Names}}\t{{.Status}}'
```

### View logs
```bash
# All services
docker compose -f docker-compose.prod.yml logs -f

# Specific service
docker logs savedbythemaid-api-1 --tail 50 -f
docker logs savedbythemaid-mysql-1 --tail 50 -f
```

### Restart a service
```bash
docker compose -f docker-compose.prod.yml restart api
```

### Stop everything
```bash
docker compose -f docker-compose.prod.yml down
```

---

## MySQL

### Configuration

Custom config mounted at `/etc/mysql/conf.d/custom.cnf` (source: `mysql-custom.cnf` in repo):

- `innodb_buffer_pool_size = 4G` (50% of RAM)
- `max_connections = 150`
- `slow_query_log = ON` — logs queries > 2s to `/var/log/mysql/slow.log`
- `utf8mb4` charset
- `skip_name_resolve = ON`

### Connect to MySQL shell
```bash
docker exec -it savedbythemaid-mysql-1 mysql -u root -pSTPmn4ccf5XnVGz0QExvs15q
```

### Slow query log
```bash
docker exec savedbythemaid-mysql-1 tail -f /var/log/mysql/slow.log
```

---

## Backups

Automated daily backup at **2:00 AM UTC** via the `mysql-backup` container.  
Stored in Docker volume `savedbythemaid_mysql_backups`. Retention: **7 days**.

### Check backup files
```bash
docker run --rm -v savedbythemaid_mysql_backups:/backup alpine ls -lh /backup
```

### Manual backup (on demand)
```bash
docker exec savedbythemaid-mysql-1 \
  mysqldump -u root -pSTPmn4ccf5XnVGz0QExvs15q --all-databases \
  | gzip > /root/manual-backup-$(date +%Y%m%d).sql.gz
```

### Restore from backup
```bash
# List available backups
docker run --rm -v savedbythemaid_mysql_backups:/backup alpine ls -lh /backup

# Restore a specific backup
gunzip < backup.sql.gz | \
  docker exec -i savedbythemaid-mysql-1 mysql -u root -pSTPmn4ccf5XnVGz0QExvs15q
```

---

## Firewall (UFW)

```
22/tcp   ALLOW   (SSH)
80/tcp   ALLOW   (HTTP)
443/tcp  ALLOW   (HTTPS — for future SSL)
```

Check status: `ufw status`

---

## Admin Account

- **URL:** `http://2.24.205.36/login`
- **Email:** `julendy20@gmail.com`
- Credentials stored in `docker-compose.prod.yml` under `AdminSeed__*`

---

## Health Checks

```bash
# Frontend
curl -o /dev/null -w '%{http_code}' http://2.24.205.36/

# API liveness
curl http://2.24.205.36/api/health/live

# API readiness (includes DB check)
curl http://2.24.205.36/api/health/ready
```

---

## Legacy Server (GCP)

The original GCP VM (`34.69.216.97`) is still running as staging/backup.

```bash
ssh -i ~/.ssh/gcp_savedbythemaid eberlus@34.69.216.97
cd /opt/savedbythemaid
git pull && docker compose --env-file .env up -d --build
```
