import axios from 'axios';

export const XSRF_HEADER_NAME = 'X-XSRF-TOKEN';

/** HTTP verbs that must carry the CSRF header. */
export const CSRF_METHODS = new Set(['POST', 'PUT', 'PATCH', 'DELETE']);

// In-memory storage for the request token (separate from the cookie token).
let requestToken: string | null = null;
let seedInFlight: Promise<void> | null = null;

// Generation counter — incremented on clearCsrfToken() so any in-flight
// seed request from a previous generation does not overwrite the current token.
let generation = 0;

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
  generation++;
}

/**
 * Ensures a CSRF request token is available. Safe to call multiple times —
 * concurrent callers share a single in-flight request and no-op once the
 * token is present. A generation counter ensures that a stale in-flight
 * request (started before a clearCsrfToken() call) cannot overwrite a
 * fresher token obtained after the clear.
 */
export async function ensureCsrfToken(): Promise<void> {
  if (requestToken) return;
  if (seedInFlight) return seedInFlight;

  const gen = generation;

  seedInFlight = axios
    .get<{ requestToken: string }>('/api/antiforgery/token', { withCredentials: true })
    .then((response) => {
      if (generation === gen) {
        requestToken = response.data.requestToken;
      }
      seedInFlight = null;
    })
    .catch((error) => {
      seedInFlight = null;
      console.warn('Failed to seed CSRF token', error);
    });

  return seedInFlight;
}
