---
paths:
  - "backend/**/*.cs"
  - "frontend/**/*.{ts,tsx}"
---

# Code Style Rules

## General
- All comments, log messages, and error strings in English
- No commented-out code — delete it
- No TODO comments — file an issue instead

## Backend (.NET)
- DTOs use C# record types
- Controllers return `ActionResult<T>`
- Validation via `ValidateAndReturnErrors` extension (returns `ActionResult?`)
- JSON: CamelCase naming policy configured in Program.cs — do not override per-controller
- Auth: JWT HttpOnly cookies — never return tokens in response body

## Frontend (React/TypeScript)
- Use `@/` path alias — never relative `../` chains
- Type-only imports for Docker build: `import { type Foo } from '...'`
- Never `localStorage.setItem` directly — always `authStorage` from `@/shared/lib/auth-storage`
- Icons: lucide-react only — no other icon libraries
- Colors: Tailwind design tokens only — no hardcoded hex values
