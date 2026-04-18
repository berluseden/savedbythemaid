import { type AdminRecurrenceDiscount, RECURRENCE_TYPES } from './types';

interface DiscountTableProps {
  discounts: AdminRecurrenceDiscount[];
}

export function DiscountTable({ discounts }: DiscountTableProps) {
  return (
    <div className="bg-white rounded-xl shadow-sm border p-6">
      <h3 className="font-semibold text-gray-900 mb-4">Current Discounts</h3>
      {discounts.length === 0 ? (
        <p className="text-gray-500 text-center py-4">No recurrence discounts configured</p>
      ) : (
        <div className="grid gap-3 md:grid-cols-3">
          {discounts.map((d) => (
            <div key={d.id} className="p-4 border rounded-lg">
              <p className="text-sm text-gray-500">
                {RECURRENCE_TYPES.find(r => r.value === d.recurrenceType)?.label || `Type ${d.recurrenceType}`}
              </p>
              <p className="text-2xl font-bold text-brand">{d.discountPercent}%</p>
              <p className="text-xs text-gray-400">discount</p>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
