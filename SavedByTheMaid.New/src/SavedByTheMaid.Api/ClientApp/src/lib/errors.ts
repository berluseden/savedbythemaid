import axios, { type AxiosError } from 'axios';

export interface ApiErrorResponse {
  message?: string;
  errors?: Record<string, string[]>;
  title?: string;
  detail?: string;
}

export function isAxiosError(error: unknown): error is AxiosError<ApiErrorResponse> {
  return axios.isAxiosError(error);
}

export function getErrorMessage(error: unknown, fallback = 'An unexpected error occurred'): string {
  if (isAxiosError(error)) {
    const data = error.response?.data;
    return data?.message || data?.detail || data?.title || error.message || fallback;
  }
  if (error instanceof Error) return error.message;
  if (typeof error === 'string') return error;
  return fallback;
}

export function getValidationErrors(error: unknown): Record<string, string[]> | null {
  if (isAxiosError(error) && error.response?.status === 400) {
    return error.response.data?.errors || null;
  }
  return null;
}
