import { describe, it, expect } from 'vitest';
import { AxiosError, type AxiosResponse } from 'axios';
import { getErrorMessage, getFieldErrors } from '../error-utils';

function makeAxiosError(responseData?: unknown, status = 400): AxiosError {
  const error = new AxiosError('Request failed');
  if (responseData !== undefined) {
    error.response = {
      data: responseData,
      status,
      statusText: 'Bad Request',
      headers: {},
      config: {} as never,
    } as AxiosResponse;
  }
  return error;
}

describe('getErrorMessage', () => {
  it('extracts message from AxiosError response data', () => {
    const err = makeAxiosError({ message: 'Invalid credentials' });
    expect(getErrorMessage(err)).toBe('Invalid credentials');
  });

  it('joins field errors from AxiosError response', () => {
    const err = makeAxiosError({ errors: { email: ['Email is required'], name: ['Name is required'] } });
    expect(getErrorMessage(err)).toBe('Email is required Name is required');
  });

  it('prefers message over errors', () => {
    const err = makeAxiosError({ message: 'Top-level message', errors: { f: ['detail'] } });
    expect(getErrorMessage(err)).toBe('Top-level message');
  });

  it('uses userMessage from interceptor-enriched AxiosError', () => {
    const err = makeAxiosError({});
    (err as AxiosError & { userMessage?: string }).userMessage = 'Interceptor message';
    expect(getErrorMessage(err)).toBe('Interceptor message');
  });

  it('extracts message from regular Error', () => {
    expect(getErrorMessage(new Error('Something broke'))).toBe('Something broke');
  });

  it('returns fallback for unknown error types', () => {
    expect(getErrorMessage('some string')).toBe('Something went wrong');
  });

  it('returns custom fallback', () => {
    expect(getErrorMessage(null, 'Custom fallback')).toBe('Custom fallback');
  });

  it('falls back to Error.message for AxiosError without response', () => {
    const err = new AxiosError('Network Error');
    // AxiosError extends Error, so error.message is returned
    expect(getErrorMessage(err)).toBe('Network Error');
  });

  it('handles undefined/null error', () => {
    expect(getErrorMessage(undefined)).toBe('Something went wrong');
    expect(getErrorMessage(null)).toBe('Something went wrong');
  });
});

describe('getFieldErrors', () => {
  it('extracts field errors from AxiosError', () => {
    const err = makeAxiosError({ errors: { email: ['Invalid'], password: ['Too short'] } });
    expect(getFieldErrors(err)).toEqual({ email: ['Invalid'], password: ['Too short'] });
  });

  it('returns null when no errors in response', () => {
    const err = makeAxiosError({ message: 'No fields' });
    expect(getFieldErrors(err)).toBeNull();
  });

  it('returns null for non-Axios errors', () => {
    expect(getFieldErrors(new Error('plain'))).toBeNull();
  });

  it('returns null for unknown types', () => {
    expect(getFieldErrors('string error')).toBeNull();
  });

  it('returns null for AxiosError without response', () => {
    expect(getFieldErrors(new AxiosError('Network Error'))).toBeNull();
  });
});
