import axios from 'axios';
import { pushToast } from '@/lib/toast';

const api = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json',
  },
});

// Helper to get token from either storage
const getToken = () => {
  return localStorage.getItem('token') || sessionStorage.getItem('token');
};

// Interceptor to add JWT token
api.interceptors.request.use((config) => {
  const token = getToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Interceptor to handle errors
api.interceptors.response.use(
  (response) => response,
  (error) => {
    // Normalize common errors into a user-friendly message on the error object
    const status = error.response?.status;
    let userMessage = '';

    if (!error.response) {
      userMessage = 'Could not connect to the server. Check your connection and try again.';
    } else if (status === 401) {
      // Clear tokens and redirect to login when unauthorized
      localStorage.removeItem('token');
      localStorage.removeItem('rememberMe');
      sessionStorage.removeItem('token');
      window.location.href = '/login';
      userMessage = 'Unauthorized. Please sign in.';
    } else if (status === 400) {
      userMessage = error.response?.data?.message
        || (error.response?.data?.errors && Object.values(error.response.data.errors).flat().join(' '))
        || 'Invalid request. Please check your input and try again.';
    } else if (status === 404) {
      userMessage = 'Resource not found.';
    } else if (status >= 500) {
      userMessage = 'Internal server error. Please try again later.';
    } else {
      userMessage = error.response?.data?.message || error.message || 'Unexpected error. Please try again.';
    }

    // Attach a friendly message for consumers (components/hooks)
    try {
      error.userMessage = userMessage;
    } catch {
      // ignore if readonly
    }

    // Also emit a global toast for user-friendly feedback
    try {
      if (userMessage) pushToast(userMessage, status && status >= 500 ? 'error' : 'warning');
    } catch {
      // swallow
    }

    return Promise.reject(error);
  }
);

// ==========================================
// Booking API
// ==========================================

export interface CoverageResponse {
  isCovered: boolean;
  serviceAreaId?: number;
  serviceAreaName?: string;
  city?: string;
  state?: string;
  county?: string;
  message: string;
}

export interface ServiceType {
  id: number;
  name: string;
  description: string;
  price: number;
  pricePerBedroom: number;
  pricePerBathroom: number;
  estimatedMinutes: number;
  minutesPerBedroom: number;
  minutesPerBathroom: number;
  iconUrl?: string;
}

export interface CleaningPlaceRoom {
  id: number;
  name: string;
  description?: string;
  baseMinutes: number;
  basePrice: number;
}

export interface CleaningPlace {
  id: number;
  name: string;
  description?: string;
  rooms: CleaningPlaceRoom[];
}

export interface RoomSelection {
  roomId: number;
  quantity: number;
}

export interface EstimateRequest {
  serviceTypeId: number;
  cleaningPlaceId?: number;
  rooms?: RoomSelection[];
  additionalServiceIds?: number[];
  bedrooms?: number;
  bathrooms?: number;
  squareFootage?: number;
  dirtLevel?: string;
  hasPets?: boolean;
  hasElevator?: boolean;
  isFirstTime?: boolean;
  recurrenceType?: string;
}

export interface EstimateResponse {
  estimatedMinutes: number;
  formattedDuration: string;
  subtotal: number;
  discount: number;
  total: number;
  recurrenceType: string;
  discountPercent: number;
}

export interface AvailabilityResponse {
  date: string;
  zipCode: string;
  serviceAreaId: number;
  slots: TimeSlotDto[];
  totalSlotsAvailable: number;
}

export interface TimeSlotDto {
  date: string;
  startTime: string;
  endTime: string;
  formattedTime: string;
  availableEmployeeIds: number[];
}

export interface SoftReserveRequest {
  date: Date;
  startTime: string;
  estimatedMinutes: number;
  zipCode: string;
  employeeId: number;
  customerId?: string;
  sessionId?: string;
}

export interface SoftReserveResponse {
  softReserveId: number;
  sessionId: string;
  scheduledStart: string;
  scheduledEnd: string;
  expiresAt: string;
  ttlSeconds: number;
  message: string;
}

export interface ConfirmBookingRequest {
  softReserveId: number;
  sessionId: string;
  customerId?: string;
  
  // Address
  zipCode: string;
  address: string;
  addressLine2?: string;
  city?: string;
  state?: string;
  
  // Service
  serviceTypeId: number;
  cleaningPlaceId?: number;
  bedrooms: number;
  bathrooms: number;
  squareFootage?: number;
  dirtLevel?: string;
  hasPets?: boolean;
  floorLevel?: number;
  hasElevator?: boolean;
  additionalServiceIds?: number[];
  
  // Pricing
  subtotal: number;
  tax: number;
  discount: number;
  total: number;
  
  // Recurrence
  recurrenceType?: string;
  recurrenceEndDate?: string;
  
  // Contact
  contactName?: string;
  contactPhone?: string;
  contactEmail: string;
  password?: string;
  specialInstructions?: string;
}

export interface BookingConfirmation {
  orderId: number;
  meetId: number;
  confirmationNumber: string;
  scheduledStart: string;
  scheduledEnd: string;
  total: number;
  orderStatus: string;
  message: string;
  authToken?: {
    accessToken: string;
    refreshToken: string;
    expiresAt: string;
    isNewUser: boolean;
  };
  isGuest: boolean;
}

export interface AdditionalService {
  id: number;
  title: string;
  description?: string;
  price: number;
  additionalMinutes: number;
}

export interface RecurrenceDiscount {
  recurrenceType: string;
  recurrenceTypeName: string;
  discountPercent: number;
}

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

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName?: string;
  lastName?: string;
  phone?: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: UserInfo;
}

export interface UserInfo {
  id: string;
  email: string;
  firstName?: string;
  lastName?: string;
  phone?: string;
  roles: string[];
}

// Alias for backward compatibility
export type User = UserInfo;

export const authApi = {
  login: (request: LoginRequest) =>
    api.post<AuthResponse>('/auth/login', request),

  register: (request: RegisterRequest) =>
    api.post<AuthResponse>('/auth/register', request),

  me: () => api.get<UserInfo>('/auth/me'),

  checkEmail: (email: string) =>
    api.get<{ email: string; exists: boolean }>(`/auth/check-email?email=${encodeURIComponent(email)}`),

  logout: () => {
    localStorage.removeItem('token');
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('rememberMe');
    sessionStorage.removeItem('token');
  },
};

export default api;
