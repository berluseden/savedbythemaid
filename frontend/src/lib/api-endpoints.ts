/**
 * Typed API endpoint helpers.
 *
 * These provide a thin, typed layer over the axios instance exported from
 * `./api`. They do NOT replace the existing `bookingApi` / `authApi` objects
 * in api.ts -- those remain the canonical imports for booking & auth flows.
 *
 * This module centralises admin, customer, and contact endpoints that were
 * previously scattered as raw `api.get/post/put/delete` calls in page
 * components.
 */

import { api } from './api';
import { type CustomerOrdersResponse } from '@/shared/types/api.types';

// ==========================================
// Shared types
// ==========================================

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

// ==========================================
// Contact endpoint
// ==========================================

export interface ContactRequest {
  name: string;
  email: string;
  phone?: string;
  serviceType?: string;
  message: string;
}

export const contactApi = {
  send: (data: ContactRequest) => api.post('/contact', data),
};

// ==========================================
// Customer endpoints
// ==========================================

export interface CustomerStats {
  completedBookings: number;
  totalSpent: number;
  nextBooking: string | null;
  loyaltyPoints: number;
}

export interface CustomerBooking {
  id: number;
  orderStatus: string;
  scheduledStart: string;
  scheduledEnd: string;
  total: number;
  serviceTypeName: string;
  address: string;
  confirmationNumber: string;
  meetings: CustomerMeeting[];
}

export interface CustomerMeeting {
  id: number;
  status: string;
  scheduledStart: string;
  scheduledEnd: string;
  employeeName?: string;
}

