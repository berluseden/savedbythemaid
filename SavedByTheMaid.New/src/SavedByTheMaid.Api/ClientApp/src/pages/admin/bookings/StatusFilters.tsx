import { orderStatusConfig } from '@/shared/lib/status-config';
import type { OrderSummary } from './types';

interface StatusFiltersProps {
  bookings: OrderSummary[];
  filterStatus: string;
  onFilterChange: (status: string) => void;
}

export function StatusFilters({ bookings, filterStatus, onFilterChange }: StatusFiltersProps) {
  return (
    <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
      {Object.entries(orderStatusConfig).map(([status, config]) => (
        <button
          key={status}
          onClick={() => onFilterChange(filterStatus === status ? 'all' : status)}
          className={`p-4 rounded-lg border transition-all ${
            filterStatus === status
              ? 'ring-2 ring-brand border-brand'
              : 'hover:border-gray-300'
          } bg-white`}
        >
          <p className="text-sm text-gray-500">{config.label}</p>
          <p className={`text-2xl font-bold ${config.color}`}>
            {bookings.filter(b => b.orderStatus === status).length}
          </p>
        </button>
      ))}
    </div>
  );
}
