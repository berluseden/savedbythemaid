import type { ReactNode } from 'react';
import { Suspense } from 'react';

function LoadingSpinner({ message = 'Loading...' }: { message?: string }) {
  return (
    <div className="flex min-h-[400px] items-center justify-center" role="status">
      <div className="flex flex-col items-center gap-3">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-brand border-t-transparent" aria-hidden="true" />
        <span className="text-sm text-gray-500">{message}</span>
      </div>
    </div>
  );
}

function PageLoadingSpinner() {
  return (
    <div className="flex min-h-screen items-center justify-center" role="status">
      <div className="flex flex-col items-center gap-3">
        <div className="h-10 w-10 animate-spin rounded-full border-4 border-brand border-t-transparent" aria-hidden="true" />
        <span className="text-sm text-gray-500">Loading page...</span>
      </div>
    </div>
  );
}

interface SuspenseBoundaryProps {
  children: ReactNode;
  variant?: 'page' | 'section';
  message?: string;
}

export function SuspenseBoundary({ children, variant = 'section', message }: SuspenseBoundaryProps) {
  return (
    <Suspense
      fallback={
        variant === 'page' ? <PageLoadingSpinner /> : <LoadingSpinner message={message} />
      }
    >
      {children}
    </Suspense>
  );
}

export { LoadingSpinner, PageLoadingSpinner };
