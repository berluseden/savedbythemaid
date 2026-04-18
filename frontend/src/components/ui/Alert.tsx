import React from 'react';
import { cn } from '@/lib/utils';
import { AlertCircle, CheckCircle, Info, XCircle } from 'lucide-react';

export type AlertVariant = 'info' | 'success' | 'warning' | 'error';

interface AlertProps {
  variant?: AlertVariant;
  title?: string;
  children: React.ReactNode;
  className?: string;
  onClose?: () => void;
}

export function Alert({ variant = 'info', title, children, className, onClose }: AlertProps) {
  const variants = {
    info: 'bg-blue-50 text-blue-800 border-blue-200',
    success: 'bg-green-50 text-green-800 border-green-200',
    warning: 'bg-yellow-50 text-yellow-800 border-yellow-200',
    error: 'bg-red-50 text-red-800 border-red-200'
  };

  const icons = {
    info: Info,
    success: CheckCircle,
    warning: AlertCircle,
    error: XCircle
  };

  const Icon = icons[variant];

  return (
    <div className={cn('flex items-start gap-3 rounded-lg border p-4', variants[variant], className)} role="alert">
      <Icon className="h-5 w-5 shrink-0" />
      <div className="flex-1">
        {title && <h3 className="font-semibold mb-1">{title}</h3>}
        <div className="text-sm opacity-90">{children}</div>
      </div>
      {onClose && (
        <button
          onClick={onClose}
          className="ml-auto -mr-1 -mt-1 rounded-full p-1 opacity-70 hover:opacity-100 transition-opacity"
          aria-label="Close alert"
        >
          <XCircle className="h-4 w-4" />
        </button>
      )}
    </div>
  );
}
