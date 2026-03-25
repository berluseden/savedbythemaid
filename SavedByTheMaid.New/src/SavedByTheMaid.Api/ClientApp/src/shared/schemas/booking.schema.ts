import { z } from 'zod';
import { emailSchema, phoneSchema, requiredString, zipCodeSchema } from './common.schema';

export const zipCodeStepSchema = z.object({
  zipCode: zipCodeSchema,
});
export type ZipCodeStepData = z.infer<typeof zipCodeStepSchema>;

export const serviceStepSchema = z.object({
  serviceTypeId: z.number().min(1, 'Please select a service type'),
});
export type ServiceStepData = z.infer<typeof serviceStepSchema>;

export const detailsStepSchema = z.object({
  cleaningPlaceId: z.number().min(1, 'Please select a property type'),
  bedrooms: z.number().min(1, 'At least 1 bedroom required'),
  bathrooms: z.number().min(1, 'At least 1 bathroom required'),
  squareFootage: z.number().min(100, 'Square footage must be at least 100'),
  additionalServiceIds: z.array(z.number()).optional(),
});
export type DetailsStepData = z.infer<typeof detailsStepSchema>;

export const scheduleStepSchema = z.object({
  date: requiredString('Date'),
  timeSlot: requiredString('Time slot'),
  employeeId: z.number().min(1, 'Please select a time slot'),
});
export type ScheduleStepData = z.infer<typeof scheduleStepSchema>;

export const contactStepSchema = z.object({
  firstName: requiredString('First name'),
  lastName: requiredString('Last name'),
  email: emailSchema,
  phone: phoneSchema,
  password: z.string().optional(),
  address: requiredString('Address'),
  city: requiredString('City'),
  state: requiredString('State'),
  specialInstructions: z.string().optional(),
});
export type ContactStepData = z.infer<typeof contactStepSchema>;

export const bookingSchema = z.object({
  zipCode: zipCodeSchema,
  serviceTypeId: z.number().min(1, 'Please select a service type'),
  cleaningPlaceId: z.number().min(1, 'Please select a property type'),
  bedrooms: z.number().min(1),
  bathrooms: z.number().min(1),
  squareFootage: z.number().min(100),
  additionalServiceIds: z.array(z.number()),
  date: requiredString('Date'),
  timeSlot: requiredString('Time slot'),
  employeeId: z.number().min(1),
  firstName: requiredString('First name'),
  lastName: requiredString('Last name'),
  email: emailSchema,
  phone: phoneSchema,
  password: z.string().optional(),
  address: requiredString('Address'),
  city: requiredString('City'),
  state: requiredString('State'),
  specialInstructions: z.string().optional(),
});
export type BookingFormData = z.infer<typeof bookingSchema>;
