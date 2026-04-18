# Deferred Technical Work — Re-Entry Criteria

This document tracks decisions to **postpone** specific refactors. Each
entry has the trigger that should pull it back into scope. The goal is
to avoid both premature investment and forgotten debt.

---

## 1. Anemic Domain Model → Rich Domain

**Status:** Deferred (intentional, 2026-04 audit).

**Current state:** Entities (`ServiceOrder`, `ServiceMeet`, `Employee`,
etc.) are data bags. Behavior lives in:
- `Domain/Services/OrderStatusTransitions` + `MeetStatusTransitions`
  (state machines)
- `Api/Services/BookingService`, `SchedulingService`,
  `OrderCancellationService` (orchestration)

**Why deferred:**
Pre-PMF SaaS — the 2026 community consensus (Fowler, Milan Jovanović,
Microsoft Architecture guide) is "anemic is fine while business logic
is uncertain; refactor when invariants stabilize". We have not yet
validated the booking flow against real customers; reshaping entities
now would freeze the wrong abstractions.

**Re-enter when ANY of these is true:**
- A bug is filed where state was mutated bypassing the state machine
  (i.e. without going through `OrderStatusTransitions.Validate`).
  We already had one such bug in `OrderCancellationService` and patched
  it; if it recurs in a different service, that's the signal — entities
  must own their transitions.
- `OrderStatus` or `MeetStatus` exceeds 8 enum members or grows
  branching transitions (e.g. partial completion, dispute states).
- More than 3 services need to coordinate the same mutation
  (e.g. cancellation now lives in 1 place; if billing + audit + comms
  + workflow each replicate logic, time to centralize in the entity).
- Onboarding a new dev takes >1h to figure out where a status change
  "really" happens.

**Proposed approach when re-entered:**
- Keep entities as `class` (not `record`) so encapsulation works.
- Move state-changing methods inside (`order.Cancel(reason, by)`,
  `meet.MarkOnTheWay()`).
- Make setters `private` for fields touched by those methods.
- The existing `*Transitions` static classes stay — call them from
  inside the entity methods.
- Migrate one aggregate at a time. ServiceOrder + ServiceMeet first.

---

## 2. Value Objects — Money + Address

**Status:** Partially done. Email + ZipCode shipped (Apr 2026) as
validation-only VOs (storage stays `string`, but
`Email.IsValid` / `ZipCode.IsValid` is the single source of truth used
by FluentValidation extensions). See
`SavedByTheMaid.Domain.ValueObjects` and
`SavedByTheMaid.Application.Validators.ValueObjectRules`.

**Money + Address deferred** because:
- `Money` requires deciding currency representation (single-currency
  decimal vs `(amount, ISO4217)` tuple). We are USD-only; refactor cost
  is high vs benefit.
- `Address` involves 5 fields (Street, City, State, ZipCode, Country),
  each with its own normalization. Affects ~15 entities and DTOs.

**Re-enter `Money` when:**
- We accept a second currency (e.g. CAD pilot in Toronto).
- Anyone files a rounding bug — `Money` would have controlled rounding
  in one place.

**Re-enter `Address` when:**
- We need address validation against USPS / Google Address Validation
  API. That validation will live cleanly in the VO.
