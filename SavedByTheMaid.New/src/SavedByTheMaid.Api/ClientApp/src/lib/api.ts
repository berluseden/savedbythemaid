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

// Interceptor to handle errors
api.interceptors.response.use(
  (response) => response,
  (error) => {
    const status = error.response?.status;
    let userMessage: string;

    if (!error.response) {
      userMessage = 'Could not connect to the server. Check your connection and try again.';
    } else if (status === 401) {
      authStorage.clear();
      window.location.href = '/login';
      userMessage = 'Unauthorized. Please sign in.';
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

  logout: () => {
    authStorage.clear();
  },
};

export { api };
export default api;
