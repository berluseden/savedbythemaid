import { useState, useEffect } from 'react';
import {
  Search,
  Calendar,
  Clock,
  MapPin,
  User,
  Phone,
  Eye,
  CheckCircle,
  XCircle,
  AlertCircle,
  ChevronLeft,
  ChevronRight,
  X,
  Users,
  PlayCircle,
} from 'lucide-react';
import { AdminLayout } from '../../components/admin/AdminLayout';
import api from '../../lib/api';

type OrderStatus = 'PendingReview' | 'Draft' | 'Confirmed' | 'InProgress' | 'Completed' | 'Cancelled' | 'NoShow';
type PaymentStatus = 'Pending' | 'Authorized' | 'Paid' | 'Refunded' | 'Failed';
type MeetStatus = 'Scheduled' | 'Assigned' | 'OnTheWay' | 'InProgress' | 'Completed' | 'Cancelled' | 'Rescheduled' | 'NoShow';

interface OrderSummary {
  id: number;
  confirmationNumber: string;
  contactName: string | null;
  contactPhone: string | null;
  address: string;
  city: string | null;
  zipCode: string;
  serviceAreaName: string | null;
  serviceTypeName: string | null;
  total: number;
  paymentStatus: PaymentStatus;
  orderStatus: OrderStatus;
  recurrenceType: string;
  createdAt: string;
}

interface MeetingSummary {
  id: number;
  orderId: number;
  scheduledStart: string;
  scheduledEnd: string;
  actualStart: string | null;
  actualEnd: string | null;
  employeeId: number | null;
  employeeName: string | null;
  status: MeetStatus;
  estimatedDurationMinutes: number;
}

interface Employee {
  id: number;
  firstName: string;
  lastName: string;
  isActive: boolean;
}

const statusConfig: Record<OrderStatus, { label: string; color: string; bgColor: string }> = {
  PendingReview: { label: 'Por Aprobar', color: 'text-orange-700', bgColor: 'bg-orange-100' },
  Draft: { label: 'Borrador', color: 'text-gray-700', bgColor: 'bg-gray-100' },
  Confirmed: { label: 'Confirmada', color: 'text-blue-700', bgColor: 'bg-blue-100' },
  InProgress: { label: 'En Progreso', color: 'text-purple-700', bgColor: 'bg-purple-100' },
  Completed: { label: 'Completada', color: 'text-green-700', bgColor: 'bg-green-100' },
  Cancelled: { label: 'Cancelada', color: 'text-red-700', bgColor: 'bg-red-100' },
  NoShow: { label: 'No Presentó', color: 'text-red-700', bgColor: 'bg-red-200' },
};

const meetStatusConfig: Record<MeetStatus, { label: string; color: string; bgColor: string }> = {
  Scheduled: { label: 'Programada', color: 'text-gray-700', bgColor: 'bg-gray-100' },
  Assigned: { label: 'Asignada', color: 'text-blue-700', bgColor: 'bg-blue-100' },
  OnTheWay: { label: 'En Camino', color: 'text-cyan-700', bgColor: 'bg-cyan-100' },
  InProgress: { label: 'En Curso', color: 'text-purple-700', bgColor: 'bg-purple-100' },
  Completed: { label: 'Completada', color: 'text-green-700', bgColor: 'bg-green-100' },
  Cancelled: { label: 'Cancelada', color: 'text-red-700', bgColor: 'bg-red-100' },
  Rescheduled: { label: 'Reprogramada', color: 'text-orange-700', bgColor: 'bg-orange-100' },
  NoShow: { label: 'No Presentó', color: 'text-red-700', bgColor: 'bg-red-100' },
};

