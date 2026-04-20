// Centralized API types for the ecoMaid frontend

// ==========================================
// Error types
// ==========================================

export interface ApiErrorResponse {
  message?: string;
  details?: string;
  errors?: Record<string, string[]>;
  statusCode?: number;
}

export class ApiError extends Error {
  readonly statusCode: number;
  readonly userMessage: string;
  readonly details?: string;
  readonly fieldErrors?: Record<string, string[]>;

  constructor(
    statusCode: number,
    userMessage: string,
    details?: string,
    fieldErrors?: Record<string, string[]>,
  ) {
    super(userMessage);
    this.name = 'ApiError';
    this.statusCode = statusCode;
    this.userMessage = userMessage;
    this.details = details;
    this.fieldErrors = fieldErrors;
  }
}

// ==========================================
// Booking types
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

// 0 = Light, 1 = Normal, 2 = Heavy
export type DirtLevel = 0 | 1 | 2;

export interface EstimateRequest {
  serviceTypeId: number;
  cleaningPlaceId?: number;
  rooms?: RoomSelection[];
  additionalServiceIds?: number[];
  bedrooms?: number;
  bathrooms?: number;
  squareFootage?: number;
  dirtLevel?: DirtLevel;
  hasPets?: boolean;
  hasElevator?: boolean;
  isFirstTime?: boolean;
}

export interface PriceLineItem {
  label: string;
  amount: number;
}

export interface EstimateResponse {
  estimatedMinutes: number;
  formattedDuration: string;
  subtotal: number;
  discount: number;
  total: number;
  discountPercent: number;
  lineItems: PriceLineItem[];
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
  dirtLevel?: DirtLevel;
  hasPets?: boolean;
  floorLevel?: number;
  hasElevator?: boolean;
  isFirstTime?: boolean;
  additionalServiceIds?: number[];
  rooms?: RoomSelection[];

  // Pricing
  subtotal: number;
  tax: number;
  discount: number;
  total: number;

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

// ==========================================
// Auth types
// ==========================================

export interface LoginRequest {
  email: string;
  password: string;
  rememberMe?: boolean;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName?: string;
  lastName?: string;
  phone?: string;
}

/**
 * Auth response shape.
 * Backend sets access + refresh tokens as HttpOnly cookies — NEVER in the body.
 * Client only receives session metadata + the user profile.
 */
export interface AuthResponse {
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

// ==========================================
// Customer order types
// ==========================================

export interface CustomerOrderDto {
  id: number;
  serviceTypeName: string;
  serviceAreaName: string;
  cleaningPlaceName: string;
  address: string;
  city: string;
  state: string;
  zipCode: string;
  scheduledDate: string | null;
  scheduledTime: string | null;
  estimatedDuration: number;
  totalAmount: number;
  status: string;
  recurrenceType: string;
  createdAt: string;
  specialInstructions?: string | null;
}

export interface CustomerOrdersResponse {
  items: CustomerOrderDto[];
  totalItems: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
