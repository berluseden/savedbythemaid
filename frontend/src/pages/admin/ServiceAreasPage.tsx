import { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
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
import {
  serviceAreaSchema,
  type ServiceAreaFormData,
} from '@/shared/schemas/admin.schema';
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
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/shared/components/ui/dialog';
import { Spinner } from '@/shared/components/ui/spinner';

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

const defaultValues: ServiceAreaFormData = {
  name: '',
  description: '',
  isActive: true,
};

export function AdminServiceAreasPage() {
  const [serviceAreas, setServiceAreas] = useState<ServiceArea[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [expandedArea, setExpandedArea] = useState<number | null>(null);

  // Modal state
  const [showModal, setShowModal] = useState(false);
  const [editingArea, setEditingArea] = useState<ServiceArea | null>(null);

  // Zip code modal
  const [showZipModal, setShowZipModal] = useState(false);
  const [selectedAreaId, setSelectedAreaId] = useState<number | null>(null);
  const [newZipCode, setNewZipCode] = useState('');
  const [zipError, setZipError] = useState('');

  // Delete confirmation state
  const [deleteAreaId, setDeleteAreaId] = useState<number | null>(null);
  const [deleteZip, setDeleteZip] = useState<{ areaId: number; zipId: number; zipCode: string } | null>(null);

  const form = useForm<ServiceAreaFormData>({
    resolver: zodResolver(serviceAreaSchema),
    defaultValues,
  });

  useEffect(() => {
    fetchServiceAreas();
  }, []);

  const fetchServiceAreas = async () => {
    setIsLoading(true);
    try {
      const response = await api.get<ServiceArea[]>('/serviceareas');
      setServiceAreas(response.data);
    } catch {
      // Error handled by API interceptor
    } finally {
      setIsLoading(false);
    }
  };

  const handleOpenModal = (area?: ServiceArea) => {
    if (area) {
      setEditingArea(area);
      form.reset({
        name: area.name,
        description: area.description || '',
        isActive: area.isActive,
      });
    } else {
      setEditingArea(null);
      form.reset(defaultValues);
    }
    setShowModal(true);
  };

  const handleCloseModal = () => {
    setShowModal(false);
    setEditingArea(null);
    form.reset(defaultValues);
  };

  const onSubmit = form.handleSubmit(async (data) => {
    try {
      if (editingArea) {
        await api.put(`/serviceareas/${editingArea.id}`, data);
      } else {
        await api.post('/serviceareas', data);
      }
      handleCloseModal();
      fetchServiceAreas();
    } catch {
      // Error handled by API interceptor
    }
  });

  const handleDelete = async (id: number) => {
    try {
      await api.delete(`/serviceareas/${id}`);
      setDeleteAreaId(null);
      fetchServiceAreas();
    } catch {
      // Error handled by API interceptor
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

  const handleDeleteZipCode = async (areaId: number, zipId: number) => {
    try {
      await api.delete(`/serviceareas/${areaId}/zipcodes/${zipId}`);
      setDeleteZip(null);
      fetchServiceAreas();
    } catch {
      // Error handled by API interceptor
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
        {/* Page header */}
        <div className="mb-8 flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Service Areas</h1>
            <p className="mt-1 text-sm text-gray-500">Manage coverage zones and ZIP codes</p>
          </div>
          <button
            onClick={() => handleOpenModal()}
            className="inline-flex items-center gap-2 rounded-lg bg-brand px-4 py-2 text-sm font-medium text-white hover:bg-brand-dark transition-colors"
          >
            <Plus className="h-4 w-4" />
            New Zone
          </button>
        </div>

        {/* Stats */}
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
          <div className="rounded-xl border border-gray-100 bg-white p-5 shadow-sm">
            <div className="flex items-center justify-between mb-4">
              <p className="text-sm text-gray-500">Total zones</p>
              <div className="h-9 w-9 rounded-lg bg-brand/10 flex items-center justify-center">
                <Globe className="h-4 w-4 text-brand" />
              </div>
            </div>
            <p className="text-2xl font-bold text-gray-900">{serviceAreas.length}</p>
          </div>
          <div className="rounded-xl border border-gray-100 bg-white p-5 shadow-sm">
            <div className="flex items-center justify-between mb-4">
              <p className="text-sm text-gray-500">Active zones</p>
              <div className="h-9 w-9 rounded-lg bg-brand/10 flex items-center justify-center">
                <CheckCircle className="h-4 w-4 text-brand" />
              </div>
            </div>
            <p className="text-2xl font-bold text-gray-900">{activeAreas}</p>
          </div>
          <div className="rounded-xl border border-gray-100 bg-white p-5 shadow-sm">
            <div className="flex items-center justify-between mb-4">
              <p className="text-sm text-gray-500">Zip Codes</p>
              <div className="h-9 w-9 rounded-lg bg-brand/10 flex items-center justify-center">
                <MapPin className="h-4 w-4 text-brand" />
              </div>
            </div>
            <p className="text-2xl font-bold text-gray-900">{totalZipCodes}</p>
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
            className="w-full rounded-lg border border-gray-300 bg-white py-2 pl-10 pr-4 text-sm focus:border-brand focus:outline-none focus:ring-1 focus:ring-brand"
          />
        </div>

        {/* Service Areas List */}
        {isLoading ? (
          <div className="flex items-center justify-center py-12">
            <Spinner size="md" />
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
                className="mt-4 inline-flex items-center gap-2 rounded-lg bg-brand px-4 py-2 text-sm font-medium text-white hover:bg-brand-dark"
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
                          {area.isActive ? 'Active' : 'Inactive'}
                        </span>
                      </div>
                      {area.description && (
                        <p className="mt-1 text-sm text-gray-500">{area.description}</p>
                      )}
                      <p className="mt-1 text-xs text-gray-400">
                        {area.zipCodes.length} zip code{area.zipCodes.length !== 1 ? 's' : ''}
                        {''}
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
                      onClick={() => setDeleteAreaId(area.id)}
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
                        className="inline-flex items-center gap-1 rounded-md bg-brand px-3 py-1.5 text-xs font-medium text-white hover:bg-brand-dark"
                      >
                        <Plus className="h-3 w-3" />
                        Add ZIP
                      </button>
                    </div>
                    {area.zipCodes.length === 0 ? (
                      <p className="text-sm text-gray-500 italic">
                        No zip codes configured
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
                              onClick={() => setDeleteZip({ areaId: area.id, zipId: zip.id, zipCode: zip.zipCode })}
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
      </div>

      {/* Delete Area Confirmation */}
      <AlertDialog open={deleteAreaId !== null} onOpenChange={(open) => !open && setDeleteAreaId(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete service area?</AlertDialogTitle>
            <AlertDialogDescription>
              This will also delete all associated zip codes. This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => deleteAreaId !== null && handleDelete(deleteAreaId)}
            >
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Delete Zip Code Confirmation */}
      <AlertDialog open={deleteZip !== null} onOpenChange={(open) => !open && setDeleteZip(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete zip code {deleteZip?.zipCode}?</AlertDialogTitle>
            <AlertDialogDescription>
              This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => deleteZip && handleDeleteZipCode(deleteZip.areaId, deleteZip.zipId)}
            >
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Create/Edit Area Modal */}
      <Dialog open={showModal} onOpenChange={(open) => !open && handleCloseModal()}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>
              {editingArea ? 'Edit Zone' : 'New Service Area'}
            </DialogTitle>
          </DialogHeader>

          <form onSubmit={onSubmit} className="space-y-4">
            {form.formState.errors.root && (
              <p className="text-sm text-red-500">{form.formState.errors.root.message}</p>
            )}

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Name *
              </label>
              <input
                type="text"
                {...form.register('name')}
                placeholder="E.g.: North Miami Zone"
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-brand focus:outline-none focus:ring-1 focus:ring-brand"
              />
              {form.formState.errors.name && (
                <p className="mt-1 text-sm text-red-500">{form.formState.errors.name.message}</p>
              )}
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Description
              </label>
              <textarea
                {...form.register('description')}
                placeholder="Optional zone description"
                rows={3}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-brand focus:outline-none focus:ring-1 focus:ring-brand"
              />
              {form.formState.errors.description && (
                <p className="mt-1 text-sm text-red-500">{form.formState.errors.description.message}</p>
              )}
            </div>

            {editingArea && (
              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  id="isActive"
                  {...form.register('isActive')}
                  className="h-4 w-4 rounded border-gray-300 text-brand focus:ring-brand"
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
                className="rounded-lg bg-brand px-4 py-2 text-sm font-medium text-white hover:bg-brand-dark"
              >
                {editingArea ? 'Save Changes' : 'Create Zone'}
              </button>
            </div>
          </form>
        </DialogContent>
      </Dialog>

      {/* Add Zip Code Modal */}
      <Dialog open={showZipModal} onOpenChange={(open) => !open && setShowZipModal(false)}>
        <DialogContent className="max-w-sm">
          <DialogHeader>
            <DialogTitle>Add Zip Code</DialogTitle>
          </DialogHeader>

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
                placeholder="E.g.: 33101"
                className={`w-full rounded-lg border px-3 py-2 text-sm font-mono focus:outline-none focus:ring-1 ${
                  zipError
                    ? 'border-red-300 focus:border-red-500 focus:ring-red-500'
                    : 'border-gray-300 focus:border-brand focus:ring-brand'
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
                className="rounded-lg bg-brand px-4 py-2 text-sm font-medium text-white hover:bg-brand-dark"
              >
                Add
              </button>
            </div>
          </form>
        </DialogContent>
      </Dialog>
    </AdminLayout>
  );
}