export function AdminBookingsPage() {
  const [bookings, setBookings] = useState<OrderSummary[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [filterStatus, setFilterStatus] = useState<string>('all');
  const [filterDate, setFilterDate] = useState<string>('');
  const [selectedBooking, setSelectedBooking] = useState<OrderSummary | null>(null);
  const [currentPage, setCurrentPage] = useState(1);
  const [error, setError] = useState('');
  const [meetings, setMeetings] = useState<MeetingSummary[]>([]);
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [assigningMeetingId, setAssigningMeetingId] = useState<number | null>(null);
  const itemsPerPage = 10;

  useEffect(() => {
    fetchBookings();
    fetchEmployees();
  }, []);

  useEffect(() => {
    if (selectedBooking) {
      fetchMeetings(selectedBooking.id);
    }
  }, [selectedBooking]);

  const fetchEmployees = async () => {
    try {
      const response = await api.get<Employee[]>('/admin/employees');
      setEmployees(response.data.filter(e => e.isActive));
    } catch (err) {
      console.error('Error loading employees:', err);
    }
  };

  const fetchMeetings = async (orderId: number) => {
    try {
      const response = await api.get<MeetingSummary[]>(`/admin/orders/meetings?orderId=${orderId}`);
      setMeetings(response.data);
    } catch (err) {
      console.error('Error loading meetings:', err);
      setMeetings([]);
    }
  };

  const fetchBookings = async () => {
    setIsLoading(true);
    try {
      const params: Record<string, string> = { pageSize: '100' };
      const response = await api.get<OrderSummary[]>('/admin/orders', { params });
      setBookings(response.data);
    } catch (err) {
      setError('Error al cargar las reservas');
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  const updateBookingStatus = async (bookingId: number, newStatus: OrderStatus) => {
    try {
      await api.put(`/admin/orders/${bookingId}/status`, { orderStatus: newStatus });
      await fetchBookings();
      if (selectedBooking?.id === bookingId) {
        setSelectedBooking(prev => prev ? { ...prev, orderStatus: newStatus } : null);
      }
    } catch (err) {
      setError('Error al actualizar el estado');
      console.error(err);
    }
  };

  const cancelOrder = async (bookingId: number) => {
    try {
      await api.post(`/admin/orders/${bookingId}/cancel`, { reason: 'Cancelado por administrador' });
      await fetchBookings();
      setSelectedBooking(null);
    } catch (err) {
      setError('Error al cancelar la orden');
      console.error(err);
    }
  };

  const assignEmployee = async (meetingId: number, employeeId: number) => {
    try {
      await api.put(`/admin/orders/meetings/${meetingId}/assign`, { employeeId });
      if (selectedBooking) {
        await fetchMeetings(selectedBooking.id);
      }
      setAssigningMeetingId(null);
    } catch (err) {
      setError('Error al asignar empleado');
      console.error(err);
    }
  };

  const updateMeetingStatus = async (meetingId: number, status: MeetStatus) => {
    try {
      await api.put(`/admin/orders/meetings/${meetingId}/status`, { status });
      if (selectedBooking) {
        await fetchMeetings(selectedBooking.id);
      }
    } catch (err) {
      setError('Error al actualizar estado de cita');
      console.error(err);
    }
  };

  const filteredBookings = bookings.filter(booking => {
    const matchesSearch =
      (booking.contactName?.toLowerCase().includes(searchTerm.toLowerCase()) ?? false) ||
      booking.confirmationNumber.toLowerCase().includes(searchTerm.toLowerCase()) ||
      booking.address.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesStatus = filterStatus === 'all' || booking.orderStatus === filterStatus;
    const matchesDate = !filterDate || booking.createdAt.startsWith(filterDate);
    return matchesSearch && matchesStatus && matchesDate;
  });

  const totalPages = Math.ceil(filteredBookings.length / itemsPerPage);
  const paginatedBookings = filteredBookings.slice(
    (currentPage - 1) * itemsPerPage,
    currentPage * itemsPerPage
  );

  const formatCurrency = (amount: number) =>
    new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(amount);

  const formatDate = (date: string) =>
    new Date(date).toLocaleDateString('es-ES', {
      weekday: 'short',
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });

  const formatDateTime = (dateStr: string) => {
    const date = new Date(dateStr);
    return date.toLocaleString('es-ES', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
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
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Reservas</h1>
          <p className="text-gray-600">Gestiona todas las reservas de limpieza</p>
        </div>

        {/* Stats */}
        <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
          {Object.entries(statusConfig).map(([status, config]) => (
            <button
              key={status}
              onClick={() => setFilterStatus(filterStatus === status ? 'all' : status)}
              className={`p-4 rounded-lg border transition-all ${
                filterStatus === status
                  ? 'ring-2 ring-[#00205B] border-[#00205B]'
                  : 'hover:border-gray-300'
              } bg-white`}
            >
              <p className="text-sm text-gray-500">{config.label}</p>
              <p className={`text-2xl font-bold ${config.color}`}>
                {bookings.filter(b => b.orderStatus === status).length}
              </p>
            </button>
          ))}
        </div>

        {error && (
          <div className="bg-red-50 text-red-600 px-4 py-3 rounded-lg">
            {error}
            <button onClick={() => setError('')} className="ml-2 underline">Cerrar</button>
          </div>
        )}

        {/* Filters */}
        <div className="flex flex-col sm:flex-row gap-4">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-gray-400" />
            <input
              type="search"
              placeholder="Buscar por nombre, ID o dirección..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
            />
          </div>
          <input
            type="date"
            value={filterDate}
            onChange={(e) => setFilterDate(e.target.value)}
            className="px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
          />
          <select
            value={filterStatus}
            onChange={(e) => setFilterStatus(e.target.value)}
            className="px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
          >
            <option value="all">Todos los estados</option>
            {Object.entries(statusConfig).map(([status, config]) => (
              <option key={status} value={status}>
                {config.label}
              </option>
            ))}
          </select>
        </div>

        {/* Table */}
        <div className="bg-white rounded-xl shadow-sm border overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="text-left text-sm text-gray-500 bg-gray-50 border-b">
                  <th className="px-6 py-3 font-medium">Confirmación</th>
                  <th className="px-6 py-3 font-medium">Cliente</th>
                  <th className="px-6 py-3 font-medium">Servicio</th>
                  <th className="px-6 py-3 font-medium">Fecha</th>
                  <th className="px-6 py-3 font-medium">Estado</th>
                  <th className="px-6 py-3 font-medium">Teléfono</th>
                  <th className="px-6 py-3 font-medium text-right">Total</th>
                  <th className="px-6 py-3 font-medium text-right">Acciones</th>
                </tr>
              </thead>
              <tbody>
                {paginatedBookings.map((booking) => (
                  <tr key={booking.id} className="border-b last:border-0 hover:bg-gray-50">
                    <td className="px-6 py-4 text-sm font-medium text-gray-900">{booking.confirmationNumber}</td>
                    <td className="px-6 py-4">
                      <p className="text-sm font-medium text-gray-900">{booking.contactName || '-'}</p>
                      <p className="text-xs text-gray-500">{booking.address}</p>
                    </td>
                    <td className="px-6 py-4">
                      <p className="text-sm text-gray-900">{booking.serviceTypeName || '-'}</p>
                      <p className="text-xs text-gray-500">{booking.serviceAreaName}</p>
                    </td>
                    <td className="px-6 py-4">
                      <p className="text-sm text-gray-900">{formatDate(booking.createdAt)}</p>
                      <p className="text-xs text-gray-500">{booking.recurrenceType}</p>
                    </td>
                    <td className="px-6 py-4">
                      <span
                        className={`px-2 py-1 rounded-full text-xs font-medium ${
                          statusConfig[booking.orderStatus]?.bgColor || 'bg-gray-100'
                        } ${statusConfig[booking.orderStatus]?.color || 'text-gray-700'}`}
                      >
                        {statusConfig[booking.orderStatus]?.label || booking.orderStatus}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-600">
                      {booking.contactPhone || '-'}
                    </td>
                    <td className="px-6 py-4 text-sm font-medium text-gray-900 text-right">
                      {formatCurrency(booking.total)}
                    </td>
                    <td className="px-6 py-4 text-right">
                      <div className="flex items-center justify-end gap-1">
                        <button
                          onClick={() => setSelectedBooking(booking)}
                          className="p-2 text-gray-400 hover:text-[#00205B] hover:bg-[#FFE44D]/10 rounded-lg"
                          title="Ver detalles"
                        >
                          <Eye className="h-4 w-4" />
                        </button>
                        {(booking.orderStatus === 'PendingReview' || booking.orderStatus === 'Draft') && (
                          <>
                            <button
                              onClick={() => updateBookingStatus(booking.id, 'Confirmed')}
                              className="p-2 text-gray-400 hover:text-green-600 hover:bg-green-50 rounded-lg"
                              title="Aprobar y Confirmar"
                            >
                              <CheckCircle className="h-4 w-4" />
                            </button>
                            <button
                              onClick={() => cancelOrder(booking.id)}
                              className="p-2 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded-lg"
                              title="Rechazar"
                            >
                              <XCircle className="h-4 w-4" />
                            </button>
                          </>
                        )}
                        {booking.orderStatus === 'Confirmed' && (
                          <button
                            onClick={() => updateBookingStatus(booking.id, 'InProgress')}
                            className="p-2 text-gray-400 hover:text-purple-600 hover:bg-purple-50 rounded-lg"
                            title="Iniciar"
                          >
                            <AlertCircle className="h-4 w-4" />
                          </button>
                        )}
                        {booking.orderStatus === 'InProgress' && (
                          <button
                            onClick={() => updateBookingStatus(booking.id, 'Completed')}
                            className="p-2 text-gray-400 hover:text-green-600 hover:bg-green-50 rounded-lg"
                            title="Completar"
                          >
                            <CheckCircle className="h-4 w-4" />
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between px-6 py-4 border-t">
              <p className="text-sm text-gray-500">
                Mostrando {(currentPage - 1) * itemsPerPage + 1} -{' '}
                {Math.min(currentPage * itemsPerPage, filteredBookings.length)} de{' '}
                {filteredBookings.length}
              </p>
              <div className="flex gap-2">
                <button
                  onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
                  disabled={currentPage === 1}
                  className="p-2 border rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
                >
                  <ChevronLeft className="h-5 w-5" />
                </button>
                <button
                  onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))}
                  disabled={currentPage === totalPages}
                  className="p-2 border rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
                >
                  <ChevronRight className="h-5 w-5" />
                </button>
              </div>
            </div>
          )}
        </div>

        {filteredBookings.length === 0 && (
          <div className="text-center py-12">
            <p className="text-gray-500">No se encontraron reservas</p>
          </div>
        )}
      </div>

      {/* Detail Modal */}
      {selectedBooking && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-2xl max-h-[90vh] overflow-y-auto">
            <div className="flex items-center justify-between p-6 border-b">
              <div>
                <h2 className="text-xl font-semibold text-gray-900">
                  {selectedBooking.confirmationNumber}
                </h2>
                <span
                  className={`inline-block mt-1 px-2 py-1 rounded-full text-xs font-medium ${
                    statusConfig[selectedBooking.orderStatus]?.bgColor || 'bg-gray-100'
                  } ${statusConfig[selectedBooking.orderStatus]?.color || 'text-gray-700'}`}
                >
                  {statusConfig[selectedBooking.orderStatus]?.label || selectedBooking.orderStatus}
                </span>
              </div>
              <button
                onClick={() => setSelectedBooking(null)}
                className="p-2 text-gray-400 hover:text-gray-600"
              >
                <X className="h-5 w-5" />
              </button>
            </div>

            <div className="p-6 space-y-6">
              {/* Customer Info */}
              <div>
                <h3 className="text-sm font-medium text-gray-500 mb-3">Información del Cliente</h3>
                <div className="bg-gray-50 rounded-lg p-4 space-y-3">
                  <div className="flex items-center gap-3">
                    <User className="h-5 w-5 text-gray-400" />
                    <span className="text-gray-900">{selectedBooking.contactName || 'Sin nombre'}</span>
                  </div>
                  <div className="flex items-center gap-3">
                    <Phone className="h-5 w-5 text-gray-400" />
                    <span className="text-gray-900">{selectedBooking.contactPhone || 'Sin teléfono'}</span>
                  </div>
                  <div className="flex items-start gap-3">
                    <MapPin className="h-5 w-5 text-gray-400 mt-0.5" />
                    <div>
                      <p className="text-gray-900">{selectedBooking.address}</p>
                      <p className="text-sm text-gray-500">{selectedBooking.city} - ZIP: {selectedBooking.zipCode}</p>
                    </div>
                  </div>
                </div>
              </div>

              {/* Service Info */}
              <div>
                <h3 className="text-sm font-medium text-gray-500 mb-3">Detalles del Servicio</h3>
                <div className="bg-gray-50 rounded-lg p-4 grid grid-cols-2 gap-4">
                  <div>
                    <p className="text-xs text-gray-500">Tipo de Servicio</p>
                    <p className="font-medium text-gray-900">{selectedBooking.serviceTypeName || '-'}</p>
                  </div>
                  <div>
                    <p className="text-xs text-gray-500">Zona de Servicio</p>
                    <p className="font-medium text-gray-900">{selectedBooking.serviceAreaName || '-'}</p>
                  </div>
                  <div>
                    <p className="text-xs text-gray-500">Recurrencia</p>
                    <p className="font-medium text-gray-900">{selectedBooking.recurrenceType}</p>
                  </div>
                  <div>
                    <p className="text-xs text-gray-500">Total</p>
                    <p className="font-medium text-gray-900 text-lg">{formatCurrency(selectedBooking.total)}</p>
                  </div>
                </div>
              </div>

              {/* Schedule */}
              <div>
                <h3 className="text-sm font-medium text-gray-500 mb-3">Información de la Orden</h3>
                <div className="bg-gray-50 rounded-lg p-4 grid grid-cols-2 gap-4">
                  <div className="flex items-center gap-3">
                    <Calendar className="h-5 w-5 text-gray-400" />
                    <div>
                      <p className="text-xs text-gray-500">Creada</p>
                      <p className="font-medium text-gray-900">{formatDate(selectedBooking.createdAt)}</p>
                    </div>
                  </div>
                  <div className="flex items-center gap-3">
                    <Clock className="h-5 w-5 text-gray-400" />
                    <div>
                      <p className="text-xs text-gray-500">Estado de Pago</p>
                      <p className="font-medium text-gray-900">{selectedBooking.paymentStatus}</p>
                    </div>
                  </div>
                </div>
              </div>

              {/* Meetings Section */}
              <div>
                <h3 className="text-sm font-medium text-gray-500 mb-3 flex items-center gap-2">
                  <Users className="h-4 w-4" />
                  Citas Programadas
                </h3>
                {meetings.length === 0 ? (
                  <div className="bg-gray-50 rounded-lg p-4 text-center text-gray-500 text-sm">
                    No hay citas programadas para esta orden
                  </div>
                ) : (
                  <div className="space-y-3">
                    {meetings.map((meeting) => (
                      <div key={meeting.id} className="bg-gray-50 rounded-lg p-4 space-y-3">
                        <div className="flex items-start justify-between">
                          <div className="flex-1">
                            <div className="flex items-center gap-2 mb-2">
                              <span
                                className={`inline-block px-2 py-1 rounded-full text-xs font-medium ${
                                  meetStatusConfig[meeting.status]?.bgColor || 'bg-gray-100'
                                } ${meetStatusConfig[meeting.status]?.color || 'text-gray-700'}`}
                              >
                                {meetStatusConfig[meeting.status]?.label || meeting.status}
                              </span>
                            </div>
                            <div className="grid grid-cols-2 gap-3 text-sm">
                              <div>
                                <p className="text-xs text-gray-500">Inicio Programado</p>
                                <p className="font-medium text-gray-900">{formatDateTime(meeting.scheduledStart)}</p>
                              </div>
                              <div>
                                <p className="text-xs text-gray-500">Fin Programado</p>
                                <p className="font-medium text-gray-900">{formatDateTime(meeting.scheduledEnd)}</p>
                              </div>
                              {meeting.actualStart && (
                                <div>
                                  <p className="text-xs text-gray-500">Inicio Real</p>
                                  <p className="font-medium text-green-700">{formatDateTime(meeting.actualStart)}</p>
                                </div>
                              )}
                              {meeting.actualEnd && (
                                <div>
                                  <p className="text-xs text-gray-500">Fin Real</p>
                                  <p className="font-medium text-green-700">{formatDateTime(meeting.actualEnd)}</p>
                                </div>
                              )}
                            </div>
                          </div>
                        </div>

                        {/* Employee Assignment */}
                        <div className="pt-3 border-t border-gray-200">
                          <p className="text-xs text-gray-500 mb-2">Empleado Asignado</p>
                          {assigningMeetingId === meeting.id ? (
                            <div className="flex gap-2">
                              <select
                                onChange={(e) => assignEmployee(meeting.id, parseInt(e.target.value))}
                                className="flex-1 px-3 py-2 border rounded-lg text-sm focus:ring-2 focus:ring-[#00205B]"
                                defaultValue=""
                              >
                                <option value="" disabled>Seleccionar empleado...</option>
                                {employees.map((emp) => (
                                  <option key={emp.id} value={emp.id}>
                                    {emp.firstName} {emp.lastName}
                                  </option>
                                ))}
                              </select>
                              <button
                                onClick={() => setAssigningMeetingId(null)}
                                className="px-3 py-2 border rounded-lg hover:bg-gray-100"
                              >
                                Cancelar
                              </button>
                            </div>
                          ) : (
                            <div className="flex items-center justify-between">
                              <div className="flex items-center gap-2">
                                <User className="h-4 w-4 text-gray-400" />
                                <span className="font-medium text-gray-900">
                                  {meeting.employeeName || 'Sin asignar'}
                                </span>
                              </div>
                              <button
                                onClick={() => setAssigningMeetingId(meeting.id)}
                                className="text-sm text-[#00205B] hover:text-[#001440] font-medium"
                              >
                                {meeting.employeeId ? 'Cambiar' : 'Asignar'}
                              </button>
                            </div>
                          )}
                        </div>

                        {/* Meeting Actions */}
                        {meeting.status !== 'Completed' && meeting.status !== 'Cancelled' && (
                          <div className="pt-3 border-t border-gray-200 flex gap-2">
                            {meeting.status === 'Assigned' && (
                              <button
                                onClick={() => updateMeetingStatus(meeting.id, 'InProgress')}
                                className="flex-1 px-3 py-1.5 bg-purple-500 text-white rounded text-sm hover:bg-purple-600 flex items-center justify-center gap-1"
                              >
                                <PlayCircle className="h-4 w-4" />
                                Iniciar
                              </button>
                            )}
                            {meeting.status === 'InProgress' && (
                              <button
                                onClick={() => updateMeetingStatus(meeting.id, 'Completed')}
                                className="flex-1 px-3 py-1.5 bg-green-500 text-white rounded text-sm hover:bg-green-600 flex items-center justify-center gap-1"
                              >
                                <CheckCircle className="h-4 w-4" />
                                Completar
                              </button>
                            )}
                            {(meeting.status === 'Scheduled' || meeting.status === 'Assigned') && (
                              <button
                                onClick={() => updateMeetingStatus(meeting.id, 'Cancelled')}
                                className="px-3 py-1.5 bg-red-500 text-white rounded text-sm hover:bg-red-600 flex items-center justify-center gap-1"
                              >
                                <XCircle className="h-4 w-4" />
                                Cancelar
                              </button>
                            )}
                          </div>
                        )}
                      </div>
                    ))}
                  </div>
                )}
              </div>

              {/* Actions */}
              <div className="flex gap-3 pt-4 border-t">
                {selectedBooking.orderStatus === 'PendingReview' && (
                  <>
                    <button
                      onClick={() => updateBookingStatus(selectedBooking.id, 'Confirmed')}
                      className="flex-1 px-4 py-2 bg-green-500 text-white rounded-lg hover:bg-green-600 transition-colors"
                    >
                      Confirmar Reserva
                    </button>
                    <button
                      onClick={() => cancelOrder(selectedBooking.id)}
                      className="flex-1 px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600 transition-colors"
                    >
                      Cancelar Reserva
                    </button>
                  </>
                )}
                {selectedBooking.orderStatus === 'Confirmed' && (
                  <button
                    onClick={() => updateBookingStatus(selectedBooking.id, 'InProgress')}
                    className="flex-1 px-4 py-2 bg-purple-500 text-white rounded-lg hover:bg-purple-600 transition-colors"
                  >
                    Iniciar Servicio
                  </button>
                )}
                {selectedBooking.orderStatus === 'InProgress' && (
                  <button
                    onClick={() => updateBookingStatus(selectedBooking.id, 'Completed')}
                    className="flex-1 px-4 py-2 bg-green-500 text-white rounded-lg hover:bg-green-600 transition-colors"
                  >
                    Marcar como Completado
                  </button>
                )}
                <button
                  onClick={() => setSelectedBooking(null)}
                  className="px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 transition-colors"
                >
                  Cerrar
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </AdminLayout>
  );
}
