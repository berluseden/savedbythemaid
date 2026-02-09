import { useState, useEffect } from 'react';
import {
  Plus,
  Search,
  Edit2,
  Trash2,
  Mail,
  Phone,
  X,
  User,
  Calendar,
  Clock,
  CalendarOff,
  MapPin,
  Wrench,
  Check,
} from 'lucide-react';
import { AdminLayout } from '../../components/admin/AdminLayout';
import api from '../../lib/api';

interface EmployeeDto {
  id: number;
  firstName: string;
  lastName: string;
  email: string | null;
  phone: string | null;
  isActive: boolean;
  primaryServiceAreaName: string | null;
  serviceAreaCount: number;
  maxDailyHours: number | null;
  maxDailyServices: number | null;
}

interface ServiceArea {
  id: number;
  name: string;
}

interface EmployeeServiceArea {
  serviceAreaId: number;
  serviceArea: { id: number; name: string };
  isPrimary: boolean;
}

interface Equipment {
  id: number;
  name: string;
  description: string | null;
}

interface EmployeeEquipment {
  equipmentId: number;
  equipment: { id: number; name: string };
  isAvailable: boolean;
}

interface EmployeeSchedule {
  id: number;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  bufferMinutes: number;
  isAvailable: boolean;
}

interface EmployeeTimeOff {
  id: number;
  startDateTime: string;
  endDateTime: string;
  isAllDay: boolean;
  type: number;
  reason: string;
  status: number;
}

const TIME_OFF_TYPES = [
  { value: 0, label: 'Permiso General', color: 'gray' },
  { value: 1, label: 'Vacaciones', color: 'blue' },
  { value: 2, label: 'Enfermedad', color: 'red' },
  { value: 3, label: 'Personal', color: 'purple' },
  { value: 4, label: 'Bloqueo Manual', color: 'orange' },
];

const TIME_OFF_STATUS = [
  { value: 0, label: 'Pendiente', color: 'yellow' },
  { value: 1, label: 'Aprobado', color: 'green' },
  { value: 2, label: 'Rechazado', color: 'red' },
];

const DAYS_OF_WEEK = ['Domingo', 'Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado'];
const DAYS_SHORT = ['Dom', 'Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb'];

type ModalTab = 'info' | 'zones' | 'equipment' | 'schedule' | 'timeoff';

