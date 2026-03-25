import { describe, it, expect } from 'vitest';
import { contactSchema } from '../contact.schema';

describe('contactSchema', () => {
  const validContact = {
    name: 'John Doe',
    email: 'john@example.com',
    phone: '555-123-4567',
    subject: 'Inquiry',
    message: 'I would like to know more about your cleaning services.',
  };

  it('accepts valid contact data', () => {
    expect(contactSchema.safeParse(validContact).success).toBe(true);
  });

  it('accepts contact with empty phone (optional)', () => {
    expect(contactSchema.safeParse({ ...validContact, phone: '' }).success).toBe(true);
  });

  it('rejects empty name', () => {
    expect(contactSchema.safeParse({ ...validContact, name: '' }).success).toBe(false);
  });

  it('rejects empty email', () => {
    expect(contactSchema.safeParse({ ...validContact, email: '' }).success).toBe(false);
  });

  it('rejects invalid email', () => {
    expect(contactSchema.safeParse({ ...validContact, email: 'bad' }).success).toBe(false);
  });

  it('rejects empty subject', () => {
    expect(contactSchema.safeParse({ ...validContact, subject: '' }).success).toBe(false);
  });

  it('rejects message shorter than 10 chars', () => {
    expect(contactSchema.safeParse({ ...validContact, message: 'Short' }).success).toBe(false);
  });

  it('accepts message exactly 10 chars', () => {
    expect(contactSchema.safeParse({ ...validContact, message: '1234567890' }).success).toBe(true);
  });

  it('rejects empty message', () => {
    expect(contactSchema.safeParse({ ...validContact, message: '' }).success).toBe(false);
  });

  it('rejects invalid phone format', () => {
    expect(contactSchema.safeParse({ ...validContact, phone: 'abc' }).success).toBe(false);
  });
});
