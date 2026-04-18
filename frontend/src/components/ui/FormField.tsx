import { useId, type ReactNode } from 'react';
import { cn } from '@/lib/utils';

export interface FormFieldProps {
  /** Visible label text */
  label: string;
  /** Validation error message */
  error?: string;
  /** Mark the field as required (appends asterisk) */
  required?: boolean;
  /** Extra helper text shown below the input */
  hint?: string;
  /** Additional className for the wrapper */
  className?: string;
  /** The input / select / textarea element */
  children: ReactNode;
}

export function FormField({
  label,
  error,
  required,
  hint,
  className,
  children,
}: FormFieldProps) {
  const generatedId = useId();
  const fieldId = `field-${generatedId}`;
  const errorId = error ? `${fieldId}-error` : undefined;
  const hintId = hint ? `${fieldId}-hint` : undefined;

  // Build aria-describedby from available IDs
  const describedBy = [errorId, hintId].filter(Boolean).join(' ') || undefined;

  return (
    <div className={cn('space-y-1.5', className)}>
      <label htmlFor={fieldId} className="block text-sm font-medium text-text-primary">
        {label}
        {required && <span className="ml-0.5 text-danger" aria-hidden="true">*</span>}
      </label>

      {/* Wrapper passes data attributes for consumers that need field metadata */}
      <div
        data-field-id={fieldId}
        data-described-by={describedBy}
        data-invalid={!!error || undefined}
      >
        {children}
      </div>

      {hint && !error && (
        <p id={hintId} className="text-xs text-text-secondary">
          {hint}
        </p>
      )}

      {error && (
        <p id={errorId} className="text-sm text-danger" role="alert">
          {error}
        </p>
      )}
    </div>
  );
}