- Multi-address support per customer (the audit flagged this as a
  customer-flow gap; that's also when `Address` becomes worth it).

**Proposed approach for Money:**
- `record Money(decimal Amount, string Currency = "USD")` with
  rounding helpers (banker's rounding for half-cents).
- EF Core `Complex Type` mapping (.NET 8+). MySQL may need `HasConversion`
  fallback if the provider rejects complex types.

---

## 3. GDPR / CCPA — Account Deletion + Data Export

**Status:** Deferred (intentional, 2026-04 audit).

**Why deferred:**
- **CCPA** applies to businesses with $26.6M revenue OR processing
  100k+ California residents. We are well under both.
- **GDPR** applies to EU data subjects. We serve US only.
- Other items (payments integration, employee app) are higher leverage
  for the next 6 months.

**Re-enter when ANY of these is true:**
- Annual revenue projects to cross $25M within 12 months.
- We acquire >50k California residents (~50% of the CCPA threshold —
  give ourselves 6mo runway).
- Any expansion plan to EU, UK, or Brazil (LGPD).
- A customer files a deletion request (we should still honor it
  manually via DBA in the meantime — log a procedure when this happens
  the first time).
- We enter B2B and a customer's procurement insists on data-handling
  guarantees in the contract.

**Proposed approach when re-entered:**
- `DELETE /api/customer/profile` → anonymize, do NOT hard-delete:
  - `ApplicationUser.Email` → `deleted-{guid}@redacted.invalid`
  - `FirstName/LastName/PhoneNumber/Address` → `null`
  - `PasswordHash/SecurityStamp` → `null` (no future logins)
  - `IsDeleted = true`
  - `ServiceOrder` rows kept for accounting + employee payroll history,
    but `CustomerId` left dangling (already nullable).
  - Log the request in a new `DataSubjectRequest` table for compliance audit.
- `GET /api/customer/profile/export` → JSON dump of profile + bookings
  + status history. Includes a header `Content-Disposition: attachment`.
- 30-day SLA (GDPR) / 45-day SLA (CCPA) — set up an alert on the
  `DataSubjectRequest` table.

**Cost when re-entered:** ~3-5h dev + 1h policy doc.

---

## 4. EF Core Migrations Adoption

**Status:** Infrastructure ready (Apr 2026), baseline migration
**not yet generated**.

**What's done:**
- `Microsoft.EntityFrameworkCore.Design` added to Infrastructure
  (Debug-only).
- `dotnet-ef` pinned in `backend/.config/dotnet-tools.json`.
- `ApplicationDbContextFactory` reads from env vars, no hardcoded
  password.
- `DataSeeder.EnsureAdditionalTablesAsync` deleted — schema is now
  EF's job.
- `DatabaseExtensions.ApplyDatabaseMigrationsAsync` simplified to
  `MigrateAsync` only.
- Workflow documented in `backend/MIGRATIONS.md`.

**Action needed:** A developer (you) must run the baseline once:

```bash
cd backend
docker run --rm -v "$PWD:/src" -w /src mcr.microsoft.com/dotnet/sdk:10.0 \
  bash -c "dotnet tool restore && dotnet ef migrations add InitialCreate \
    -p src/SavedByTheMaid.Infrastructure \
    -s src/SavedByTheMaid.Api \
    -o Migrations"
```

Commit the generated files. From then on, schema changes follow the
standard `dotnet ef migrations add <Name>` flow.

---

## 5. Pomelo MySQL Provider Migration

**Status:** Deferred. Currently using Oracle's
`MySql.EntityFrameworkCore` 10.0.0-rc.

**Why deferred:**
- Works for the current model.
- Pomelo has better community support, more EF Core features, and
  exposes `EnableRetryOnFailure` (Oracle's provider does not).

**Re-enter when:**
- We hit a feature wall (e.g. JSON column support, complex types,
  spatial types).
- Frequent transient connection errors that would benefit from
  `EnableRetryOnFailure` (currently mitigated by `WaitForDatabaseAsync`
  at startup but not by per-query retries).

---

## 6. Hangfire (Durable Email Queue)

**Status:** Deferred. Currently using `BackgroundService` + bounded
`Channel<EmailEnvelope>` (in-memory).

**Re-enter when:**
- We need a job dashboard (admin visibility into queued/failed jobs).
- We add a second background workflow (reminders, recurring billing,
  cleanup) and want a unified scheduler.
- We deploy to multiple instances and need the queue to be shared
  (current channel is per-process — emails can be processed twice or
  zero times if we scale out).
- Email volume crosses ~10k/day where in-process drains start to feel
  the pressure.
