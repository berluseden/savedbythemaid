# SavedByTheMaid / ecoMaid

Cleaning service booking platform. Monorepo with .NET backend + React frontend.

## Workflow (always follow this)
1. **Before coding**: `WebSearch` for latest 2026 docs of the library/API being used
2. **After coding**: use the `validator` subagent to run build + type check + convention review
3. **After fixing**: commit and push — never leave changes only on the VM

## Tech Stack

- **Backend**: .NET 10, ASP.NET Core Web API, Clean Architecture
- **Frontend**: React 19 + TypeScript 5.9 + Vite 7 + TailwindCSS 4 + TanStack Query v5
- **Database**: MySQL 8.0 + EF Core (InMemory for tests)
- **Auth**: JWT with HttpOnly cookies, refresh token rotation, ASP.NET Identity
- **Infra**: Docker Compose, GCP VM (Ubuntu), nginx reverse proxy

## Project Structure

```
backend/
├── src/
│   ├── SavedByTheMaid.Api/          # Controllers, middleware, auth, Program.cs
│   ├── SavedByTheMaid.Application/  # DTOs, validators (FluentValidation), service interfaces
│   ├── SavedByTheMaid.Domain/       # Entities, enums, domain services
│   └── SavedByTheMaid.Infrastructure/ # EF Core DbContext, data seeder, migrations
├── tests/
│   └── SavedByTheMaid.Api.Tests/    # xUnit + FluentAssertions + Moq
├── SavedByTheMaid.sln
└── Dockerfile.api
frontend/
├── src/                             # React app source
├── public/
├── nginx-frontend.conf              # nginx with /api/ proxy
└── Dockerfile.frontend
docker-compose.yml                   # Orchestrates mysql + api + frontend
```

## Build & Test Commands

```bash
# Backend (from repo root)
dotnet build backend
dotnet test backend --no-build

# Frontend (from frontend/)
npm run dev          # Dev server
npx tsc --noEmit     # Type check
npm run build        # Production build

# Docker (from repo root)
docker compose --env-file .env up -d --build
```

## Key Architecture Decisions

- **Clean Architecture**: Domain has zero dependencies. Application references Domain. Infrastructure references both. Api references all.
- **SlotOccupancy model**: Anti-collision for employee scheduling (30-min granularity, UNIQUE constraint at DB level)
- **SoftReserve**: Temporary booking hold during checkout (expires after ~10 min)
- **BookingService**: Extracted from BookingController — all booking business logic lives in `Api/Services/BookingService.cs`
- **JSON serialization**: `PropertyNamingPolicy = CamelCase` in Program.cs — frontend expects camelCase
- **Validation**: FluentValidation with `ValidateAndReturnErrors` extension returning `ActionResult?`

## Code Style

- All code comments, log messages, and error strings in English only
- Use existing design tokens (brand, accent, etc.) from Tailwind config — no hardcoded hex colors
- Frontend uses `@/` path alias for imports
- Backend uses record types for DTOs
- Prefer `authStorage` from `@/shared/lib/auth-storage` (single source of truth for token management)

## Deployment

- Production VM: GCP `instancia-gratis-ubuntu` at `34.69.216.97`
- SSH: `ssh -i ~/.ssh/gcp_savedbythemaid eberlus@34.69.216.97`
- Deploy: `git push` then on VM: `git pull && docker compose --env-file .env up -d --build`
- `.env` required on VM (not in repo) — see `.env.example` for template
- Original DB credentials in git history (commit `131baa6`)
- DataSeeder auto-creates tables (SlotOccupancies, RefreshTokens, StatusHistories) on startup

## Common Gotchas

- Docker frontend nginx proxies `/api/` to backend — don't remove `nginx-frontend.conf`
- `appsettings.Production.json` has `AllowedHosts: "*"` for IP-based access
- `Jwt__Secret` env var (double underscore) maps to `Jwt:Secret` in .NET config
- The 401 interceptor in `api.ts` skips refresh for auth endpoints to prevent loops
- `vite.config.docker.ts` has stricter TS settings (`verbatimModuleSyntax`) than local dev

## Additional Context

- @README.md
- @.env.example
