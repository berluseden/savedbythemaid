import { useState, useEffect } from 'react';
import {
  Plus,
  Search,
  Edit2,
  Trash2,
  Home,
  ChevronDown,
  ChevronRight,
  X,
  DollarSign,
  Clock,
} from 'lucide-react';
import { AdminLayout } from '../../components/admin/AdminLayout';
import api from '../../lib/api';

interface CleaningPlaceRoom {
  id: number;
  name: string;
  description: string | null;
  baseMinutes: number;
  basePrice: number;
  displayOrder: number;
  isActive: boolean;
}

interface CleaningPlace {
  id: number;
  name: string;
  description: string | null;
  isActive: boolean;
  rooms: CleaningPlaceRoom[];
}

export function AdminCleaningPlacesPage() {
  const [places, setPlaces] = useState<CleaningPlace[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [showRoomModal, setShowRoomModal] = useState(false);
  const [editingPlace, setEditingPlace] = useState<CleaningPlace | null>(null);
  const [editingRoom, setEditingRoom] = useState<CleaningPlaceRoom | null>(null);
  const [selectedPlaceId, setSelectedPlaceId] = useState<number | null>(null);
  const [expandedPlaces, setExpandedPlaces] = useState<Set<number>>(new Set());
  const [deleteConfirm, setDeleteConfirm] = useState<{ type: 'place' | 'room'; placeId: number; roomId?: number } | null>(null);
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);

  const [placeForm, setPlaceForm] = useState({ name: '', description: '', isActive: true });
  const [roomForm, setRoomForm] = useState({
    name: '',
    description: '',
    baseMinutes: 15,
    basePrice: 10,
    displayOrder: 0,
    isActive: true,
  });

  useEffect(() => {
    fetchPlaces();
  }, []);

  const fetchPlaces = async () => {
    setIsLoading(true);
    try {
      const response = await api.get<CleaningPlace[]>('/admin/cleaningplaces');
      setPlaces(response.data);
    } catch (err) {
      setError('Error al cargar tipos de inmueble');
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  const toggleExpand = (id: number) => {
    setExpandedPlaces(prev => {
      const next = new Set(prev);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  };

  // Place handlers
  const handlePlaceSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError('');

    try {
      if (editingPlace) {
        await api.put(`/admin/cleaningplaces/${editingPlace.id}`, placeForm);
      } else {
        await api.post('/admin/cleaningplaces', placeForm);
      }
      await fetchPlaces();
      closePlaceModal();
    } catch (err) {
      setError('Error al guardar el property type');
      console.error(err);
    } finally {
      setSaving(false);
    }
  };

  const handleEditPlace = (place: CleaningPlace) => {
    setEditingPlace(place);
    setPlaceForm({
      name: place.name,
      description: place.description || '',
      isActive: place.isActive,
    });
    setShowModal(true);
  };

  const handleDeletePlace = async (id: number) => {
    try {
      await api.delete(`/admin/cleaningplaces/${id}`);
      await fetchPlaces();
      setDeleteConfirm(null);
    } catch (err) {
      setError('Error al eliminar el property type');
      console.error(err);
    }
  };

  const closePlaceModal = () => {
    setShowModal(false);
    setEditingPlace(null);
    setPlaceForm({ name: '', description: '', isActive: true });
  };

  // Room handlers
  const handleRoomSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedPlaceId) return;
    setSaving(true);
    setError('');

    try {
      if (editingRoom) {
        await api.put(`/admin/cleaningplaces/${selectedPlaceId}/rooms/${editingRoom.id}`, roomForm);
      } else {
        await api.post(`/admin/cleaningplaces/${selectedPlaceId}/rooms`, roomForm);
      }
      await fetchPlaces();
      closeRoomModal();
    } catch (err) {
      setError('Error saving room');
      console.error(err);
    } finally {
      setSaving(false);
    }
  };

  const handleAddRoom = (placeId: number) => {
    setSelectedPlaceId(placeId);
    setEditingRoom(null);
    setRoomForm({ name: '', description: '', baseMinutes: 15, basePrice: 10, displayOrder: 0, isActive: true });
    setShowRoomModal(true);
  };

  const handleEditRoom = (placeId: number, room: CleaningPlaceRoom) => {
    setSelectedPlaceId(placeId);
    setEditingRoom(room);
    setRoomForm({
      name: room.name,
      description: room.description || '',
      baseMinutes: room.baseMinutes,
      basePrice: room.basePrice,
      displayOrder: room.displayOrder,
      isActive: room.isActive,
    });
    setShowRoomModal(true);
  };

  const handleDeleteRoom = async (placeId: number, roomId: number) => {
    try {
      await api.delete(`/admin/cleaningplaces/${placeId}/rooms/${roomId}`);
      await fetchPlaces();
      setDeleteConfirm(null);
    } catch (err) {
      setError('Error deleting room');
      console.error(err);
    }
  };

  const closeRoomModal = () => {
    setShowRoomModal(false);
    setEditingRoom(null);
    setSelectedPlaceId(null);
    setRoomForm({ name: '', description: '', baseMinutes: 15, basePrice: 10, displayOrder: 0, isActive: true });
  };

  const filteredPlaces = places.filter(
    place =>
      place.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      (place.description?.toLowerCase().includes(searchTerm.toLowerCase()) ?? false)
  );

  const formatCurrency = (amount: number) =>
    new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(amount);

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
            <h1 className="text-2xl font-bold text-gray-900">Property Types</h1>
            <p className="text-gray-600">Gestiona los tipos de propiedades y sus rooms</p>
          </div>
          <button
            onClick={() => setShowModal(true)}
            className="inline-flex items-center gap-2 px-4 py-2 bg-[#00205B] text-white rounded-lg hover:bg-[#001440] transition-colors"
          >
            <Plus className="h-5 w-5" />
            New Type
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
            placeholder="Search property types..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
          />
        </div>

        {/* Stats */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div className="bg-white rounded-lg p-4 border">
            <p className="text-sm text-gray-500">Total Types</p>
            <p className="text-2xl font-bold text-gray-900">{places.length}</p>
          </div>
          <div className="bg-white rounded-lg p-4 border">
            <p className="text-sm text-gray-500">Activos</p>
            <p className="text-2xl font-bold text-green-600">
              {places.filter(p => p.isActive).length}
            </p>
          </div>
          <div className="bg-white rounded-lg p-4 border">
            <p className="text-sm text-gray-500">Total Rooms</p>
            <p className="text-2xl font-bold text-[#00205B]">
              {places.reduce((acc, p) => acc + p.rooms.length, 0)}
            </p>
          </div>
          <div className="bg-white rounded-lg p-4 border">
            <p className="text-sm text-gray-500">Hab. Activas</p>
            <p className="text-2xl font-bold text-purple-600">
              {places.reduce((acc, p) => acc + p.rooms.filter(r => r.isActive).length, 0)}
            </p>
          </div>
        </div>

        {/* Places List */}
        <div className="space-y-4">
          {filteredPlaces.map((place) => (
            <div key={place.id} className={`bg-white rounded-xl shadow-sm border ${!place.isActive ? 'opacity-60' : ''}`}>
              {/* Place Header */}
              <div className="flex items-center justify-between p-4">
                <div className="flex items-center gap-4">
                  <button
                    onClick={() => toggleExpand(place.id)}
                    className="p-2 text-gray-400 hover:text-gray-600 rounded-lg hover:bg-gray-100"
                  >
                    {expandedPlaces.has(place.id) ? (
                      <ChevronDown className="h-5 w-5" />
                    ) : (
                      <ChevronRight className="h-5 w-5" />
                    )}
                  </button>
                  <div className="w-10 h-10 bg-[#FFE44D]/20 rounded-lg flex items-center justify-center">
                    <Home className="h-5 w-5 text-[#00205B]" />
                  </div>
                  <div>
                    <h3 className="font-semibold text-gray-900">{place.name}</h3>
                    <p className="text-sm text-gray-500">
                      {place.rooms.length} rooms | {place.description || 'No description'}
                    </p>
                  </div>
                </div>
                <div className="flex items-center gap-2">
                  <span className={`px-2 py-1 rounded-full text-xs font-medium ${
                    place.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'
                  }`}>
                    {place.isActive ? 'Activo' : 'Inactivo'}
                  </span>
                  <button
                    onClick={() => handleAddRoom(place.id)}
                    className="p-2 text-gray-400 hover:text-[#00205B] hover:bg-[#FFE44D]/10 rounded-lg"
                    title="Add room"
                  >
                    <Plus className="h-4 w-4" />
                  </button>
                  <button
                    onClick={() => handleEditPlace(place)}
                    className="p-2 text-gray-400 hover:text-[#00205B] hover:bg-[#FFE44D]/10 rounded-lg"
                    title="Edit"
                  >
                    <Edit2 className="h-4 w-4" />
                  </button>
                  <button
                    onClick={() => setDeleteConfirm({ type: 'place', placeId: place.id })}
                    className="p-2 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded-lg"
                    title="Delete"
                  >
                    <Trash2 className="h-4 w-4" />
                  </button>
                </div>
              </div>

              {/* Rooms */}
              {expandedPlaces.has(place.id) && place.rooms.length > 0 && (
                <div className="border-t px-4 pb-4">
                  <div className="mt-4 space-y-2">
                    {place.rooms.map((room) => (
                      <div
                        key={room.id}
                        className={`flex items-center justify-between p-3 bg-gray-50 rounded-lg ${
                          !room.isActive ? 'opacity-60' : ''
                        }`}
                      >
                        <div className="flex items-center gap-4">
                          <span className="text-sm font-medium text-gray-500 w-8">{room.displayOrder}</span>
                          <div>
                            <p className="font-medium text-gray-900">{room.name}</p>
                            <div className="flex items-center gap-4 text-sm text-gray-500">
                              <span className="flex items-center gap-1">
                                <Clock className="h-3 w-3" />
                                {room.baseMinutes} min
                              </span>
                              <span className="flex items-center gap-1">
                                <DollarSign className="h-3 w-3" />
                                {formatCurrency(room.basePrice)}
                              </span>
                            </div>
                          </div>
                        </div>
                        <div className="flex items-center gap-2">
                          <span className={`px-2 py-1 rounded-full text-xs font-medium ${
                            room.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'
                          }`}>
                            {room.isActive ? 'Activo' : 'Inactivo'}
                          </span>
                          <button
                            onClick={() => handleEditRoom(place.id, room)}
                            className="p-1 text-gray-400 hover:text-[#00205B]"
                          >
                            <Edit2 className="h-4 w-4" />
                          </button>
                          <button
                            onClick={() => setDeleteConfirm({ type: 'room', placeId: place.id, roomId: room.id })}
                            className="p-1 text-gray-400 hover:text-red-600"
                          >
                            <Trash2 className="h-4 w-4" />
                          </button>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {expandedPlaces.has(place.id) && place.rooms.length === 0 && (
                <div className="border-t p-4 text-center text-gray-500 text-sm">
                  No hay rooms configuradas.{' '}
                  <button
                    onClick={() => handleAddRoom(place.id)}
                    className="text-[#00205B] hover:underline"
                  >
                    Agregar una
                  </button>
                </div>
              )}
            </div>
          ))}
        </div>

        {filteredPlaces.length === 0 && (
          <div className="text-center py-12">
            <p className="text-gray-500">No property types found</p>
          </div>
        )}
      </div>

      {/* Delete Confirmation Modal */}
      {deleteConfirm && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50">
          <div className="bg-white rounded-xl shadow-xl p-6 w-full max-w-sm">
            <h3 className="text-lg font-semibold text-gray-900 mb-2">
              Delete {deleteConfirm.type === 'place' ? 'property type' : 'room'}?
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
                onClick={() => {
                  if (deleteConfirm.type === 'place') {
                    handleDeletePlace(deleteConfirm.placeId);
                  } else if (deleteConfirm.roomId) {
                    handleDeleteRoom(deleteConfirm.placeId, deleteConfirm.roomId);
                  }
                }}
                className="flex-1 px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600"
              >
                Delete
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Place Modal */}
      {showModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg">
            <div className="flex items-center justify-between p-6 border-b">
              <h2 className="text-xl font-semibold text-gray-900">
                {editingPlace ? 'Edit Property Type' : 'New Type de Inmueble'}
              </h2>
              <button onClick={closePlaceModal} className="p-2 text-gray-400 hover:text-gray-600">
                <X className="h-5 w-5" />
              </button>
            </div>

            <form onSubmit={handlePlaceSubmit} className="p-6 space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Name *</label>
                <input
                  type="text"
                  value={placeForm.name}
                  onChange={(e) => setPlaceForm({ ...placeForm, name: e.target.value })}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
                  required
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Description</label>
                <textarea
                  value={placeForm.description}
                  onChange={(e) => setPlaceForm({ ...placeForm, description: e.target.value })}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
                  rows={3}
                />
              </div>

              {editingPlace && (
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={placeForm.isActive}
                    onChange={(e) => setPlaceForm({ ...placeForm, isActive: e.target.checked })}
                    className="w-4 h-4 text-[#00205B] border-gray-300 rounded focus:ring-[#00205B]"
                  />
                  <span className="text-sm text-gray-700">Activo</span>
                </label>
              )}

              <div className="flex gap-3 pt-4">
                <button
                  type="button"
                  onClick={closePlaceModal}
                  className="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={saving}
                  className="flex-1 px-4 py-2 bg-[#00205B] text-white rounded-lg hover:bg-[#001440] disabled:opacity-50"
                >
                  {saving ? 'Saving...' : editingPlace ? 'Save' : 'Crear'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Room Modal */}
      {showRoomModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg">
            <div className="flex items-center justify-between p-6 border-b">
              <h2 className="text-xl font-semibold text-gray-900">
                {editingRoom ? 'Edit Room' : 'New Room'}
              </h2>
              <button onClick={closeRoomModal} className="p-2 text-gray-400 hover:text-gray-600">
                <X className="h-5 w-5" />
              </button>
            </div>

            <form onSubmit={handleRoomSubmit} className="p-6 space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Name *</label>
                <input
                  type="text"
                  value={roomForm.name}
                  onChange={(e) => setRoomForm({ ...roomForm, name: e.target.value })}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
                  required
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Description</label>
                <textarea
                  value={roomForm.description}
                  onChange={(e) => setRoomForm({ ...roomForm, description: e.target.value })}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
                  rows={2}
                />
              </div>

              <div className="grid grid-cols-3 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Minutos base</label>
                  <input
                    type="number"
                    min="5"
                    step="5"
                    value={roomForm.baseMinutes}
                    onChange={(e) => setRoomForm({ ...roomForm, baseMinutes: parseInt(e.target.value) || 15 })}
                    className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Precio base ($)</label>
                  <input
                    type="number"
                    min="0"
                    step="0.01"
                    value={roomForm.basePrice}
                    onChange={(e) => setRoomForm({ ...roomForm, basePrice: parseFloat(e.target.value) || 0 })}
                    className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Orden</label>
                  <input
                    type="number"
                    min="0"
                    value={roomForm.displayOrder}
                    onChange={(e) => setRoomForm({ ...roomForm, displayOrder: parseInt(e.target.value) || 0 })}
                    className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
                  />
                </div>
              </div>

              {editingRoom && (
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={roomForm.isActive}
                    onChange={(e) => setRoomForm({ ...roomForm, isActive: e.target.checked })}
                    className="w-4 h-4 text-[#00205B] border-gray-300 rounded focus:ring-[#00205B]"
                  />
                  <span className="text-sm text-gray-700">Activo</span>
                </label>
              )}

              <div className="flex gap-3 pt-4">
                <button
                  type="button"
                  onClick={closeRoomModal}
                  className="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={saving}
                  className="flex-1 px-4 py-2 bg-[#00205B] text-white rounded-lg hover:bg-[#001440] disabled:opacity-50"
                >
                  {saving ? 'Saving...' : editingRoom ? 'Save' : 'Crear'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </AdminLayout>
  );
}
