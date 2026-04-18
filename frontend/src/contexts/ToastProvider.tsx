import React from 'react';
import { X } from 'lucide-react';
import { useToasts } from '@/lib/toast';
import { cn } from '@/lib/utils';

const variantStyles = {
  error: 'bg-red-50 border-red-200 text-red-800',
  success: 'bg-green-50 border-green-200 text-green-800',
  warning: 'bg-yellow-50 border-yellow-200 text-yellow-800',
  info: 'bg-blue-50 border-blue-200 text-blue-800',
} as const;

/**
 * Renders the toast tray. The store owns state + timers; this component
 * is just a subscriber that maps `toasts[]` to DOM. Auto-dismiss happens
 * inside the store (single source of truth).
 *
 * Mounted once at the app root via React tree — keeps the `{children}`
 * wrapping for backwards compatibility with the previous Provider API.
 */
export function ToastProvider({ children }: { children: React.ReactNode }) {
  const { toasts, dismiss } = useToasts();

  return (
    <>
      {children}
      <div
        aria-live="polite"
        aria-atomic="false"
        className="fixed right-4 bottom-4 z-50 flex flex-col gap-3 pb-safe"
      >
        {toasts.map((t) => (
          <div
            key={t.id}
            role={t.variant === 'error' ? 'alert' : 'status'}
            className={cn(
              'max-w-sm w-full rounded-lg p-3 shadow-md border flex items-start gap-2',
              variantStyles[t.variant]
            )}
          >
            <div className="text-sm flex-1">{t.message}</div>
            <button
              type="button"
              onClick={() => dismiss(t.id)}
              aria-label="Dismiss notification"
              className="shrink-0 rounded p-0.5 opacity-70 hover:opacity-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-current"
            >
              <X className="h-4 w-4" aria-hidden="true" />
            </button>
          </div>
        ))}
      </div>
    </>
  );
}
