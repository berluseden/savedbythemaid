import { useState, useEffect } from 'react';
import {
  Plus,
  Search,
  Edit2,
  Trash2,
  MoreVertical,
  DollarSign,
  Clock,
  Check,
  X,
} from 'lucide-react';
import { AdminLayout } from '../../components/admin/AdminLayout';

interface Service {
  id: string;
  name: string;
  description: string;
  basePrice: number;
  duration: number; // in minutes
  isActive: boolean;
  category: string;
  features: string[];
}

export function AdminServicesPage() {
  const [services, setServices] = useState<Service[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [editingService, setEditingService] = useState<Service | null>(null);
  const [deleteConfirm, setDeleteConfirm] = useState<string | null>(null);

  const [formData, setFormData] = useState({
    name: '',
    description: '',
    basePrice: 0,
    duration: 60,
    category: 'regular',
    isActive: true,
    features: '',
  });

  useEffect(() => {
    fetchServices();
  }, []);

  const fetchServices = async () => {
    setIsLoading(true);
    // Simulated data - replace with actual API call
    await new Promise(resolve => setTimeout(resolve, 500));
    
    setServices([
      {
        id: '1',
        name: 'Limpieza Regular',
        description: 'Limpieza estándar de mantenimiento para el hogar',
        basePrice: 85.00,
        duration: 120,
        isActive: true,
        category: 'regular',
        features: ['Limpieza de superficies', 'Aspirado', 'Baños', 'Cocina'],
      },
      {
        id: '2',
        name: 'Limpieza Profunda',
        description: 'Limpieza exhaustiva y detallada de cada rincón',
        basePrice: 150.00,
        duration: 240,
        isActive: true,
        category: 'deep',
        features: ['Todo lo regular', 'Interior de armarios', 'Detrás de muebles', 'Ventanas interiores'],
      },
      {
        id: '3',
        name: 'Limpieza de Mudanza',
        description: 'Preparación del hogar para nuevos ocupantes',
        basePrice: 250.00,
        duration: 360,
        isActive: true,
        category: 'move',
        features: ['Limpieza completa', 'Interior de electrodomésticos', 'Todas las ventanas', 'Garaje'],
      },
      {
        id: '4',
        name: 'Limpieza Post-Obra',
        description: 'Eliminación de polvo y residuos de construcción',
        basePrice: 300.00,
        duration: 480,
        isActive: false,
        category: 'construction',
        features: ['Remoción de escombros', 'Limpieza de polvo fino', 'Cristales', 'Pulido de superficies'],
      },
    ]);
    setIsLoading(false);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    const serviceData: Service = {
      id: editingService?.id || Date.now().toString(),
      name: formData.name,
      description: formData.description,
      basePrice: formData.basePrice,
      duration: formData.duration,
      category: formData.category,
      isActive: formData.isActive,
      features: formData.features.split(',').map(f => f.trim()).filter(Boolean),
    };

    if (editingService) {
      setServices(prev => prev.map(s => s.id === editingService.id ? serviceData : s));
    } else {
      setServices(prev => [...prev, serviceData]);
    }

    closeModal();
  };

  const handleEdit = (service: Service) => {
    setEditingService(service);
    setFormData({
      name: service.name,
      description: service.description,
      basePrice: service.basePrice,
      duration: service.duration,
      category: service.category,
      isActive: service.isActive,
      features: service.features.join(', '),
    });
    setShowModal(true);
  };

  const handleDelete = async (id: string) => {
    setServices(prev => prev.filter(s => s.id !== id));
    setDeleteConfirm(null);
  };

  const toggleActive = async (id: string) => {
    setServices(prev =>
      prev.map(s => (s.id === id ? { ...s, isActive: !s.isActive } : s))
    );
  };

  const closeModal = () => {
    setShowModal(false);
    setEditingService(null);
    setFormData({
      name: '',
      description: '',
      basePrice: 0,
      duration: 60,
      category: 'regular',
      isActive: true,
      features: '',
    });
  };

  const filteredServices = services.filter(
    service =>
      service.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      service.description.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const formatCurrency = (amount: number) =>
    new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(amount);

  const formatDuration = (minutes: number) => {
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    return hours > 0 ? `${hours}h ${mins > 0 ? `${mins}m` : ''}` : `${mins}m`;
  };

  const getCategoryLabel = (category: string) => {
    const labels: Record<string, string> = {
      regular: 'Regular',
      deep: 'Profunda',
      move: 'Mudanza',
      construction: 'Post-Obra',
    };
    return labels[category] || category;
  };

  if (isLoading) {
    return (
      <AdminLayout>
        <div className="flex items-center justify-center h-64">
          <div className="w-8 h-8 border-4 border-sky-500 border-t-transparent rounded-full animate-spin" />
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
            <h1 className="text-2xl font-bold text-gray-900">Servicios</h1>
            <p className="text-gray-600">Gestiona los tipos de servicios de limpieza</p>
          </div>
          <button
            onClick={() => setShowModal(true)}
            className="inline-flex items-center gap-2 px-4 py-2 bg-sky-500 text-white rounded-lg hover:bg-sky-600 transition-colors"
          >
            <Plus className="h-5 w-5" />
            Nuevo Servicio
          </button>
        </div>

        {/* Search */}
        <div className="relative max-w-md">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-gray-400" />
          <input
            type="search"
            placeholder="Buscar servicios..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sky-500 focus:border-transparent"
          />
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
                    {getCategoryLabel(service.category)}
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
                        onClick={() => handleEdit(service)}
                        className="w-full flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
                      >
                        <Edit2 className="h-4 w-4" />
                        Editar
                      </button>
                      <button
                        onClick={() => handleDelete(service.id)}
                        className="w-full flex items-center gap-2 px-4 py-2 text-sm text-red-600 hover:bg-red-50"
                      >
                        <Trash2 className="h-4 w-4" />
                        Eliminar
                      </button>
                    </div>
                  )}
                </div>
              </div>

              <p className="text-sm text-gray-600 mb-4">{service.description}</p>

              <div className="flex items-center gap-4 mb-4">
                <div className="flex items-center gap-1 text-gray-600">
                  <DollarSign className="h-4 w-4" />
                  <span className="font-semibold">{formatCurrency(service.basePrice)}</span>
                </div>
                <div className="flex items-center gap-1 text-gray-600">
                  <Clock className="h-4 w-4" />
                  <span>{formatDuration(service.duration)}</span>
                </div>
              </div>

              <div className="mb-4">
                <p className="text-xs font-medium text-gray-500 mb-2">Incluye:</p>
                <div className="flex flex-wrap gap-1">
                  {service.features.slice(0, 3).map((feature, i) => (
                    <span key={i} className="text-xs bg-sky-50 text-sky-700 px-2 py-1 rounded">
                      {feature}
                    </span>
                  ))}
                  {service.features.length > 3 && (
                    <span className="text-xs text-gray-500">+{service.features.length - 3} más</span>
                  )}
                </div>
              </div>

              <div className="flex items-center justify-between pt-4 border-t">
                <span className={`text-sm font-medium ${service.isActive ? 'text-green-600' : 'text-gray-400'}`}>
                  {service.isActive ? 'Activo' : 'Inactivo'}
                </span>
                <button
                  onClick={() => toggleActive(service.id)}
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
            <p className="text-gray-500">No se encontraron servicios</p>
          </div>
        )}
      </div>

      {/* Modal */}
      {showModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg max-h-[90vh] overflow-y-auto">
            <div className="flex items-center justify-between p-6 border-b">
              <h2 className="text-xl font-semibold text-gray-900">
                {editingService ? 'Editar Servicio' : 'Nuevo Servicio'}
              </h2>
              <button onClick={closeModal} className="p-2 text-gray-400 hover:text-gray-600">
                <X className="h-5 w-5" />
              </button>
            </div>

            <form onSubmit={handleSubmit} className="p-6 space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Nombre del servicio
                </label>
                <input
                  type="text"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sky-500 focus:border-transparent"
                  required
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Descripción
                </label>
                <textarea
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sky-500 focus:border-transparent"
                  rows={3}
                  required
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Precio base ($)
                  </label>
                  <input
                    type="number"
                    step="0.01"
                    min="0"
                    value={formData.basePrice}
                    onChange={(e) => setFormData({ ...formData, basePrice: parseFloat(e.target.value) })}
                    className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sky-500 focus:border-transparent"
                    required
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Duración (minutos)
                  </label>
                  <input
                    type="number"
                    min="15"
                    step="15"
                    value={formData.duration}
                    onChange={(e) => setFormData({ ...formData, duration: parseInt(e.target.value) })}
                    className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sky-500 focus:border-transparent"
                    required
                  />
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Categoría
                </label>
                <select
                  value={formData.category}
                  onChange={(e) => setFormData({ ...formData, category: e.target.value })}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sky-500 focus:border-transparent"
                >
                  <option value="regular">Limpieza Regular</option>
                  <option value="deep">Limpieza Profunda</option>
                  <option value="move">Mudanza</option>
                  <option value="construction">Post-Obra</option>
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Características (separadas por coma)
                </label>
                <textarea
                  value={formData.features}
                  onChange={(e) => setFormData({ ...formData, features: e.target.value })}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sky-500 focus:border-transparent"
                  rows={2}
                  placeholder="Limpieza de superficies, Aspirado, Baños..."
                />
              </div>

              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={formData.isActive}
                  onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                  className="w-4 h-4 text-sky-500 border-gray-300 rounded focus:ring-sky-500"
                />
                <span className="text-sm text-gray-700">Servicio activo</span>
              </label>

              <div className="flex gap-3 pt-4">
                <button
                  type="button"
                  onClick={closeModal}
                  className="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 transition-colors"
                >
                  Cancelar
                </button>
                <button
                  type="submit"
                  className="flex-1 px-4 py-2 bg-sky-500 text-white rounded-lg hover:bg-sky-600 transition-colors"
                >
                  {editingService ? 'Guardar Cambios' : 'Crear Servicio'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </AdminLayout>
  );
}
