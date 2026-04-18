import { describe, it, expect } from 'vitest';
import {
  zipCodeStepSchema,
  serviceStepSchema,
  detailsStepSchema,
  contactStepSchema,
  scheduleStepSchema,
} from '../booking.schema';

describe('zipCodeStepSchema', () => {
  it('accepts valid zip code', () => {
    expect(zipCodeStepSchema.safeParse({ zipCode: '12345' }).success).toBe(true);
  });

  it('rejects invalid zip code', () => {
    expect(zipCodeStepSchema.safeParse({ zipCode: '123' }).success).toBe(false);
  });

  it('rejects empty zip code', () => {
    expect(zipCodeStepSchema.safeParse({ zipCode: '' }).success).toBe(false);
  });

  it('rejects missing zip code', () => {
    expect(zipCodeStepSchema.safeParse({}).success).toBe(false);
  });
});

describe('serviceStepSchema', () => {
  it('accepts valid service type ID', () => {
    expect(serviceStepSchema.safeParse({ serviceTypeId: 1 }).success).toBe(true);
  });

  it('rejects zero service type ID', () => {
    expect(serviceStepSchema.safeParse({ serviceTypeId: 0 }).success).toBe(false);
  });

  it('rejects negative service type ID', () => {
    expect(serviceStepSchema.safeParse({ serviceTypeId: -1 }).success).toBe(false);
  });

  it('rejects missing service type ID', () => {
    expect(serviceStepSchema.safeParse({}).success).toBe(false);
  });
});

describe('detailsStepSchema', () => {
  const validDetails = {
    cleaningPlaceId: 1,
    bedrooms: 2,
    bathrooms: 1,
    squareFootage: 1000,
  };

  it('accepts valid details', () => {
    expect(detailsStepSchema.safeParse(validDetails).success).toBe(true);
  });

  it('accepts details with additional services', () => {
    expect(detailsStepSchema.safeParse({ ...validDetails, additionalServiceIds: [1, 2] }).success).toBe(true);
  });

  it('accepts details without additional services', () => {
    expect(detailsStepSchema.safeParse({ ...validDetails, additionalServiceIds: undefined }).success).toBe(true);
  });

  it('rejects zero cleaning place ID', () => {
    expect(detailsStepSchema.safeParse({ ...validDetails, cleaningPlaceId: 0 }).success).toBe(false);
  });

  it('rejects zero bedrooms', () => {
    expect(detailsStepSchema.safeParse({ ...validDetails, bedrooms: 0 }).success).toBe(false);
  });

  it('rejects zero bathrooms', () => {
    expect(detailsStepSchema.safeParse({ ...validDetails, bathrooms: 0 }).success).toBe(false);
  });

  it('rejects square footage below 100', () => {
    expect(detailsStepSchema.safeParse({ ...validDetails, squareFootage: 50 }).success).toBe(false);
  });

  it('accepts minimum square footage of 100', () => {
    expect(detailsStepSchema.safeParse({ ...validDetails, squareFootage: 100 }).success).toBe(true);
  });
});

describe('contactStepSchema', () => {
  const validContact = {
    firstName: 'John',
    lastName: 'Doe',
    email: 'john@example.com',
    phone: '555-123-4567',
    address: '123 Main St',
    city: 'Springfield',
    state: 'IL',
  };

  it('accepts valid contact data', () => {
    expect(contactStepSchema.safeParse(validContact).success).toBe(true);
  });

  it('accepts contact with optional fields', () => {
    expect(contactStepSchema.safeParse({
      ...validContact,
      password: 'optional',
      specialInstructions: 'Please ring bell',
    }).success).toBe(true);
  });

  it('rejects empty first name', () => {
    expect(contactStepSchema.safeParse({ ...validContact, firstName: '' }).success).toBe(false);
  });

  it('rejects empty last name', () => {
    expect(contactStepSchema.safeParse({ ...validContact, lastName: '' }).success).toBe(false);
  });

  it('rejects invalid email', () => {
    expect(contactStepSchema.safeParse({ ...validContact, email: 'notvalid' }).success).toBe(false);
  });

  it('rejects empty address', () => {
    expect(contactStepSchema.safeParse({ ...validContact, address: '' }).success).toBe(false);
  });

  it('rejects empty city', () => {
    expect(contactStepSchema.safeParse({ ...validContact, city: '' }).success).toBe(false);
  });

  it('rejects empty state', () => {
    expect(contactStepSchema.safeParse({ ...validContact, state: '' }).success).toBe(false);
  });
});

describe('scheduleStepSchema', () => {
  const validSchedule = {
    date: '2026-04-01',
    timeSlot: '9:00 AM - 12:00 PM',
    employeeId: 1,
  };

  it('accepts valid schedule data', () => {
    expect(scheduleStepSchema.safeParse(validSchedule).success).toBe(true);
  });

  it('rejects empty date', () => {
    expect(scheduleStepSchema.safeParse({ ...validSchedule, date: '' }).success).toBe(false);
  });

  it('rejects empty time slot', () => {
    expect(scheduleStepSchema.safeParse({ ...validSchedule, timeSlot: '' }).success).toBe(false);
  });

  it('rejects zero employee ID', () => {
    expect(scheduleStepSchema.safeParse({ ...validSchedule, employeeId: 0 }).success).toBe(false);
  });
});
