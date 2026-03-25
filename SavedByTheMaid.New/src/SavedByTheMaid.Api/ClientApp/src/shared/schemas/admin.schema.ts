import { z } from 'zod';
import { emailSchema, passwordSchema, phoneSchema, requiredString } from './common.schema';

// --- Employee ---

export const employeeSchema = z.object({
  firstName: requiredString('First name'),
  lastName: requiredString('Last name'),
  email: emailSchema.optional().or(z.literal('')),
  phone: phoneSchema,
  address: z.string().optional().or(z.literal('')),
  primaryServiceAreaId: z.number().nullable(),
  maxDailyHours: z.number().min(1, 'Must be at least 1 hour').max(24),
  maxDailyServices: z.number().min(1, 'Must be at least 1 service').max(50),
  isActive: z.boolean(),
});
export type EmployeeFormData = z.infer<typeof employeeSchema>;

// --- Service Type ---

export const serviceTypeSchema = z.object({
  name: requiredString('Service name'),
  description: z.string().optional().or(z.literal('')),
  price: z.number().min(0, 'Price must be non-negative'),
  pricePerBedroom: z.number().min(0),
  pricePerBathroom: z.number().min(0),
  estimatedMinutes: z.number().min(15, 'Must be at least 15 minutes'),
  minutesPerBedroom: z.number().min(0),
  minutesPerBathroom: z.number().min(0),
  displayOrder: z.number().min(0),
  isActive: z.boolean(),
});
export type ServiceTypeFormData = z.infer<typeof serviceTypeSchema>;

// --- Cleaning Place ---

export const cleaningPlaceSchema = z.object({
  name: requiredString('Name'),
  description: z.string().optional().or(z.literal('')),
  isActive: z.boolean(),
});
export type CleaningPlaceFormData = z.infer<typeof cleaningPlaceSchema>;

// --- Cleaning Place Room ---

export const cleaningPlaceRoomSchema = z.object({
  name: requiredString('Room name'),
  description: z.string().optional().or(z.literal('')),
  baseMinutes: z.number().min(5, 'Must be at least 5 minutes'),
  basePrice: z.number().min(0, 'Price must be non-negative'),
  displayOrder: z.number().min(0),
  isActive: z.boolean(),
});
export type CleaningPlaceRoomFormData = z.infer<typeof cleaningPlaceRoomSchema>;

// --- Additional Service ---

export const additionalServiceSchema = z.object({
  title: requiredString('Title'),
  description: z.string().optional().or(z.literal('')),
  price: z.number().min(0, 'Price must be non-negative'),
  additionalMinutes: z.number().min(0),
  isActive: z.boolean(),
});
export type AdditionalServiceFormData = z.infer<typeof additionalServiceSchema>;

// --- Price Multiplier ---

export const priceMultiplierSchema = z.object({
  name: requiredString('Name'),
  description: z.string().optional().or(z.literal('')),
  conditionType: z.number().min(0).max(6),
  factor: z.number().min(0, 'Factor must be positive'),
  minValue: z.number().nullable(),
  maxValue: z.number().nullable(),
  appliesToTime: z.boolean(),
  appliesToPrice: z.boolean(),
  serviceTypeId: z.number().nullable(),
  displayOrder: z.number().min(0),
  isActive: z.boolean(),
});
export type PriceMultiplierFormData = z.infer<typeof priceMultiplierSchema>;

// --- Recurrence Discount ---

export const recurrenceDiscountSchema = z.object({
  recurrenceType: z.number().min(1, 'Select a recurrence type').max(3),
  discountPercent: z.number().min(0, 'Discount cannot be negative').max(100, 'Discount cannot exceed 100%'),
});
export type RecurrenceDiscountFormData = z.infer<typeof recurrenceDiscountSchema>;

// --- Service Area ---

export const serviceAreaSchema = z.object({
  name: requiredString('Name'),
  description: z.string().optional().or(z.literal('')),
  isActive: z.boolean(),
});
export type ServiceAreaFormData = z.infer<typeof serviceAreaSchema>;

// --- User ---

export const userSchema = z.object({
  email: emailSchema,
  password: passwordSchema.optional().or(z.literal('')),
  firstName: z.string().optional().or(z.literal('')),
  lastName: z.string().optional().or(z.literal('')),
  phoneNumber: phoneSchema,
  isActive: z.boolean(),
  roles: z.array(z.string()),
});
export type UserFormData = z.infer<typeof userSchema>;

// --- Password Reset (admin) ---

export const passwordResetSchema = z.object({
  newPassword: passwordSchema,
});
export type PasswordResetFormData = z.infer<typeof passwordResetSchema>;