export function AdminEmployeesPage() {
  const [employees, setEmployees] = useState<EmployeeDto[]>([]);
  const [serviceAreas, setServiceAreas] = useState<ServiceArea[]>([]);
  const [allEquipment, setAllEquipment] = useState<Equipment[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [filterStatus, setFilterStatus] = useState<string>('all');
  const [showModal, setShowModal] = useState(false);
  const [editingEmployee, setEditingEmployee] = useState<EmployeeDto | null>(null);
  const [deleteConfirm, setDeleteConfirm] = useState<number | null>(null);
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);
  const [activeTab, setActiveTab] = useState<ModalTab>('info');
  const [schedules, setSchedules] = useState<EmployeeSchedule[]>([]);
  const [timeOffs, setTimeOffs] = useState<EmployeeTimeOff[]>([]);
  const [assignedZones, setAssignedZones] = useState<EmployeeServiceArea[]>([]);
  const [assignedEquipment, setAssignedEquipment] = useState<EmployeeEquipment[]>([]);
  const [loadingSchedules, setLoadingSchedules] = useState(false);

  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    address: '',
    primaryServiceAreaId: null as number | null,
    maxDailyHours: 8,
    maxDailyServices: 4,
    isActive: true,
  });

  const [scheduleForm, setScheduleForm] = useState({
    dayOfWeek: 1,
    startTime: '08:00',
    endTime: '18:00',
    bufferMinutes: 15,
    isAvailable: true,
  });

  const [timeOffForm, setTimeOffForm] = useState({
    startDate: '',
    endDate: '',
    isAllDay: false,
    type: 0,
    reason: '',
  });

  useEffect(() => {
    fetchEmployees();
    fetchServiceAreas();
    fetchAllEquipment();
  }, []);

  const fetchServiceAreas = async () => {
    try {
      const response = await api.get<ServiceArea[]>('/serviceareas');
      setServiceAreas(response.data);
    } catch (err) {
      console.error('Error loading service areas', err);
    }
  };

  const fetchAllEquipment = async () => {
    try {
      const response = await api.get<Equipment[]>('/admin/equipment');
      setAllEquipment(response.data);
    } catch (err) {
      console.error('Error loading equipment', err);
    }
  };

  const fetchEmployees = async () => {
    setIsLoading(true);
    try {
      const response = await api.get<EmployeeDto[]>('/admin/employees');
      setEmployees(response.data);
    } catch (err) {
      setError('Error al cargar empleados');
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  const fetchEmployeeDetails = async (employeeId: number) => {
    try {
      const response = await api.get<{
        serviceAreas: EmployeeServiceArea[];
        equipment: EmployeeEquipment[];
      }>(`/admin/employees/${employeeId}`);
      setAssignedZones(response.data.serviceAreas || []);
      setAssignedEquipment(response.data.equipment || []);
    } catch (err) {
      console.error('Error loading employee details', err);
    }
  };

  const fetchSchedules = async (employeeId: number) => {
    setLoadingSchedules(true);
    try {
      const response = await api.get<EmployeeSchedule[]>(`/admin/employees/${employeeId}/schedules`);
      setSchedules(response.data);
    } catch (err) {
      console.error('Error loading schedules', err);
    } finally {
      setLoadingSchedules(false);
    }
  };

  const fetchTimeOffs = async (employeeId: number) => {
    try {
      const response = await api.get<EmployeeTimeOff[]>(`/admin/employees/${employeeId}/time-off`);
      setTimeOffs(response.data);
    } catch (err) {
      console.error('Error loading time-offs', err);
    }
  };

  const handleAddSchedule = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingEmployee) return;
    setSaving(true);
    try {
      await api.post(`/admin/employees/${editingEmployee.id}/schedules`, scheduleForm);
      await fetchSchedules(editingEmployee.id);
      setScheduleForm({ dayOfWeek: 1, startTime: '08:00', endTime: '18:00', bufferMinutes: 15, isAvailable: true });
    } catch (_err) {
      setError('Error al guardar horario');
    } finally {
      setSaving(false);
    }
  };

  const handleDeleteSchedule = async (dayOfWeek: number) => {
    if (!editingEmployee) return;
    try {
      await api.delete(`/admin/employees/${editingEmployee.id}/schedules/${dayOfWeek}`);
      await fetchSchedules(editingEmployee.id);
    } catch (_err) {
      setError('Error al eliminar horario');
    }
  };

  const handleAddTimeOff = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingEmployee) return;
    setSaving(true);
    try {
      await api.post(`/admin/employees/${editingEmployee.id}/time-off`, {
        startDateTime: new Date(timeOffForm.startDate).toISOString(),
        endDateTime: new Date(timeOffForm.endDate).toISOString(),
        isAllDay: timeOffForm.isAllDay,
        type: timeOffForm.type,
        reason: timeOffForm.reason,
      });
      await fetchTimeOffs(editingEmployee.id);
      setTimeOffForm({ startDate: '', endDate: '', isAllDay: false, type: 0, reason: '' });
    } catch (_err) {
      setError('Error al guardar bloqueo');
    } finally {
      setSaving(false);
    }
  };

  const handleDeleteTimeOff = async (timeOffId: number) => {
    if (!editingEmployee) return;
    try {
      await api.delete(`/admin/employees/${editingEmployee.id}/time-off/${timeOffId}`);
      await fetchTimeOffs(editingEmployee.id);
    } catch (_err) {
      setError('Error al eliminar bloqueo');
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError('');

    try {
      if (editingEmployee) {
        await api.put(`/admin/employees/${editingEmployee.id}`, formData);
      } else {
        await api.post('/admin/employees', formData);
      }
      await fetchEmployees();
      closeModal();
    } catch (err) {
      setError('Error al guardar empleado');
      console.error(err);
    } finally {
      setSaving(false);
    }
  };

  const handleEdit = (employee: EmployeeDto) => {
    setEditingEmployee(employee);
    setFormData({
      firstName: employee.firstName,
      lastName: employee.lastName,
      email: employee.email || '',
      phone: employee.phone || '',
      address: '',
      primaryServiceAreaId: null,
      maxDailyHours: employee.maxDailyHours || 8,
      maxDailyServices: employee.maxDailyServices || 4,
      isActive: employee.isActive,
    });
    setActiveTab('info');
    fetchSchedules(employee.id);
    fetchTimeOffs(employee.id);
    fetchEmployeeDetails(employee.id);
    setShowModal(true);
  };

  // Zone management
  const handleAddZone = async (serviceAreaId: number, isPrimary: boolean = false) => {
    if (!editingEmployee) return;
    setSaving(true);
    try {
      await api.post(`/admin/employees/${editingEmployee.id}/service-areas/${serviceAreaId}?isPrimary=${isPrimary}`);
      await fetchEmployeeDetails(editingEmployee.id);
      await fetchEmployees();
    } catch (_err) {
      setError('Error al agregar zona');
    } finally {
      setSaving(false);
    }
  };

  const handleRemoveZone = async (serviceAreaId: number) => {
    if (!editingEmployee) return;
    try {
      await api.delete(`/admin/employees/${editingEmployee.id}/service-areas/${serviceAreaId}`);
      await fetchEmployeeDetails(editingEmployee.id);
      await fetchEmployees();
    } catch (_err) {
      setError('Error al eliminar zona');
    }
  };

  const handleSetPrimaryZone = async (serviceAreaId: number) => {
    if (!editingEmployee) return;
    try {
      await api.post(`/admin/employees/${editingEmployee.id}/service-areas/${serviceAreaId}?isPrimary=true`);
      await fetchEmployeeDetails(editingEmployee.id);
      await fetchEmployees();
    } catch (_err) {
      setError('Error al establecer zona principal');
    }
  };

  // Equipment management
  const handleAddEquipment = async (equipmentId: number) => {
    if (!editingEmployee) return;
    setSaving(true);
    try {
      await api.post(`/admin/employees/${editingEmployee.id}/equipment/${equipmentId}`);
      await fetchEmployeeDetails(editingEmployee.id);
    } catch (_err) {
      setError('Error al agregar equipamiento');
    } finally {
      setSaving(false);
    }
  };

  const handleRemoveEquipment = async (equipmentId: number) => {
    if (!editingEmployee) return;
    try {
      await api.delete(`/admin/employees/${editingEmployee.id}/equipment/${equipmentId}`);
      await fetchEmployeeDetails(editingEmployee.id);
    } catch (_err) {
      setError('Error al eliminar equipamiento');
    }
  };

  const handleToggleEquipmentAvailability = async (equipmentId: number, currentAvailable: boolean) => {
    if (!editingEmployee) return;
    try {
      // Primero eliminamos y luego volvemos a agregar con el nuevo estado
      await api.post(`/admin/employees/${editingEmployee.id}/equipment/${equipmentId}?isAvailable=${!currentAvailable}`);
      await fetchEmployeeDetails(editingEmployee.id);
    } catch (_err) {
      setError('Error al cambiar disponibilidad');
    }
  };

  const handleDelete = async (id: number) => {
    try {
      await api.delete(`/admin/employees/${id}`);
      await fetchEmployees();
      setDeleteConfirm(null);
    } catch (err) {
      setError('Error al eliminar empleado');
      console.error(err);
    }
  };

  const closeModal = () => {
    setShowModal(false);
    setEditingEmployee(null);
    setActiveTab('info');
    setSchedules([]);
    setTimeOffs([]);
    setAssignedZones([]);
    setAssignedEquipment([]);
    setFormData({
      firstName: '',
      lastName: '',
      email: '',
      phone: '',
      address: '',
      primaryServiceAreaId: null,
      maxDailyHours: 8,
      maxDailyServices: 4,
      isActive: true,
    });
  };

  const filteredEmployees = employees.filter(employee => {
    const matchesSearch =
      employee.firstName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      employee.lastName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      (employee.email?.toLowerCase().includes(searchTerm.toLowerCase()) ?? false);
    const matchesStatus = filterStatus === 'all' || 
      (filterStatus === 'active' && employee.isActive) || 
      (filterStatus === 'inactive' && !employee.isActive);
    return matchesSearch && matchesStatus;
  });

  const getStatusBadge = (isActive: boolean) => {
    return isActive ? (
      <span className="px-2 py-1 rounded-full text-xs font-medium bg-green-100 text-green-700">
        Activo
      </span>
    ) : (
      <span className="px-2 py-1 rounded-full text-xs font-medium bg-gray-100 text-gray-700">
        Inactivo
      </span>
    );
  };

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
            <h1 className="text-2xl font-bold text-gray-900">Empleados</h1>
            <p className="text-gray-600">Gestiona el equipo de limpieza</p>
          </div>
          <button
            onClick={() => setShowModal(true)}
            className="inline-flex items-center gap-2 px-4 py-2 bg-[#00205B] text-white rounded-lg hover:bg-[#001440] transition-colors"
          >
            <Plus className="h-5 w-5" />
            Nuevo Empleado
          </button>
        </div>

        {/* Filters */}
        <div className="flex flex-col sm:flex-row gap-4">
          <div className="relative flex-1 max-w-md">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-gray-400" />
            <input
              type="search"
              placeholder="Buscar empleados..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
            />
          </div>
          <select
            value={filterStatus}
            onChange={(e) => setFilterStatus(e.target.value)}
            className="px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
          >
            <option value="all">Todos los estados</option>
            <option value="active">Activos</option>
            <option value="inactive">Inactivos</option>
            <option value="on-leave">En permiso</option>
          </select>
        </div>

        {/* Stats */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div className="bg-white rounded-lg p-4 border">
            <p className="text-sm text-gray-500">Total</p>
            <p className="text-2xl font-bold text-gray-900">{employees.length}</p>
          </div>
          <div className="bg-white rounded-lg p-4 border">
            <p className="text-sm text-gray-500">Activos</p>
            <p className="text-2xl font-bold text-green-600">
              {employees.filter(e => e.isActive).length}
            </p>
          </div>
          <div className="bg-white rounded-lg p-4 border">
            <p className="text-sm text-gray-500">Inactivos</p>
            <p className="text-2xl font-bold text-gray-600">
              {employees.filter(e => !e.isActive).length}
            </p>
          </div>
          <div className="bg-white rounded-lg p-4 border">
            <p className="text-sm text-gray-500">Zonas cubiertas</p>
            <p className="text-2xl font-bold text-[#00205B]">
              {employees.reduce((acc, e) => acc + e.serviceAreaCount, 0)}
            </p>
          </div>
        </div>

        {/* Employees Table */}
        <div className="bg-white rounded-xl shadow-sm border overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="text-left text-sm text-gray-500 bg-gray-50 border-b">
                  <th className="px-6 py-3 font-medium">Empleado</th>
                  <th className="px-6 py-3 font-medium">Contacto</th>
                  <th className="px-6 py-3 font-medium">Estado</th>
                  <th className="px-6 py-3 font-medium">Zona Principal</th>
                  <th className="px-6 py-3 font-medium text-right">Acciones</th>
                </tr>
              </thead>
              <tbody>
                {filteredEmployees.map((employee) => (
                  <tr key={employee.id} className="border-b last:border-0 hover:bg-gray-50">
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-3">
                        <div className="w-10 h-10 bg-[#FFE44D]/20 rounded-full flex items-center justify-center">
                          <User className="h-5 w-5 text-[#00205B]" />
                        </div>
                        <div>
                          <p className="font-medium text-gray-900">
                            {employee.firstName} {employee.lastName}
                          </p>
                          <p className="text-sm text-gray-500">
                            {employee.serviceAreaCount} zonas | Max {employee.maxDailyServices || 4} servicios/día
                          </p>
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <div className="space-y-1">
                        <div className="flex items-center gap-2 text-sm text-gray-600">
                          <Mail className="h-4 w-4" />
                          {employee.email || '-'}
                        </div>
                        <div className="flex items-center gap-2 text-sm text-gray-600">
                          <Phone className="h-4 w-4" />
                          {employee.phone || '-'}
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      {getStatusBadge(employee.isActive)}
                    </td>
                    <td className="px-6 py-4">
                      <span className="text-sm text-gray-600">
                        {employee.primaryServiceAreaName || 'Sin asignar'}
                      </span>
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex items-center justify-end gap-2">
                        <button
                          onClick={() => handleEdit(employee)}
                          className="p-2 text-gray-400 hover:text-[#00205B] hover:bg-[#FFE44D]/10 rounded-lg transition-colors"
                        >
                          <Edit2 className="h-4 w-4" />
                        </button>
                        <button
                          onClick={() => setDeleteConfirm(employee.id)}
                          className="p-2 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                        >
                          <Trash2 className="h-4 w-4" />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        {filteredEmployees.length === 0 && (
          <div className="text-center py-12">
            <p className="text-gray-500">No se encontraron empleados</p>
          </div>
        )}
      </div>

      {/* Delete Confirmation Modal */}
      {deleteConfirm && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50">
          <div className="bg-white rounded-xl shadow-xl p-6 w-full max-w-sm">
            <h3 className="text-lg font-semibold text-gray-900 mb-2">¿Eliminar empleado?</h3>
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

      {/* Add/Edit Modal */}
      {showModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-2xl max-h-[90vh] overflow-hidden flex flex-col">
            <div className="flex items-center justify-between p-6 border-b">
              <h2 className="text-xl font-semibold text-gray-900">
                {editingEmployee ? `${editingEmployee.firstName} ${editingEmployee.lastName}` : 'Nuevo Empleado'}
              </h2>
              <button onClick={closeModal} className="p-2 text-gray-400 hover:text-gray-600">
                <X className="h-5 w-5" />
              </button>
            </div>

            {/* Tabs - solo mostrar si estamos editando */}
            {editingEmployee && (
              <div className="flex border-b bg-gray-50 overflow-x-auto">
                <button
                  onClick={() => setActiveTab('info')}
                  className={`flex-1 min-w-[80px] px-3 py-3 text-sm font-medium flex items-center justify-center gap-1 transition-colors ${
                    activeTab === 'info' 
                      ? 'text-[#00205B] border-b-2 border-[#00205B] bg-white' 
                      : 'text-gray-500 hover:text-gray-700'
                  }`}
                >
                  <User className="h-4 w-4" />
                  <span className="hidden sm:inline">Info</span>
                </button>
                <button
                  onClick={() => setActiveTab('zones')}
                  className={`flex-1 min-w-[80px] px-3 py-3 text-sm font-medium flex items-center justify-center gap-1 transition-colors ${
                    activeTab === 'zones' 
                      ? 'text-[#00205B] border-b-2 border-[#00205B] bg-white' 
                      : 'text-gray-500 hover:text-gray-700'
                  }`}
                >
                  <MapPin className="h-4 w-4" />
                  <span className="hidden sm:inline">Zonas</span> ({assignedZones.length})
                </button>
                <button
                  onClick={() => setActiveTab('equipment')}
                  className={`flex-1 min-w-[80px] px-3 py-3 text-sm font-medium flex items-center justify-center gap-1 transition-colors ${
                    activeTab === 'equipment' 
                      ? 'text-[#00205B] border-b-2 border-[#00205B] bg-white' 
                      : 'text-gray-500 hover:text-gray-700'
                  }`}
                >
                  <Wrench className="h-4 w-4" />
                  <span className="hidden sm:inline">Equipo</span> ({assignedEquipment.length})
                </button>
                <button
                  onClick={() => setActiveTab('schedule')}
                  className={`flex-1 min-w-[80px] px-3 py-3 text-sm font-medium flex items-center justify-center gap-1 transition-colors ${
                    activeTab === 'schedule' 
                      ? 'text-[#00205B] border-b-2 border-[#00205B] bg-white' 
                      : 'text-gray-500 hover:text-gray-700'
                  }`}
                >
                  <Clock className="h-4 w-4" />
                  <span className="hidden sm:inline">Horarios</span> ({schedules.length})
                </button>
                <button
                  onClick={() => setActiveTab('timeoff')}
                  className={`flex-1 min-w-[80px] px-3 py-3 text-sm font-medium flex items-center justify-center gap-1 transition-colors ${
                    activeTab === 'timeoff' 
                      ? 'text-[#00205B] border-b-2 border-[#00205B] bg-white' 
                      : 'text-gray-500 hover:text-gray-700'
                  }`}
                >
                  <CalendarOff className="h-4 w-4" />
                  <span className="hidden sm:inline">Bloqueos</span> ({timeOffs.length})
                </button>
              </div>
            )}

            <div className="overflow-y-auto flex-1 p-6">
              {/* Tab: Info */}
              {activeTab === 'info' && (
                <form onSubmit={handleSubmit} className="space-y-4">
                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-2">Nombre</label>
                      <input
                        type="text"
                        value={formData.firstName}
                        onChange={(e) => setFormData({ ...formData, firstName: e.target.value })}
                        className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
                        required
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-2">Apellido</label>
                      <input
                        type="text"
                        value={formData.lastName}
                        onChange={(e) => setFormData({ ...formData, lastName: e.target.value })}
                        className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
                        required
                      />
                    </div>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">Email</label>
                    <input
                      type="email"
                      value={formData.email}
                      onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                      className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
                    />
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">Teléfono</label>
                    <input
                      type="tel"
                      value={formData.phone}
                      onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
                      className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
                    />
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">Dirección</label>
                    <input
                      type="text"
                      value={formData.address}
                      onChange={(e) => setFormData({ ...formData, address: e.target.value })}
                      className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
                    />
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">Zona Principal</label>
                    <select
                      value={formData.primaryServiceAreaId || ''}
                      onChange={(e) => setFormData({ ...formData, primaryServiceAreaId: e.target.value ? Number(e.target.value) : null })}
                      className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
                    >
                      <option value="">Sin asignar</option>
                      {serviceAreas.map(area => (
                        <option key={area.id} value={area.id}>{area.name}</option>
                      ))}
                    </select>
                  </div>

                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-2">Max horas/día</label>
                      <input
                        type="number"
                        min="1"
                        max="24"
                        value={formData.maxDailyHours}
                        onChange={(e) => setFormData({ ...formData, maxDailyHours: parseInt(e.target.value) || 8 })}
                        className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-2">Max servicios/día</label>
                      <input
                        type="number"
                        min="1"
                        max="20"
                        value={formData.maxDailyServices}
                        onChange={(e) => setFormData({ ...formData, maxDailyServices: parseInt(e.target.value) || 4 })}
                        className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
                      />
                    </div>
                  </div>

                  {editingEmployee && (
                    <label className="flex items-center gap-2 cursor-pointer">
                      <input
                        type="checkbox"
                        checked={formData.isActive}
                        onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                        className="w-4 h-4 text-[#00205B] border-gray-300 rounded focus:ring-[#00205B]"
                      />
                      <span className="text-sm text-gray-700">Empleado activo</span>
                    </label>
                  )}

                  {error && <p className="text-red-600 text-sm">{error}</p>}

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
                      disabled={saving}
                      className="flex-1 px-4 py-2 bg-[#00205B] text-white rounded-lg hover:bg-[#001440] transition-colors disabled:opacity-50"
                    >
                      {saving ? 'Guardando...' : editingEmployee ? 'Guardar Cambios' : 'Crear Empleado'}
                    </button>
                  </div>
                </form>
              )}

              {/* Tab: Zonas de Servicio */}
              {activeTab === 'zones' && (
                <div className="space-y-6">
                  <div>
                    <h3 className="font-medium text-gray-900 mb-3">Zonas asignadas</h3>
                    {assignedZones.length === 0 ? (
                      <p className="text-gray-500 text-center py-4">No hay zonas asignadas</p>
                    ) : (
                      <div className="space-y-2">
                        {assignedZones.map((zone) => (
                          <div key={zone.serviceAreaId} className="flex items-center justify-between p-3 bg-white border rounded-lg">
                            <div className="flex items-center gap-3">
                              <MapPin className={`h-4 w-4 ${zone.isPrimary ? 'text-[#00205B]' : 'text-gray-400'}`} />
                              <span className="font-medium">{zone.serviceArea?.name || `Zona ${zone.serviceAreaId}`}</span>
                              {zone.isPrimary && (
                                <span className="px-2 py-0.5 bg-[#FFE44D]/20 text-[#001440] text-xs rounded-full">Principal</span>
                              )}
                            </div>
                            <div className="flex items-center gap-2">
                              {!zone.isPrimary && (
                                <button
                                  onClick={() => handleSetPrimaryZone(zone.serviceAreaId)}
                                  className="p-1 text-gray-400 hover:text-[#00205B] text-xs"
                                  title="Establecer como principal"
                                >
                                  <Check className="h-4 w-4" />
                                </button>
                              )}
                              <button
                                onClick={() => handleRemoveZone(zone.serviceAreaId)}
                                className="p-1 text-gray-400 hover:text-red-500"
                              >
                                <Trash2 className="h-4 w-4" />
                              </button>
                            </div>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>

                  <div>
                    <h3 className="font-medium text-gray-900 mb-3">Agregar zona</h3>
                    <div className="grid grid-cols-2 sm:grid-cols-3 gap-2">
                      {serviceAreas
                        .filter(area => !assignedZones.some(z => z.serviceAreaId === area.id))
                        .map(area => (
                          <button
                            key={area.id}
                            onClick={() => handleAddZone(area.id, assignedZones.length === 0)}
                            disabled={saving}
                            className="p-3 text-left border rounded-lg hover:border-[#FFE44D] hover:bg-[#FFE44D]/10 transition-colors disabled:opacity-50"
                          >
                            <div className="flex items-center gap-2">
                              <Plus className="h-4 w-4 text-gray-400" />
                              <span className="text-sm font-medium">{area.name}</span>
                            </div>
                          </button>
                        ))}
                    </div>
                    {serviceAreas.filter(area => !assignedZones.some(z => z.serviceAreaId === area.id)).length === 0 && (
                      <p className="text-gray-500 text-center py-4">Todas las zonas ya están asignadas</p>
                    )}
                  </div>
                </div>
              )}

              {/* Tab: Equipamiento */}
              {activeTab === 'equipment' && (
                <div className="space-y-6">
                  <div>
                    <h3 className="font-medium text-gray-900 mb-3">Equipamiento asignado</h3>
                    {assignedEquipment.length === 0 ? (
                      <p className="text-gray-500 text-center py-4">No hay equipamiento asignado</p>
                    ) : (
                      <div className="space-y-2">
                        {assignedEquipment.map((eq) => (
                          <div key={eq.equipmentId} className="flex items-center justify-between p-3 bg-white border rounded-lg">
                            <div className="flex items-center gap-3">
                              <Wrench className="h-4 w-4 text-gray-400" />
                              <span className="font-medium">{eq.equipment?.name || `Equipo ${eq.equipmentId}`}</span>
                            </div>
                            <div className="flex items-center gap-2">
                              {/* Toggle disponibilidad */}
                              <button
                                onClick={() => handleToggleEquipmentAvailability(eq.equipmentId, eq.isAvailable)}
                                className={`px-3 py-1 text-xs rounded-full transition-colors ${
                                  eq.isAvailable 
                                    ? 'bg-green-100 text-green-700 hover:bg-green-200' 
                                    : 'bg-red-100 text-red-700 hover:bg-red-200'
                                }`}
                                title={eq.isAvailable ? 'Click para marcar como no disponible' : 'Click para marcar como disponible'}
                              >
                                {eq.isAvailable ? '✓ Disponible' : '✗ En uso'}
                              </button>
                              <button
                                onClick={() => handleRemoveEquipment(eq.equipmentId)}
                                className="p-1 text-gray-400 hover:text-red-500"
                                title="Quitar equipamiento"
                              >
                                <Trash2 className="h-4 w-4" />
                              </button>
                            </div>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>

                  <div>
                    <h3 className="font-medium text-gray-900 mb-3">Agregar equipamiento</h3>
                    <div className="grid grid-cols-2 sm:grid-cols-3 gap-2">
                      {allEquipment
                        .filter(eq => !assignedEquipment.some(ae => ae.equipmentId === eq.id))
                        .map(eq => (
                          <button
                            key={eq.id}
                            onClick={() => handleAddEquipment(eq.id)}
                            disabled={saving}
                            className="p-3 text-left border rounded-lg hover:border-[#FFE44D] hover:bg-[#FFE44D]/10 transition-colors disabled:opacity-50"
                          >
                            <div className="flex items-center gap-2">
                              <Plus className="h-4 w-4 text-gray-400" />
                              <span className="text-sm font-medium">{eq.name}</span>
                            </div>
                          </button>
                        ))}
                    </div>
                    {allEquipment.filter(eq => !assignedEquipment.some(ae => ae.equipmentId === eq.id)).length === 0 && (
                      <p className="text-gray-500 text-center py-4">
                        {allEquipment.length === 0 ? 'No hay equipamiento disponible en el sistema' : 'Todo el equipamiento ya está asignado'}
                      </p>
                    )}
                  </div>
                </div>
              )}

              {/* Tab: Horarios - Vista Calendario */}
              {activeTab === 'schedule' && (
                <div className="space-y-6">
                  {/* Vista Calendario Semanal */}
                  <div>
                    <h3 className="font-medium text-gray-900 mb-3">Horario Semanal</h3>
                    <div className="bg-white border rounded-lg overflow-hidden">
                      <div className="grid grid-cols-7 bg-gray-50">
                        {DAYS_SHORT.map((day, idx) => {
                          const schedule = schedules.find(s => s.dayOfWeek === idx);
                          return (
                            <div key={idx} className="text-center border-r last:border-r-0">
                              <div className="py-2 border-b font-medium text-sm text-gray-700">{day}</div>
                              <div className="min-h-[80px] p-2">
                                {schedule ? (
                                  <div 
                                    className="bg-[#FFE44D]/20 text-sky-800 rounded p-1.5 text-xs cursor-pointer hover:bg-[#FFE44D]/30"
                                    onClick={() => handleDeleteSchedule(idx)}
                                    title="Click para eliminar"
                                  >
                                    <div className="font-medium">{schedule.startTime.substring(0, 5)}</div>
                                    <div className="text-[#00205B]">a</div>
                                    <div className="font-medium">{schedule.endTime.substring(0, 5)}</div>
                                  </div>
                                ) : (
                                  <button
                                    onClick={() => setScheduleForm({ ...scheduleForm, dayOfWeek: idx })}
                                    className="w-full h-full min-h-[60px] border-2 border-dashed border-gray-200 rounded text-gray-400 hover:border-[#FFE44D] hover:text-[#00205B] flex items-center justify-center"
                                  >
                                    <Plus className="h-4 w-4" />
                                  </button>
                                )}
                              </div>
                            </div>
                          );
                        })}
                      </div>
                    </div>
                  </div>

                  {/* Formulario para agregar */}
                  <form onSubmit={handleAddSchedule} className="bg-gray-50 rounded-lg p-4 space-y-4">
                    <h3 className="font-medium text-gray-900">Agregar horario</h3>
                    <div className="grid grid-cols-2 gap-4">
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-2">Día</label>
                        <select
                          value={scheduleForm.dayOfWeek}
                          onChange={(e) => setScheduleForm({ ...scheduleForm, dayOfWeek: parseInt(e.target.value) })}
                          className="w-full px-4 py-2 border border-gray-300 rounded-lg"
                        >
                          {DAYS_OF_WEEK.map((day, idx) => (
                            <option key={idx} value={idx}>{day}</option>
                          ))}
                        </select>
                      </div>
                      <div className="flex gap-2">
                        <div className="flex-1">
                          <label className="block text-sm font-medium text-gray-700 mb-2">Desde</label>
                          <input
                            type="time"
                            value={scheduleForm.startTime}
                            onChange={(e) => setScheduleForm({ ...scheduleForm, startTime: e.target.value })}
                            className="w-full px-4 py-2 border border-gray-300 rounded-lg"
                          />
                        </div>
                        <div className="flex-1">
                          <label className="block text-sm font-medium text-gray-700 mb-2">Hasta</label>
                          <input
                            type="time"
                            value={scheduleForm.endTime}
                            onChange={(e) => setScheduleForm({ ...scheduleForm, endTime: e.target.value })}
                            className="w-full px-4 py-2 border border-gray-300 rounded-lg"
                          />
                        </div>
                      </div>
                    </div>
                    <div className="grid grid-cols-2 gap-4">
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-2">Buffer entre servicios (min)</label>
                        <select
                          value={scheduleForm.bufferMinutes}
                          onChange={(e) => setScheduleForm({ ...scheduleForm, bufferMinutes: parseInt(e.target.value) })}
                          className="w-full px-4 py-2 border border-gray-300 rounded-lg"
                        >
                          <option value={0}>Sin buffer</option>
                          <option value={15}>15 minutos</option>
                          <option value={30}>30 minutos</option>
                          <option value={45}>45 minutos</option>
                          <option value={60}>1 hora</option>
                        </select>
                      </div>
                      <div className="flex items-end pb-2">
                        <label className="flex items-center gap-2 cursor-pointer">
                          <input
                            type="checkbox"
                            checked={scheduleForm.isAvailable}
                            onChange={(e) => setScheduleForm({ ...scheduleForm, isAvailable: e.target.checked })}
                            className="w-4 h-4 text-[#00205B] border-gray-300 rounded focus:ring-[#00205B]"
                          />
                          <span className="text-sm text-gray-700">Disponible para servicios</span>
                        </label>
                      </div>
                    </div>
                    <button
                      type="submit"
                      disabled={saving}
                      className="px-4 py-2 bg-[#00205B] text-white rounded-lg hover:bg-[#001440] disabled:opacity-50"
                    >
                      {saving ? 'Guardando...' : 'Agregar'}
                    </button>
                  </form>

                  <div>
                    <h3 className="font-medium text-gray-900 mb-3">Horarios configurados</h3>
                    {loadingSchedules ? (
                      <div className="text-center py-4">
                        <div className="w-6 h-6 border-2 border-[#00205B] border-t-transparent rounded-full animate-spin mx-auto" />
                      </div>
                    ) : schedules.length === 0 ? (
                      <p className="text-gray-500 text-center py-4">No hay horarios configurados</p>
                    ) : (
                      <div className="space-y-2">
                        {schedules.map((schedule) => (
                          <div key={schedule.dayOfWeek} className={`flex items-center justify-between p-3 border rounded-lg ${schedule.isAvailable ? 'bg-white' : 'bg-gray-100'}`}>
                            <div className="flex items-center gap-3 flex-wrap">
                              <Calendar className={`h-4 w-4 ${schedule.isAvailable ? 'text-[#00205B]' : 'text-gray-400'}`} />
                              <span className="font-medium">{DAYS_OF_WEEK[schedule.dayOfWeek]}</span>
                              <span className="text-gray-500">
                                {schedule.startTime.substring(0, 5)} - {schedule.endTime.substring(0, 5)}
                              </span>
                              {schedule.bufferMinutes > 0 && (
                                <span className="px-2 py-0.5 bg-blue-100 text-blue-700 text-xs rounded-full">
                                  Buffer: {schedule.bufferMinutes} min
                                </span>
                              )}
                              {!schedule.isAvailable && (
                                <span className="px-2 py-0.5 bg-gray-200 text-gray-600 text-xs rounded-full">
                                  No disponible
                                </span>
                              )}
                            </div>
                            <button
                              onClick={() => handleDeleteSchedule(schedule.dayOfWeek)}
                              className="p-1 text-gray-400 hover:text-red-500"
                            >
                              <Trash2 className="h-4 w-4" />
                            </button>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                </div>
              )}

              {/* Tab: Bloqueos/Time-off */}
              {activeTab === 'timeoff' && (
                <div className="space-y-6">
                  <form onSubmit={handleAddTimeOff} className="bg-gray-50 rounded-lg p-4 space-y-4">
                    <h3 className="font-medium text-gray-900">Agregar bloqueo o permiso</h3>
                    
                    {/* Tipo de bloqueo */}
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-2">Tipo</label>
                      <select
                        value={timeOffForm.type}
                        onChange={(e) => setTimeOffForm({ ...timeOffForm, type: parseInt(e.target.value) })}
                        className="w-full px-4 py-2 border border-gray-300 rounded-lg"
                      >
                        {TIME_OFF_TYPES.map(type => (
                          <option key={type.value} value={type.value}>{type.label}</option>
                        ))}
                      </select>
                    </div>

                    {/* Día completo toggle */}
                    <label className="flex items-center gap-2 cursor-pointer">
                      <input
                        type="checkbox"
                        checked={timeOffForm.isAllDay}
                        onChange={(e) => setTimeOffForm({ ...timeOffForm, isAllDay: e.target.checked })}
                        className="w-4 h-4 text-[#00205B] border-gray-300 rounded focus:ring-[#00205B]"
                      />
                      <span className="text-sm text-gray-700">Día completo</span>
                    </label>

                    <div className="grid grid-cols-2 gap-4">
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-2">
                          {timeOffForm.isAllDay ? 'Fecha inicio' : 'Fecha y hora inicio'}
                        </label>
                        <input
                          type={timeOffForm.isAllDay ? 'date' : 'datetime-local'}
                          value={timeOffForm.startDate}
                          onChange={(e) => setTimeOffForm({ ...timeOffForm, startDate: e.target.value })}
                          className="w-full px-4 py-2 border border-gray-300 rounded-lg"
                          required
                        />
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-2">
                          {timeOffForm.isAllDay ? 'Fecha fin' : 'Fecha y hora fin'}
                        </label>
                        <input
                          type={timeOffForm.isAllDay ? 'date' : 'datetime-local'}
                          value={timeOffForm.endDate}
                          onChange={(e) => setTimeOffForm({ ...timeOffForm, endDate: e.target.value })}
                          className="w-full px-4 py-2 border border-gray-300 rounded-lg"
                          required
                        />
                      </div>
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-2">Motivo (opcional)</label>
                      <input
                        type="text"
                        value={timeOffForm.reason}
                        onChange={(e) => setTimeOffForm({ ...timeOffForm, reason: e.target.value })}
                        placeholder="Vacaciones, cita médica, emergencia familiar..."
                        className="w-full px-4 py-2 border border-gray-300 rounded-lg"
                      />
                    </div>
                    <button
                      type="submit"
                      disabled={saving}
                      className="px-4 py-2 bg-[#00205B] text-white rounded-lg hover:bg-[#001440] disabled:opacity-50"
                    >
                      {saving ? 'Guardando...' : 'Agregar'}
                    </button>
                  </form>

                  <div>
                    <h3 className="font-medium text-gray-900 mb-3">Bloqueos y permisos programados</h3>
                    {timeOffs.length === 0 ? (
                      <p className="text-gray-500 text-center py-4">No hay bloqueos programados</p>
                    ) : (
                      <div className="space-y-2">
                        {timeOffs.map((timeOff) => {
                          const typeInfo = TIME_OFF_TYPES.find(t => t.value === timeOff.type) || TIME_OFF_TYPES[0];
                          const statusInfo = TIME_OFF_STATUS.find(s => s.value === timeOff.status) || TIME_OFF_STATUS[1];
                          return (
                            <div key={timeOff.id} className="flex items-center justify-between p-3 bg-white border rounded-lg">
                              <div className="flex-1">
                                <div className="flex items-center gap-2 flex-wrap">
                                  <CalendarOff className={`h-4 w-4 text-${typeInfo.color}-500`} />
                                  <span className={`px-2 py-0.5 text-xs rounded-full bg-${typeInfo.color}-100 text-${typeInfo.color}-700`}>
                                    {typeInfo.label}
                                  </span>
                                  <span className="font-medium text-sm">
                                    {new Date(timeOff.startDateTime).toLocaleDateString()} 
                                    {!timeOff.isAllDay && ` ${new Date(timeOff.startDateTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`}
                                    {' - '}
                                    {new Date(timeOff.endDateTime).toLocaleDateString()}
                                    {!timeOff.isAllDay && ` ${new Date(timeOff.endDateTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`}
                                  </span>
                                  {timeOff.isAllDay && (
                                    <span className="text-xs text-gray-500">(Todo el día)</span>
                                  )}
                                  <span className={`px-2 py-0.5 text-xs rounded-full ${
                                    statusInfo.value === 1 ? 'bg-green-100 text-green-700' :
                                    statusInfo.value === 0 ? 'bg-yellow-100 text-yellow-700' :
                                    'bg-red-100 text-red-700'
                                  }`}>
                                    {statusInfo.label}
                                  </span>
                                </div>
                                {timeOff.reason && (
                                  <p className="text-sm text-gray-500 ml-6 mt-1">{timeOff.reason}</p>
                                )}
                              </div>
                              <button
                                onClick={() => handleDeleteTimeOff(timeOff.id)}
                                className="p-1 text-gray-400 hover:text-red-500"
                              >
                                <Trash2 className="h-4 w-4" />
                              </button>
                            </div>
                          );
                        })}
                      </div>
                    )}
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </AdminLayout>
  );
}
