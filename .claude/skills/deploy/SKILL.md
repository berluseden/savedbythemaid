---
name: deploy
description: Deploy the application to GCP production VM
---

# Deploy to Production

Deploy the current branch to GCP VM at 34.69.216.97.

## Steps

1. Verify all changes are committed: `git status`
2. Push to remote: `git push`
3. SSH to VM: `ssh -i ~/.ssh/gcp_savedbythemaid eberlus@34.69.216.97`
4. On VM: `git pull && docker compose --env-file .env up -d --build`
5. Verify health:
   - API: `curl http://34.69.216.97:5000/health`
   - Frontend: `curl -s -o /dev/null -w "%{http_code}" http://34.69.216.97:3000`
6. Check logs if anything fails: `docker compose logs --tail=50 api`

## Notes
- `.env` must exist on VM (not in repo) — see `.env.example`
- If DB connection fails, check original credentials in git history (commit `131baa6`)
- If 405 on /api/ routes, check `frontend/nginx-frontend.conf` is intact
