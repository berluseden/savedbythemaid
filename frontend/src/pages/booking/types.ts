import type { MapPin } from 'lucide-react';
import type { EstimateResponse, BookingConfirmation } from '@/lib/api';
import type { RoomSelection, DirtLevel } from '@/shared/types/api.types';

// Re-export API types used across booking steps
export type { EstimateResponse, BookingConfirmation };

export type BookingStep = 'zipcode' | 'service' | 'details' | 'schedule' | 'contact' | 'confirm';

export interface BookingData {
  zipCode: string;
  serviceTypeId: number;
  cleaningPlaceId: number;
  bedrooms: number;
  bathrooms: number;
  squareFootage: number;
  additionalServiceIds: number[];
  // Per-room selections from the cleaning-place catalog (room-based pricing)
  rooms: RoomSelection[];
  // Property modifiers feeding the pricing engine
  dirtLevel: DirtLevel;        // 0=Light, 1=Normal, 2=Heavy
  hasPets: boolean;
  hasElevator: boolean;
  floorLevel: number;          // 0 = ground floor / N/A
  isFirstTime: boolean;        // first-time-customer surcharge flag
  date: string;
  timeSlot: string;
  employeeId: number | null;
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  phone: string;
  address: string;
  city: string;
  state: string;
  specialInstructions: string;
  softReserveId?: number;
  sessionId?: string;
  expiresAt?: string;
}

export const initialBookingData: BookingData = {
  zipCode: '',
  serviceTypeId: 0,
  cleaningPlaceId: 0,
  bedrooms: 2,
  bathrooms: 1,
  squareFootage: 1000,
  additionalServiceIds: [],
  rooms: [],
  dirtLevel: 1,        // Normal
  hasPets: false,
  hasElevator: true,
  floorLevel: 0,
  isFirstTime: true,   // most bookings are first-time
  date: '',
  timeSlot: '',
  employeeId: 0,
  firstName: '',
  lastName: '',
  email: '',
  password: '',
  phone: '',
  address: '',
  city: '',
  state: '',
  specialInstructions: '',
};

export interface StepDefinition {
  id: BookingStep;
  title: string;
  icon: typeof MapPin;
}

export interface StepProps {
  data: BookingData;
  onUpdate: (updates: Partial<BookingData>) => void;
  onNext: () => void;
  onBack?: () => void;
}
