import { useState } from 'react';
import {
  Plus,
  Search,
  Edit2,
  Trash2,
  Sparkles,
  DollarSign,
  Clock,
  X,
  ToggleLeft,
  ToggleRight,
} from 'lucide-react';
import { AdminLayout } from '../../components/admin/AdminLayout';
import { useCRUD } from '@/shared/hooks/use-crud';
import { useFormModal } from '@/shared/hooks/use-form-modal';
import { getErrorMessage } from '@/shared/lib/error-utils';

interface AdditionalService {
  id: number;
  title: string;
  description: string | null;
  price: number;
  additionalMinutes: number;
  isActive: boolean;
}

export function AdminAdditionalServicesPage() {
  const { data: services, isLoading, create, update, remove } = useCRUD<AdditionalService>({
    endpoint: '/admin/additionalservices',
    queryKey: ['admin', 'additional-services'],
  });

  const modal = useFormModal<AdditionalService>();
  const [searchTerm, setSearchTerm] = useState('');
  const [deleteConfirm, setDeleteConfirm] = useState<number | null>(null);
  const [error, setError] = useState('');

  const [form, setForm] = useState({
    title: '',
    description: '',
    price: 0,
    additionalMinutes: 30,
    isActive: true,
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    try {
      if (modal.isEditing && modal.editingItem) {
        await update.mutateAsync({ ...form, id: modal.editingItem.id } as AdditionalService);
      } else {
        await create.mutateAsync(form);
      }
      closeModal();
    } catch (err) {
      setError(getErrorMessage(err, 'Error saving additional service'));
    }
  };

  const handleEdit = (service: AdditionalService) => {
    setForm({
      title: service.title,
      description: service.description || '',
      price: service.price,
      additionalMinutes: service.additionalMinutes,
      isActive: service.isActive,
    });
    modal.open(service);
  };

  const handleDelete = async (id: number) => {
    try {
      await remove.mutateAsync(id);
      setDeleteConfirm(null);
    } catch (err) {
      setError(getErrorMessage(err, 'Error deleting service'));
    }
  };

  const handleToggleActive = async (service: AdditionalService) => {
    try {
      await update.mutateAsync({
        ...service,
        isActive: !service.isActive,
      });
    } catch (err) {
      setError(getErrorMessage(err, 'Error updating service'));
    }
  };

  const openCreateModal = () => {
    setForm({ title: '', description: '', price: 0, additionalMinutes: 30, isActive: true });
    modal.open();
  };

  const closeModal = () => {
    modal.close();
    setForm({ title: '', description: '', price: 0, additionalMinutes: 30, isActive: true });
  };

  const filteredServices = services.filter(
    service =>
      service.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
      (service.description?.toLowerCase().includes(searchTerm.toLowerCase()) ?? false)
  );

  const formatCurrency = (amount: number) =>
    new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(amount);

  const totalRevenue = services.reduce((acc, s) => acc + s.price, 0);

  const saving = create.isPending || update.isPending;

  if (isLoading) {
    return (
      <AdminLayout>
        <div className="flex items-center justify-center h-64">
          <div className="w-8 h-8 border-4 border-brand border-t-transparent rounded-full animate-spin" />
        </div>
      </AdminLayout>
    );
  }

  return (
    <AdminLayout>
      <div className="space-y-6">
        {/* Header */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Additional Services</h1>
            <p className="text-gray-600">Manage extra services that can be added to bookings</p>
          </div>
          <button
            onClick={openCreateModal}
            className="inline-flex items-center gap-2 px-4 py-2 bg-brand text-white rounded-lg hover:bg-brand-dark transition-colors"
          >
            <Plus className="h-5 w-5" />
            New Service
          </button>
        </div>

        {error && (
          <div className="bg-red-50 text-red-600 px-4 py-3 rounded-lg">
            {error}
            <button onClick={() => setError('')} className="ml-2 underline">Close</button>
          </div>
        )}

        {/* Search */}
        <div className="relative max-w-md">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-gray-400" />
          <input
            type="search"
            placeholder="Search additional services..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
          />
        </div>

        {/* Stats */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div className="bg-white rounded-lg p-4 border">
            <div className="flex items-center gap-2">
              <Sparkles className="h-5 w-5 text-purple-500" />
              <p className="text-sm text-gray-500">Total Services</p>
            </div>
            <p className="text-2xl font-bold text-gray-900 mt-1">{services.length}</p>
          </div>
          <div className="bg-white rounded-lg p-4 border">
            <div className="flex items-center gap-2">
              <ToggleRight className="h-5 w-5 text-green-500" />
              <p className="text-sm text-gray-500">Active</p>
            </div>
            <p className="text-2xl font-bold text-green-600 mt-1">
              {services.filter(s => s.isActive).length}
            </p>
          </div>
          <div className="bg-white rounded-lg p-4 border">
            <div className="flex items-center gap-2">
              <DollarSign className="h-5 w-5 text-brand" />
              <p className="text-sm text-gray-500">Average Price</p>
            </div>
            <p className="text-2xl font-bold text-brand mt-1">
              {formatCurrency(services.length > 0 ? totalRevenue / services.length : 0)}
            </p>
          </div>
          <div className="bg-white rounded-lg p-4 border">
            <div className="flex items-center gap-2">
              <Clock className="h-5 w-5 text-emerald-500" />
              <p className="text-sm text-gray-500">Avg. Time</p>
            </div>
            <p className="text-2xl font-bold text-emerald-600 mt-1">
              {services.length > 0 ? Math.round(services.reduce((acc, s) => acc + s.additionalMinutes, 0) / services.length) : 0} min
            </p>
          </div>
        </div>

        {/* Services Grid */}
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {filteredServices.map((service) => (
            <div
              key={service.id}
              className={`bg-white rounded-xl shadow-sm border p-5 ${
                !service.isActive ? 'opacity-60' : ''
              }`}
            >
              <div className="flex items-start justify-between mb-3">
                <div className="w-10 h-10 bg-purple-100 rounded-lg flex items-center justify-center">
                  <Sparkles className="h-5 w-5 text-purple-600" />
                </div>
                <div className="flex items-center gap-1">
                  <button
                    onClick={() => handleToggleActive(service)}
                    className={`p-1 rounded ${
                      service.isActive
                        ? 'text-green-600 hover:bg-green-50'
                        : 'text-gray-400 hover:bg-gray-50'
                    }`}
                    title={service.isActive ? 'Deactivate' : 'Activate'}
                  >
                    {service.isActive ? (
                      <ToggleRight className="h-5 w-5" />
                    ) : (
                      <ToggleLeft className="h-5 w-5" />
                    )}
                  </button>
                  <button
                    onClick={() => handleEdit(service)}
                    className="p-1 text-gray-400 hover:text-brand hover:bg-accent-light/10 rounded"
                    title="Edit"
                  >
                    <Edit2 className="h-4 w-4" />
                  </button>
                  <button
                    onClick={() => setDeleteConfirm(service.id)}
                    className="p-1 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded"
                    title="Delete"
                  >
                    <Trash2 className="h-4 w-4" />
                  </button>
                </div>
              </div>

              <h3 className="font-semibold text-gray-900 mb-1">{service.title}</h3>
              <p className="text-sm text-gray-500 mb-4 line-clamp-2">
                {service.description || 'No description'}
              </p>

              <div className="grid grid-cols-2 gap-2 text-sm">
                <div className="text-center p-2 bg-accent-light/10 rounded-lg">
                  <p className="text-brand text-xs">Price</p>
                  <p className="font-semibold text-brand-dark">{formatCurrency(service.price)}</p>
                </div>
                <div className="text-center p-2 bg-purple-50 rounded-lg">
                  <p className="text-purple-600 text-xs">Time</p>
                  <p className="font-semibold text-purple-700 flex items-center justify-center gap-1">
                    <Clock className="h-3 w-3" />
                    {service.additionalMinutes}m
                  </p>
                </div>
              </div>
            </div>
          ))}
        </div>

        {filteredServices.length === 0 && (
          <div className="text-center py-12">
            <Sparkles className="h-12 w-12 text-gray-300 mx-auto mb-4" />
            <p className="text-gray-500">No additional services found</p>
            <button
              onClick={openCreateModal}
              className="mt-4 text-brand hover:underline"
            >
              Create the first one
            </button>
          </div>
        )}
      </div>

      {/* Delete Confirmation Modal */}
      {deleteConfirm !== null && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50">
          <div className="bg-white rounded-xl shadow-xl p-6 w-full max-w-sm">
            <h3 className="text-lg font-semibold text-gray-900 mb-2">
              Delete additional service?
            </h3>
            <p className="text-gray-600 mb-6">This action cannot be undone.</p>
            <div className="flex gap-3">
              <button
                onClick={() => setDeleteConfirm(null)}
                className="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={() => handleDelete(deleteConfirm)}
                className="flex-1 px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600"
              >
                Delete
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Create/Edit Modal */}
      {modal.isOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg">
            <div className="flex items-center justify-between p-6 border-b">
              <h2 className="text-xl font-semibold text-gray-900">
                {modal.isEditing ? 'Edit Additional Service' : 'New Additional Service'}
              </h2>
              <button onClick={closeModal} className="p-2 text-gray-400 hover:text-gray-600">
                <X className="h-5 w-5" />
              </button>
            </div>

            <form onSubmit={handleSubmit} className="p-6 space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Title *</label>
                <input
                  type="text"
                  value={form.title}
                  onChange={(e) => setForm({ ...form, title: e.target.value })}
                  placeholder="E.g.: Refrigerator cleaning"
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
                  required
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Description</label>
                <textarea
                  value={form.description}
                  onChange={(e) => setForm({ ...form, description: e.target.value })}
                  placeholder="Additional service description"
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
                  rows={3}
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Price ($) *</label>
                  <input
                    type="number"
                    min="0"
                    step="0.01"
                    value={form.price}
                    onChange={(e) => setForm({ ...form, price: parseFloat(e.target.value) || 0 })}
                    className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
                    required
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Extra minutes</label>
                  <input
                    type="number"
                    min="0"
                    step="5"
                    value={form.additionalMinutes}
                    onChange={(e) => setForm({ ...form, additionalMinutes: parseInt(e.target.value) || 0 })}
                    className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
                  />
                </div>
              </div>

              {modal.isEditing && (
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
                  {saving ? 'Saving...' : modal.isEditing ? 'Save' : 'Create'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </AdminLayout>
  );
}
