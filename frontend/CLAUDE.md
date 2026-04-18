# Frontend — React + TypeScript + TailwindCSS

## Dev Commands

```bash
npm run dev              # Vite dev server on :5173
npx tsc --noEmit         # Type check (local, lenient)
npm run build            # Production build (strict, uses vite.config.docker.ts in Docker)
```

## Conventions

- Use `@/` path alias for all imports (configured in tsconfig)
- Type-only imports required for Docker build: `import { type Foo } from '...'`
- Forms: React Hook Form + Zod schemas (in `shared/schemas/`)
- API calls: Axios via `@/lib/api.ts` (bookingApi, authApi) and `@/lib/api-endpoints.ts` (admin, customer, contact)
- State: TanStack Query for server state, React context for auth
- Icons: lucide-react only
- UI components: `@/components/ui/` (Button, Card, Alert, etc.) and `@/shared/components/ui/` (Dialog, AlertDialog)
- Auth tokens: always use `authStorage` from `@/shared/lib/auth-storage` — never `localStorage.setItem` directly

## Key Files

- `lib/api.ts` — Axios instance, interceptors (401 refresh, 403 handler), booking & auth APIs
- `lib/api-endpoints.ts` — Admin, customer, contact typed endpoints
- `contexts/AuthContext.tsx` — Auth state, login/register/logout
- `shared/types/api.types.ts` — All TypeScript interfaces matching backend DTOs
- `pages/BookingPage.tsx` — 6-step booking wizard (the main revenue flow)
- `pages/booking/` — Individual step components

## Testing

- Vitest + React Testing Library configured
- Run: `npx vitest run`
