import axios from 'axios';

const api = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json',
  },
});

// Interceptor para agregar token JWT
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Interceptor para manejar errores
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('accessToken');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

// ==========================================
// Booking API
// ==========================================

export interface CoverageResponse {
  zipCode: string;
  isCovered: boolean;
  message: string;
}

export interface ServiceType {
  id: number;
  name: string;
  description: string;
  basePrice: number;
  estimatedMinutes: number;
  iconUrl?: string;
}

export interface CleaningPlace {
  id: number;
  name: string;
  description: string;
  iconUrl?: string;
}

export interface EstimateRequest {
  serviceTypeId: number;
  cleaningPlaceId: number;
  numberOfRooms: number;
  numberOfBathrooms: number;
  squareFeet: number;
  additionalServiceIds?: number[];
}

export interface EstimateResponse {
  basePrice: number;
  additionalServicesPrice: number;
  subtotal: number;
  tax: number;
  total: number;
  estimatedMinutes: number;
  breakdown: PriceBreakdownItem[];
}

export interface PriceBreakdownItem {
  description: string;
  amount: number;
}

export interface TimeSlot {
  startTime: string;
  endTime: string;
  isAvailable: boolean;
}

export interface AvailabilityResponse {
  date: string;
  timeSlots: TimeSlot[];
}

export interface SoftReserveRequest {
  date: string;
  timeSlot: string;
  serviceTypeId: number;
  cleaningPlaceId: number;
  estimatedMinutes: number;
  zipCode: string;
}

export interface SoftReserveResponse {
  reservationToken: string;
  expiresAt: string;
  holdMinutes: number;
}

export interface ConfirmBookingRequest {
  reservationToken: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  address: string;
  city: string;
  state: string;
  zipCode: string;
  specialInstructions?: string;
  serviceTypeId: number;
  cleaningPlaceId: number;
  numberOfRooms: number;
  numberOfBathrooms: number;
  squareFeet: number;
  additionalServiceIds?: number[];
}

export interface BookingConfirmation {
  orderId: number;
  confirmationNumber: string;
  status: string;
  scheduledDate: string;
  scheduledTime: string;
  totalAmount: number;
  message: string;
}

export const bookingApi = {
  // Check coverage
  checkCoverage: (zipCode: string) =>
    api.get<CoverageResponse>(`/booking/coverage/${zipCode}`),

  // Get service types
  getServiceTypes: () => api.get<ServiceType[]>('/booking/service-types'),

  // Get cleaning places
  getCleaningPlaces: () => api.get<CleaningPlace[]>('/booking/cleaning-places'),

  // Get estimate
  getEstimate: (request: EstimateRequest) =>
    api.post<EstimateResponse>('/booking/estimate', request),

  // Get availability
  getAvailability: (zipCode: string, date: string, estimatedMinutes: number) =>
    api.get<AvailabilityResponse>(`/booking/availability`, {
      params: { zipCode, date, estimatedMinutes },
    }),

  // Create soft reservation
  createSoftReserve: (request: SoftReserveRequest) =>
    api.post<SoftReserveResponse>('/booking/soft-reserve', request),

  // Confirm booking
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

export const authApi = {
  login: (request: LoginRequest) =>
    api.post<AuthResponse>('/auth/login', request),

  register: (request: RegisterRequest) =>
    api.post<AuthResponse>('/auth/register', request),

  me: () => api.get<UserInfo>('/auth/me'),

  logout: () => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
  },
};

export default api;
