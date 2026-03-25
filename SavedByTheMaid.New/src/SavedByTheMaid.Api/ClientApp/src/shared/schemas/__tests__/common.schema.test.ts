import { describe, it, expect } from 'vitest';
import { emailSchema, phoneSchema, zipCodeSchema, passwordSchema, requiredString } from '../common.schema';

describe('emailSchema', () => {
  it('accepts valid email', () => {
    expect(emailSchema.safeParse('user@example.com').success).toBe(true);
  });

  it('accepts email with subdomain', () => {
    expect(emailSchema.safeParse('user@mail.example.com').success).toBe(true);
  });

  it('rejects empty string', () => {
    expect(emailSchema.safeParse('').success).toBe(false);
  });

  it('rejects string without @', () => {
    expect(emailSchema.safeParse('notanemail').success).toBe(false);
  });

  it('rejects string without domain', () => {
    expect(emailSchema.safeParse('user@').success).toBe(false);
  });
});

describe('phoneSchema', () => {
  it('accepts 10-digit phone with dashes', () => {
    expect(phoneSchema.safeParse('555-123-4567').success).toBe(true);
  });

  it('accepts 10-digit phone with dots', () => {
    expect(phoneSchema.safeParse('555.123.4567').success).toBe(true);
  });

  it('accepts phone with parentheses', () => {
    expect(phoneSchema.safeParse('(555) 123-4567').success).toBe(true);
  });

  it('accepts empty string (optional)', () => {
    expect(phoneSchema.safeParse('').success).toBe(true);
  });

  it('accepts undefined (optional)', () => {
    expect(phoneSchema.safeParse(undefined).success).toBe(true);
  });

  it('rejects too short phone', () => {
    expect(phoneSchema.safeParse('555-123').success).toBe(false);
  });

  it('rejects letters in phone', () => {
    expect(phoneSchema.safeParse('555-abc-4567').success).toBe(false);
  });
});

describe('zipCodeSchema', () => {
  it('accepts valid 5-digit zip', () => {
    expect(zipCodeSchema.safeParse('12345').success).toBe(true);
  });

  it('accepts zip starting with zero', () => {
    expect(zipCodeSchema.safeParse('01234').success).toBe(true);
  });

  it('rejects 4-digit zip', () => {
    expect(zipCodeSchema.safeParse('1234').success).toBe(false);
  });

  it('rejects 6-digit zip', () => {
    expect(zipCodeSchema.safeParse('123456').success).toBe(false);
  });

  it('rejects zip with letters', () => {
    expect(zipCodeSchema.safeParse('1234a').success).toBe(false);
  });

  it('rejects zip+4 format', () => {
    expect(zipCodeSchema.safeParse('12345-6789').success).toBe(false);
  });
});

describe('passwordSchema', () => {
  it('accepts strong password', () => {
    expect(passwordSchema.safeParse('Test123!@').success).toBe(true);
  });

  it('rejects password under 8 chars', () => {
    expect(passwordSchema.safeParse('Te1!').success).toBe(false);
  });

  it('rejects password without uppercase', () => {
    expect(passwordSchema.safeParse('test123!@').success).toBe(false);
  });

  it('rejects password without lowercase', () => {
    expect(passwordSchema.safeParse('TEST123!@').success).toBe(false);
  });

  it('rejects password without number', () => {
    expect(passwordSchema.safeParse('Testtest!@').success).toBe(false);
  });

  it('rejects password without special char', () => {
    expect(passwordSchema.safeParse('Test12345').success).toBe(false);
  });
});

describe('requiredString', () => {
  it('accepts non-empty string', () => {
    const schema = requiredString('Name');
    expect(schema.safeParse('John').success).toBe(true);
  });

  it('rejects empty string', () => {
    const schema = requiredString('Name');
    const result = schema.safeParse('');
    expect(result.success).toBe(false);
  });
});
