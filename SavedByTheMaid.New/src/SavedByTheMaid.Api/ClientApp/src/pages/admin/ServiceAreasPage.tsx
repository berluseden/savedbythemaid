import { useState, useEffect } from 'react';
import {
  Plus,
  Search,
  Edit2,
  Trash2,
  MapPin,
  X,
  ChevronDown,
  ChevronUp,
  CheckCircle,
  Globe,
} from 'lucide-react';
import { AdminLayout } from '../../components/admin/AdminLayout';
import api from '../../lib/api';

interface ServiceAreaZip {
  id: number;
  zipCode: string;
  serviceAreaId: number;
}

interface ServiceArea {
  id: number;
  name: string;
  description: string | null;
  isActive: boolean;
  zipCodes: ServiceAreaZip[];
}

export function AdminServiceAreasPage() {
  const [serviceAreas, setServiceAreas] = useState<ServiceArea[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [expandedArea, setExpandedArea] = useState<number | null>(null);
  
  // Modal state
  const [showModal, setShowModal] = useState(false);
  const [editingArea, setEditingArea] = useState<ServiceArea | null>(null);
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    isActive: true,
  });
  
  // Zip code modal
  const [showZipModal, setShowZipModal] = useState(false);
  const [selectedAreaId, setSelectedAreaId] = useState<number | null>(null);
  const [newZipCode, setNewZipCode] = useState('');
  const [zipError, setZipError] = useState('');

  useEffect(() => {
    fetchServiceAreas();
  }, []);

  const fetchServiceAreas = async () => {
    setIsLoading(true);
    try {
      const response = await api.get<ServiceArea[]>('/serviceareas');
      setServiceAreas(response.data);
    } catch (error) {
      console.error('Error fetching service areas:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleOpenModal = (area?: ServiceArea) => {
    if (area) {
      setEditingArea(area);
      setFormData({
        name: area.name,
        description: area.description || '',
        isActive: area.isActive,
      });
    } else {
      setEditingArea(null);
      setFormData({ name: '', description: '', isActive: true });
    }
    setShowModal(true);
  };

  const handleCloseModal = () => {
    setShowModal(false);
    setEditingArea(null);
    setFormData({ name: '', description: '', isActive: true });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (editingArea) {
        await api.put(`/serviceareas/${editingArea.id}`, formData);
      } else {
        await api.post('/serviceareas', formData);
      }
      handleCloseModal();
      fetchServiceAreas();
    } catch (error) {
      console.error('Error saving service area:', error);
    }
  };

  const handleDelete = async (id: number) => {
    if (!confirm('Are you sure you want to delete this service area? This will also delete all associated zip codes.')) {
      return;
    }
    try {
      await api.delete(`/serviceareas/${id}`);
      fetchServiceAreas();
    } catch (error) {
      console.error('Error deleting service area:', error);
    }
  };

  const handleOpenZipModal = (areaId: number) => {
    setSelectedAreaId(areaId);
    setNewZipCode('');
    setZipError('');
    setShowZipModal(true);
  };

  const handleAddZipCode = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedAreaId || !newZipCode.trim()) return;

    // Validate zip code format (5 digits)
    if (!/^\d{5}$/.test(newZipCode.trim())) {
      setZipError('Zip code must be 5 digits');
      return;
    }

    try {
      await api.post(`/serviceareas/${selectedAreaId}/zipcodes`, {
        zipCode: newZipCode.trim(),
      });
      setShowZipModal(false);
      setNewZipCode('');
      fetchServiceAreas();
    } catch (error: unknown) {
      const maybe = error as { response?: { status?: number } };
      if (maybe.response?.status === 409) {
        setZipError('This zip code is already assigned to another zone');
      } else {
        setZipError('Error adding zip code');
      }
    }
  };

  const handleDeleteZipCode = async (areaId: number, zipId: number, zipCode: string) => {
    if (!confirm(`Delete zip code ${zipCode}?`)) return;
    
    try {
      await api.delete(`/serviceareas/${areaId}/zipcodes/${zipId}`);
      fetchServiceAreas();
    } catch (error) {
      console.error('Error deleting zip code:', error);
    }
  };

  const toggleExpand = (areaId: number) => {
    setExpandedArea(expandedArea === areaId ? null : areaId);
  };

  const filteredAreas = serviceAreas.filter(
    (area) =>
      area.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      area.description?.toLowerCase().includes(searchTerm.toLowerCase()) ||
      area.zipCodes.some((z) => z.zipCode.includes(searchTerm))
  );

  const totalZipCodes = serviceAreas.reduce((sum, area) => sum + area.zipCodes.length, 0);
  const activeAreas = serviceAreas.filter((a) => a.isActive).length;

  return (
    <AdminLayout>
      <div className="space-y-6">
        {/* Header */}
        <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Service Areas</h1>
            <p className="mt-1 text-sm text-gray-500">
              Manage geographic areas where you offer service
            </p>
          </div>
          <button
            onClick={() => handleOpenModal()}
            className="inline-flex items-center gap-2 rounded-lg bg-[#00205B] px-4 py-2 text-sm font-medium text-white hover:bg-[#001440] transition-colors"
          >
            <Plus className="h-4 w-4" />
            New Zone
          </button>
        </div>

        {/* Stats */}
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
          <div className="rounded-lg border border-gray-200 bg-white p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-[#FFE44D]/20 p-2">
                <Globe className="h-5 w-5 text-[#00205B]" />
              </div>
              <div>
                <p className="text-2xl font-bold text-gray-900">{serviceAreas.length}</p>
                <p className="text-sm text-gray-500">Total zones</p>
              </div>
            </div>
          </div>
          <div className="rounded-lg border border-gray-200 bg-white p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-green-100 p-2">
                <CheckCircle className="h-5 w-5 text-green-600" />
              </div>
              <div>
                <p className="text-2xl font-bold text-gray-900">{activeAreas}</p>
                <p className="text-sm text-gray-500">Active zones</p>
              </div>
            </div>
          </div>
          <div className="rounded-lg border border-gray-200 bg-white p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-purple-100 p-2">
                <MapPin className="h-5 w-5 text-purple-600" />
              </div>
              <div>
                <p className="text-2xl font-bold text-gray-900">{totalZipCodes}</p>
                <p className="text-sm text-gray-500">Zip Codes</p>
              </div>
            </div>
          </div>
        </div>

        {/* Search */}
        <div className="relative">
          <Search className="absolute left-3 top-1/2 h-5 w-5 -translate-y-1/2 text-gray-400" />
          <input
            type="text"
            placeholder="Search by name, description or zip code..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full rounded-lg border border-gray-300 bg-white py-2 pl-10 pr-4 text-sm focus:border-[#00205B] focus:outline-none focus:ring-1 focus:ring-[#00205B]"
          />
        </div>

        {/* Service Areas List */}
        {isLoading ? (
          <div className="flex items-center justify-center py-12">
            <div className="h-8 w-8 animate-spin rounded-full border-4 border-[#00205B] border-t-transparent" />
          </div>
        ) : filteredAreas.length === 0 ? (
          <div className="rounded-lg border border-gray-200 bg-white p-12 text-center">
            <MapPin className="mx-auto h-12 w-12 text-gray-400" />
            <h3 className="mt-4 text-lg font-medium text-gray-900">No service areas</h3>
            <p className="mt-2 text-sm text-gray-500">
              {searchTerm
                ? 'No zones matching your search were found'
                : 'Start by creating your first service area'}
            </p>
            {!searchTerm && (
              <button
                onClick={() => handleOpenModal()}
                className="mt-4 inline-flex items-center gap-2 rounded-lg bg-[#00205B] px-4 py-2 text-sm font-medium text-white hover:bg-[#001440]"
              >
                <Plus className="h-4 w-4" />
                New Zone
              </button>
            )}
          </div>
        ) : (
          <div className="space-y-4">
            {filteredAreas.map((area) => (
              <div
                key={area.id}
                className="rounded-lg border border-gray-200 bg-white overflow-hidden"
              >
                {/* Area Header */}
                <div className="flex items-center justify-between p-4">
                  <div className="flex items-center gap-4">
                    <button
                      onClick={() => toggleExpand(area.id)}
                      className="rounded-lg p-1 hover:bg-gray-100"
                    >
                      {expandedArea === area.id ? (
                        <ChevronUp className="h-5 w-5 text-gray-500" />
                      ) : (
                        <ChevronDown className="h-5 w-5 text-gray-500" />
                      )}
                    </button>
                    <div>
                      <div className="flex items-center gap-2">
                        <h3 className="font-medium text-gray-900">{area.name}</h3>
                        <span
                          className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${
                            area.isActive
                              ? 'bg-green-100 text-green-700'
                              : 'bg-gray-100 text-gray-700'
                          }`}
                        >
                          {area.isActive ? 'Activa' : 'Inactiva'}
                        </span>
                      </div>
                      {area.description && (
                        <p className="mt-1 text-sm text-gray-500">{area.description}</p>
                      )}
                      <p className="mt-1 text-xs text-gray-400">
                        {area.zipCodes.length} zip code{area.zipCodes.length !== 1 ? 's' : ''}
                        {area.zipCodes.length !== 1 ? 'es' : ''}
                      </p>
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <button
                      onClick={() => handleOpenModal(area)}
                      className="rounded-lg p-2 text-gray-500 hover:bg-gray-100 hover:text-gray-700"
                      title="Edit zone"
                    >
                      <Edit2 className="h-4 w-4" />
                    </button>
                    <button
                      onClick={() => handleDelete(area.id)}
                      className="rounded-lg p-2 text-gray-500 hover:bg-red-50 hover:text-red-600"
                      title="Delete zone"
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  </div>
                </div>

                {/* Expanded: Zip Codes */}
                {expandedArea === area.id && (
                  <div className="border-t border-gray-200 bg-gray-50 p-4">
                    <div className="flex items-center justify-between mb-3">
                      <h4 className="text-sm font-medium text-gray-700">Zip Codes</h4>
                      <button
                        onClick={() => handleOpenZipModal(area.id)}
                        className="inline-flex items-center gap-1 rounded-md bg-[#00205B] px-3 py-1.5 text-xs font-medium text-white hover:bg-[#001440]"
                      >
                        <Plus className="h-3 w-3" />
                        Agregar ZIP
                      </button>
                    </div>
                    {area.zipCodes.length === 0 ? (
                      <p className="text-sm text-gray-500 italic">
                        No hay zip codes configured
                      </p>
                    ) : (
                      <div className="flex flex-wrap gap-2">
                        {area.zipCodes.map((zip) => (
                          <div
                            key={zip.id}
                            className="group flex items-center gap-1 rounded-full bg-white border border-gray-200 px-3 py-1 text-sm"
                          >
                            <MapPin className="h-3 w-3 text-gray-400" />
                            <span className="font-mono">{zip.zipCode}</span>
                            <button
                              onClick={() => handleDeleteZipCode(area.id, zip.id, zip.zipCode)}
                              className="ml-1 rounded-full p-0.5 text-gray-400 hover:bg-red-100 hover:text-red-600 opacity-0 group-hover:opacity-100 transition-opacity"
                            >
                              <X className="h-3 w-3" />
                            </button>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                )}
              </div>
            ))}
          </div>
        )}

        {/* Create/Edit Area Modal */}
        {showModal && (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
            <div className="w-full max-w-md rounded-lg bg-white p-6 shadow-xl">
              <div className="flex items-center justify-between mb-4">
                <h2 className="text-lg font-semibold text-gray-900">
                  {editingArea ? 'Edit Zone' : 'New Service Area'}
                </h2>
                <button
                  onClick={handleCloseModal}
                  className="rounded-lg p-1 text-gray-400 hover:bg-gray-100 hover:text-gray-600"
                >
                  <X className="h-5 w-5" />
                </button>
              </div>

              <form onSubmit={handleSubmit} className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Name *
                  </label>
                  <input
                    type="text"
                    required
                    value={formData.name}
                    onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                    placeholder="E.g.: North Miami Zone"
                    className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-[#00205B] focus:outline-none focus:ring-1 focus:ring-[#00205B]"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Description
                  </label>
                  <textarea
                    value={formData.description}
                    onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                    placeholder="Optional zone description"
                    rows={3}
                    className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-[#00205B] focus:outline-none focus:ring-1 focus:ring-[#00205B]"
                  />
                </div>

                {editingArea && (
                  <div className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      id="isActive"
                      checked={formData.isActive}
                      onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                      className="h-4 w-4 rounded border-gray-300 text-[#00205B] focus:ring-[#00205B]"
                    />
                    <label htmlFor="isActive" className="text-sm text-gray-700">
                      Zone active
                    </label>
                  </div>
                )}

                <div className="flex justify-end gap-3 pt-4">
                  <button
                    type="button"
                    onClick={handleCloseModal}
                    className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    className="rounded-lg bg-[#00205B] px-4 py-2 text-sm font-medium text-white hover:bg-[#001440]"
                  >
                    {editingArea ? 'Save Changes' : 'Create Zone'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}

        {/* Add Zip Code Modal */}
        {showZipModal && (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
            <div className="w-full max-w-sm rounded-lg bg-white p-6 shadow-xl">
              <div className="flex items-center justify-between mb-4">
                <h2 className="text-lg font-semibold text-gray-900">Add Zip Code</h2>
                <button
                  onClick={() => setShowZipModal(false)}
                  className="rounded-lg p-1 text-gray-400 hover:bg-gray-100 hover:text-gray-600"
                >
                  <X className="h-5 w-5" />
                </button>
              </div>

              <form onSubmit={handleAddZipCode} className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Zip Code (5 digits) *
                  </label>
                  <input
                    type="text"
                    required
                    maxLength={5}
                    value={newZipCode}
                    onChange={(e) => {
                      setNewZipCode(e.target.value.replace(/\D/g, ''));
                      setZipError('');
                    }}
                    placeholder="Ej: 33101"
                    className={`w-full rounded-lg border px-3 py-2 text-sm font-mono focus:outline-none focus:ring-1 ${
                      zipError
                        ? 'border-red-300 focus:border-red-500 focus:ring-red-500'
                        : 'border-gray-300 focus:border-[#00205B] focus:ring-[#00205B]'
                    }`}
                  />
                  {zipError && <p className="mt-1 text-xs text-red-600">{zipError}</p>}
                </div>

                <div className="flex justify-end gap-3 pt-2">
                  <button
                    type="button"
                    onClick={() => setShowZipModal(false)}
                    className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    className="rounded-lg bg-[#00205B] px-4 py-2 text-sm font-medium text-white hover:bg-[#001440]"
                  >
                    Agregar
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}
      </div>
    </AdminLayout>
  );
}
