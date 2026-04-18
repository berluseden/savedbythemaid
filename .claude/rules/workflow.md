# Development Workflow Rules

## Before Any Code Change

**Always search for latest documentation before implementing:**

1. Use `WebSearch` to find the latest docs (2025/2026) for the library or feature you're about to use
2. Verify the API/syntax hasn't changed — especially for:
   - TailwindCSS v4 (`@theme`, `@tailwindcss/vite` — different from v3)
   - TanStack Query v5 (breaking changes from v4: `useQuery` options format)
   - .NET 10 (minimal API changes, new EF Core APIs)
   - React 19 (new hooks, compiler changes)
3. If docs show a newer pattern than what's in the codebase, use the newer pattern

## After Any Code Change

**Always run the validator agent:**
- Use the `validator` subagent to verify build + types + conventions
- Fix all issues before reporting the task as done
- Zero TypeScript errors and zero build errors required

## Tech Stack Versions (keep up to date)
- React: 19.x
- TypeScript: 5.9.x
- Vite: 7.x
- TailwindCSS: 4.x
- TanStack Query: 5.x
- .NET: 10.x
- Node: 20.x (Docker)
