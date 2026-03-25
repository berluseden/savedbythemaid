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
// Re-exports from lib/api.ts
// ==========================================

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
} from '@/lib/api';
