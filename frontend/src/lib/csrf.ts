/**
 * CSRF token helpers — double-submit cookie pattern.
 *
 * Backend (ASP.NET Core antiforgery) sets an `XSRF-TOKEN` cookie that is
 * READABLE from JS (not HttpOnly). On every mutating request we copy its
 * value into the `X-XSRF-TOKEN` header; the server compares both and
 * rejects the request if they don't match.
 *
 * The backend renews the cookie after login / register / refresh; it also
 * exposes `GET /api/antiforgery/token` to seed the cookie for anonymous
 * users (so the first login POST has a valid token).
 */

import axios from 'axios';

export const XSRF_COOKIE_NAME = 'XSRF-TOKEN';
export const XSRF_HEADER_NAME = 'X-XSRF-TOKEN';

/** HTTP verbs that must carry the CSRF header. */
export const CSRF_METHODS = new Set(['POST', 'PUT', 'PATCH', 'DELETE']);

/** Reads the current XSRF cookie value or null if absent. */
export function getXsrfCookie(): string | null {
  if (typeof document === 'undefined') return null;
  const prefix = `${XSRF_COOKIE_NAME}=`;
  const raw = document.cookie
    .split(';')
    .map((c) => c.trim())
    .find((c) => c.startsWith(prefix));
  if (!raw) return null;
  try {
    return decodeURIComponent(raw.slice(prefix.length));
  } catch {
    return raw.slice(prefix.length);
  }
}

let seedInFlight: Promise<void> | null = null;

/**
 * Ensures an XSRF cookie is present. Safe to call multiple times —
 * concurrent callers share a single in-flight request and no-op once a
 * cookie exists.
 */
export async function ensureCsrfToken(): Promise<void> {
  if (getXsrfCookie()) return;
  if (seedInFlight) return seedInFlight;

  seedInFlight = axios
    .get('/api/antiforgery/token', { withCredentials: true })
    .then(() => {
      seedInFlight = null;
    })
    .catch((error) => {
      seedInFlight = null;
      // Surface in console but don't throw — the next mutation will 403
      // with a clear backend error if the cookie truly never arrives.
      console.warn('Failed to seed CSRF token', error);
    });

  return seedInFlight;
}
