import { describe, it, expect } from 'vitest';
import { formatCurrency, formatDate, formatDateShort, formatTime, formatPhone } from '../format-utils';

describe('formatCurrency', () => {
  it('formats whole dollar amount', () => {
    expect(formatCurrency(100)).toBe('$100.00');
  });

  it('formats cents', () => {
    expect(formatCurrency(49.99)).toBe('$49.99');
  });

  it('formats zero', () => {
    expect(formatCurrency(0)).toBe('$0.00');
  });

  it('formats large amounts with comma separator', () => {
    expect(formatCurrency(1234.56)).toBe('$1,234.56');
  });

  it('formats negative amounts', () => {
    expect(formatCurrency(-50)).toBe('-$50.00');
  });
});

describe('formatDate', () => {
  it('formats Date object', () => {
    // Use explicit time to avoid UTC midnight timezone shift
    const result = formatDate(new Date('2026-03-25T12:00:00'));
    expect(result).toContain('March');
    expect(result).toContain('25');
    expect(result).toContain('2026');
  });

  it('formats ISO date string', () => {
    const result = formatDate('2026-01-01T00:00:00');
    expect(result).toContain('January');
    expect(result).toContain('2026');
  });

  it('includes weekday', () => {
    // 2026-03-25 is a Wednesday
    const result = formatDate('2026-03-25T12:00:00');
    expect(result).toContain('Wednesday');
  });
});

describe('formatDateShort', () => {
  it('formats date in short format', () => {
    const result = formatDateShort('2026-03-25T12:00:00');
    expect(result).toContain('Mar');
    expect(result).toContain('25');
    expect(result).toContain('2026');
  });
});

describe('formatTime', () => {
  it('formats morning time', () => {
    const result = formatTime('2026-03-25T09:30:00');
    expect(result).toContain('9:30');
    expect(result).toMatch(/AM/i);
  });

  it('formats afternoon time', () => {
    const result = formatTime('2026-03-25T14:00:00');
    expect(result).toContain('2:00');
    expect(result).toMatch(/PM/i);
  });

  it('formats midnight', () => {
    const result = formatTime('2026-03-25T00:00:00');
    expect(result).toContain('12:00');
    expect(result).toMatch(/AM/i);
  });
});

describe('formatPhone', () => {
  it('formats 10-digit phone number', () => {
    expect(formatPhone('5551234567')).toBe('(555) 123-4567');
  });

  it('strips non-digits before formatting', () => {
    expect(formatPhone('555-123-4567')).toBe('(555) 123-4567');
  });

  it('returns original if not 10 digits', () => {
    expect(formatPhone('123')).toBe('123');
  });

  it('returns original for international numbers', () => {
    expect(formatPhone('+15551234567')).toBe('+15551234567');
  });

  it('handles empty string', () => {
    expect(formatPhone('')).toBe('');
  });
});
