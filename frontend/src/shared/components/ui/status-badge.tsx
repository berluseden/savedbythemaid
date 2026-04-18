import type { ElementType } from 'react';
import {
  CheckCircle,
  Clock,
  CheckCheck,
  XCircle,
  AlertTriangle,
  FileText,
  Calendar,
  UserCheck,
  Truck,
} from 'lucide-react';
import { cn } from '@/lib/utils';

interface StatusConfig {
  label: string;
  icon: ElementType;
  className: string;
}

const statusConfig: Record<string, StatusConfig> = {
  Confirmed: {
    label: 'Confirmed',
    icon: CheckCircle,
    className: 'bg-blue-100 text-blue-800',
  },
  InProgress: {
    label: 'In Progress',
    icon: Clock,
    className: 'bg-purple-100 text-purple-800',
  },
  Completed: {
    label: 'Completed',
    icon: CheckCheck,
    className: 'bg-green-100 text-green-800',
  },
  Cancelled: {
    label: 'Cancelled',
    icon: XCircle,
    className: 'bg-red-100 text-red-800',
  },
  NoShow: {
    label: 'No Show',
    icon: AlertTriangle,
    className: 'bg-orange-100 text-orange-800',
  },
  PendingReview: {
    label: 'Pending Review',
    icon: Clock,
    className: 'bg-yellow-100 text-yellow-800',
  },
  Draft: {
    label: 'Draft',
    icon: FileText,
    className: 'bg-gray-100 text-gray-800',
  },
  Scheduled: {
    label: 'Scheduled',
    icon: Calendar,
    className: 'bg-blue-100 text-blue-800',
  },
  Assigned: {
    label: 'Assigned',
    icon: UserCheck,
    className: 'bg-indigo-100 text-indigo-800',
  },
  OnTheWay: {
    label: 'On the Way',
    icon: Truck,
    className: 'bg-cyan-100 text-cyan-800',
  },
};

export interface StatusBadgeProps {
  status: string;
  className?: string;
}

function StatusBadge({ status, className }: StatusBadgeProps) {
  const config = statusConfig[status];

  if (!config) {
    return (
      <span className={cn('inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-medium bg-gray-100 text-gray-800', className)}>
        {status}
      </span>
    );
  }

  const Icon = config.icon;

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-medium',
        config.className,
        className
      )}
    >
      <Icon className="h-3.5 w-3.5" aria-hidden="true" />
      {config.label}
    </span>
  );
}
StatusBadge.displayName = 'StatusBadge';

export { StatusBadge, statusConfig };
