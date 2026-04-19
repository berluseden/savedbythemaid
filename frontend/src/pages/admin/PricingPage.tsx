import { useState, useEffect } from 'react';
import {
  Plus,
  Edit2,
  Trash2,
  Percent,
  Settings,
  Repeat,
} from 'lucide-react';
import { Spinner } from '@/shared/components/ui/spinner';
import { AdminLayout } from '../../components/admin/AdminLayout';
import api from '../../lib/api';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/shared/components/ui/dialog';
import { AlertDialog, AlertDialogContent, AlertDialogHeader, AlertDialogTitle, AlertDialogDescription, AlertDialogFooter, AlertDialogAction, AlertDialogCancel } from '@/shared/components/ui/alert-dialog';

// Backend types
interface PriceMultiplier {
  id: number;
  name: string;
  description: string | null;
  conditionType: number;
  factor: number;
  minValue: number | null;
  maxValue: number | null;
  appliesToTime: boolean;
  appliesToPrice: boolean;
  serviceTypeId: number | null;
  serviceType: { id: number; name: string } | null;
  displayOrder: number;
  isActive: boolean;
}

import type { AdminRecurrenceDiscount } from './pricing/types';

const CONDITION_TYPES = [
  { value: 0, label: 'Square Footage', description: 'Multiplier based on property size (sq ft)' },
  { value: 1, label: 'Dirt Level', description: 'Multiplier based on cleaning intensity needed' },
  { value: 2, label: 'Has Pets', description: 'Adjustment for homes with pets' },
  { value: 3, label: 'First Time', description: 'First-time cleaning surcharge' },
  { value: 4, label: 'Floor Level', description: 'Multiplier based on floor number' },
  { value: 5, label: 'No Elevator', description: 'Surcharge for no elevator access' },
  { value: 6, label: 'Extra Rooms', description: 'Additional rooms multiplier' },
];

const RECURRENCE_TYPES = [
  { value: 0, label: 'One-time' },
  { value: 1, label: 'Weekly' },
  { value: 2, label: 'Bi-Weekly' },
  { value: 3, label: 'Monthly' },
];

type ActiveTab = 'multipliers' | 'recurrence';

