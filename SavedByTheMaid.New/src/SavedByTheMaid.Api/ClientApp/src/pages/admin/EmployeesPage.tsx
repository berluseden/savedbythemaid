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
  reason: string;
  status: string;
}

const DAYS_OF_WEEK = ['Domingo', 'Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado'];

type ModalTab = 'info' | 'schedule' | 'timeoff';

export function AdminEmployeesPage() {
  const [employees, setEmployees] = useState<EmployeeDto[]>([]);
  const [serviceAreas, setServiceAreas] = useState<ServiceArea[]>([]);
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
    reason: '',
  });

  useEffect(() => {
    fetchEmployees();
    fetchServiceAreas();
  }, []);

  const fetchServiceAreas = async () => {
    try {
      const response = await api.get<ServiceArea[]>('/serviceareas');
      setServiceAreas(response.data);
    } catch (err) {
      console.error('Error loading service areas', err);
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
    } catch (err) {
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
    } catch (err) {
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
        reason: timeOffForm.reason,
      });
      await fetchTimeOffs(editingEmployee.id);
      setTimeOffForm({ startDate: '', endDate: '', reason: '' });
    } catch (err) {
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
    } catch (err) {
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
    setShowModal(true);
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
            <h1 className="text-2xl font-bold text-gray-900">Empleados</h1>
            <p className="text-gray-600">Gestiona el equipo de limpieza</p>
          </div>
          <button
            onClick={() => setShowModal(true)}
            className="inline-flex items-center gap-2 px-4 py-2 bg-sky-500 text-white rounded-lg hover:bg-sky-600 transition-colors"
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
              className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sky-500 focus:border-transparent"
            />
          </div>
          <select
            value={filterStatus}
            onChange={(e) => setFilterStatus(e.target.value)}
            className="px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sky-500 focus:border-transparent"
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
            <p className="text-2xl font-bold text-sky-600">
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
                        <div className="w-10 h-10 bg-sky-100 rounded-full flex items-center justify-center">
                          <User className="h-5 w-5 text-sky-600" />
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
                          className="p-2 text-gray-400 hover:text-sky-600 hover:bg-sky-50 rounded-lg transition-colors"
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
              <div className="flex border-b bg-gray-50">
                <button
                  onClick={() => setActiveTab('info')}
                  className={`flex-1 px-4 py-3 text-sm font-medium flex items-center justify-center gap-2 transition-colors ${
                    activeTab === 'info' 
                      ? 'text-sky-600 border-b-2 border-sky-500 bg-white' 
                      : 'text-gray-500 hover:text-gray-700'
                  }`}
                >
                  <User className="h-4 w-4" />
                  Información
                </button>
                <button
                  onClick={() => setActiveTab('schedule')}
                  className={`flex-1 px-4 py-3 text-sm font-medium flex items-center justify-center gap-2 transition-colors ${
                    activeTab === 'schedule' 
                      ? 'text-sky-600 border-b-2 border-sky-500 bg-white' 
                      : 'text-gray-500 hover:text-gray-700'
                  }`}
                >
                  <Clock className="h-4 w-4" />
                  Horarios ({schedules.length})
                </button>
                <button
                  onClick={() => setActiveTab('timeoff')}
                  className={`flex-1 px-4 py-3 text-sm font-medium flex items-center justify-center gap-2 transition-colors ${
                    activeTab === 'timeoff' 
                      ? 'text-sky-600 border-b-2 border-sky-500 bg-white' 
                      : 'text-gray-500 hover:text-gray-700'
                  }`}
                >
                  <CalendarOff className="h-4 w-4" />
                  Bloqueos ({timeOffs.length})
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
                        className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sky-500 focus:border-transparent"
                        required
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-2">Apellido</label>
                      <input
                        type="text"
                        value={formData.lastName}
                        onChange={(e) => setFormData({ ...formData, lastName: e.target.value })}
                        className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sky-500 focus:border-transparent"
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
                      className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sky-500 focus:border-transparent"
                    />
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">Teléfono</label>
                    <input
                      type="tel"
                      value={formData.phone}
                      onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
                      className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sky-500 focus:border-transparent"
                    />
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">Dirección</label>
                    <input
                      type="text"
                      value={formData.address}
                      onChange={(e) => setFormData({ ...formData, address: e.target.value })}
                      className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sky-500 focus:border-transparent"
                    />
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">Zona Principal</label>
                    <select
                      value={formData.primaryServiceAreaId || ''}
                      onChange={(e) => setFormData({ ...formData, primaryServiceAreaId: e.target.value ? Number(e.target.value) : null })}
                      className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sky-500 focus:border-transparent"
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
                        className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sky-500 focus:border-transparent"
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
                        className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sky-500 focus:border-transparent"
                      />
                    </div>
                  </div>

                  {editingEmployee && (
                    <label className="flex items-center gap-2 cursor-pointer">
                      <input
                        type="checkbox"
                        checked={formData.isActive}
                        onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                        className="w-4 h-4 text-sky-500 border-gray-300 rounded focus:ring-sky-500"
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
                      className="flex-1 px-4 py-2 bg-sky-500 text-white rounded-lg hover:bg-sky-600 transition-colors disabled:opacity-50"
                    >
                      {saving ? 'Guardando...' : editingEmployee ? 'Guardar Cambios' : 'Crear Empleado'}
                    </button>
                  </div>
                </form>
              )}

              {/* Tab: Horarios */}
              {activeTab === 'schedule' && (
                <div className="space-y-6">
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
                    <button
                      type="submit"
                      disabled={saving}
                      className="px-4 py-2 bg-sky-500 text-white rounded-lg hover:bg-sky-600 disabled:opacity-50"
                    >
                      {saving ? 'Guardando...' : 'Agregar'}
                    </button>
                  </form>

                  <div>
                    <h3 className="font-medium text-gray-900 mb-3">Horarios configurados</h3>
                    {loadingSchedules ? (
                      <div className="text-center py-4">
                        <div className="w-6 h-6 border-2 border-sky-500 border-t-transparent rounded-full animate-spin mx-auto" />
                      </div>
                    ) : schedules.length === 0 ? (
                      <p className="text-gray-500 text-center py-4">No hay horarios configurados</p>
                    ) : (
                      <div className="space-y-2">
                        {schedules.map((schedule) => (
                          <div key={schedule.dayOfWeek} className="flex items-center justify-between p-3 bg-white border rounded-lg">
                            <div className="flex items-center gap-3">
                              <Calendar className="h-4 w-4 text-gray-400" />
                              <span className="font-medium">{DAYS_OF_WEEK[schedule.dayOfWeek]}</span>
                              <span className="text-gray-500">
                                {schedule.startTime.substring(0, 5)} - {schedule.endTime.substring(0, 5)}
                              </span>
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
                    <h3 className="font-medium text-gray-900">Agregar bloqueo</h3>
                    <div className="grid grid-cols-2 gap-4">
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-2">Fecha inicio</label>
                        <input
                          type="datetime-local"
                          value={timeOffForm.startDate}
                          onChange={(e) => setTimeOffForm({ ...timeOffForm, startDate: e.target.value })}
                          className="w-full px-4 py-2 border border-gray-300 rounded-lg"
                          required
                        />
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-2">Fecha fin</label>
                        <input
                          type="datetime-local"
                          value={timeOffForm.endDate}
                          onChange={(e) => setTimeOffForm({ ...timeOffForm, endDate: e.target.value })}
                          className="w-full px-4 py-2 border border-gray-300 rounded-lg"
                          required
                        />
                      </div>
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-2">Motivo</label>
                      <input
                        type="text"
                        value={timeOffForm.reason}
                        onChange={(e) => setTimeOffForm({ ...timeOffForm, reason: e.target.value })}
                        placeholder="Vacaciones, cita médica, etc."
                        className="w-full px-4 py-2 border border-gray-300 rounded-lg"
                      />
                    </div>
                    <button
                      type="submit"
                      disabled={saving}
                      className="px-4 py-2 bg-sky-500 text-white rounded-lg hover:bg-sky-600 disabled:opacity-50"
                    >
                      {saving ? 'Guardando...' : 'Agregar bloqueo'}
                    </button>
                  </form>

                  <div>
                    <h3 className="font-medium text-gray-900 mb-3">Bloqueos programados</h3>
                    {timeOffs.length === 0 ? (
                      <p className="text-gray-500 text-center py-4">No hay bloqueos programados</p>
                    ) : (
                      <div className="space-y-2">
                        {timeOffs.map((timeOff) => (
                          <div key={timeOff.id} className="flex items-center justify-between p-3 bg-white border rounded-lg">
                            <div>
                              <div className="flex items-center gap-2">
                                <CalendarOff className="h-4 w-4 text-orange-500" />
                                <span className="font-medium">
                                  {new Date(timeOff.startDateTime).toLocaleDateString()} - {new Date(timeOff.endDateTime).toLocaleDateString()}
                                </span>
                              </div>
                              {timeOff.reason && (
                                <p className="text-sm text-gray-500 ml-6">{timeOff.reason}</p>
                              )}
                            </div>
                            <button
                              onClick={() => handleDeleteTimeOff(timeOff.id)}
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
            </div>
          </div>
        </div>
      )}
    </AdminLayout>
  );
}
