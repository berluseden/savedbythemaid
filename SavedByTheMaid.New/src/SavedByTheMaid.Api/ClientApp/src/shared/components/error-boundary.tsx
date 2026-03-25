import { ErrorBoundary as ReactErrorBoundary, type FallbackProps } from 'react-error-boundary';
import type { ReactNode, ErrorInfo } from 'react';

function getErrorMessage(error: unknown): string {
  if (error instanceof Error) return error.message;
  if (typeof error === 'string') return error;
  return 'An unexpected error occurred. Please try again.';
}

function DefaultFallback({ error, resetErrorBoundary }: FallbackProps) {
  return (
    <div className="flex min-h-[400px] flex-col items-center justify-center p-8 text-center" role="alert">
      <div className="rounded-full bg-red-100 p-4 mb-4">
        <svg className="h-8 w-8 text-red-500" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.082 16.5c-.77.833.192 2.5 1.732 2.5z" />
        </svg>
      </div>
      <h2 className="text-xl font-semibold text-gray-900 mb-2">Something went wrong</h2>
      <p className="text-gray-500 mb-6 max-w-md">
        {getErrorMessage(error)}
      </p>
      <button
        onClick={resetErrorBoundary}
        className="rounded-lg bg-brand px-4 py-2 text-sm font-medium text-white hover:bg-brand-dark transition-colors"
      >
        Try Again
      </button>
    </div>
  );
}

function PageFallback({ resetErrorBoundary }: FallbackProps) {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center p-8 text-center" role="alert">
      <div className="rounded-full bg-red-100 p-6 mb-6">
        <svg className="h-12 w-12 text-red-500" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.082 16.5c-.77.833.192 2.5 1.732 2.5z" />
        </svg>
      </div>
      <h1 className="text-2xl font-bold text-gray-900 mb-2">Application Error</h1>
      <p className="text-gray-500 mb-8 max-w-lg">
        We're sorry, something went wrong. Please try refreshing the page.
      </p>
      <div className="flex gap-4">
        <button
          onClick={resetErrorBoundary}
          className="rounded-lg bg-brand px-6 py-3 text-sm font-medium text-white hover:bg-brand-dark transition-colors"
        >
          Try Again
        </button>
        <a
          href="/"
          className="rounded-lg border border-gray-300 px-6 py-3 text-sm font-medium text-gray-700 hover:bg-gray-50 transition-colors"
        >
          Go Home
        </a>
      </div>
    </div>
  );
}

interface AppErrorBoundaryProps {
  children: ReactNode;
  variant?: 'page' | 'section';
  onError?: (error: unknown, info: ErrorInfo) => void;
}

export function AppErrorBoundary({ children, variant = 'section', onError }: AppErrorBoundaryProps) {
  const handleError = (error: unknown, info: ErrorInfo) => {
    // Log to console in development
    if (import.meta.env.DEV) {
      console.error('ErrorBoundary caught:', error, info);
    }
    // Call custom handler if provided (e.g., Sentry)
    onError?.(error, info);
  };

  return (
    <ReactErrorBoundary
      FallbackComponent={variant === 'page' ? PageFallback : DefaultFallback}
      onError={handleError}
      onReset={() => window.location.reload()}
    >
      {children}
    </ReactErrorBoundary>
  );
}
