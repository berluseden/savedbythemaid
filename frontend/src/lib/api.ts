import axios from 'axios';
import { pushToast } from '@/lib/toast';
import { getErrorMessage } from '@/shared/lib/error-utils';
import {
  CSRF_METHODS,
  XSRF_HEADER_NAME,
  ensureCsrfToken,
  clearCsrfToken,
  getCsrfRequestToken,
  setCsrfRequestToken,
} from '@/lib/csrf';

import type {
  CoverageResponse,
  ServiceType,
  CleaningPlace,
  EstimateRequest,
  EstimateResponse,
  AvailabilityResponse,
  SoftReserveRequest,
  SoftReserveResponse,
  ConfirmBookingRequest,
  BookingConfirmation,
  AdditionalService,
  RecurrenceDiscount,
  LoginRequest,
  RegisterRequest,
  AuthResponse,
  UserInfo,
} from '@/shared/types/api.types';

// Re-export all types for backward compatibility
export type {
  CoverageResponse,
  ServiceType,
  CleaningPlace,
  CleaningPlaceRoom,
  RoomSelection,
  EstimateRequest,
  EstimateResponse,
  AvailabilityResponse,
  TimeSlotDto,
  SoftReserveRequest,
  SoftReserveResponse,
  ConfirmBookingRequest,
  BookingConfirmation,
  AdditionalService,
  RecurrenceDiscount,
  LoginRequest,
  RegisterRequest,
  AuthResponse,
  UserInfo,
  User,
} from '@/shared/types/api.types';

const api = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true, // Send HttpOnly cookies automatically
  // Align axios defaults with the backend's antiforgery header so its
  // built-in xsrf handling also kicks in if the manual interceptor is bypassed.
  xsrfCookieName: 'XSRF-TOKEN',
  xsrfHeaderName: XSRF_HEADER_NAME,
});

// Request interceptor — attach CSRF request token on mutating requests.
// ASP.NET Core antiforgery uses two distinct tokens: the cookie token (set
// automatically) and the request token (returned by /api/antiforgery/token
// in the response body). Only the request token is valid as the header value.
api.interceptors.request.use(async (config) => {
  const method = (config.method || 'get').toUpperCase();
  if (!CSRF_METHODS.has(method)) return config;

  let token = getCsrfRequestToken();
  if (!token) {
    await ensureCsrfToken();
    token = getCsrfRequestToken();
  }
  if (token) {
    config.headers.set(XSRF_HEADER_NAME, token);
  }
  return config;
});

// Token refresh state — shared across concurrent 401 responses.
// Tokens live in HttpOnly cookies; we only need to coalesce the refresh call.
let isRefreshing = false;
let refreshSubscribers: ((ok: boolean, error?: unknown) => void)[] = [];

function onTokenRefreshed() {
  refreshSubscribers.forEach((cb) => cb(true));
  refreshSubscribers = [];
}

function onRefreshFailed(error: unknown) {
  refreshSubscribers.forEach((cb) => cb(false, error));
  refreshSubscribers = [];
}

function addRefreshSubscriber(cb: (ok: boolean, error?: unknown) => void) {
  refreshSubscribers.push(cb);
}

/**
 * Heuristic detection of an antiforgery failure coming back from the API.
 * The backend may answer 400 or 403; the body typically mentions
 * "antiforgery" or "CSRF". We check a few common shapes.
 */
function isCsrfFailure(error: {
  response?: { status?: number; data?: unknown; headers?: Record<string, string> };
}): boolean {
  const status = error.response?.status;
  if (status !== 400 && status !== 403) return false;

  const data = error.response?.data;
  const haystacks: string[] = [];
  if (typeof data === 'string') haystacks.push(data);
  else if (data && typeof data === 'object') {
    const obj = data as Record<string, unknown>;
    for (const key of ['message', 'title', 'detail', 'error', 'code']) {
      const v = obj[key];
      if (typeof v === 'string') haystacks.push(v);
    }
  }
  return haystacks.some((s) => /antiforgery|xsrf|csrf/i.test(s));
}

