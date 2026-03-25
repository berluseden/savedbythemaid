import type { MapPin } from 'lucide-react';
import type { EstimateResponse, BookingConfirmation } from '@/lib/api';

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
  date: string;
  timeSlot: string;
  employeeId: number;
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
