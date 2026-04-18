export type OrderStatus = 'PendingReview' | 'Draft' | 'Confirmed' | 'InProgress' | 'Completed' | 'Cancelled' | 'NoShow';
export type PaymentStatus = 'Pending' | 'Authorized' | 'Paid' | 'Refunded' | 'Failed';
export type MeetStatus = 'Scheduled' | 'Assigned' | 'OnTheWay' | 'InProgress' | 'Completed' | 'Cancelled' | 'Rescheduled' | 'NoShow';

interface StatusConfig {
  label: string;
  color: string;       // Tailwind text color class
  bgColor: string;     // Tailwind bg color class
  icon: string;        // lucide-react icon name
}

export const orderStatusConfig: Record<OrderStatus, StatusConfig> = {
  PendingReview: { label: 'Pending Review', color: 'text-orange-700', bgColor: 'bg-orange-100', icon: 'Clock' },
  Draft: { label: 'Draft', color: 'text-gray-700', bgColor: 'bg-gray-100', icon: 'FileText' },
  Confirmed: { label: 'Confirmed', color: 'text-blue-700', bgColor: 'bg-blue-100', icon: 'CheckCircle' },
  InProgress: { label: 'In Progress', color: 'text-purple-700', bgColor: 'bg-purple-100', icon: 'Clock' },
  Completed: { label: 'Completed', color: 'text-green-700', bgColor: 'bg-green-100', icon: 'CheckCheck' },
  Cancelled: { label: 'Cancelled', color: 'text-red-700', bgColor: 'bg-red-100', icon: 'XCircle' },
  NoShow: { label: 'No Show', color: 'text-red-700', bgColor: 'bg-red-200', icon: 'AlertTriangle' },
};

export const meetStatusConfig: Record<MeetStatus, StatusConfig> = {
  Scheduled: { label: 'Scheduled', color: 'text-gray-700', bgColor: 'bg-gray-100', icon: 'Calendar' },
  Assigned: { label: 'Assigned', color: 'text-blue-700', bgColor: 'bg-blue-100', icon: 'UserCheck' },
  OnTheWay: { label: 'On The Way', color: 'text-cyan-700', bgColor: 'bg-cyan-100', icon: 'Navigation' },
  InProgress: { label: 'In Progress', color: 'text-purple-700', bgColor: 'bg-purple-100', icon: 'PlayCircle' },
  Completed: { label: 'Completed', color: 'text-green-700', bgColor: 'bg-green-100', icon: 'CheckCircle' },
  Cancelled: { label: 'Cancelled', color: 'text-red-700', bgColor: 'bg-red-100', icon: 'XCircle' },
  Rescheduled: { label: 'Rescheduled', color: 'text-orange-700', bgColor: 'bg-orange-100', icon: 'CalendarClock' },
  NoShow: { label: 'No Show', color: 'text-red-700', bgColor: 'bg-red-100', icon: 'AlertTriangle' },
};

export const paymentStatusConfig: Record<PaymentStatus, StatusConfig> = {
  Pending: { label: 'Pending', color: 'text-yellow-700', bgColor: 'bg-yellow-100', icon: 'Clock' },
  Authorized: { label: 'Authorized', color: 'text-blue-700', bgColor: 'bg-blue-100', icon: 'ShieldCheck' },
  Paid: { label: 'Paid', color: 'text-green-700', bgColor: 'bg-green-100', icon: 'CheckCircle' },
  Refunded: { label: 'Refunded', color: 'text-orange-700', bgColor: 'bg-orange-100', icon: 'RotateCcw' },
  Failed: { label: 'Failed', color: 'text-red-700', bgColor: 'bg-red-100', icon: 'XCircle' },
};

/**
 * Helper to get status config for any status string, with a safe fallback.
 * Useful when the status comes from an API response as a plain string.
 */
export function getOrderStatusConfig(status: string): StatusConfig {
  return orderStatusConfig[status as OrderStatus] ?? { label: status, color: 'text-gray-700', bgColor: 'bg-gray-100', icon: 'HelpCircle' };
}

export function getMeetStatusConfig(status: string): StatusConfig {
  return meetStatusConfig[status as MeetStatus] ?? { label: status, color: 'text-gray-700', bgColor: 'bg-gray-100', icon: 'HelpCircle' };
}

export function getPaymentStatusConfig(status: string): StatusConfig {
  return paymentStatusConfig[status as PaymentStatus] ?? { label: status, color: 'text-gray-700', bgColor: 'bg-gray-100', icon: 'HelpCircle' };
}
