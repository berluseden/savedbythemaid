import { useState, useEffect, useRef } from 'react';
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
  AlertTriangle,
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

function cn(...classes: (string | boolean | undefined | null)[]): string {
  return classes.filter(Boolean).join(' ');
}

// Inline toggle switch component
interface ToggleSwitchProps {
  checked: boolean;
  onChange: (checked: boolean) => void;
  label?: string;
  disabled?: boolean;
}

function ToggleSwitch({ checked, onChange, label, disabled }: ToggleSwitchProps) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      aria-label={label ?? (checked ? 'Deactivate zone' : 'Activate zone')}
      disabled={disabled}
      onClick={(e) => {
        e.stopPropagation();
        onChange(!checked);
      }}
      className={cn(
        'relative inline-flex h-5 w-9 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-brand',
        checked ? 'bg-brand' : 'bg-gray-200',
        disabled && 'cursor-not-allowed opacity-50',
      )}
    >
      <span
        className={cn(
          'inline-block h-4 w-4 rounded-full bg-white shadow transition-transform',
          checked ? 'translate-x-4' : 'translate-x-0',
        )}
      />
    </button>
  );
}

export function AdminServiceAreasPage() {
  const [serviceAreas, setServiceAreas] = useState<ServiceArea[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [expandedArea, setExpandedArea] = useState<number | null>(null);

  // Optimistic toggle tracking: areaId -> pending state
  const [togglingIds, setTogglingIds] = useState<Set<number>>(new Set());

  // Modal state
  const [showModal, setShowModal] = useState(false);
  const [editingArea, setEditingArea] = useState<ServiceArea | null>(null);

  // Inline ZIP input state per area
  const [zipInputs, setZipInputs] = useState<Record<number, string>>({});
  const [zipErrors, setZipErrors] = useState<Record<number, string>>({});
  const [zipAdding, setZipAdding] = useState<Record<number, boolean>>({});
  const [zipBulkMsg, setZipBulkMsg] = useState<Record<number, string>>({});
  const zipInputRefs = useRef<Record<number, HTMLInputElement | null>>({});

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

  const handleToggleActive = async (area: ServiceArea, newValue: boolean) => {
    // Optimistic update
    setServiceAreas((prev) =>
      prev.map((a) => (a.id === area.id ? { ...a, isActive: newValue } : a)),
    );
    setTogglingIds((prev) => new Set(prev).add(area.id));

    try {
      await api.put(`/serviceareas/${area.id}`, {
        name: area.name,
        description: area.description,
        isActive: newValue,
      });
    } catch {
      // Rollback on error
      setServiceAreas((prev) =>
        prev.map((a) => (a.id === area.id ? { ...a, isActive: area.isActive } : a)),
      );
    } finally {
      setTogglingIds((prev) => {
        const next = new Set(prev);
        next.delete(area.id);
        return next;
      });
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

  const handleZipInputChange = (areaId: number, value: string) => {
    // Allow only digits
    const digits = value.replace(/\D/g, '').slice(0, 5);
    setZipInputs((prev) => ({ ...prev, [areaId]: digits }));
    setZipErrors((prev) => ({ ...prev, [areaId]: '' }));
    setZipBulkMsg((prev) => ({ ...prev, [areaId]: '' }));
  };

  const addSingleZip = async (areaId: number, zip: string): Promise<boolean> => {
    try {
      await api.post(`/serviceareas/${areaId}/zipcodes`, { zipCode: zip });
      return true;
    } catch (error: unknown) {
      const maybe = error as { response?: { status?: number } };
      if (maybe.response?.status === 409) {
        setZipErrors((prev) => ({
          ...prev,
          [areaId]: `${zip} is already assigned to another zone`,
        }));
      } else {
        setZipErrors((prev) => ({ ...prev, [areaId]: 'Error adding zip code' }));
      }
      return false;
    }
  };

  const handleZipKeyDown = async (
    e: React.KeyboardEvent<HTMLInputElement>,
    areaId: number,
  ) => {
    if (e.key !== 'Enter') return;
    e.preventDefault();

    const zip = (zipInputs[areaId] ?? '').trim();
    if (!zip) return;

    if (!/^\d{5}$/.test(zip)) {
      setZipErrors((prev) => ({ ...prev, [areaId]: 'ZIP must be exactly 5 digits' }));
      return;
    }

    setZipAdding((prev) => ({ ...prev, [areaId]: true }));
    const ok = await addSingleZip(areaId, zip);
    setZipAdding((prev) => ({ ...prev, [areaId]: false }));

    if (ok) {
      setZipInputs((prev) => ({ ...prev, [areaId]: '' }));
      fetchServiceAreas();
    }
  };

  const handleZipPaste = async (
    e: React.ClipboardEvent<HTMLInputElement>,
    areaId: number,
  ) => {
    const text = e.clipboardData.getData('text');
    if (!text.includes(',') && !text.includes('\n') && !text.includes(' ')) return;

    e.preventDefault();
    const zips = text
      .split(/[,\n\s]+/)
      .map((z) => z.trim())
      .filter((z) => /^\d{5}$/.test(z));

    if (zips.length === 0) {
      setZipErrors((prev) => ({
        ...prev,
        [areaId]: 'No valid 5-digit ZIP codes found in pasted text',
      }));
      return;
    }

    setZipAdding((prev) => ({ ...prev, [areaId]: true }));
    setZipErrors((prev) => ({ ...prev, [areaId]: '' }));
    setZipBulkMsg((prev) => ({ ...prev, [areaId]: '' }));

    const results = await Promise.all(zips.map((z) => addSingleZip(areaId, z)));
    const addedCount = results.filter(Boolean).length;

    setZipAdding((prev) => ({ ...prev, [areaId]: false }));

    if (addedCount > 0) {
      setZipBulkMsg((prev) => ({
        ...prev,
        [areaId]: `Added ${addedCount} ZIP${addedCount !== 1 ? 's' : ''}`,
      }));
      fetchServiceAreas();
    }
  };

  const toggleExpand = (areaId: number) => {
    setExpandedArea(expandedArea === areaId ? null : areaId);
  };

  const filteredAreas = serviceAreas
    .filter(
      (area) =>
        area.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
        area.description?.toLowerCase().includes(searchTerm.toLowerCase()) ||
        area.zipCodes.some((z) => z.zipCode.includes(searchTerm)),
    )
    // Active zones first, then inactive; each group alphabetical
    .sort((a, b) => {
      if (a.isActive !== b.isActive) return a.isActive ? -1 : 1;
      return a.name.localeCompare(b.name);
    });

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
              <p className="text-sm text-gray-500">ZIP Codes</p>
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
            placeholder="Search by name, description or ZIP code..."
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
          <div className="space-y-3">
            {filteredAreas.map((area) => {
              const isExpanded = expandedArea === area.id;
              const isEmpty = area.zipCodes.length === 0;
              const isToggling = togglingIds.has(area.id);

              return (
                <div
                  key={area.id}
                  className={cn(
                    'rounded-xl border bg-white overflow-hidden shadow-sm transition-opacity',
                    area.isActive ? 'border-gray-200' : 'border-gray-200 opacity-60',
                  )}
                >
                  {/* Area Header */}
                  <div className="flex items-center justify-between p-4">
                    {/* Left: expand chevron + info */}
                    <div className="flex items-center gap-3 min-w-0">
                      <button
                        onClick={() => toggleExpand(area.id)}
                        className="shrink-0 rounded-lg p-1 hover:bg-gray-100 transition-colors"
                        aria-label={isExpanded ? 'Collapse' : 'Expand'}
                      >
                        {isExpanded ? (
                          <ChevronUp className="h-5 w-5 text-gray-400" />
                        ) : (
                          <ChevronDown className="h-5 w-5 text-gray-400" />
                        )}
                      </button>

                      <div className="min-w-0">
                        <div className="flex items-center gap-2 flex-wrap">
                          <h3
                            className={cn(
                              'font-medium truncate',
                              area.isActive ? 'text-gray-900' : 'text-gray-500',
                            )}
                          >
                            {area.name}
                          </h3>

                          {/* ZIP count badge */}
                          {!isExpanded && (
                            <span className="inline-flex items-center rounded-full bg-gray-100 px-2 py-0.5 text-xs text-gray-500 font-medium whitespace-nowrap">
                              {area.zipCodes.length} ZIP{area.zipCodes.length !== 1 ? 's' : ''}
                            </span>
                          )}

                          {/* Empty zone warning */}
                          {isEmpty && (
                            <span
                              className="inline-flex items-center gap-1 text-xs text-warning font-medium"
                              title="No ZIP codes configured"
                            >
                              <AlertTriangle className="h-3 w-3" />
                              no ZIPs
                            </span>
                          )}
                        </div>

                        {area.description && (
                          <p className="mt-0.5 text-xs text-gray-400 truncate max-w-sm">
                            {area.description}
                          </p>
                        )}
                      </div>
                    </div>

                    {/* Right: toggle + actions */}
                    <div className="flex items-center gap-3 shrink-0 ml-4">
                      <div className="flex items-center gap-2">
                        <span className="text-xs text-gray-400 hidden sm:inline">
                          {area.isActive ? 'Active' : 'Inactive'}
                        </span>
                        <ToggleSwitch
                          checked={area.isActive}
                          onChange={(val) => handleToggleActive(area, val)}
                          disabled={isToggling}
                        />
                      </div>

                      <button
                        onClick={() => handleOpenModal(area)}
                        className="rounded-lg p-2 text-gray-400 hover:bg-gray-100 hover:text-gray-700 transition-colors"
                        title="Edit zone"
                      >
                        <Edit2 className="h-4 w-4" />
                      </button>
                      <button
                        onClick={() => setDeleteAreaId(area.id)}
                        className="rounded-lg p-2 text-gray-400 hover:bg-red-50 hover:text-danger transition-colors"
                        title="Delete zone"
                      >
                        <Trash2 className="h-4 w-4" />
                      </button>
                    </div>
                  </div>

                  {/* Expanded: ZIP codes + inline add input */}
                  {isExpanded && (
                    <div className="border-t border-gray-100 bg-gray-50 px-4 py-4">
                      <p className="text-xs font-semibold uppercase tracking-wide text-gray-400 mb-3">
                        ZIP Codes
                      </p>

                      {/* ZIP chips */}
                      {area.zipCodes.length === 0 ? (
                        <p className="text-sm text-gray-400 italic mb-3">
                          No ZIP codes configured yet
                        </p>
                      ) : (
                        <div className="flex flex-wrap gap-2 mb-3">
                          {area.zipCodes.map((zip) => (
                            <div
                              key={zip.id}
                              className="group flex items-center gap-1 rounded-full bg-white border border-gray-200 px-3 py-1 text-sm shadow-sm"
                            >
                              <MapPin className="h-3 w-3 text-gray-300" />
                              <span className="font-mono text-gray-700">{zip.zipCode}</span>
                              <button
                                onClick={() =>
                                  setDeleteZip({
                                    areaId: area.id,
                                    zipId: zip.id,
                                    zipCode: zip.zipCode,
                                  })
                                }
                                className="ml-1 rounded-full p-0.5 text-gray-300 hover:bg-red-50 hover:text-danger opacity-0 group-hover:opacity-100 transition-opacity"
                                aria-label={`Remove ZIP ${zip.zipCode}`}
                              >
                                <X className="h-3 w-3" />
                              </button>
                            </div>
                          ))}
                        </div>
                      )}

                      {/* Inline ZIP add input */}
                      <div className="flex flex-col gap-1">
                        <div className="flex items-center gap-2">
                          <div className="relative flex-1 max-w-xs">
                            <input
                              ref={(el) => { zipInputRefs.current[area.id] = el; }}
                              type="text"
                              inputMode="numeric"
                              placeholder="Add ZIP (e.g. 33101) or paste multiple"
                              value={zipInputs[area.id] ?? ''}
                              onChange={(e) => handleZipInputChange(area.id, e.target.value)}
                              onKeyDown={(e) => handleZipKeyDown(e, area.id)}
                              onPaste={(e) => handleZipPaste(e, area.id)}
                              maxLength={5}
                              className={cn(
                                'w-full rounded-lg border px-3 py-1.5 text-sm font-mono placeholder:font-sans placeholder:text-gray-400 focus:outline-none focus:ring-1',
                                zipErrors[area.id]
                                  ? 'border-danger focus:border-danger focus:ring-danger/30'
                                  : 'border-gray-200 focus:border-brand focus:ring-brand/30',
                              )}
                            />
                            {zipAdding[area.id] && (
                              <span className="absolute right-2 top-1/2 -translate-y-1/2">
                                <Spinner size="sm" />
                              </span>
                            )}
                          </div>
                          <span className="text-xs text-gray-400 hidden sm:inline">
                            Press Enter to add
                          </span>
                        </div>

                        {/* Inline error */}
                        {zipErrors[area.id] && (
                          <p className="text-xs text-danger">{zipErrors[area.id]}</p>
                        )}

                        {/* Bulk add success */}
                        {zipBulkMsg[area.id] && (
                          <p className="text-xs text-success font-medium">{zipBulkMsg[area.id]}</p>
                        )}
                      </div>
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        )}
      </div>

      {/* Delete Area Confirmation */}
      <AlertDialog
        open={deleteAreaId !== null}
        onOpenChange={(open) => !open && setDeleteAreaId(null)}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete service area?</AlertDialogTitle>
            <AlertDialogDescription>
              This will also delete all associated ZIP codes. This action cannot be undone.
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

      {/* Delete ZIP Code Confirmation */}
      <AlertDialog
        open={deleteZip !== null}
        onOpenChange={(open) => !open && setDeleteZip(null)}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Remove ZIP {deleteZip?.zipCode}?</AlertDialogTitle>
            <AlertDialogDescription>
              This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={() =>
                deleteZip &&
                handleDeleteZipCode(deleteZip.areaId, deleteZip.zipId)
              }
            >
              Remove
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Create / Edit Area Modal */}
      <Dialog open={showModal} onOpenChange={(open) => !open && handleCloseModal()}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>{editingArea ? 'Edit Zone' : 'New Service Area'}</DialogTitle>
          </DialogHeader>

          <form onSubmit={onSubmit} className="space-y-4">
            {form.formState.errors.root && (
              <p className="text-sm text-danger">{form.formState.errors.root.message}</p>
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
                <p className="mt-1 text-sm text-danger">
                  {form.formState.errors.name.message}
                </p>
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
                <p className="mt-1 text-sm text-danger">
                  {form.formState.errors.description.message}
                </p>
              )}
            </div>

            {editingArea && (
              <div className="flex items-center gap-3">
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
    </AdminLayout>
  );
}
