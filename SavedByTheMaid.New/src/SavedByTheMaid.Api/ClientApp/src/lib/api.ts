import axios from 'axios';
import { pushToast } from '@/lib/toast';
import { authStorage } from '@/shared/lib/auth-storage';
import { getErrorMessage } from '@/shared/lib/error-utils';

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
});

// Interceptor to add JWT token (fallback for backward compatibility)
// With HttpOnly cookies, the browser sends the token automatically.
// This interceptor handles the legacy case where tokens are in storage.
api.interceptors.request.use((config) => {
  const token = authStorage.getToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Token refresh state — shared across concurrent 401 responses
let isRefreshing = false;
let refreshSubscribers: ((token: string) => void)[] = [];

function onTokenRefreshed(token: string) {
  refreshSubscribers.forEach(cb => cb(token));
  refreshSubscribers = [];
}

function addRefreshSubscriber(cb: (token: string) => void) {
  refreshSubscribers.push(cb);
}

// Interceptor to handle errors
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const status = error.response?.status;
    const originalRequest = error.config;

    // --- 401: attempt token refresh before giving up ---
    const requestUrl = originalRequest.url || '';
    const isAuthEndpoint = requestUrl.includes('/auth/me') || requestUrl.includes('/auth/refresh') || requestUrl.includes('/auth/login');

    if (status === 401 && !originalRequest._retry && !isAuthEndpoint) {
      if (isRefreshing) {
        return new Promise((resolve) => {
          addRefreshSubscriber((token: string) => {
            originalRequest.headers.Authorization = `Bearer ${token}`;
            resolve(api(originalRequest));
          });
        });
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        const res = await axios.post('/api/auth/refresh', {}, { withCredentials: true });
        const newToken = res.data.accessToken;
        if (newToken) {
          authStorage.setToken(newToken, authStorage.isRemembered());
          api.defaults.headers.common.Authorization = `Bearer ${newToken}`;
          onTokenRefreshed(newToken);
          originalRequest.headers.Authorization = `Bearer ${newToken}`;
          return api(originalRequest);
        }
      } catch {
        // Refresh failed — silent, let original 401 propagate
      } finally {
        isRefreshing = false;
      }
    }

    // For auth endpoints, just reject silently (AuthContext handles the state)
    if (status === 401 && isAuthEndpoint) {
      return Promise.reject(error);
    }

    // --- 403: access denied ---
    if (status === 403) {
      pushToast('Access denied. You do not have permission for this action.', 'warning');
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

  logout: () => api.post('/auth/logout').catch(() => {}).finally(() => {
    authStorage.clear();
  }),
};

export { api };
export default api;
