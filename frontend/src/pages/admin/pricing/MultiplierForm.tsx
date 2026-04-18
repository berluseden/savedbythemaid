import { useState, useEffect } from 'react';
import { X } from 'lucide-react';
import { type PriceMultiplier, CONDITION_TYPES } from './types';

interface MultiplierFormProps {
  isOpen: boolean;
  isEditing: boolean;
  editingItem: PriceMultiplier | null;
  saving: boolean;
  onSubmit: (payload: Omit<PriceMultiplier, 'id' | 'serviceType'> & { id?: number }) => Promise<void>;
  onClose: () => void;
}

const INITIAL_FORM = {
  name: '',
  description: '',
  conditionType: 0,
  factor: 1.0,
  minValue: '' as string | number,
  maxValue: '' as string | number,
  appliesToTime: true,
  appliesToPrice: true,
  serviceTypeId: null as number | null,
  displayOrder: 0,
  isActive: true,
};

export function MultiplierForm({ isOpen, isEditing, editingItem, saving, onSubmit, onClose }: MultiplierFormProps) {
  const [form, setForm] = useState(INITIAL_FORM);

  useEffect(() => {
    if (isOpen && editingItem) {
      setForm({
        name: editingItem.name,
        description: editingItem.description || '',
        conditionType: editingItem.conditionType,
        factor: editingItem.factor,
        minValue: editingItem.minValue ?? '',
        maxValue: editingItem.maxValue ?? '',
        appliesToTime: editingItem.appliesToTime,
        appliesToPrice: editingItem.appliesToPrice,
        serviceTypeId: editingItem.serviceTypeId,
        displayOrder: editingItem.displayOrder,
        isActive: editingItem.isActive,
      });
    } else if (isOpen) {
      setForm(INITIAL_FORM);
    }
  }, [isOpen, editingItem]);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const payload = {
      name: form.name,
      description: form.description || null,
      conditionType: form.conditionType,
      factor: form.factor,
      minValue: form.minValue !== '' ? Number(form.minValue) : null,
      maxValue: form.maxValue !== '' ? Number(form.maxValue) : null,
      appliesToTime: form.appliesToTime,
      appliesToPrice: form.appliesToPrice,
      serviceTypeId: form.serviceTypeId,
      displayOrder: form.displayOrder,
      isActive: form.isActive,
      ...(isEditing && editingItem ? { id: editingItem.id } : {}),
    };
    await onSubmit(payload as Omit<PriceMultiplier, 'id' | 'serviceType'> & { id?: number });
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between p-6 border-b">
          <h2 className="text-xl font-semibold text-gray-900">
            {isEditing ? 'Edit Multiplier' : 'New Multiplier'}
          </h2>
          <button onClick={onClose} className="p-2 text-gray-400 hover:text-gray-600">
            <X className="h-5 w-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="p-6 space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Name *</label>
            <input
              type="text"
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
              placeholder="E.g.: Large area surcharge"
              className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
              required
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Description</label>
            <input
              type="text"
              value={form.description}
              onChange={(e) => setForm({ ...form, description: e.target.value })}
              placeholder="Optional description"
              className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Condition Type *</label>
            <select
              value={form.conditionType}
              onChange={(e) => setForm({ ...form, conditionType: parseInt(e.target.value) })}
              className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
            >
              {CONDITION_TYPES.map(ct => (
                <option key={ct.value} value={ct.value}>{ct.label}</option>
              ))}
            </select>
          </div>

          <div className="grid grid-cols-3 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Factor *</label>
              <input
                type="number"
                step="0.01"
                min="0"
                value={form.factor}
                onChange={(e) => setForm({ ...form, factor: parseFloat(e.target.value) || 1 })}
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
                required
              />
              <p className="text-xs text-gray-400 mt-1">1.0 = no change, 1.2 = +20%</p>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Min Value</label>
              <input
                type="number"
                step="0.01"
                value={form.minValue}
                onChange={(e) => setForm({ ...form, minValue: e.target.value === '' ? '' : parseFloat(e.target.value) })}
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Max Value</label>
              <input
                type="number"
                step="0.01"
                value={form.maxValue}
                onChange={(e) => setForm({ ...form, maxValue: e.target.value === '' ? '' : parseFloat(e.target.value) })}
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={form.appliesToPrice}
                onChange={(e) => setForm({ ...form, appliesToPrice: e.target.checked })}
                className="w-4 h-4 text-brand border-gray-300 rounded focus:ring-brand"
              />
              <span className="text-sm text-gray-700">Applies to price</span>
            </label>
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={form.appliesToTime}
                onChange={(e) => setForm({ ...form, appliesToTime: e.target.checked })}
                className="w-4 h-4 text-brand border-gray-300 rounded focus:ring-brand"
              />
              <span className="text-sm text-gray-700">Applies to time</span>
            </label>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Display Order</label>
            <input
              type="number"
              min="0"
              value={form.displayOrder}
              onChange={(e) => setForm({ ...form, displayOrder: parseInt(e.target.value) || 0 })}
              className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
            />
          </div>

          {isEditing && (
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
                className="w-4 h-4 text-brand border-gray-300 rounded focus:ring-brand"
              />
              <span className="text-sm text-gray-700">Active</span>
            </label>
          )}

          <div className="flex gap-3 pt-4">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={saving}
              className="flex-1 px-4 py-2 bg-brand text-white rounded-lg hover:bg-brand-dark disabled:opacity-50"
            >
              {saving ? 'Saving...' : isEditing ? 'Save' : 'Create'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
