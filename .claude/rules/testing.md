---
paths:
  - "backend/**/*.cs"
  - "frontend/**/*.{test,spec}.{ts,tsx}"
---

# Testing Rules

## Backend (xUnit)
- Tests live in `backend/tests/SavedByTheMaid.Api.Tests/`
- Use EF Core InMemory — never mock the DbContext directly
- Use `FluentAssertions` for assertions (`BeLessThanOrEqualTo` not `BeLessOrEqualTo`)
- Use `Moq` for service mocks

## Frontend (Vitest)
- Run: `npx vitest run` from `frontend/`
- Tests in `__tests__/` folders next to source files
- Use React Testing Library — no Enzyme
- Mock API calls via `vi.mock('@/lib/api')`
