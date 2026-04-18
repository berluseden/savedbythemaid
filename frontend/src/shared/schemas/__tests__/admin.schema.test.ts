import { describe, it, expect } from 'vitest';
import {
  employeeSchema,
  serviceTypeSchema,
  cleaningPlaceSchema,
  additionalServiceSchema,
  recurrenceDiscountSchema,
} from '../admin.schema';

describe('employeeSchema', () => {
  const validEmployee = {
    firstName: 'Jane',
    lastName: 'Smith',
    email: 'jane@example.com',
    phone: '555-123-4567',
    address: '123 Main St',
    primaryServiceAreaId: 1,
    maxDailyHours: 8,
    maxDailyServices: 4,
    isActive: true,
  };

  it('accepts valid employee data', () => {
    expect(employeeSchema.safeParse(validEmployee).success).toBe(true);
  });

  it('accepts employee with optional empty email', () => {
    expect(employeeSchema.safeParse({ ...validEmployee, email: '' }).success).toBe(true);
  });

  it('accepts employee with null service area', () => {
    expect(employeeSchema.safeParse({ ...validEmployee, primaryServiceAreaId: null }).success).toBe(true);
  });

  it('rejects empty first name', () => {
    expect(employeeSchema.safeParse({ ...validEmployee, firstName: '' }).success).toBe(false);
  });

  it('rejects empty last name', () => {
    expect(employeeSchema.safeParse({ ...validEmployee, lastName: '' }).success).toBe(false);
  });

  it('rejects maxDailyHours below 1', () => {
    expect(employeeSchema.safeParse({ ...validEmployee, maxDailyHours: 0 }).success).toBe(false);
  });

  it('rejects maxDailyHours above 24', () => {
    expect(employeeSchema.safeParse({ ...validEmployee, maxDailyHours: 25 }).success).toBe(false);
  });

  it('rejects maxDailyServices below 1', () => {
    expect(employeeSchema.safeParse({ ...validEmployee, maxDailyServices: 0 }).success).toBe(false);
  });

  it('rejects maxDailyServices above 50', () => {
    expect(employeeSchema.safeParse({ ...validEmployee, maxDailyServices: 51 }).success).toBe(false);
  });
});

describe('serviceTypeSchema', () => {
  const validService = {
    name: 'Standard Cleaning',
    description: 'Basic cleaning service',
    price: 100,
    pricePerBedroom: 25,
    pricePerBathroom: 20,
    estimatedMinutes: 120,
    minutesPerBedroom: 15,
    minutesPerBathroom: 10,
    displayOrder: 1,
    isActive: true,
  };

  it('accepts valid service type', () => {
    expect(serviceTypeSchema.safeParse(validService).success).toBe(true);
  });

  it('accepts empty description', () => {
    expect(serviceTypeSchema.safeParse({ ...validService, description: '' }).success).toBe(true);
  });

  it('rejects empty name', () => {
    expect(serviceTypeSchema.safeParse({ ...validService, name: '' }).success).toBe(false);
  });

  it('rejects negative price', () => {
    expect(serviceTypeSchema.safeParse({ ...validService, price: -1 }).success).toBe(false);
  });

  it('accepts zero price', () => {
    expect(serviceTypeSchema.safeParse({ ...validService, price: 0 }).success).toBe(true);
  });

  it('rejects estimated minutes below 15', () => {
    expect(serviceTypeSchema.safeParse({ ...validService, estimatedMinutes: 10 }).success).toBe(false);
  });

  it('accepts minimum estimated minutes of 15', () => {
    expect(serviceTypeSchema.safeParse({ ...validService, estimatedMinutes: 15 }).success).toBe(true);
  });
});

describe('cleaningPlaceSchema', () => {
  it('accepts valid cleaning place', () => {
    expect(cleaningPlaceSchema.safeParse({ name: 'House', description: 'A house', isActive: true }).success).toBe(true);
  });

  it('accepts empty description', () => {
    expect(cleaningPlaceSchema.safeParse({ name: 'Apartment', description: '', isActive: true }).success).toBe(true);
  });

  it('rejects empty name', () => {
    expect(cleaningPlaceSchema.safeParse({ name: '', description: '', isActive: true }).success).toBe(false);
  });

  it('rejects missing isActive', () => {
    expect(cleaningPlaceSchema.safeParse({ name: 'House', description: '' }).success).toBe(false);
  });
});

describe('additionalServiceSchema', () => {
  const validAdditional = {
    title: 'Oven Cleaning',
    description: 'Deep clean oven',
    price: 50,
    additionalMinutes: 30,
    isActive: true,
  };

  it('accepts valid additional service', () => {
    expect(additionalServiceSchema.safeParse(validAdditional).success).toBe(true);
  });

  it('rejects empty title', () => {
    expect(additionalServiceSchema.safeParse({ ...validAdditional, title: '' }).success).toBe(false);
  });

  it('rejects negative price', () => {
    expect(additionalServiceSchema.safeParse({ ...validAdditional, price: -5 }).success).toBe(false);
  });

  it('rejects negative additional minutes', () => {
    expect(additionalServiceSchema.safeParse({ ...validAdditional, additionalMinutes: -1 }).success).toBe(false);
  });

  it('accepts zero price and minutes', () => {
    expect(additionalServiceSchema.safeParse({ ...validAdditional, price: 0, additionalMinutes: 0 }).success).toBe(true);
  });
});

describe('recurrenceDiscountSchema', () => {
  it('accepts valid recurrence discount', () => {
    expect(recurrenceDiscountSchema.safeParse({ recurrenceType: 1, discountPercent: 10 }).success).toBe(true);
  });

  it('rejects recurrence type 0', () => {
    expect(recurrenceDiscountSchema.safeParse({ recurrenceType: 0, discountPercent: 10 }).success).toBe(false);
  });

  it('rejects recurrence type above 3', () => {
    expect(recurrenceDiscountSchema.safeParse({ recurrenceType: 4, discountPercent: 10 }).success).toBe(false);
  });

  it('rejects negative discount', () => {
    expect(recurrenceDiscountSchema.safeParse({ recurrenceType: 1, discountPercent: -5 }).success).toBe(false);
  });

  it('rejects discount above 100', () => {
    expect(recurrenceDiscountSchema.safeParse({ recurrenceType: 1, discountPercent: 101 }).success).toBe(false);
  });

  it('accepts boundary values (type 1-3, discount 0-100)', () => {
    expect(recurrenceDiscountSchema.safeParse({ recurrenceType: 3, discountPercent: 100 }).success).toBe(true);
    expect(recurrenceDiscountSchema.safeParse({ recurrenceType: 1, discountPercent: 0 }).success).toBe(true);
  });
});
