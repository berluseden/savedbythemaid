# EF Core Migrations — Workflow

This project owns its schema via **EF Core migrations**. The startup
pipeline applies any pending migrations before `DataSeeder` runs, so
nothing manual is required after a deploy.

## One-time setup (per developer / CI agent)

```bash
cd backend
dotnet tool restore           # installs the pinned dotnet-ef from .config/dotnet-tools.json
```

If running inside Docker (recommended — no .NET 10 SDK needed locally):

```bash
docker run --rm -v "$PWD:/src" -w /src mcr.microsoft.com/dotnet/sdk:10.0 \
  bash -c "dotnet tool restore && dotnet ef --version"
```

## Generating the initial baseline (first time ever)

The repository ships **without** any migration files yet. Run this once
to scaffold the baseline from the current `ApplicationDbContext` model:

```bash
cd backend
dotnet ef migrations add InitialCreate \
  -p src/SavedByTheMaid.Infrastructure \
  -s src/SavedByTheMaid.Api \
  -o Migrations
```

This creates `src/SavedByTheMaid.Infrastructure/Migrations/<timestamp>_InitialCreate.cs`
plus the snapshot file. **Commit both.**

The next time the API starts, `ApplyDatabaseMigrationsAsync` will apply
the migration to whatever DB it points at.

## Adding a new migration (every schema change)

1. Modify your entity classes in `SavedByTheMaid.Domain/Entities/` (or
   `OnModelCreating` in `ApplicationDbContext`).
2. Generate the migration:

   ```bash
   dotnet ef migrations add <DescriptiveName> \
     -p src/SavedByTheMaid.Infrastructure \
     -s src/SavedByTheMaid.Api
   ```

3. **Review the generated `Up()` / `Down()`** — EF guesses well but renames
   look like drop+add. Adjust if needed.
4. Commit the migration file + snapshot.
5. Deploy. `Database.MigrateAsync()` runs it automatically on next boot.

## Rolling back a migration in dev

```bash
# Revert to the migration BEFORE the bad one (keeps the file but undoes the SQL):
dotnet ef database update <PreviousMigrationName> \
  -p src/SavedByTheMaid.Infrastructure -s src/SavedByTheMaid.Api

# Then remove the bad migration file:
dotnet ef migrations remove \
  -p src/SavedByTheMaid.Infrastructure -s src/SavedByTheMaid.Api
```

## Generating an idempotent SQL script (for manual prod deploys)

```bash
dotnet ef migrations script --idempotent \
  -p src/SavedByTheMaid.Infrastructure \
  -s src/SavedByTheMaid.Api \
  -o migration.sql
```

Run `migration.sql` against the prod database from any MySQL client; it
will skip already-applied migrations.

## Connection string for design-time

`ApplicationDbContextFactory` reads `ConnectionStrings__DefaultConnection`
or `EF_CONNECTION_STRING` env var. EF doesn't actually open the connection
when scaffolding migrations — the placeholder fallback is only used to
construct the `DbContextOptions`.

For commands that DO touch the DB (e.g. `database update`) export the
real one first:

```bash
export ConnectionStrings__DefaultConnection="Server=...;Database=...;User=...;Password=...;"
dotnet ef database update -p src/SavedByTheMaid.Infrastructure -s src/SavedByTheMaid.Api
```

## What changed (relative to the previous "raw SQL" approach)

- `DataSeeder.EnsureAdditionalTablesAsync` was **deleted** — schema is no
  longer created by `CREATE TABLE IF NOT EXISTS` blocks. EF migrations
  own everything.
- `DatabaseExtensions.ApplyManualFixesAsync` was **deleted** — the manual
  index patches lived there because the schema couldn't drop the legacy
  `PaymentStatus` column. The first migration captures the canonical
  shape of every table; no patches needed.
- `DataSeeder.SeedAllAsync` still runs — it now seeds **only data**
  (admin user, roles, master catalog), never schema.
