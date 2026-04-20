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
  ChevronDown,
  ChevronUp,
  Percent,
  Settings,
} from 'lucide-react';
import { AdminLayout } from '@/components/admin/AdminLayout';
import api from '@/lib/api';
import {
  serviceTypeSchema,
  type ServiceTypeFormData,
} from '@/shared/schemas/admin.schema';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/shared/components/ui/dialog';
import {
  AlertDialog,
  AlertDialogContent,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogAction,
  AlertDialogCancel,
} from '@/shared/components/ui/alert-dialog';
import { Spinner } from '@/shared/components/ui/spinner';

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

const CONDITION_TYPES = [
  { value: 0, label: 'Square Footage' },
  { value: 1, label: 'Dirt Level' },
  { value: 2, label: 'Has Pets' },
  { value: 3, label: 'First Time' },
  { value: 4, label: 'Floor Level' },
  { value: 5, label: 'No Elevator' },
  { value: 6, label: 'Extra Rooms' },
];

const defaultServiceValues: ServiceTypeFormData = {
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

const defaultMultiplierForm = {
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

export function AdminServicesPage() {
  const [services, setServices] = useState<ServiceType[]>([]);
  const [multipliers, setMultipliers] = useState<PriceMultiplier[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [error, setError] = useState('');

  // Service modal state
  const [showServiceModal, setShowServiceModal] = useState(false);
  const [editingService, setEditingService] = useState<ServiceType | null>(null);
  const [serviceMenuOpen, setServiceMenuOpen] = useState<number | null>(null);
  const [savingService, setSavingService] = useState(false);

  // Multiplier modal state
  const [showMultiplierModal, setShowMultiplierModal] = useState(false);
  const [editingMultiplier, setEditingMultiplier] = useState<PriceMultiplier | null>(null);
  const [multiplierForm, setMultiplierForm] = useState(defaultMultiplierForm);
  const [savingMultiplier, setSavingMultiplier] = useState(false);

  // Accordion: which service cards show their conditions
  const [expandedServices, setExpandedServices] = useState<Set<number>>(new Set());

  // Delete confirmation
  const [deleteConfirm, setDeleteConfirm] = useState<{ type: 'service' | 'multiplier'; id: number } | null>(null);

  const form = useForm<ServiceTypeFormData>({
    resolver: zodResolver(serviceTypeSchema),
    defaultValues: defaultServiceValues,
  });

  useEffect(() => {
    fetchAll();
  }, []);

  const fetchAll = async () => {
    setIsLoading(true);
    try {
      const [svcRes, multRes] = await Promise.all([
        api.get<ServiceType[]>('/admin/service-types'),
        api.get<PriceMultiplier[]>('/admin/pricemultipliers'),
      ]);
      setServices(svcRes.data);
      setMultipliers(multRes.data);
    } catch {
      setError('Error loading data');
    } finally {
      setIsLoading(false);
    }
  };

  // ── Service CRUD ──────────────────────────────────────────────────────────

  const onServiceSubmit = form.handleSubmit(async (data) => {
    setSavingService(true);
    setError('');
    try {
      if (editingService) {
        await api.put(`/admin/service-types/${editingService.id}`, data);
      } else {
        await api.post('/admin/service-types', data);
      }
      await fetchAll();
      closeServiceModal();
    } catch {
      setError('Error saving service');
    } finally {
      setSavingService(false);
    }
  });

  const handleEditService = (service: ServiceType) => {
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
    setShowServiceModal(true);
    setServiceMenuOpen(null);
  };

  const handleDeleteService = async (id: number) => {
    try {
      await api.delete(`/admin/service-types/${id}`);
      await fetchAll();
      setDeleteConfirm(null);
    } catch {
      setError('Error deleting service');
    }
  };

  const toggleServiceActive = async (service: ServiceType) => {
    try {
      await api.put(`/admin/service-types/${service.id}`, { ...service, isActive: !service.isActive });
      await fetchAll();
    } catch {
      setError('Error updating service');
    }
  };

  const closeServiceModal = () => {
    setShowServiceModal(false);
    setEditingService(null);
    form.reset(defaultServiceValues);
  };

  // ── Multiplier CRUD ───────────────────────────────────────────────────────

  const openNewMultiplier = (serviceTypeId: number | null = null) => {
    setEditingMultiplier(null);
    setMultiplierForm({ ...defaultMultiplierForm, serviceTypeId });
    setShowMultiplierModal(true);
  };

  const handleEditMultiplier = (m: PriceMultiplier) => {
    setEditingMultiplier(m);
    setMultiplierForm({
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
    setShowMultiplierModal(true);
  };

  const handleSaveMultiplier = async (e: React.FormEvent) => {
    e.preventDefault();
    setSavingMultiplier(true);
    setError('');
    const payload = {
      ...multiplierForm,
      minValue: multiplierForm.minValue !== '' ? Number(multiplierForm.minValue) : null,
      maxValue: multiplierForm.maxValue !== '' ? Number(multiplierForm.maxValue) : null,
    };
    try {
      if (editingMultiplier) {
        await api.put(`/admin/pricemultipliers/${editingMultiplier.id}`, payload);
      } else {
        await api.post('/admin/pricemultipliers', payload);
      }
      await fetchAll();
      closeMultiplierModal();
    } catch {
      setError('Error saving condition');
    } finally {
      setSavingMultiplier(false);
    }
  };

  const handleDeleteMultiplier = async (id: number) => {
    try {
      await api.delete(`/admin/pricemultipliers/${id}`);
      await fetchAll();
      setDeleteConfirm(null);
    } catch {
      setError('Error deleting condition');
    }
  };

  const closeMultiplierModal = () => {
    setShowMultiplierModal(false);
    setEditingMultiplier(null);
    setMultiplierForm(defaultMultiplierForm);
  };

  // ── Helpers ───────────────────────────────────────────────────────────────

  const formatCurrency = (amount: number) =>
    new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(amount);

  const formatDuration = (minutes: number) => {
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    return hours > 0 ? `${hours}h ${mins > 0 ? `${mins}m` : ''}` : `${mins}m`;
  };

  const formatFactor = (factor: number) => {
    const pct = ((factor - 1) * 100).toFixed(0);
    if (factor > 1) return `+${pct}%`;
    if (factor < 1) return `${pct}%`;
    return '×1.0';
  };

  const conditionLabel = (value: number) =>
    CONDITION_TYPES.find(c => c.value === value)?.label ?? `Type ${value}`;

  const toggleExpanded = (id: number) => {
    setExpandedServices(prev => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const serviceMultipliers = (serviceId: number) =>
    multipliers.filter(m => m.serviceTypeId === serviceId);

  const globalMultipliers = multipliers.filter(m => m.serviceTypeId === null);

  const filteredServices = services.filter(
    s =>
      s.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      (s.description?.toLowerCase().includes(searchTerm.toLowerCase()) ?? false),
  );

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
            <h1 className="text-2xl font-bold text-gray-900">Services & Pricing</h1>
            <p className="mt-1 text-sm text-gray-500">Manage service types and pricing conditions</p>
          </div>
          <button
            onClick={() => { form.reset(defaultServiceValues); setShowServiceModal(true); }}
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
            className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
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
            <p className="text-2xl font-bold text-green-600">{services.filter(s => s.isActive).length}</p>
          </div>
          <div className="bg-white rounded-lg p-4 border">
            <p className="text-sm text-gray-500">Average price</p>
            <p className="text-2xl font-bold text-brand">
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
          {filteredServices.map((service) => {
            const conditions = serviceMultipliers(service.id);
            const isExpanded = expandedServices.has(service.id);
            return (
              <div
                key={service.id}
                className={`bg-white rounded-xl shadow-sm border flex flex-col ${!service.isActive ? 'opacity-60' : ''}`}
              >
                <div className="p-6 flex-1">
                  <div className="flex items-start justify-between mb-4">
                    <div>
                      <h3 className="text-lg font-semibold text-gray-900">{service.name}</h3>
                      <span className="text-xs text-gray-500 bg-gray-100 px-2 py-1 rounded">
                        Order: {service.displayOrder}
                      </span>
                    </div>
                    <div className="relative">
                      <button
                        onClick={() => setServiceMenuOpen(serviceMenuOpen === service.id ? null : service.id)}
                        className="p-2 text-gray-400 hover:text-gray-600 rounded-lg hover:bg-gray-100"
                      >
                        <MoreVertical className="h-5 w-5" />
                      </button>
                      {serviceMenuOpen === service.id && (
                        <div className="absolute right-0 mt-2 w-36 bg-white rounded-lg shadow-lg border z-10">
                          <button
                            onClick={() => handleEditService(service)}
                            className="w-full flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
                          >
                            <Edit2 className="h-4 w-4" />
                            Edit
                          </button>
                          <button
                            onClick={() => { setDeleteConfirm({ type: 'service', id: service.id }); setServiceMenuOpen(null); }}
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

                  <div className="flex items-center gap-4 mb-3">
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
                </div>

                {/* Conditions accordion */}
                <div className="border-t">
                  <button
                    onClick={() => toggleExpanded(service.id)}
                    className="w-full flex items-center justify-between px-6 py-3 text-sm font-medium text-gray-600 hover:bg-gray-50 transition-colors"
                  >
                    <span className="flex items-center gap-1.5">
                      <Settings className="h-4 w-4" />
                      Conditions
                      {conditions.length > 0 && (
                        <span className="ml-1 px-1.5 py-0.5 text-xs bg-brand/10 text-brand rounded-full">
                          {conditions.length}
                        </span>
                      )}
                    </span>
                    {isExpanded ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
                  </button>

                  {isExpanded && (
                    <div className="px-6 pb-4 space-y-2">
                      {conditions.length === 0 ? (
                        <p className="text-xs text-gray-400 py-1">No service-specific conditions</p>
                      ) : (
                        conditions.map(c => (
                          <div key={c.id} className={`flex items-center justify-between py-1.5 border-b last:border-0 ${!c.isActive ? 'opacity-50' : ''}`}>
                            <div>
                              <span className="text-sm font-medium text-gray-800">{c.name}</span>
                              <span className="ml-2 text-xs text-gray-500">{conditionLabel(c.conditionType)}</span>
                            </div>
                            <div className="flex items-center gap-2">
                              <span className={`text-xs font-semibold px-1.5 py-0.5 rounded ${c.factor > 1 ? 'bg-amber-50 text-amber-700' : c.factor < 1 ? 'bg-green-50 text-green-700' : 'bg-gray-50 text-gray-600'}`}>
                                {formatFactor(c.factor)}
                              </span>
                              <button onClick={() => handleEditMultiplier(c)} className="p-1 text-gray-400 hover:text-brand rounded">
                                <Edit2 className="h-3.5 w-3.5" />
                              </button>
                              <button onClick={() => setDeleteConfirm({ type: 'multiplier', id: c.id })} className="p-1 text-gray-400 hover:text-red-500 rounded">
                                <Trash2 className="h-3.5 w-3.5" />
                              </button>
                            </div>
                          </div>
                        ))
                      )}
                      <button
                        onClick={() => openNewMultiplier(service.id)}
                        className="mt-2 flex items-center gap-1 text-xs text-brand hover:underline"
                      >
                        <Plus className="h-3.5 w-3.5" />
                        Add condition for this service
                      </button>
                    </div>
                  )}
                </div>

                {/* Active toggle */}
                <div className="flex items-center justify-between px-6 py-3 border-t bg-gray-50 rounded-b-xl">
                  <span className={`text-sm font-medium ${service.isActive ? 'text-green-600' : 'text-gray-400'}`}>
                    {service.isActive ? 'Active' : 'Inactive'}
                  </span>
                  <button
                    onClick={() => toggleServiceActive(service)}
                    className={`relative w-12 h-6 rounded-full transition-colors ${service.isActive ? 'bg-green-500' : 'bg-gray-300'}`}
                  >
                    <span className={`absolute top-1 w-4 h-4 bg-white rounded-full transition-transform ${service.isActive ? 'left-7' : 'left-1'}`} />
                  </button>
                </div>
              </div>
            );
          })}
        </div>

        {filteredServices.length === 0 && (
          <div className="text-center py-12">
            <p className="text-gray-500">No services found</p>
          </div>
        )}

        {/* Global Pricing Conditions */}
        <div className="mt-10">
          <div className="flex items-center justify-between mb-4">
            <div>
              <h2 className="text-lg font-semibold text-gray-900 flex items-center gap-2">
                <Percent className="h-5 w-5 text-brand" />
                Global Pricing Conditions
              </h2>
              <p className="text-sm text-gray-500">Apply to all services automatically based on booking details</p>
            </div>
            <button
              onClick={() => openNewMultiplier(null)}
              className="inline-flex items-center gap-2 px-4 py-2 bg-brand text-white rounded-lg hover:bg-brand-dark transition-colors text-sm"
            >
              <Plus className="h-4 w-4" />
              Add Condition
            </button>
          </div>

          {globalMultipliers.length === 0 ? (
            <div className="text-center py-10 bg-white rounded-xl border">
              <Settings className="h-10 w-10 text-gray-300 mx-auto mb-3" />
              <p className="text-gray-500 text-sm">No global conditions configured</p>
              <button onClick={() => openNewMultiplier(null)} className="mt-2 text-brand hover:underline text-sm">
                Create the first one
              </button>
            </div>
          ) : (
            <div className="bg-white rounded-xl shadow-sm border overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b bg-gray-50">
                    <th className="text-left py-3 px-6 font-medium text-gray-600 text-sm">Name</th>
                    <th className="text-left py-3 px-6 font-medium text-gray-600 text-sm">Condition</th>
                    <th className="text-left py-3 px-6 font-medium text-gray-600 text-sm">Range</th>
                    <th className="text-left py-3 px-6 font-medium text-gray-600 text-sm">Factor</th>
                    <th className="text-left py-3 px-6 font-medium text-gray-600 text-sm">Applies to</th>
                    <th className="text-left py-3 px-6 font-medium text-gray-600 text-sm">Status</th>
                    <th className="text-right py-3 px-6 font-medium text-gray-600 text-sm">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {globalMultipliers.map((m) => (
                    <tr key={m.id} className={`border-b last:border-0 ${!m.isActive ? 'opacity-50' : ''}`}>
                      <td className="py-4 px-6">
                        <p className="font-medium text-gray-900">{m.name}</p>
                        {m.description && <p className="text-xs text-gray-500">{m.description}</p>}
                      </td>
                      <td className="py-4 px-6 text-sm text-gray-600">{conditionLabel(m.conditionType)}</td>
                      <td className="py-4 px-6 text-sm text-gray-600">
                        {m.minValue != null || m.maxValue != null ? (
                          <>{m.minValue ?? '—'} → {m.maxValue ?? '∞'}</>
                        ) : (
                          <span className="text-gray-400">—</span>
                        )}
                      </td>
                      <td className="py-4 px-6">
                        <span className={`inline-flex items-center gap-1 px-2 py-1 rounded font-medium text-sm ${m.factor > 1 ? 'bg-amber-50 text-amber-700' : m.factor < 1 ? 'bg-green-50 text-green-700' : 'bg-gray-50 text-gray-700'}`}>
                          <Percent className="h-3 w-3" />
                          {formatFactor(m.factor)} (×{m.factor})
                        </span>
                      </td>
                      <td className="py-4 px-6 text-sm">
                        <div className="flex gap-2">
                          {m.appliesToPrice && <span className="px-2 py-0.5 bg-blue-50 text-blue-700 text-xs rounded">Price</span>}
                          {m.appliesToTime && <span className="px-2 py-0.5 bg-purple-50 text-purple-700 text-xs rounded">Time</span>}
                        </div>
                      </td>
                      <td className="py-4 px-6">
                        <span className={`text-sm ${m.isActive ? 'text-green-600' : 'text-gray-400'}`}>
                          {m.isActive ? 'Active' : 'Inactive'}
                        </span>
                      </td>
                      <td className="py-4 px-6 text-right">
                        <div className="flex items-center justify-end gap-1">
                          <button onClick={() => handleEditMultiplier(m)} className="p-2 text-gray-400 hover:text-brand hover:bg-brand/10 rounded-lg">
                            <Edit2 className="h-4 w-4" />
                          </button>
                          <button onClick={() => setDeleteConfirm({ type: 'multiplier', id: m.id })} className="p-2 text-gray-400 hover:text-red-500 hover:bg-red-50 rounded-lg">
                            <Trash2 className="h-4 w-4" />
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        {/* Pricing formula info */}
        <div className="bg-gradient-to-r from-brand to-brand-dark rounded-xl p-6 text-white">
          <h3 className="text-lg font-semibold mb-2">How pricing is calculated</h3>
          <p className="text-blue-100 text-sm">
            Final Price = (Base Service Price + Room Prices) × Condition Multipliers
          </p>
          <p className="text-xs text-blue-200 mt-2">
            Each active condition whose criteria matches the booking is applied. Conditions that apply to time affect estimated duration; those that apply to price affect cost.
          </p>
        </div>
      </div>

      {/* ── Service Modal ──────────────────────────────────────────────────── */}
      <Dialog open={showServiceModal} onOpenChange={(open) => !open && closeServiceModal()}>
        <DialogContent className="max-w-lg">
          <DialogHeader>
            <DialogTitle>{editingService ? 'Edit Service' : 'New Service'}</DialogTitle>
          </DialogHeader>
          <form onSubmit={onServiceSubmit} className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Service name *</label>
              <input
                type="text"
                {...form.register('name')}
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
              />
              {form.formState.errors.name && (
                <p className="text-sm text-red-500 mt-1">{form.formState.errors.name.message}</p>
              )}
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Description</label>
              <textarea
                {...form.register('description')}
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
                rows={3}
              />
            </div>

            <div className="bg-gray-50 p-4 rounded-lg space-y-4">
              <h3 className="font-medium text-gray-900">Pricing</h3>
              <div className="grid grid-cols-3 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Base Price ($) *</label>
                  <input type="number" step="0.01" min="0" {...form.register('price', { valueAsNumber: true })}
                    className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent" />
                  {form.formState.errors.price && (
                    <p className="text-sm text-red-500 mt-1">{form.formState.errors.price.message}</p>
                  )}
                  <p className="text-xs text-gray-500 mt-1">Includes 1 bedroom + 1 bathroom</p>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Per Extra Bedroom ($)</label>
                  <input type="number" step="0.01" min="0" {...form.register('pricePerBedroom', { valueAsNumber: true })}
                    className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent" />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Per Extra Bathroom ($)</label>
                  <input type="number" step="0.01" min="0" {...form.register('pricePerBathroom', { valueAsNumber: true })}
                    className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent" />
                </div>
              </div>
            </div>

            <div className="bg-gray-50 p-4 rounded-lg space-y-4">
              <h3 className="font-medium text-gray-900">Estimated Duration</h3>
              <div className="grid grid-cols-3 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Base (minutes)</label>
                  <input type="number" min="15" step="15" {...form.register('estimatedMinutes', { valueAsNumber: true })}
                    className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent" />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Per Bedroom (min)</label>
                  <input type="number" min="0" step="5" {...form.register('minutesPerBedroom', { valueAsNumber: true })}
                    className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent" />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Per Bathroom (min)</label>
                  <input type="number" min="0" step="5" {...form.register('minutesPerBathroom', { valueAsNumber: true })}
                    className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent" />
                </div>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Display order</label>
                <input type="number" min="0" {...form.register('displayOrder', { valueAsNumber: true })}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent" />
              </div>
              <div className="flex items-end">
                <label className="flex items-center gap-2 cursor-pointer pb-2">
                  <input type="checkbox" {...form.register('isActive')}
                    className="w-4 h-4 text-brand border-gray-300 rounded focus:ring-brand" />
                  <span className="text-sm text-gray-700">Service active</span>
                </label>
              </div>
            </div>

            <div className="flex gap-3 pt-4">
              <button type="button" onClick={closeServiceModal}
                className="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 transition-colors">
                Cancel
              </button>
              <button type="submit" disabled={savingService}
                className="flex-1 px-4 py-2 bg-brand text-white rounded-lg hover:bg-brand-dark transition-colors disabled:opacity-50">
                {savingService ? 'Saving...' : editingService ? 'Save Changes' : 'Create Service'}
              </button>
            </div>
          </form>
        </DialogContent>
      </Dialog>

      {/* ── Multiplier Modal ───────────────────────────────────────────────── */}
      <Dialog open={showMultiplierModal} onOpenChange={(open) => !open && closeMultiplierModal()}>
        <DialogContent className="max-w-lg">
          <DialogHeader>
            <DialogTitle>{editingMultiplier ? 'Edit Condition' : 'New Pricing Condition'}</DialogTitle>
          </DialogHeader>
          <form onSubmit={handleSaveMultiplier} className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Name *</label>
              <input type="text" value={multiplierForm.name}
                onChange={(e) => setMultiplierForm({ ...multiplierForm, name: e.target.value })}
                placeholder="E.g.: Large area surcharge"
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
                required />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Description</label>
              <input type="text" value={multiplierForm.description}
                onChange={(e) => setMultiplierForm({ ...multiplierForm, description: e.target.value })}
                placeholder="Optional description"
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent" />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Condition Type *</label>
              <select value={multiplierForm.conditionType}
                onChange={(e) => setMultiplierForm({ ...multiplierForm, conditionType: parseInt(e.target.value) })}
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent">
                {CONDITION_TYPES.map(ct => (
                  <option key={ct.value} value={ct.value}>{ct.label}</option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Applies to service</label>
              <select
                value={multiplierForm.serviceTypeId ?? ''}
                onChange={(e) => setMultiplierForm({ ...multiplierForm, serviceTypeId: e.target.value === '' ? null : parseInt(e.target.value) })}
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
              >
                <option value="">All services (global)</option>
                {services.map(s => (
                  <option key={s.id} value={s.id}>{s.name}</option>
                ))}
              </select>
            </div>

            <div className="grid grid-cols-3 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Factor *</label>
                <input type="number" step="0.01" min="0" value={multiplierForm.factor}
                  onChange={(e) => setMultiplierForm({ ...multiplierForm, factor: parseFloat(e.target.value) || 1 })}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
                  required />
                <p className="text-xs text-gray-400 mt-1">1.0 = no change, 1.2 = +20%</p>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Min Value</label>
                <input type="number" step="0.01" value={multiplierForm.minValue}
                  onChange={(e) => setMultiplierForm({ ...multiplierForm, minValue: e.target.value === '' ? '' : parseFloat(e.target.value) })}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Max Value</label>
                <input type="number" step="0.01" value={multiplierForm.maxValue}
                  onChange={(e) => setMultiplierForm({ ...multiplierForm, maxValue: e.target.value === '' ? '' : parseFloat(e.target.value) })}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent" />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <label className="flex items-center gap-2 cursor-pointer">
                <input type="checkbox" checked={multiplierForm.appliesToPrice}
                  onChange={(e) => setMultiplierForm({ ...multiplierForm, appliesToPrice: e.target.checked })}
                  className="w-4 h-4 text-brand border-gray-300 rounded focus:ring-brand" />
                <span className="text-sm text-gray-700">Applies to price</span>
              </label>
              <label className="flex items-center gap-2 cursor-pointer">
                <input type="checkbox" checked={multiplierForm.appliesToTime}
                  onChange={(e) => setMultiplierForm({ ...multiplierForm, appliesToTime: e.target.checked })}
                  className="w-4 h-4 text-brand border-gray-300 rounded focus:ring-brand" />
                <span className="text-sm text-gray-700">Applies to time</span>
              </label>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Display Order</label>
              <input type="number" min="0" value={multiplierForm.displayOrder}
                onChange={(e) => setMultiplierForm({ ...multiplierForm, displayOrder: parseInt(e.target.value) || 0 })}
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent" />
            </div>

            {editingMultiplier && (
              <label className="flex items-center gap-2 cursor-pointer">
                <input type="checkbox" checked={multiplierForm.isActive}
                  onChange={(e) => setMultiplierForm({ ...multiplierForm, isActive: e.target.checked })}
                  className="w-4 h-4 text-brand border-gray-300 rounded focus:ring-brand" />
                <span className="text-sm text-gray-700">Active</span>
              </label>
            )}

            <div className="flex gap-3 pt-4">
              <button type="button" onClick={closeMultiplierModal}
                className="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50">
                Cancel
              </button>
              <button type="submit" disabled={savingMultiplier}
                className="flex-1 px-4 py-2 bg-brand text-white rounded-lg hover:bg-brand-dark disabled:opacity-50">
                {savingMultiplier ? 'Saving...' : editingMultiplier ? 'Save' : 'Create'}
              </button>
            </div>
          </form>
        </DialogContent>
      </Dialog>

      {/* ── Delete Confirmation ────────────────────────────────────────────── */}
      <AlertDialog open={deleteConfirm !== null} onOpenChange={(open) => !open && setDeleteConfirm(null)}>
        <AlertDialogContent className="max-w-sm">
          <AlertDialogHeader>
            <AlertDialogTitle>
              {deleteConfirm?.type === 'service' ? 'Delete service?' : 'Delete condition?'}
            </AlertDialogTitle>
            <AlertDialogDescription>This action cannot be undone.</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter className="flex gap-3 sm:space-x-0">
            <AlertDialogCancel className="flex-1" onClick={() => setDeleteConfirm(null)}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              className="flex-1"
              onClick={() => {
                if (!deleteConfirm) return;
                if (deleteConfirm.type === 'service') handleDeleteService(deleteConfirm.id);
                else handleDeleteMultiplier(deleteConfirm.id);
              }}
            >
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </AdminLayout>
  );
}
