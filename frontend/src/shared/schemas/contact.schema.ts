import { z } from 'zod';
import { emailSchema, phoneSchema, requiredString } from './common.schema';

export const contactSchema = z.object({
  name: requiredString('Name'),
  email: emailSchema,
  phone: phoneSchema,
  subject: requiredString('Subject'),
  message: z.string().min(10, 'Message must be at least 10 characters'),
});
export type ContactFormData = z.infer<typeof contactSchema>;
