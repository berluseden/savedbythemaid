import { z } from 'zod';
import { phoneSchema, requiredString, zipCodeSchema } from './common.schema';

export const profileSchema = z.object({
  firstName: requiredString('First name'),
  lastName: requiredString('Last name'),
  phone: phoneSchema,
  address: z.string().optional().or(z.literal('')),
  city: z.string().optional().or(z.literal('')),
  state: z.string().optional().or(z.literal('')),
  zipCode: zipCodeSchema.optional().or(z.literal('')),
});
export type ProfileFormData = z.infer<typeof profileSchema>;
