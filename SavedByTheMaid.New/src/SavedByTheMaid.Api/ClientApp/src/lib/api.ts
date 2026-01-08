import axios from 'axios';

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

// Interceptor para agregar token JWT
api.interceptors.request.use((config) => {
  const token = getToken();
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
      localStorage.removeItem('token');
      localStorage.removeItem('rememberMe');
      sessionStorage.removeItem('token');
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
}

export interface AdditionalService {
  id: number;
  title: string;
  description?: string;
  price: number;
  additionalMinutes: number;
}

export const bookingApi = {
  // Check coverage
  checkCoverage: (zipCode: string) =>
    api.get<CoverageResponse>(`/booking/coverage/${zipCode}`),

  // Get service types
  getServiceTypes: () => api.get<ServiceType[]>('/booking/service-types'),

  // Get cleaning places
  getCleaningPlaces: () => api.get<CleaningPlace[]>('/booking/cleaning-places'),

  // Get additional services
  getAdditionalServices: () => api.get<AdditionalService[]>('/booking/additional-services'),

  // Get estimate
  getEstimate: (request: EstimateRequest) =>
    api.post<EstimateResponse>('/booking/estimate', request),

  // Get availability
  getAvailability: (zipCode: string, date: string, estimatedMinutes: number) =>
    api.post<AvailabilityResponse>('/booking/availability', {
      zipCode,
      date,
      estimatedMinutes,
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
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
  },
};

export default api;
