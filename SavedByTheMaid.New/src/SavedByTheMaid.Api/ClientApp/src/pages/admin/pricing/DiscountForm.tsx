import { useState } from 'react';
import { RECURRENCE_TYPES } from './types';

interface DiscountFormProps {
  saving: boolean;
  onSubmit: (data: { recurrenceType: number; discountPercent: number }) => Promise<void>;
}

export function DiscountForm({ saving, onSubmit }: DiscountFormProps) {
  const [form, setForm] = useState({
    recurrenceType: 1,
    discountPercent: 0,
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await onSubmit(form);
    setForm({ recurrenceType: 1, discountPercent: 0 });
  };

  return (
    <div className="bg-white rounded-xl shadow-sm border p-6">
      <h3 className="font-semibold text-gray-900 mb-4">Add / Update Discount</h3>
      <form onSubmit={handleSubmit} className="flex flex-col sm:flex-row gap-4 items-end">
        <div className="flex-1">
          <label className="block text-sm font-medium text-gray-700 mb-2">Recurrence Type</label>
          <select
            value={form.recurrenceType}
            onChange={(e) => setForm({ ...form, recurrenceType: parseInt(e.target.value) })}
            className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
          >
            {RECURRENCE_TYPES.filter(r => r.value !== 0).map(r => (
              <option key={r.value} value={r.value}>{r.label}</option>
            ))}
          </select>
        </div>
        <div className="flex-1">
          <label className="block text-sm font-medium text-gray-700 mb-2">Discount (%)</label>
          <input
            type="number"
            min="0"
            max="100"
            step="0.5"
            value={form.discountPercent}
            onChange={(e) => setForm({ ...form, discountPercent: parseFloat(e.target.value) || 0 })}
            className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
          />
        </div>
        <button
          type="submit"
          disabled={saving}
          className="px-6 py-2 bg-brand text-white rounded-lg hover:bg-brand-dark disabled:opacity-50"
        >
          {saving ? 'Saving...' : 'Save'}
        </button>
      </form>
      <p className="text-xs text-gray-400 mt-3">
        If a discount for this recurrence type already exists, it will be updated.
      </p>
    </div>
  );
}
