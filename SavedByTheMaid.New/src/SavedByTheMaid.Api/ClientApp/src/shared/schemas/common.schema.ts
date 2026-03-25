import { z } from 'zod';

export const emailSchema = z.string().min(1, 'Email is required').email('Enter a valid email');

export const phoneSchema = z.string().regex(
  /^\(?\d{3}\)?[\s.-]?\d{3}[\s.-]?\d{4}$/,
  'Enter a valid phone number'
).optional().or(z.literal(''));

export const zipCodeSchema = z.string().regex(/^\d{5}$/, 'Enter a valid 5-digit ZIP code');

export const requiredString = (field: string) => z.string().min(1, `${field} is required`);

export const passwordSchema = z.string()
  .min(8, 'At least 8 characters')
  .regex(/[A-Z]/, 'Must contain an uppercase letter')
  .regex(/[a-z]/, 'Must contain a lowercase letter')
  .regex(/\d/, 'Must contain a number')
  .regex(/[!@#$%^&*(),.?":{}|<>]/, 'Must contain a special character');
