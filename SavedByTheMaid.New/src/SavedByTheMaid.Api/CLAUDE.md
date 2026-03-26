# Backend — ASP.NET Core Web API

## Build & Test

```bash
dotnet build
dotnet test --no-build
dotnet run                # Starts on :5000 (dev) or :5000 (Docker)
```

## Architecture

Clean Architecture with 4 projects:
- **Api** — Controllers, middleware, Program.cs, DI registration
- **Application** — DTOs (record types), FluentValidation validators, service interfaces
- **Domain** — Entities, enums, domain services (OrderStateMachine, MeetStateMachine)
- **Infrastructure** — EF Core DbContext, DataSeeder, DatabaseExtensions

## Key Patterns

- Controllers return `ActionResult<T>` — validation uses `ValidateAndReturnErrors` extension
- BookingService in `Api/Services/` handles all booking logic (pricing, availability, confirmation)
- SchedulingService handles slot conflict detection and SlotOccupancy management
- DataSeeder runs on startup: creates missing tables, seeds master data (idempotent)
- JWT auth with HttpOnly cookies — `Jwt:Secret` config key, env var `Jwt__Secret`
- JSON: CamelCase naming policy + JsonStringEnumConverter configured in Program.cs

## Database

- MySQL 8.0 in Docker, EF Core InMemory for tests
- No EF migrations used — DataSeeder creates tables via raw SQL (CREATE TABLE IF NOT EXISTS)
- Key tables: ServiceOrders, ServiceMeets, SlotOccupancies, SoftReserves, RefreshTokens
