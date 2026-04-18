import { cn } from '@/lib/utils';

type StatusVariant =
  | 'pending'
  | 'confirmed'
  | 'assigned'
  | 'in-progress'
  | 'completed'
  | 'cancelled'
  | 'no-show';

const statusStyles: Record<StatusVariant, { bg: string; text: string; label: string }> = {
  pending:       { bg: 'bg-yellow-100', text: 'text-yellow-800', label: 'Pending' },
  confirmed:     { bg: 'bg-blue-100',   text: 'text-blue-700',   label: 'Confirmed' },
  assigned:      { bg: 'bg-blue-100',   text: 'text-blue-700',   label: 'Assigned' },
  'in-progress': { bg: 'bg-purple-100', text: 'text-purple-700', label: 'In Progress' },
  completed:     { bg: 'bg-green-100',  text: 'text-green-700',  label: 'Completed' },
  cancelled:     { bg: 'bg-red-100',    text: 'text-red-700',    label: 'Cancelled' },
  'no-show':     { bg: 'bg-gray-100',   text: 'text-gray-700',   label: 'No Show' },
};

export interface StatusBadgeProps {
  /** The status string (case-insensitive, will be normalised) */
  status: string;
  /** Override the displayed label */
  label?: string;
  className?: string;
}

function normaliseStatus(raw: string): StatusVariant {
  const key = raw.toLowerCase().replace(/[\s_]+/g, '-') as StatusVariant;
  return key in statusStyles ? key : 'pending';
}

export function StatusBadge({ status, label, className }: StatusBadgeProps) {
  const variant = normaliseStatus(status);
  const style = statusStyles[variant];

  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium',
        style.bg,
        style.text,
        className
      )}
    >
      {label ?? style.label}
    </span>
  );
}
