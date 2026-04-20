# Deployment Rules

## Production — Hostinger VPS (primary)
- IP: `2.24.205.36`
- SSH: `ssh -i ~/.ssh/id_ed25519 root@2.24.205.36`
- Repo: `/opt/savedbythemaid`

## Deploy Flow — Hostinger
```bash
git push
ssh -i ~/.ssh/id_ed25519 root@2.24.205.36
cd /opt/savedbythemaid && git pull && docker compose -f docker-compose.yml -f docker-compose.prod.yml --env-file .env up -d --build
```

## GCP VM (legacy / staging)
- IP `34.69.216.97`
- SSH: `ssh -i ~/.ssh/gcp_savedbythemaid eberlus@34.69.216.97`
- Repo: `/opt/savedbythemaid`
- Deploy: `git pull && docker compose --env-file .env up -d --build`

## Critical: Always Commit First
**Never fix something only on the VM.** The VM is ephemeral — on next deploy it resets.
Any fix made on VM must also be committed to the repo.

## Docker Structure
- `backend/Dockerfile.api` — context: `./backend`
- `frontend/Dockerfile.frontend` — context: `./frontend`
- `nginx-frontend.conf` in `frontend/` proxies `/api/` to backend container

## Common Issues
- `Jwt__Secret` (double underscore) → `Jwt:Secret` in .NET — NOT `JwtSettings__Secret`
- MySQL env vars only apply on first volume creation — existing volumes keep old credentials
- `appsettings.Production.json` needs `AllowedHosts: "*"` for IP-based access
- 401 interceptor in `api.ts` skips refresh for auth endpoints to prevent loops
