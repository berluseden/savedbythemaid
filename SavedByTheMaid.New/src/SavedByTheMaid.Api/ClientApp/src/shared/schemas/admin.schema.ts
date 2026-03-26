import { z } from 'zod';

const requiredString = z.string().min(1, 'This field is required');
const nonNegativeNumber = z.number().min(0, 'Must be 0 or greater');

// --- Employees ---
export const employeeSchema = z.object({
  firstName: requiredString,
  lastName: requiredString,
  email: z.string(),
  phone: z.string(),
  address: z.string(),
  primaryServiceAreaId: z.number().nullable(),
  maxDailyHours: z.number().min(1, 'Minimum 1 hour').max(24, 'Maximum 24 hours'),
  maxDailyServices: z.number().min(1, 'Minimum 1 service').max(50, 'Maximum 50 services'),
  isActive: z.boolean(),
});
export type EmployeeFormData = z.infer<typeof employeeSchema>;

// --- Recurrence Discounts ---
export const recurrenceDiscountSchema = z.object({
  recurrenceType: z.number().min(1, 'Required').max(3, 'Maximum type 3'),
  discountPercent: z.number().min(0, 'Must be 0 or greater').max(100, 'Maximum 100%'),
});
export type RecurrenceDiscountFormData = z.infer<typeof recurrenceDiscountSchema>;

// --- Additional Services ---
export const additionalServiceSchema = z.object({
  title: requiredString,
  description: z.string(),
  price: nonNegativeNumber,
  additionalMinutes: nonNegativeNumber,
  isActive: z.boolean(),
});
export type AdditionalServiceFormData = z.infer<typeof additionalServiceSchema>;

// --- Service Areas ---
export const serviceAreaSchema = z.object({
  name: requiredString,
  description: z.string(),
  isActive: z.boolean(),
});
export type ServiceAreaFormData = z.infer<typeof serviceAreaSchema>;

// --- Service Types (admin ServicesPage) ---
export const serviceTypeSchema = z.object({
  name: requiredString,
  description: z.string(),
  price: nonNegativeNumber,
  pricePerBedroom: nonNegativeNumber,
  pricePerBathroom: nonNegativeNumber,
  estimatedMinutes: z.number().min(15, 'Minimum 15 minutes'),
  minutesPerBedroom: nonNegativeNumber,
  minutesPerBathroom: nonNegativeNumber,
  displayOrder: nonNegativeNumber,
  isActive: z.boolean(),
});
export type ServiceTypeFormData = z.infer<typeof serviceTypeSchema>;

// --- Cleaning Places ---
export const cleaningPlaceSchema = z.object({
  name: requiredString,
  description: z.string(),
  isActive: z.boolean(),
});
export type CleaningPlaceFormData = z.infer<typeof cleaningPlaceSchema>;

// --- Cleaning Place Rooms ---
export const cleaningPlaceRoomSchema = z.object({
  name: requiredString,
  description: z.string(),
  baseMinutes: z.number().min(5, 'Minimum 5 minutes'),
  basePrice: nonNegativeNumber,
  displayOrder: nonNegativeNumber,
  isActive: z.boolean(),
});
export type CleaningPlaceRoomFormData = z.infer<typeof cleaningPlaceRoomSchema>;

// --- Price Multipliers ---
export const priceMultiplierSchema = z.object({
  name: requiredString,
  description: z.string(),
  conditionType: z.number(),
  factor: nonNegativeNumber,
  minValue: z.number().nullable(),
  maxValue: z.number().nullable(),
  appliesToTime: z.boolean(),
  appliesToPrice: z.boolean(),
  serviceTypeId: z.number().nullable(),
  displayOrder: nonNegativeNumber,
  isActive: z.boolean(),
});
export type PriceMultiplierFormData = z.infer<typeof priceMultiplierSchema>;

// --- Users ---
export const userSchema = z.object({
  firstName: requiredString,
  lastName: requiredString,
  email: z.string().email('Enter a valid email'),
  phone: z.string(),
  role: requiredString,
  isActive: z.boolean(),
});
export type UserFormData = z.infer<typeof userSchema>;

// --- Password Reset ---
export const passwordResetSchema = z.object({
  newPassword: z.string().min(8, 'At least 8 characters'),
  confirmPassword: z.string().min(1, 'Please confirm the password'),
}).refine(data => data.newPassword === data.confirmPassword, {
  message: 'Passwords do not match',
  path: ['confirmPassword'],
});
export type PasswordResetFormData = z.infer<typeof passwordResetSchema>;
