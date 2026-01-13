import { useState, useEffect } from 'react';
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
import api from '../../lib/api';

interface AdditionalService {
  id: number;
  title: string;
  description: string | null;
  price: number;
  additionalMinutes: number;
  isActive: boolean;
}

export function AdminAdditionalServicesPage() {
  const [services, setServices] = useState<AdditionalService[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [editingService, setEditingService] = useState<AdditionalService | null>(null);
  const [deleteConfirm, setDeleteConfirm] = useState<number | null>(null);
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);

  const [form, setForm] = useState({
    title: '',
    description: '',
    price: 0,
    additionalMinutes: 30,
    isActive: true,
  });

  useEffect(() => {
    fetchServices();
  }, []);

  const fetchServices = async () => {
    setIsLoading(true);
    try {
      const response = await api.get<AdditionalService[]>('/admin/additionalservices');
      setServices(response.data);
    } catch (err) {
      setError('Error al cargar servicios adicionales');
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError('');

    try {
      if (editingService) {
        await api.put(`/admin/additionalservices/${editingService.id}`, form);
      } else {
        await api.post('/admin/additionalservices', form);
      }
      await fetchServices();
      closeModal();
    } catch (err) {
      setError('Error al guardar el servicio adicional');
      console.error(err);
    } finally {
      setSaving(false);
    }
  };

  const handleEdit = (service: AdditionalService) => {
    setEditingService(service);
    setForm({
      title: service.title,
      description: service.description || '',
      price: service.price,
      additionalMinutes: service.additionalMinutes,
      isActive: service.isActive,
    });
    setShowModal(true);
  };

  const handleDelete = async (id: number) => {
    try {
      await api.delete(`/admin/additionalservices/${id}`);
      await fetchServices();
      setDeleteConfirm(null);
    } catch (err) {
      setError('Error al eliminar el servicio');
      console.error(err);
    }
  };

  const handleToggleActive = async (service: AdditionalService) => {
    try {
      await api.put(`/admin/additionalservices/${service.id}`, {
        title: service.title,
        description: service.description,
        price: service.price,
        additionalMinutes: service.additionalMinutes,
        isActive: !service.isActive,
      });
      await fetchServices();
    } catch (err) {
      setError('Error al actualizar el servicio');
      console.error(err);
    }
  };

  const closeModal = () => {
    setShowModal(false);
    setEditingService(null);
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

  if (isLoading) {
    return (
      <AdminLayout>
        <div className="flex items-center justify-center h-64">
          <div className="w-8 h-8 border-4 border-[#00205B] border-t-transparent rounded-full animate-spin" />
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
            <h1 className="text-2xl font-bold text-gray-900">Servicios Adicionales</h1>
            <p className="text-gray-600">Gestiona los servicios extra que se pueden añadir a las reservas</p>
          </div>
          <button
            onClick={() => setShowModal(true)}
            className="inline-flex items-center gap-2 px-4 py-2 bg-[#00205B] text-white rounded-lg hover:bg-[#001440] transition-colors"
          >
            <Plus className="h-5 w-5" />
            Nuevo Servicio
          </button>
        </div>

        {error && (
          <div className="bg-red-50 text-red-600 px-4 py-3 rounded-lg">
            {error}
            <button onClick={() => setError('')} className="ml-2 underline">Cerrar</button>
          </div>
        )}

        {/* Search */}
        <div className="relative max-w-md">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-gray-400" />
          <input
            type="search"
            placeholder="Buscar servicios adicionales..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
          />
        </div>

        {/* Stats */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div className="bg-white rounded-lg p-4 border">
            <div className="flex items-center gap-2">
              <Sparkles className="h-5 w-5 text-purple-500" />
              <p className="text-sm text-gray-500">Total Servicios</p>
            </div>
            <p className="text-2xl font-bold text-gray-900 mt-1">{services.length}</p>
          </div>
          <div className="bg-white rounded-lg p-4 border">
            <div className="flex items-center gap-2">
              <ToggleRight className="h-5 w-5 text-green-500" />
              <p className="text-sm text-gray-500">Activos</p>
            </div>
            <p className="text-2xl font-bold text-green-600 mt-1">
              {services.filter(s => s.isActive).length}
            </p>
          </div>
          <div className="bg-white rounded-lg p-4 border">
            <div className="flex items-center gap-2">
              <DollarSign className="h-5 w-5 text-[#00205B]" />
              <p className="text-sm text-gray-500">Precio Promedio</p>
            </div>
            <p className="text-2xl font-bold text-[#00205B] mt-1">
              {formatCurrency(services.length > 0 ? totalRevenue / services.length : 0)}
            </p>
          </div>
          <div className="bg-white rounded-lg p-4 border">
            <div className="flex items-center gap-2">
              <Clock className="h-5 w-5 text-emerald-500" />
              <p className="text-sm text-gray-500">Tiempo Prom.</p>
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
                    title={service.isActive ? 'Desactivar' : 'Activar'}
                  >
                    {service.isActive ? (
                      <ToggleRight className="h-5 w-5" />
                    ) : (
                      <ToggleLeft className="h-5 w-5" />
                    )}
                  </button>
                  <button
                    onClick={() => handleEdit(service)}
                    className="p-1 text-gray-400 hover:text-[#00205B] hover:bg-[#FFE44D]/10 rounded"
                    title="Editar"
                  >
                    <Edit2 className="h-4 w-4" />
                  </button>
                  <button
                    onClick={() => setDeleteConfirm(service.id)}
                    className="p-1 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded"
                    title="Eliminar"
                  >
                    <Trash2 className="h-4 w-4" />
                  </button>
                </div>
              </div>

              <h3 className="font-semibold text-gray-900 mb-1">{service.title}</h3>
              <p className="text-sm text-gray-500 mb-4 line-clamp-2">
                {service.description || 'Sin descripción'}
              </p>

              <div className="grid grid-cols-2 gap-2 text-sm">
                <div className="text-center p-2 bg-[#FFE44D]/10 rounded-lg">
                  <p className="text-[#00205B] text-xs">Precio</p>
                  <p className="font-semibold text-[#001440]">{formatCurrency(service.price)}</p>
                </div>
                <div className="text-center p-2 bg-purple-50 rounded-lg">
                  <p className="text-purple-600 text-xs">Tiempo</p>
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
            <p className="text-gray-500">No se encontraron servicios adicionales</p>
            <button
              onClick={() => setShowModal(true)}
              className="mt-4 text-[#00205B] hover:underline"
            >
              Crear el primero
            </button>
          </div>
        )}
      </div>

      {/* Delete Confirmation Modal */}
      {deleteConfirm !== null && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50">
          <div className="bg-white rounded-xl shadow-xl p-6 w-full max-w-sm">
            <h3 className="text-lg font-semibold text-gray-900 mb-2">
              ¿Eliminar servicio adicional?
            </h3>
            <p className="text-gray-600 mb-6">Esta acción no se puede deshacer.</p>
            <div className="flex gap-3">
              <button
                onClick={() => setDeleteConfirm(null)}
                className="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50"
              >
                Cancelar
              </button>
              <button
                onClick={() => handleDelete(deleteConfirm)}
                className="flex-1 px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600"
              >
                Eliminar
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Create/Edit Modal */}
      {showModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg">
            <div className="flex items-center justify-between p-6 border-b">
              <h2 className="text-xl font-semibold text-gray-900">
                {editingService ? 'Editar Servicio Adicional' : 'Nuevo Servicio Adicional'}
              </h2>
              <button onClick={closeModal} className="p-2 text-gray-400 hover:text-gray-600">
                <X className="h-5 w-5" />
              </button>
            </div>

            <form onSubmit={handleSubmit} className="p-6 space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Título *</label>
                <input
                  type="text"
                  value={form.title}
                  onChange={(e) => setForm({ ...form, title: e.target.value })}
                  placeholder="Ej: Limpieza de refrigerador"
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
                  required
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Descripción</label>
                <textarea
                  value={form.description}
                  onChange={(e) => setForm({ ...form, description: e.target.value })}
                  placeholder="Descripción del servicio adicional"
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
                  rows={3}
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Precio ($) *</label>
                  <input
                    type="number"
                    min="0"
                    step="0.01"
                    value={form.price}
                    onChange={(e) => setForm({ ...form, price: parseFloat(e.target.value) || 0 })}
                    className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
                    required
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Minutos extra</label>
                  <input
                    type="number"
                    min="0"
                    step="5"
                    value={form.additionalMinutes}
                    onChange={(e) => setForm({ ...form, additionalMinutes: parseInt(e.target.value) || 0 })}
                    className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
                  />
                </div>
              </div>

              {editingService && (
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={form.isActive}
                    onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
                    className="w-4 h-4 text-[#00205B] border-gray-300 rounded focus:ring-[#00205B]"
                  />
                  <span className="text-sm text-gray-700">Activo</span>
                </label>
              )}

              <div className="flex gap-3 pt-4">
                <button
                  type="button"
                  onClick={closeModal}
                  className="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50"
                >
                  Cancelar
                </button>
                <button
                  type="submit"
                  disabled={saving}
                  className="flex-1 px-4 py-2 bg-[#00205B] text-white rounded-lg hover:bg-[#001440] disabled:opacity-50"
                >
                  {saving ? 'Guardando...' : editingService ? 'Guardar' : 'Crear'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </AdminLayout>
  );
}
