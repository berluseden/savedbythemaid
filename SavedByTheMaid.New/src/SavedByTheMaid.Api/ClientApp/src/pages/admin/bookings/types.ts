import type { OrderStatus, MeetStatus, PaymentStatus } from '@/shared/lib/status-config';

export interface OrderSummary {
  id: number;
  confirmationNumber: string;
  contactName: string | null;
  contactPhone: string | null;
  address: string;
  city: string | null;
  zipCode: string;
  serviceAreaName: string | null;
  serviceTypeName: string | null;
  total: number;
  paymentStatus: PaymentStatus;
  orderStatus: OrderStatus;
  recurrenceType: string;
  createdAt: string;
}

export interface MeetingSummary {
  id: number;
  orderId: number;
  scheduledStart: string;
  scheduledEnd: string;
  actualStart: string | null;
  actualEnd: string | null;
  employeeId: number | null;
  employeeName: string | null;
  status: MeetStatus;
  estimatedDurationMinutes: number;
}

export interface Employee {
  id: number;
  firstName: string;
  lastName: string;
  isActive: boolean;
}

export const formatCurrency = (amount: number) =>
  new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(amount);

export const formatDate = (date: string) =>
  new Date(date).toLocaleDateString('en-US', {
    weekday: 'short',
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });

export const formatDateTime = (dateStr: string) => {
  const date = new Date(dateStr);
  return date.toLocaleString('en-US', {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
};