// Interceptor to handle errors
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const status = error.response?.status;
    const originalRequest = error.config;

    // --- 400/403 CSRF failure: re-seed the token and retry once ---
    // Common cause: backend rotated the antiforgery cookie (login/logout)
    // and an in-flight request carried the old one.
    if (originalRequest && !originalRequest._csrfRetry && isCsrfFailure(error)) {
      originalRequest._csrfRetry = true;
      try {
        // Force a fresh token regardless of current state.
        const response = await axios.get<{ requestToken: string }>('/api/antiforgery/token', { withCredentials: true });
        if (response.data?.requestToken) {
          setCsrfRequestToken(response.data.requestToken);
        }
      } catch {
        // If we cannot re-seed, fall through to the generic error path.
      }
      const fresh = getCsrfRequestToken();
      if (fresh) {
        originalRequest.headers = originalRequest.headers ?? {};
        originalRequest.headers[XSRF_HEADER_NAME] = fresh;
        return api(originalRequest);
      }
    }

    // --- 401: attempt token refresh before giving up ---
    const requestUrl = originalRequest.url || '';
    const isAuthEndpoint = requestUrl.includes('/auth/me') || requestUrl.includes('/auth/refresh') || requestUrl.includes('/auth/login');

    if (status === 401 && !originalRequest._retry && !isAuthEndpoint) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          addRefreshSubscriber((ok, refreshError) => {
            if (!ok) {
              reject(refreshError ?? error);
              return;
            }
            resolve(api(originalRequest));
          });
        });
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        // Backend sets new HttpOnly cookies on success — we do not read tokens.
        // Use the api instance so the CSRF interceptor attaches the X-XSRF-TOKEN header.
        // The _retry flag on originalRequest prevents the 401 interceptor from
        // triggering a second refresh cycle for this specific call.
        await api.post('/auth/refresh', {});
        // Re-seed the CSRF token: any auth cookie change can invalidate the
        // current request token, so fetch a fresh one before retrying.
        clearCsrfToken();
        await ensureCsrfToken();
        onTokenRefreshed();
        return api(originalRequest);
      } catch (refreshError) {
        onRefreshFailed(refreshError);
        if (typeof window !== 'undefined' && !window.location.pathname.startsWith('/login')) {
          window.location.assign('/login');
        }
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    // For auth endpoints, just reject silently (AuthContext handles the state)
    if (status === 401 && isAuthEndpoint) {
      return Promise.reject(error);
    }

    // --- 403: access denied or CSRF retry exhausted ---
    if (status === 403) {
      const msg = isCsrfFailure(error)
        ? 'Session error. Please refresh the page and try again.'
        : 'Access denied. You do not have permission for this action.';
      pushToast(msg, 'warning');
      return Promise.reject(error);
    }

    let userMessage: string;

    if (!error.response) {
      userMessage = 'Could not connect to the server. Check your connection and try again.';
    } else if (status === 400) {
      userMessage = getErrorMessage(error, 'Invalid request. Please check your input and try again.');
    } else if (status === 404) {
      userMessage = 'Resource not found.';
    } else if (status >= 500) {
      userMessage = 'Internal server error. Please try again later.';
    } else {
      userMessage = getErrorMessage(error, 'Unexpected error. Please try again.');
    }

    // Attach a friendly message for consumers (components/hooks)
    error.userMessage = userMessage;

    // Emit a global toast for user-friendly feedback
    if (userMessage) {
      pushToast(userMessage, status && status >= 500 ? 'error' : 'warning');
    }

    return Promise.reject(error);
  }
);

// ==========================================
// Booking API
// ==========================================

export const bookingApi = {
  checkCoverage: (zipCode: string) =>
    api.get<CoverageResponse>(`/booking/coverage/${zipCode}`),

  getServiceTypes: () => api.get<ServiceType[]>('/booking/service-types'),

  getCleaningPlaces: () => api.get<CleaningPlace[]>('/booking/cleaning-places'),

  getAdditionalServices: () => api.get<AdditionalService[]>('/booking/additional-services'),

  getRecurrenceDiscounts: () => api.get<RecurrenceDiscount[]>('/booking/recurrence-discounts'),

  getEstimate: (request: EstimateRequest) =>
    api.post<EstimateResponse>('/booking/estimate', request),

  getAvailability: (zipCode: string, date: string, estimatedMinutes: number) =>
    api.post<AvailabilityResponse>('/booking/availability', {
      zipCode,
      date,
      estimatedMinutes,
    }),

  createSoftReserve: (request: SoftReserveRequest) =>
    api.post<SoftReserveResponse>('/booking/soft-reserve', request),

  confirmBooking: (request: ConfirmBookingRequest) =>
    api.post<BookingConfirmation>('/booking/confirm', request),
};

// ==========================================
// Auth API
// ==========================================

export const authApi = {
  login: (request: LoginRequest) =>
    api.post<AuthResponse>('/auth/login', request),

  register: (request: RegisterRequest) =>
    api.post<AuthResponse>('/auth/register', request),

  me: () => api.get<UserInfo>('/auth/me'),

  checkEmail: (email: string) =>
    api.get<{ email: string; exists: boolean }>(`/auth/check-email?email=${encodeURIComponent(email)}`),

  logout: () => api.post('/auth/logout').catch((err) => console.warn('logout failed', err)),
};

export { api };
export default api;