export function AdminPricingPage() {
  const [activeTab, setActiveTab] = useState<ActiveTab>('multipliers');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);

  // Multipliers state
  const [multipliers, setMultipliers] = useState<PriceMultiplier[]>([]);
  const [showModal, setShowModal] = useState(false);
  const [editingMultiplier, setEditingMultiplier] = useState<PriceMultiplier | null>(null);
  const [deleteConfirm, setDeleteConfirm] = useState<number | null>(null);

  const [form, setForm] = useState({
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
  });

  // Recurrence discounts state
  const [discounts, setDiscounts] = useState<AdminRecurrenceDiscount[]>([]);
  const [discountForm, setDiscountForm] = useState({
    recurrenceType: 1,
    discountPercent: 0,
  });

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    setIsLoading(true);
    try {
      const [multipliersRes, discountsRes] = await Promise.all([
        api.get<PriceMultiplier[]>('/admin/pricemultipliers'),
        api.get<AdminRecurrenceDiscount[]>('/admin/pricemultipliers/recurrence-discounts'),
      ]);
      setMultipliers(multipliersRes.data);
      setDiscounts(discountsRes.data);
    } catch {
      setError('Error loading pricing data');
    } finally {
      setIsLoading(false);
    }
  };

  // Multiplier CRUD
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError('');

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
    };

    try {
      if (editingMultiplier) {
        await api.put(`/admin/pricemultipliers/${editingMultiplier.id}`, payload);
      } else {
        await api.post('/admin/pricemultipliers', payload);
      }
      await fetchData();
      closeModal();
    } catch {
      setError('Error saving multiplier');
    } finally {
      setSaving(false);
    }
  };

  const handleEdit = (m: PriceMultiplier) => {
    setEditingMultiplier(m);
    setForm({
      name: m.name,
      description: m.description || '',
      conditionType: m.conditionType,
      factor: m.factor,
      minValue: m.minValue ?? '',
      maxValue: m.maxValue ?? '',
      appliesToTime: m.appliesToTime,
      appliesToPrice: m.appliesToPrice,
      serviceTypeId: m.serviceTypeId,
      displayOrder: m.displayOrder,
      isActive: m.isActive,
    });
    setShowModal(true);
  };

  const handleDelete = async (id: number) => {
    try {
      await api.delete(`/admin/pricemultipliers/${id}`);
      await fetchData();
      setDeleteConfirm(null);
    } catch {
      setError('Error deleting multiplier');
    }
  };

  const closeModal = () => {
    setShowModal(false);
    setEditingMultiplier(null);
    setForm({
      name: '', description: '', conditionType: 0, factor: 1.0,
      minValue: '', maxValue: '', appliesToTime: true, appliesToPrice: true,
      serviceTypeId: null, displayOrder: 0, isActive: true,
    });
  };

  // Recurrence discounts
  const handleSaveDiscount = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      await api.post('/admin/pricemultipliers/recurrence-discounts', discountForm);
      await fetchData();
      setDiscountForm({ recurrenceType: 1, discountPercent: 0 });
    } catch {
      setError('Error saving discount');
    } finally {
      setSaving(false);
    }
  };

  const formatFactor = (factor: number) => {
    const pct = ((factor - 1) * 100).toFixed(0);
    if (factor > 1) return `+${pct}%`;
    if (factor < 1) return `${pct}%`;
    return 'Base (×1.0)';
  };

  // Group multipliers by conditionType
  const grouped = CONDITION_TYPES.map(ct => ({
    ...ct,
    items: multipliers.filter(m => m.conditionType === ct.value),
  })).filter(g => g.items.length > 0);

  if (isLoading) {
    return (
      <AdminLayout>
        <div className="flex items-center justify-center h-64">
          <Spinner size="md" />
        </div>
      </AdminLayout>
    );
  }

  return (
    <AdminLayout>
      <div className="space-y-6">
        {/* Page header */}
        <div className="mb-8 flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Pricing</h1>
            <p className="mt-1 text-sm text-gray-500">Configure pricing multipliers and discounts</p>
          </div>
          {activeTab === 'multipliers' && (
            <button
              onClick={() => setShowModal(true)}
              className="inline-flex items-center gap-2 px-4 py-2 bg-brand text-white rounded-lg hover:bg-brand-dark transition-colors"
            >
              <Plus className="h-5 w-5" />
              New Multiplier
            </button>
          )}
        </div>

        {error && (
          <div className="bg-red-50 text-red-600 px-4 py-3 rounded-lg">
            {error}
            <button onClick={() => setError('')} className="ml-2 underline">Close</button>
          </div>
        )}

        {/* Tabs */}
        <div className="border-b border-gray-200">
          <nav className="flex gap-8">
            <button
              onClick={() => setActiveTab('multipliers')}
              className={`flex items-center gap-2 pb-4 border-b-2 font-medium text-sm transition-colors ${
                activeTab === 'multipliers'
                  ? 'border-brand text-brand'
                  : 'border-transparent text-gray-500 hover:text-gray-700'
              }`}
            >
              <Settings className="h-4 w-4" />
              Price Multipliers ({multipliers.length})
            </button>
            <button
              onClick={() => setActiveTab('recurrence')}
              className={`flex items-center gap-2 pb-4 border-b-2 font-medium text-sm transition-colors ${
                activeTab === 'recurrence'
                  ? 'border-brand text-brand'
                  : 'border-transparent text-gray-500 hover:text-gray-700'
              }`}
            >
              <Repeat className="h-4 w-4" />
              Recurrence Discounts ({discounts.length})
            </button>
          </nav>
        </div>

        {/* Multipliers Tab */}
        {activeTab === 'multipliers' && (
          <div className="space-y-6">
            {grouped.length === 0 ? (
              <div className="text-center py-12 bg-white rounded-xl border">
                <Settings className="h-12 w-12 text-gray-300 mx-auto mb-4" />
                <p className="text-gray-500">No price multipliers configured</p>
                <button
                  onClick={() => setShowModal(true)}
                  className="mt-4 text-brand hover:underline"
                >
                  Create the first one
                </button>
              </div>
            ) : (
              grouped.map((group) => (
                <div key={group.value} className="bg-white rounded-xl shadow-sm border">
                  <div className="px-6 py-4 border-b bg-gray-50 rounded-t-xl">
                    <h3 className="font-semibold text-gray-900">{group.label}</h3>
                    <p className="text-sm text-gray-500">{group.description}</p>
                  </div>
                  <div className="overflow-x-auto">
                    <table className="w-full">
                      <thead>
                        <tr className="border-b">
                          <th className="text-left py-3 px-6 font-medium text-gray-600 text-sm">Name</th>
                          <th className="text-left py-3 px-6 font-medium text-gray-600 text-sm">Range</th>
                          <th className="text-left py-3 px-6 font-medium text-gray-600 text-sm">Factor</th>
                          <th className="text-left py-3 px-6 font-medium text-gray-600 text-sm">Applies To</th>
                          <th className="text-left py-3 px-6 font-medium text-gray-600 text-sm">Status</th>
                          <th className="text-right py-3 px-6 font-medium text-gray-600 text-sm">Actions</th>
                        </tr>
                      </thead>
                      <tbody>
                        {group.items.map((m) => (
                          <tr key={m.id} className={`border-b last:border-0 ${!m.isActive ? 'opacity-50' : ''}`}>
                            <td className="py-4 px-6">
                              <p className="font-medium text-gray-900">{m.name}</p>
                              {m.description && <p className="text-xs text-gray-500">{m.description}</p>}
                            </td>
                            <td className="py-4 px-6 text-sm text-gray-600">
                              {m.minValue != null || m.maxValue != null ? (
                                <>
                                  {m.minValue != null ? m.minValue : '—'} → {m.maxValue != null ? m.maxValue : '∞'}
                                </>
                              ) : (
                                <span className="text-gray-400">—</span>
                              )}
                            </td>
                            <td className="py-4 px-6">
                              <span className={`inline-flex items-center gap-1 px-2 py-1 rounded font-medium text-sm ${
                                m.factor > 1
                                  ? 'bg-amber-50 text-amber-700'
                                  : m.factor < 1
                                    ? 'bg-green-50 text-green-700'
                                    : 'bg-gray-50 text-gray-700'
                              }`}>
                                <Percent className="h-3 w-3" />
                                {formatFactor(m.factor)} (×{m.factor})
                              </span>
                            </td>
                            <td className="py-4 px-6 text-sm">
                              <div className="flex gap-2">
                                {m.appliesToPrice && (
                                  <span className="px-2 py-0.5 bg-blue-50 text-blue-700 text-xs rounded">Price</span>
                                )}
                                {m.appliesToTime && (
                                  <span className="px-2 py-0.5 bg-purple-50 text-purple-700 text-xs rounded">Time</span>
                                )}
                              </div>
                            </td>
                            <td className="py-4 px-6">
                              <span className={`text-sm ${m.isActive ? 'text-green-600' : 'text-gray-400'}`}>
                                {m.isActive ? 'Active' : 'Inactive'}
                              </span>
                            </td>
                            <td className="py-4 px-6 text-right">
                              <div className="flex items-center justify-end gap-1">
                                <button
                                  onClick={() => handleEdit(m)}
                                  className="p-2 text-gray-400 hover:text-brand hover:bg-brand/10 rounded-lg"
                                >
                                  <Edit2 className="h-4 w-4" />
                                </button>
                                <button
                                  onClick={() => setDeleteConfirm(m.id)}
                                  className="p-2 text-gray-400 hover:text-red-500 hover:bg-red-50 rounded-lg"
                                >
                                  <Trash2 className="h-4 w-4" />
                                </button>
                              </div>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              ))
            )}
          </div>
        )}

        {/* Recurrence Discounts Tab */}
        {activeTab === 'recurrence' && (
          <div className="space-y-6">
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

            <div className="bg-white rounded-xl shadow-sm border p-6">
              <h3 className="font-semibold text-gray-900 mb-4">Add / Update Discount</h3>
              <form onSubmit={handleSaveDiscount} className="flex flex-col sm:flex-row gap-4 items-end">
                <div className="flex-1">
                  <label className="block text-sm font-medium text-gray-700 mb-2">Recurrence Type</label>
                  <select
                    value={discountForm.recurrenceType}
                    onChange={(e) => setDiscountForm({ ...discountForm, recurrenceType: parseInt(e.target.value) })}
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
                    value={discountForm.discountPercent}
                    onChange={(e) => setDiscountForm({ ...discountForm, discountPercent: parseFloat(e.target.value) || 0 })}
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
          </div>
        )}

        {/* Formula Info */}
        <div className="bg-gradient-to-r from-brand to-brand-dark rounded-xl p-6 text-white">
          <h3 className="text-lg font-semibold mb-2">How pricing is calculated</h3>
          <p className="text-blue-100 text-sm">
            Final Price = (Base Service Price + Room Prices) × Condition Multipliers − Recurrence Discount
          </p>
          <p className="text-xs text-blue-200 mt-2">
            Each active multiplier whose condition matches the booking is applied. Multipliers that apply to time affect estimated duration; those that apply to price affect cost.
          </p>
        </div>
      </div>

      {/* Delete Confirmation */}
      <AlertDialog open={deleteConfirm !== null} onOpenChange={(open) => !open && setDeleteConfirm(null)}>
        <AlertDialogContent className="max-w-sm">
          <AlertDialogHeader>
            <AlertDialogTitle>Delete multiplier?</AlertDialogTitle>
            <AlertDialogDescription>This action cannot be undone.</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter className="flex gap-3 sm:space-x-0">
            <AlertDialogCancel className="flex-1" onClick={() => setDeleteConfirm(null)}>Cancel</AlertDialogCancel>
            <AlertDialogAction className="flex-1" onClick={() => deleteConfirm !== null && handleDelete(deleteConfirm)}>Delete</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Create/Edit Modal */}
      <Dialog open={showModal} onOpenChange={(open) => !open && closeModal()}>
        <DialogContent className="max-w-lg">
          <DialogHeader>
            <DialogTitle>{editingMultiplier ? 'Edit Multiplier' : 'New Multiplier'}</DialogTitle>
          </DialogHeader>

            <form onSubmit={handleSubmit} className="space-y-4">
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

              {editingMultiplier && (
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
                  onClick={closeModal}
                  className="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={saving}
                  className="flex-1 px-4 py-2 bg-brand text-white rounded-lg hover:bg-brand-dark disabled:opacity-50"
                >
                  {saving ? 'Saving...' : editingMultiplier ? 'Save' : 'Create'}
                </button>
              </div>
            </form>
        </DialogContent>
      </Dialog>
    </AdminLayout>
  );
}
