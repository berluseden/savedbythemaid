# Architecture Rules

## Dependency Flow (Clean Architecture)
Domain → (no deps) | Application → Domain | Infrastructure → Application + Domain | Api → all

Never add a reference that breaks this flow.

## Key Services
- `BookingService` in `backend/src/SavedByTheMaid.Api/Services/` — all booking logic lives here, not in the controller
- `SchedulingService` — slot conflict detection + SlotOccupancy management
- `DataSeeder` — runs on startup, idempotent, creates missing tables via raw SQL (`CREATE TABLE IF NOT EXISTS`)

## Database
- No EF migrations — DataSeeder owns table creation
- If you add a new table, add it to DataSeeder
- SlotOccupancies: UNIQUE constraint at DB level — no application-level dedup needed

## Auth
- JWT stored in HttpOnly cookies — never localStorage
- Refresh token rotation via RefreshTokens table
- `Jwt__Secret` env var (double underscore) maps to `Jwt:Secret` in .NET config
