/**
 * CSRF token helpers — ASP.NET Core antiforgery two-token pattern.
 *
 * ASP.NET Core antiforgery uses TWO distinct tokens:
 *  - Cookie token: set automatically in the XSRF-TOKEN cookie (not HttpOnly).
 *  - Request token: a separate value that must be sent as the X-XSRF-TOKEN header.
 *
 * Sending the cookie value as the header is WRONG and always fails validation.
 * The backend /api/antiforgery/token endpoint returns { requestToken } in the
 * response body — that value must be stored and sent as the header on mutations.
 *
 * The backend also renews both tokens after login / register / refresh.
 */

import axios from 'axios';

export const XSRF_HEADER_NAME = 'X-XSRF-TOKEN';

/** HTTP verbs that must carry the CSRF header. */
export const CSRF_METHODS = new Set(['POST', 'PUT', 'PATCH', 'DELETE']);

// In-memory storage for the request token (separate from the cookie token).
let requestToken: string | null = null;

/** Returns the current CSRF request token, or null if not yet seeded. */
export function getCsrfRequestToken(): string | null {
  return requestToken;
}

/** Stores the CSRF request token received from the backend. */
export function setCsrfRequestToken(token: string): void {
  requestToken = token;
}

/**
 * Clears the stored request token, forcing a re-fetch on the next mutation.
 * Call this after login/logout/register to ensure the rotated token is used.
 */
export function clearCsrfToken(): void {
  requestToken = null;
  seedInFlight = null;
}

let seedInFlight: Promise<void> | null = null;

/**
 * Ensures a CSRF request token is available. Safe to call multiple times —
 * concurrent callers share a single in-flight request and no-op once the
 * token is present.
 */
export async function ensureCsrfToken(): Promise<void> {
  if (requestToken) return;
  if (seedInFlight) return seedInFlight;

  seedInFlight = axios
    .get<{ requestToken: string }>('/api/antiforgery/token', { withCredentials: true })
    .then((response) => {
      requestToken = response.data.requestToken;
      seedInFlight = null;
    })
    .catch((error) => {
      seedInFlight = null;
      // Surface in console but don't throw — the next mutation will 403
      // with a clear backend error if the token truly never arrives.
      console.warn('Failed to seed CSRF token', error);
    });

  return seedInFlight;
}