export interface CustomerProfileUpdate {
  firstName: string;
  lastName: string;
  phone: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export const customerApi = {
  getOrders: (params?: { status?: string; page?: number; pageSize?: number }) =>
    api.get<CustomerOrdersResponse>('/customer/my-orders', { params }),

  getStats: () => api.get<CustomerStats>('/customer/stats'),

  updateProfile: (data: CustomerProfileUpdate) =>
    api.put('/customer/profile', data),

  cancelOrder: (orderId: number, reason: string) =>
    api.post(`/customer/my-orders/${orderId}/cancel`, { reason }),

  rescheduleOrder: (orderId: number, data: { newDate: string; newTime: string }) =>
    api.post(`/customer/my-orders/${orderId}/reschedule`, data),
};

// ==========================================
// Auth extended endpoints (beyond core login/register)
// ==========================================

export const authExtendedApi = {
  changePassword: (data: ChangePasswordRequest) =>
    api.post('/auth/change-password', data),

  forgotPassword: (email: string) =>
    api.post('/auth/forgot-password', { email }),

  resetPassword: (data: { token: string; newPassword: string }) =>
    api.post('/auth/reset-password', data),
};

// ==========================================
// Admin endpoints
// ==========================================

// --- Orders / Bookings ---

export interface AdminOrderSummary {
  id: number;
  orderStatus: string;
  scheduledStart: string;
  scheduledEnd: string;
  total: number;
  customerName: string;
  customerEmail: string;
  serviceTypeName: string;
  address: string;
  confirmationNumber: string;
  meetings: AdminMeeting[];
}

export interface AdminMeeting {
  id: number;
  status: string;
  scheduledStart: string;
  scheduledEnd: string;
  employeeId?: number;
  employeeName?: string;
}

export const adminOrdersApi = {
  updateStatus: (bookingId: number, orderStatus: string) =>
    api.put(`/admin/orders/${bookingId}/status`, { orderStatus }),

  cancel: (bookingId: number, reason: string) =>
    api.post(`/admin/orders/${bookingId}/cancel`, { reason }),

  assignMeeting: (meetingId: number, employeeId: number) =>
    api.put(`/admin/orders/meetings/${meetingId}/assign`, { employeeId }),

  updateMeetingStatus: (meetingId: number, status: string) =>
    api.put(`/admin/orders/meetings/${meetingId}/status`, { status }),
};

// --- Employees ---

export interface EmployeeFormData {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  hireDate?: string;
  isActive: boolean;
}

export interface ScheduleFormData {
  dayOfWeek: number;
  startTime: string;
  endTime: string;
}

export interface TimeOffData {
  startDate: string;
  endDate: string;
  type: number;
  reason?: string;
}

export const adminEmployeesApi = {
  create: (data: EmployeeFormData) =>
    api.post('/admin/employees', data),

  update: (id: number, data: EmployeeFormData) =>
    api.put(`/admin/employees/${id}`, data),

  delete: (id: number) =>
    api.delete(`/admin/employees/${id}`),

  // Schedules
  addSchedule: (employeeId: number, data: ScheduleFormData) =>
    api.post(`/admin/employees/${employeeId}/schedules`, data),

  deleteSchedule: (employeeId: number, dayOfWeek: number) =>
    api.delete(`/admin/employees/${employeeId}/schedules/${dayOfWeek}`),

  // Time-off
  addTimeOff: (employeeId: number, data: TimeOffData) =>
    api.post(`/admin/employees/${employeeId}/time-off`, data),

  deleteTimeOff: (employeeId: number, timeOffId: number) =>
    api.delete(`/admin/employees/${employeeId}/time-off/${timeOffId}`),

  // Service areas
  addServiceArea: (employeeId: number, serviceAreaId: number, isPrimary: boolean) =>
    api.post(`/admin/employees/${employeeId}/service-areas/${serviceAreaId}?isPrimary=${isPrimary}`),

  removeServiceArea: (employeeId: number, serviceAreaId: number) =>
    api.delete(`/admin/employees/${employeeId}/service-areas/${serviceAreaId}`),

  setPrimaryServiceArea: (employeeId: number, serviceAreaId: number) =>
    api.post(`/admin/employees/${employeeId}/service-areas/${serviceAreaId}?isPrimary=true`),

  // Equipment
  addEquipment: (employeeId: number, equipmentId: number) =>
    api.post(`/admin/employees/${employeeId}/equipment/${equipmentId}`),

  removeEquipment: (employeeId: number, equipmentId: number) =>
    api.delete(`/admin/employees/${employeeId}/equipment/${equipmentId}`),

  toggleEquipmentAvailability: (employeeId: number, equipmentId: number, isAvailable: boolean) =>
    api.post(`/admin/employees/${employeeId}/equipment/${equipmentId}?isAvailable=${isAvailable}`),
};

// --- Services ---

export interface ServiceTypeFormData {
  name: string;
  description: string;
  price: number;
  pricePerBedroom: number;
  pricePerBathroom: number;
  estimatedMinutes: number;
  minutesPerBedroom: number;
  minutesPerBathroom: number;
  isActive?: boolean;
}

export const adminServicesApi = {
  create: (data: ServiceTypeFormData) =>
    api.post('/admin/service-types', data),

  update: (id: number, data: Partial<ServiceTypeFormData>) =>
    api.put(`/admin/service-types/${id}`, data),

  delete: (id: number) =>
    api.delete(`/admin/service-types/${id}`),
};

// --- Additional Services ---

export interface AdditionalServiceFormData {
  title: string;
  description?: string;
  price: number;
  additionalMinutes: number;
  isActive?: boolean;
}

export const adminAdditionalServicesApi = {
  create: (data: AdditionalServiceFormData) =>
    api.post('/admin/additionalservices', data),

  update: (id: number, data: Partial<AdditionalServiceFormData>) =>
    api.put(`/admin/additionalservices/${id}`, data),

  delete: (id: number) =>
    api.delete(`/admin/additionalservices/${id}`),
};

// --- Cleaning Places ---

export interface CleaningPlaceFormData {
  name: string;
  description?: string;
  isActive?: boolean;
}

export interface CleaningPlaceRoomFormData {
  name: string;
  description?: string;
  baseMinutes: number;
  basePrice: number;
  isActive?: boolean;
}

export const adminCleaningPlacesApi = {
  create: (data: CleaningPlaceFormData) =>
    api.post('/admin/cleaningplaces', data),

  update: (id: number, data: CleaningPlaceFormData) =>
    api.put(`/admin/cleaningplaces/${id}`, data),

  delete: (id: number) =>
    api.delete(`/admin/cleaningplaces/${id}`),

  // Rooms
  createRoom: (placeId: number, data: CleaningPlaceRoomFormData) =>
    api.post(`/admin/cleaningplaces/${placeId}/rooms`, data),

  updateRoom: (placeId: number, roomId: number, data: CleaningPlaceRoomFormData) =>
    api.put(`/admin/cleaningplaces/${placeId}/rooms/${roomId}`, data),

  deleteRoom: (placeId: number, roomId: number) =>
    api.delete(`/admin/cleaningplaces/${placeId}/rooms/${roomId}`),
};

// --- Pricing / Price Multipliers ---

export interface PriceMultiplierFormData {
  name: string;
  condition: string;
  conditionValue: string;
  multiplier: number;
  isActive?: boolean;
}

export interface RecurrenceDiscountFormData {
  recurrenceType: string;
  discountPercent: number;
}

export const adminPricingApi = {
  createMultiplier: (data: PriceMultiplierFormData) =>
    api.post('/admin/pricemultipliers', data),

  updateMultiplier: (id: number, data: PriceMultiplierFormData) =>
    api.put(`/admin/pricemultipliers/${id}`, data),

  deleteMultiplier: (id: number) =>
    api.delete(`/admin/pricemultipliers/${id}`),

  saveRecurrenceDiscount: (data: RecurrenceDiscountFormData) =>
    api.post('/admin/pricemultipliers/recurrence-discounts', data),
};

// --- Service Areas ---

export interface ServiceAreaFormData {
  name: string;
  description?: string;
  isActive?: boolean;
}

export const adminServiceAreasApi = {
  create: (data: ServiceAreaFormData) =>
    api.post('/serviceareas', data),

  update: (id: number, data: ServiceAreaFormData) =>
    api.put(`/serviceareas/${id}`, data),

  delete: (id: number) =>
    api.delete(`/serviceareas/${id}`),

  addZipCode: (areaId: number, zipCode: string) =>
    api.post(`/serviceareas/${areaId}/zipcodes`, { zipCode }),

  removeZipCode: (areaId: number, zipId: number) =>
    api.delete(`/serviceareas/${areaId}/zipcodes/${zipId}`),
};

// --- Users ---

export interface UserFormData {
  email: string;
  firstName: string;
  lastName: string;
  phone?: string;
  password?: string;
  roles: string[];
}

export const adminUsersApi = {
  create: (data: UserFormData) =>
    api.post('/admin/users', data),

  update: (id: string, data: Partial<UserFormData>) =>
    api.put(`/admin/users/${id}`, data),

  delete: (id: string) =>
    api.delete(`/admin/users/${id}`),

  changePassword: (userId: string, newPassword: string) =>
    api.put(`/admin/users/${userId}/password`, { newPassword }),

  // Roles
  createRole: (name: string) =>
    api.post('/admin/users/roles', { name }),

  deleteRole: (roleId: string) =>
    api.delete(`/admin/users/roles/${roleId}`),
};
