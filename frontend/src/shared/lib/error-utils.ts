// Centralized error handling utilities
// Replaces ugly type assertions in catch blocks throughout the codebase.

import { AxiosError } from 'axios';

export interface ApiErrorData {
  message?: string;
  details?: string;
  errors?: Record<string, string[]>;
}

export function getErrorMessage(error: unknown, fallback = 'Something went wrong'): string {
  if (error instanceof AxiosError) {
    const data = error.response?.data as ApiErrorData | undefined;
    if (data?.message) return data.message;
    if (data?.errors) {
      return Object.values(data.errors).flat().join(' ');
    }
    // Check for userMessage attached by interceptor
    const errWithMsg = error as AxiosError & { userMessage?: string };
    if (errWithMsg.userMessage) return errWithMsg.userMessage;
  }
  if (error instanceof Error) return error.message;
  return fallback;
}

export function getFieldErrors(error: unknown): Record<string, string[]> | null {
  if (error instanceof AxiosError) {
    const data = error.response?.data as ApiErrorData | undefined;
    return data?.errors ?? null;
  }
  return null;
}
