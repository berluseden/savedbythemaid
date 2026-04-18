import { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  Plus,
  Search,
  Edit2,
  Trash2,
  MoreVertical,
  DollarSign,
  Clock,
  X,
} from 'lucide-react';
import { AdminLayout } from '../../components/admin/AdminLayout';
import api from '../../lib/api';
import {
  serviceTypeSchema,
  type ServiceTypeFormData,
} from '@/shared/schemas/admin.schema';

interface ServiceType {
  id: number;
  name: string;
  description: string;
  price: number;
  pricePerBedroom: number;
  pricePerBathroom: number;
  estimatedMinutes: number;
  minutesPerBedroom: number;
  minutesPerBathroom: number;
  displayOrder: number;
  isActive: boolean;
}

const defaultValues: ServiceTypeFormData = {
  name: '',
  description: '',
  price: 0,
  pricePerBedroom: 15,
  pricePerBathroom: 20,
  estimatedMinutes: 60,
  minutesPerBedroom: 20,
  minutesPerBathroom: 15,
  displayOrder: 0,
  isActive: true,
};

export function AdminServicesPage() {
  const [services, setServices] = useState<ServiceType[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [editingService, setEditingService] = useState<ServiceType | null>(null);
  const [deleteConfirm, setDeleteConfirm] = useState<number | null>(null);
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);

  const form = useForm<ServiceTypeFormData>({
    resolver: zodResolver(serviceTypeSchema),
    defaultValues,
  });

  useEffect(() => {
    fetchServices();
  }, []);

  const fetchServices = async () => {
    setIsLoading(true);
    try {
      const response = await api.get<ServiceType[]>('/admin/service-types');
      setServices(response.data);
    } catch (_err) {
      setError('Error loading services');
    } finally {
      setIsLoading(false);
    }
  };

  const onSubmit = form.handleSubmit(async (data) => {
    setSaving(true);
    setError('');

    try {
      if (editingService) {
        await api.put(`/admin/service-types/${editingService.id}`, data);
      } else {
        await api.post('/admin/service-types', data);
      }
      await fetchServices();
      closeModal();
    } catch (_err) {
      setError('Error saving service');
    } finally {
      setSaving(false);
    }
  });

  const handleEdit = (service: ServiceType) => {
    setEditingService(service);
    form.reset({
      name: service.name,
      description: service.description || '',
      price: service.price,
      pricePerBedroom: service.pricePerBedroom || 15,
      pricePerBathroom: service.pricePerBathroom || 20,
      estimatedMinutes: service.estimatedMinutes,
      minutesPerBedroom: service.minutesPerBedroom || 20,
      minutesPerBathroom: service.minutesPerBathroom || 15,
      displayOrder: service.displayOrder,
      isActive: service.isActive,
    });
    setShowModal(true);
  };

  const handleDelete = async (id: number) => {
    try {
      await api.delete(`/admin/service-types/${id}`);
      await fetchServices();
      setDeleteConfirm(null);
    } catch (_err) {
      setError('Error deleting service');
    }
  };

  const toggleActive = async (service: ServiceType) => {
    try {
      await api.put(`/admin/service-types/${service.id}`, {
        ...service,
        isActive: !service.isActive,
      });
      await fetchServices();
    } catch (_err) {
      setError('Error updating service');
    }
  };

  const closeModal = () => {
    setShowModal(false);
    setEditingService(null);
    form.reset(defaultValues);
  };

  const filteredServices = services.filter(
    service =>
      service.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      (service.description?.toLowerCase().includes(searchTerm.toLowerCase()) ?? false)
  );

  const formatCurrency = (amount: number) =>
    new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(amount);

  const formatDuration = (minutes: number) => {
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    return hours > 0 ? `${hours}h ${mins > 0 ? `${mins}m` : ''}` : `${mins}m`;
  };

  if (isLoading) {
    return (
      <AdminLayout>
        <div className="flex items-center justify-center h-64">
          <div className="w-8 h-8 border-4 border-[#2196f3] border-t-transparent rounded-full animate-spin" />
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
            <h1 className="text-2xl font-bold text-gray-900">Services</h1>
            <p className="mt-1 text-sm text-gray-500">Manage service types and pricing</p>
          </div>
          <button
            onClick={() => { form.reset(defaultValues); setShowModal(true); }}
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
            placeholder="Search services..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#2196f3] focus:border-transparent"
          />
        </div>

        {/* Stats */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div className="bg-white rounded-lg p-4 border">
            <p className="text-sm text-gray-500">Total</p>
            <p className="text-2xl font-bold text-gray-900">{services.length}</p>
          </div>
          <div className="bg-white rounded-lg p-4 border">
            <p className="text-sm text-gray-500">Active</p>
            <p className="text-2xl font-bold text-green-600">
              {services.filter(s => s.isActive).length}
            </p>
          </div>
          <div className="bg-white rounded-lg p-4 border">
            <p className="text-sm text-gray-500">Average price</p>
            <p className="text-2xl font-bold text-[#2196f3]">
              {services.length > 0
                ? formatCurrency(services.reduce((acc, s) => acc + s.price, 0) / services.length)
                : '$0'}
            </p>
          </div>
          <div className="bg-white rounded-lg p-4 border">
            <p className="text-sm text-gray-500">Average duration</p>
            <p className="text-2xl font-bold text-purple-600">
              {services.length > 0
                ? formatDuration(Math.round(services.reduce((acc, s) => acc + s.estimatedMinutes, 0) / services.length))
                : '0m'}
            </p>
          </div>
        </div>

        {/* Services Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {filteredServices.map((service) => (
            <div
              key={service.id}
              className={`bg-white rounded-xl shadow-sm border p-6 ${
                !service.isActive ? 'opacity-60' : ''
              }`}
            >
              <div className="flex items-start justify-between mb-4">
                <div>
                  <h3 className="text-lg font-semibold text-gray-900">{service.name}</h3>
                  <span className="text-xs text-gray-500 bg-gray-100 px-2 py-1 rounded">
                    Order: {service.displayOrder}
                  </span>
                </div>
                <div className="relative">
                  <button
                    onClick={() => setDeleteConfirm(deleteConfirm === service.id ? null : service.id)}
                    className="p-2 text-gray-400 hover:text-gray-600 rounded-lg hover:bg-gray-100"
                  >
                    <MoreVertical className="h-5 w-5" />
                  </button>
                  {deleteConfirm === service.id && (
                    <div className="absolute right-0 mt-2 w-36 bg-white rounded-lg shadow-lg border z-10">
                      <button
                        onClick={() => {
                          handleEdit(service);
                          setDeleteConfirm(null);
                        }}
                        className="w-full flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
                      >
                        <Edit2 className="h-4 w-4" />
                        Edit
                      </button>
                      <button
                        onClick={() => handleDelete(service.id)}
                        className="w-full flex items-center gap-2 px-4 py-2 text-sm text-red-600 hover:bg-red-50"
                      >
                        <Trash2 className="h-4 w-4" />
                        Delete
                      </button>
                    </div>
                  )}
                </div>
              </div>

              <p className="text-sm text-gray-600 mb-4 line-clamp-2">
                {service.description || 'No description'}
              </p>

              <div className="flex items-center gap-4 mb-4">
                <div className="flex items-center gap-1 text-gray-600">
                  <DollarSign className="h-4 w-4" />
                  <span className="font-semibold">{formatCurrency(service.price)}</span>
                </div>
                <div className="flex items-center gap-1 text-gray-600">
                  <Clock className="h-4 w-4" />
                  <span>{formatDuration(service.estimatedMinutes)}</span>
                </div>
              </div>

              <div className="text-xs text-gray-500 mb-4">
                +{formatCurrency(service.pricePerBedroom)}/bedroom | +{formatCurrency(service.pricePerBathroom)}/bathroom
              </div>

              <div className="flex items-center justify-between pt-4 border-t">
                <span className={`text-sm font-medium ${service.isActive ? 'text-green-600' : 'text-gray-400'}`}>
                  {service.isActive ? 'Active' : 'Inactive'}
                </span>
                <button
                  onClick={() => toggleActive(service)}
                  className={`relative w-12 h-6 rounded-full transition-colors ${
                    service.isActive ? 'bg-green-500' : 'bg-gray-300'
                  }`}
                >
                  <span
                    className={`absolute top-1 w-4 h-4 bg-white rounded-full transition-transform ${
                      service.isActive ? 'left-7' : 'left-1'
                    }`}
                  />
                </button>
              </div>
            </div>
          ))}
        </div>

        {filteredServices.length === 0 && (
          <div className="text-center py-12">
            <p className="text-gray-500">No services found</p>
          </div>
        )}
      </div>

      {/* Modal */}
      {showModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg max-h-[90vh] overflow-y-auto">
            <div className="flex items-center justify-between p-6 border-b">
              <h2 className="text-xl font-semibold text-gray-900">
                {editingService ? 'Edit Service' : 'New Service'}
              </h2>
              <button onClick={closeModal} className="p-2 text-gray-400 hover:text-gray-600">
                <X className="h-5 w-5" />
              </button>
            </div>

            <form onSubmit={onSubmit} className="p-6 space-y-4">
              {form.formState.errors.root && (
                <p className="text-sm text-red-500">{form.formState.errors.root.message}</p>
              )}

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Service name *
                </label>
                <input
                  type="text"
                  {...form.register('name')}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#2196f3] focus:border-transparent"
                />
                {form.formState.errors.name && (
                  <p className="text-sm text-red-500 mt-1">{form.formState.errors.name.message}</p>
                )}
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Description
                </label>
                <textarea
                  {...form.register('description')}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#2196f3] focus:border-transparent"
                  rows={3}
                />
                {form.formState.errors.description && (
                  <p className="text-sm text-red-500 mt-1">{form.formState.errors.description.message}</p>
                )}
              </div>

              <div className="bg-gray-50 p-4 rounded-lg space-y-4">
                <h3 className="font-medium text-gray-900">Pricing</h3>
                <div className="grid grid-cols-3 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Base Price ($) *
                    </label>
                    <input
                      type="number"
                      step="0.01"
                      min="0"
                      {...form.register('price', { valueAsNumber: true })}
                      className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#2196f3] focus:border-transparent"
                    />
                    {form.formState.errors.price && (
                      <p className="text-sm text-red-500 mt-1">{form.formState.errors.price.message}</p>
                    )}
                    <p className="text-xs text-gray-500 mt-1">Includes 1 bedroom + 1 bathroom</p>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Per Extra Bedroom ($)
                    </label>
                    <input
                      type="number"
                      step="0.01"
                      min="0"
                      {...form.register('pricePerBedroom', { valueAsNumber: true })}
                      className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#2196f3] focus:border-transparent"
                    />
                    {form.formState.errors.pricePerBedroom && (
                      <p className="text-sm text-red-500 mt-1">{form.formState.errors.pricePerBedroom.message}</p>
                    )}
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Per Extra Bathroom ($)
                    </label>
                    <input
                      type="number"
                      step="0.01"
                      min="0"
                      {...form.register('pricePerBathroom', { valueAsNumber: true })}
                      className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#2196f3] focus:border-transparent"
                    />
                    {form.formState.errors.pricePerBathroom && (
                      <p className="text-sm text-red-500 mt-1">{form.formState.errors.pricePerBathroom.message}</p>
                    )}
                  </div>
                </div>
              </div>

              <div className="bg-gray-50 p-4 rounded-lg space-y-4">
                <h3 className="font-medium text-gray-900">Estimated Duration</h3>
                <div className="grid grid-cols-3 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Base (minutes)
                    </label>
                    <input
                      type="number"
                      min="15"
                      step="15"
                      {...form.register('estimatedMinutes', { valueAsNumber: true })}
                      className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#2196f3] focus:border-transparent"
                    />
                    {form.formState.errors.estimatedMinutes && (
                      <p className="text-sm text-red-500 mt-1">{form.formState.errors.estimatedMinutes.message}</p>
                    )}
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Per Bedroom (min)
                    </label>
                    <input
                      type="number"
                      min="0"
                      step="5"
                      {...form.register('minutesPerBedroom', { valueAsNumber: true })}
                      className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#2196f3] focus:border-transparent"
                    />
                    {form.formState.errors.minutesPerBedroom && (
                      <p className="text-sm text-red-500 mt-1">{form.formState.errors.minutesPerBedroom.message}</p>
                    )}
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Per Bathroom (min)
                    </label>
                    <input
                      type="number"
                      min="0"
                      step="5"
                      {...form.register('minutesPerBathroom', { valueAsNumber: true })}
                      className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#2196f3] focus:border-transparent"
                    />
                    {form.formState.errors.minutesPerBathroom && (
                      <p className="text-sm text-red-500 mt-1">{form.formState.errors.minutesPerBathroom.message}</p>
                    )}
                  </div>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Display order
                  </label>
                  <input
                    type="number"
                    min="0"
                    {...form.register('displayOrder', { valueAsNumber: true })}
                    className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#2196f3] focus:border-transparent"
                  />
                  {form.formState.errors.displayOrder && (
                    <p className="text-sm text-red-500 mt-1">{form.formState.errors.displayOrder.message}</p>
                  )}
                </div>
                <div className="flex items-end">
                  <label className="flex items-center gap-2 cursor-pointer pb-2">
                    <input
                      type="checkbox"
                      {...form.register('isActive')}
                      className="w-4 h-4 text-[#2196f3] border-gray-300 rounded focus:ring-[#2196f3]"
                    />
                    <span className="text-sm text-gray-700">Service active</span>
                  </label>
                </div>
              </div>

              <div className="flex gap-3 pt-4">
                <button
                  type="button"
                  onClick={closeModal}
                  className="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 transition-colors"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={saving}
                  className="flex-1 px-4 py-2 bg-[#2196f3] text-white rounded-lg hover:bg-[#29338c] transition-colors disabled:opacity-50"
                >
                  {saving ? 'Saving...' : editingService ? 'Save Changes' : 'Create Service'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </AdminLayout>
  );
}
