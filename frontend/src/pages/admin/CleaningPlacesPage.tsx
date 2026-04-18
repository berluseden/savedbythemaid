import { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
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
import {
  cleaningPlaceSchema,
  cleaningPlaceRoomSchema,
  type CleaningPlaceFormData,
  type CleaningPlaceRoomFormData,
} from '@/shared/schemas/admin.schema';

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

const placeDefaults: CleaningPlaceFormData = { name: '', description: '', isActive: true };
const roomDefaults: CleaningPlaceRoomFormData = {
  name: '',
  description: '',
  baseMinutes: 15,
  basePrice: 10,
  displayOrder: 0,
  isActive: true,
};

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

  const placeForm = useForm<CleaningPlaceFormData>({
    resolver: zodResolver(cleaningPlaceSchema),
    defaultValues: placeDefaults,
  });

  const roomForm = useForm<CleaningPlaceRoomFormData>({
    resolver: zodResolver(cleaningPlaceRoomSchema),
    defaultValues: roomDefaults,
  });

  useEffect(() => {
    fetchPlaces();
  }, []);

  const fetchPlaces = async () => {
    setIsLoading(true);
    try {
      const response = await api.get<CleaningPlace[]>('/admin/cleaningplaces');
      setPlaces(response.data);
    } catch (_err) {
      setError('Error loading property types');
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
  const onPlaceSubmit = placeForm.handleSubmit(async (data) => {
    setSaving(true);
    setError('');

    try {
      if (editingPlace) {
        await api.put(`/admin/cleaningplaces/${editingPlace.id}`, data);
      } else {
        await api.post('/admin/cleaningplaces', data);
      }
      await fetchPlaces();
      closePlaceModal();
    } catch (_err) {
      setError('Error saving property type');
    } finally {
      setSaving(false);
    }
  });

  const handleEditPlace = (place: CleaningPlace) => {
    setEditingPlace(place);
    placeForm.reset({
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
    } catch (_err) {
      setError('Error deleting property type');
    }
  };

  const closePlaceModal = () => {
    setShowModal(false);
    setEditingPlace(null);
    placeForm.reset(placeDefaults);
  };

  // Room handlers
  const onRoomSubmit = roomForm.handleSubmit(async (data) => {
    if (!selectedPlaceId) return;
    setSaving(true);
    setError('');

    try {
      if (editingRoom) {
        await api.put(`/admin/cleaningplaces/${selectedPlaceId}/rooms/${editingRoom.id}`, data);
      } else {
        await api.post(`/admin/cleaningplaces/${selectedPlaceId}/rooms`, data);
      }
      await fetchPlaces();
      closeRoomModal();
    } catch (_err) {
      setError('Error saving room');
    } finally {
      setSaving(false);
    }
  });

  const handleAddRoom = (placeId: number) => {
    setSelectedPlaceId(placeId);
    setEditingRoom(null);
    roomForm.reset(roomDefaults);
    setShowRoomModal(true);
  };

  const handleEditRoom = (placeId: number, room: CleaningPlaceRoom) => {
    setSelectedPlaceId(placeId);
    setEditingRoom(room);
    roomForm.reset({
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
    } catch (_err) {
      setError('Error deleting room');
    }
  };

  const closeRoomModal = () => {
    setShowRoomModal(false);
    setEditingRoom(null);
    setSelectedPlaceId(null);
    roomForm.reset(roomDefaults);
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
            <h1 className="text-2xl font-bold text-gray-900">Property Types</h1>
            <p className="mt-1 text-sm text-gray-500">Manage cleaning place types and rooms</p>
          </div>
          <button
            onClick={() => { placeForm.reset(placeDefaults); setShowModal(true); }}
            className="inline-flex items-center gap-2 px-4 py-2 bg-brand text-white rounded-lg hover:bg-brand-dark transition-colors"
          >
            <Plus className="h-5 w-5" />
            New Type
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
            placeholder="Search property types..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#2196f3] focus:border-transparent"
          />
        </div>

        {/* Stats */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div className="bg-white rounded-lg p-4 border">
            <p className="text-sm text-gray-500">Total Types</p>
            <p className="text-2xl font-bold text-gray-900">{places.length}</p>
          </div>
          <div className="bg-white rounded-lg p-4 border">
            <p className="text-sm text-gray-500">Active</p>
            <p className="text-2xl font-bold text-green-600">
              {places.filter(p => p.isActive).length}
            </p>
          </div>
          <div className="bg-white rounded-lg p-4 border">
            <p className="text-sm text-gray-500">Total Rooms</p>
            <p className="text-2xl font-bold text-[#2196f3]">
              {places.reduce((acc, p) => acc + p.rooms.length, 0)}
            </p>
          </div>
          <div className="bg-white rounded-lg p-4 border">
            <p className="text-sm text-gray-500">Active Rooms</p>
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
                  <div className="w-10 h-10 bg-[#b8e07c]/20 rounded-lg flex items-center justify-center">
                    <Home className="h-5 w-5 text-[#2196f3]" />
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
                    {place.isActive ? 'Active' : 'Inactive'}
                  </span>
                  <button
                    onClick={() => handleAddRoom(place.id)}
                    className="p-2 text-gray-400 hover:text-[#2196f3] hover:bg-[#b8e07c]/10 rounded-lg"
                    title="Add room"
                  >
                    <Plus className="h-4 w-4" />
                  </button>
                  <button
                    onClick={() => handleEditPlace(place)}
                    className="p-2 text-gray-400 hover:text-[#2196f3] hover:bg-[#b8e07c]/10 rounded-lg"
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
                            {room.isActive ? 'Active' : 'Inactive'}
                          </span>
                          <button
                            onClick={() => handleEditRoom(place.id, room)}
                            className="p-1 text-gray-400 hover:text-[#2196f3]"
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
                  No rooms configured.{' '}
                  <button
                    onClick={() => handleAddRoom(place.id)}
                    className="text-[#2196f3] hover:underline"
                  >
                    Add one
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
                {editingPlace ? 'Edit Property Type' : 'New Property Type'}
              </h2>
              <button onClick={closePlaceModal} className="p-2 text-gray-400 hover:text-gray-600">
                <X className="h-5 w-5" />
              </button>
            </div>

            <form onSubmit={onPlaceSubmit} className="p-6 space-y-4">
              {placeForm.formState.errors.root && (
                <p className="text-sm text-red-500">{placeForm.formState.errors.root.message}</p>
              )}

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Name *</label>
                <input
                  type="text"
                  {...placeForm.register('name')}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#2196f3] focus:border-transparent"
                />
                {placeForm.formState.errors.name && (
                  <p className="text-sm text-red-500 mt-1">{placeForm.formState.errors.name.message}</p>
                )}
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Description</label>
                <textarea
                  {...placeForm.register('description')}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#2196f3] focus:border-transparent"
                  rows={3}
                />
                {placeForm.formState.errors.description && (
                  <p className="text-sm text-red-500 mt-1">{placeForm.formState.errors.description.message}</p>
                )}
              </div>

              {editingPlace && (
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    {...placeForm.register('isActive')}
                    className="w-4 h-4 text-[#2196f3] border-gray-300 rounded focus:ring-[#2196f3]"
                  />
                  <span className="text-sm text-gray-700">Active</span>
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
                  className="flex-1 px-4 py-2 bg-[#2196f3] text-white rounded-lg hover:bg-[#29338c] disabled:opacity-50"
                >
                  {saving ? 'Saving...' : editingPlace ? 'Save' : 'Create'}
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

            <form onSubmit={onRoomSubmit} className="p-6 space-y-4">
              {roomForm.formState.errors.root && (
                <p className="text-sm text-red-500">{roomForm.formState.errors.root.message}</p>
              )}

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Name *</label>
                <input
                  type="text"
                  {...roomForm.register('name')}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#2196f3] focus:border-transparent"
                />
                {roomForm.formState.errors.name && (
                  <p className="text-sm text-red-500 mt-1">{roomForm.formState.errors.name.message}</p>
                )}
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Description</label>
                <textarea
                  {...roomForm.register('description')}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#2196f3] focus:border-transparent"
                  rows={2}
                />
                {roomForm.formState.errors.description && (
                  <p className="text-sm text-red-500 mt-1">{roomForm.formState.errors.description.message}</p>
                )}
              </div>

              <div className="grid grid-cols-3 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Base minutes</label>
                  <input
                    type="number"
                    min="5"
                    step="5"
                    {...roomForm.register('baseMinutes', { valueAsNumber: true })}
                    className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#2196f3] focus:border-transparent"
                  />
                  {roomForm.formState.errors.baseMinutes && (
                    <p className="text-sm text-red-500 mt-1">{roomForm.formState.errors.baseMinutes.message}</p>
                  )}
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Base price ($)</label>
                  <input
                    type="number"
                    min="0"
                    step="0.01"
                    {...roomForm.register('basePrice', { valueAsNumber: true })}
                    className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#2196f3] focus:border-transparent"
                  />
                  {roomForm.formState.errors.basePrice && (
                    <p className="text-sm text-red-500 mt-1">{roomForm.formState.errors.basePrice.message}</p>
                  )}
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Order</label>
                  <input
                    type="number"
                    min="0"
                    {...roomForm.register('displayOrder', { valueAsNumber: true })}
                    className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#2196f3] focus:border-transparent"
                  />
                  {roomForm.formState.errors.displayOrder && (
                    <p className="text-sm text-red-500 mt-1">{roomForm.formState.errors.displayOrder.message}</p>
                  )}
                </div>
              </div>

              {editingRoom && (
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    {...roomForm.register('isActive')}
                    className="w-4 h-4 text-[#2196f3] border-gray-300 rounded focus:ring-[#2196f3]"
                  />
                  <span className="text-sm text-gray-700">Active</span>
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
                  className="flex-1 px-4 py-2 bg-[#2196f3] text-white rounded-lg hover:bg-[#29338c] disabled:opacity-50"
                >
                  {saving ? 'Saving...' : editingRoom ? 'Save' : 'Create'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </AdminLayout>
  );
}
