import { describe, it, expect } from 'vitest';
import { loginSchema, registerSchema, forgotPasswordSchema, changePasswordSchema } from '../auth.schema';

describe('loginSchema', () => {
  it('accepts valid login data', () => {
    expect(loginSchema.safeParse({ email: 'test@example.com', password: 'pass123' }).success).toBe(true);
  });

  it('accepts login with rememberMe', () => {
    expect(loginSchema.safeParse({ email: 'test@example.com', password: 'x', rememberMe: true }).success).toBe(true);
  });

  it('rejects empty email', () => {
    expect(loginSchema.safeParse({ email: '', password: 'pass' }).success).toBe(false);
  });

  it('rejects invalid email format', () => {
    expect(loginSchema.safeParse({ email: 'notanemail', password: 'pass' }).success).toBe(false);
  });

  it('rejects missing email', () => {
    expect(loginSchema.safeParse({ password: 'pass' }).success).toBe(false);
  });

  it('rejects empty password', () => {
    expect(loginSchema.safeParse({ email: 'test@example.com', password: '' }).success).toBe(false);
  });

  it('rejects missing password', () => {
    expect(loginSchema.safeParse({ email: 'test@example.com' }).success).toBe(false);
  });
});

describe('registerSchema', () => {
  const validData = {
    firstName: 'John',
    lastName: 'Doe',
    email: 'john@example.com',
    phone: '555-123-4567',
    password: 'Test123!@',
    confirmPassword: 'Test123!@',
    acceptTerms: true as const,
  };

  it('accepts valid registration data', () => {
    expect(registerSchema.safeParse(validData).success).toBe(true);
  });

  it('accepts registration with optional empty phone', () => {
    expect(registerSchema.safeParse({ ...validData, phone: '' }).success).toBe(true);
  });

  it('rejects mismatched passwords', () => {
    expect(registerSchema.safeParse({ ...validData, confirmPassword: 'different' }).success).toBe(false);
  });

  it('rejects weak password (no uppercase)', () => {
    expect(registerSchema.safeParse({ ...validData, password: 'test123!@', confirmPassword: 'test123!@' }).success).toBe(false);
  });

  it('rejects weak password (no special char)', () => {
    expect(registerSchema.safeParse({ ...validData, password: 'Test1234a', confirmPassword: 'Test1234a' }).success).toBe(false);
  });

  it('rejects weak password (no number)', () => {
    expect(registerSchema.safeParse({ ...validData, password: 'Testtest!@', confirmPassword: 'Testtest!@' }).success).toBe(false);
  });

  it('rejects weak password (no lowercase)', () => {
    expect(registerSchema.safeParse({ ...validData, password: 'TEST123!@', confirmPassword: 'TEST123!@' }).success).toBe(false);
  });

  it('rejects password shorter than 8 chars', () => {
    expect(registerSchema.safeParse({ ...validData, password: 'Te1!', confirmPassword: 'Te1!' }).success).toBe(false);
  });

  it('rejects without accepting terms', () => {
    expect(registerSchema.safeParse({ ...validData, acceptTerms: false }).success).toBe(false);
  });

  it('rejects empty first name', () => {
    expect(registerSchema.safeParse({ ...validData, firstName: '' }).success).toBe(false);
  });

  it('rejects empty last name', () => {
    expect(registerSchema.safeParse({ ...validData, lastName: '' }).success).toBe(false);
  });
});

describe('forgotPasswordSchema', () => {
  it('accepts valid email', () => {
    expect(forgotPasswordSchema.safeParse({ email: 'test@example.com' }).success).toBe(true);
  });

  it('rejects invalid email', () => {
    expect(forgotPasswordSchema.safeParse({ email: 'notvalid' }).success).toBe(false);
  });

  it('rejects empty email', () => {
    expect(forgotPasswordSchema.safeParse({ email: '' }).success).toBe(false);
  });
});

describe('changePasswordSchema', () => {
  it('accepts valid change password data', () => {
    expect(changePasswordSchema.safeParse({
      currentPassword: 'oldPass1',
      newPassword: 'NewPass1!@',
    }).success).toBe(true);
  });

  it('rejects empty current password', () => {
    expect(changePasswordSchema.safeParse({
      currentPassword: '',
      newPassword: 'NewPass1!@',
    }).success).toBe(false);
  });

  it('rejects weak new password', () => {
    expect(changePasswordSchema.safeParse({
      currentPassword: 'oldPass1',
      newPassword: 'weak',
    }).success).toBe(false);
  });

  it('rejects same current and new password', () => {
    expect(changePasswordSchema.safeParse({
      currentPassword: 'SamePass1!@',
      newPassword: 'SamePass1!@',
    }).success).toBe(false);
  });
});
