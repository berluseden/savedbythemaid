import { useState, useEffect } from 'react';
import {
  Search,
  Calendar,
  Clock,
  MapPin,
  User,
  Phone,
  Mail,
  Eye,
  CheckCircle,
  XCircle,
  AlertCircle,
  ChevronLeft,
  ChevronRight,
  X,
} from 'lucide-react';
import { AdminLayout } from '../../components/admin/AdminLayout';

type BookingStatus = 'pending' | 'confirmed' | 'in-progress' | 'completed' | 'cancelled';

interface Booking {
  id: string;
  customerName: string;
  customerEmail: string;
  customerPhone: string;
  address: string;
  zipCode: string;
  serviceName: string;
  serviceType: string;
  date: string;
  time: string;
  duration: number;
  status: BookingStatus;
  amount: number;
  employeeId?: string;
  employeeName?: string;
  notes?: string;
  rooms: number;
  bathrooms: number;
  squareFeet: number;
  createdAt: string;
}

const statusConfig: Record<BookingStatus, { label: string; color: string; bgColor: string }> = {
  pending: { label: 'Pendiente', color: 'text-yellow-700', bgColor: 'bg-yellow-100' },
  confirmed: { label: 'Confirmada', color: 'text-blue-700', bgColor: 'bg-blue-100' },
  'in-progress': { label: 'En Progreso', color: 'text-purple-700', bgColor: 'bg-purple-100' },
  completed: { label: 'Completada', color: 'text-green-700', bgColor: 'bg-green-100' },
  cancelled: { label: 'Cancelada', color: 'text-red-700', bgColor: 'bg-red-100' },
};

export function AdminBookingsPage() {
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [filterStatus, setFilterStatus] = useState<string>('all');
  const [filterDate, setFilterDate] = useState<string>('');
  const [selectedBooking, setSelectedBooking] = useState<Booking | null>(null);
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 10;

  useEffect(() => {
    fetchBookings();
  }, []);

  const fetchBookings = async () => {
    setIsLoading(true);
    await new Promise(resolve => setTimeout(resolve, 500));
    
    // Simulated data
    const mockBookings: Booking[] = [
      {
        id: 'BK-001',
        customerName: 'María García',
        customerEmail: 'maria.garcia@email.com',
        customerPhone: '(555) 123-4567',
        address: '123 Main St, Apt 4B',
        zipCode: '33101',
        serviceName: 'Limpieza Profunda',
        serviceType: 'deep',
        date: '2026-01-06',
        time: '09:00',
        duration: 240,
        status: 'confirmed',
        amount: 175.00,
        employeeId: '1',
        employeeName: 'María González',
        rooms: 3,
        bathrooms: 2,
        squareFeet: 1500,
        notes: 'Mascota en casa - gato',
        createdAt: '2026-01-03T10:30:00',
      },
      {
        id: 'BK-002',
        customerName: 'Carlos López',
        customerEmail: 'carlos.lopez@email.com',
        customerPhone: '(555) 234-5678',
        address: '456 Oak Ave',
        zipCode: '33102',
        serviceName: 'Limpieza Regular',
        serviceType: 'regular',
        date: '2026-01-06',
        time: '11:00',
        duration: 120,
        status: 'in-progress',
        amount: 95.00,
        employeeId: '2',
        employeeName: 'Carlos Rodríguez',
        rooms: 2,
        bathrooms: 1,
        squareFeet: 900,
        createdAt: '2026-01-04T14:20:00',
      },
      {
        id: 'BK-003',
        customerName: 'Ana Martínez',
        customerEmail: 'ana.martinez@email.com',
        customerPhone: '(555) 345-6789',
        address: '789 Pine Rd',
        zipCode: '33103',
        serviceName: 'Limpieza de Mudanza',
        serviceType: 'move',
        date: '2026-01-06',
        time: '14:00',
        duration: 360,
        status: 'pending',
        amount: 280.00,
        rooms: 4,
        bathrooms: 3,
        squareFeet: 2200,
        notes: 'Casa vacía, acceso con código: 1234',
        createdAt: '2026-01-05T09:15:00',
      },
      {
        id: 'BK-004',
        customerName: 'José Rodríguez',
        customerEmail: 'jose.rodriguez@email.com',
        customerPhone: '(555) 456-7890',
        address: '321 Elm St, Unit 12',
        zipCode: '33104',
        serviceName: 'Limpieza Regular',
        serviceType: 'regular',
        date: '2026-01-05',
        time: '10:00',
        duration: 120,
        status: 'completed',
        amount: 95.00,
        employeeId: '4',
        employeeName: 'José López',
        rooms: 2,
        bathrooms: 1,
        squareFeet: 850,
        createdAt: '2026-01-02T16:45:00',
      },
      {
        id: 'BK-005',
        customerName: 'Laura Sánchez',
        customerEmail: 'laura.sanchez@email.com',
        customerPhone: '(555) 567-8901',
        address: '654 Maple Dr',
        zipCode: '33105',
        serviceName: 'Limpieza Profunda',
        serviceType: 'deep',
        date: '2026-01-05',
        time: '13:00',
        duration: 240,
        status: 'completed',
        amount: 195.00,
        employeeId: '1',
        employeeName: 'María González',
        rooms: 4,
        bathrooms: 2,
        squareFeet: 1800,
        createdAt: '2026-01-01T11:30:00',
      },
      {
        id: 'BK-006',
        customerName: 'Pedro Hernández',
        customerEmail: 'pedro.hernandez@email.com',
        customerPhone: '(555) 678-9012',
        address: '987 Cedar Ln',
        zipCode: '33106',
        serviceName: 'Limpieza Regular',
        serviceType: 'regular',
        date: '2026-01-04',
        time: '09:00',
        duration: 120,
        status: 'cancelled',
        amount: 85.00,
        rooms: 2,
        bathrooms: 1,
        squareFeet: 1000,
        notes: 'Cancelado por el cliente',
        createdAt: '2025-12-30T08:00:00',
      },
      {
        id: 'BK-007',
        customerName: 'Carmen Díaz',
        customerEmail: 'carmen.diaz@email.com',
        customerPhone: '(555) 789-0123',
        address: '147 Birch St',
        zipCode: '33107',
        serviceName: 'Limpieza Profunda',
        serviceType: 'deep',
        date: '2026-01-07',
        time: '10:00',
        duration: 240,
        status: 'confirmed',
        amount: 165.00,
        employeeId: '2',
        employeeName: 'Carlos Rodríguez',
        rooms: 3,
        bathrooms: 2,
        squareFeet: 1400,
        createdAt: '2026-01-05T15:20:00',
      },
      {
        id: 'BK-008',
        customerName: 'Miguel Torres',
        customerEmail: 'miguel.torres@email.com',
        customerPhone: '(555) 890-1234',
        address: '258 Walnut Ave',
        zipCode: '33108',
        serviceName: 'Limpieza de Mudanza',
        serviceType: 'move',
        date: '2026-01-08',
        time: '08:00',
        duration: 480,
        status: 'pending',
        amount: 350.00,
        rooms: 5,
        bathrooms: 3,
        squareFeet: 2800,
        notes: 'Casa grande, necesita equipo de 2 personas',
        createdAt: '2026-01-06T10:00:00',
      },
    ];

    setBookings(mockBookings);
    setIsLoading(false);
  };

  const updateBookingStatus = async (bookingId: string, newStatus: BookingStatus) => {
    setBookings(prev =>
      prev.map(b => (b.id === bookingId ? { ...b, status: newStatus } : b))
    );
    if (selectedBooking?.id === bookingId) {
      setSelectedBooking(prev => prev ? { ...prev, status: newStatus } : null);
    }
  };

  const filteredBookings = bookings.filter(booking => {
    const matchesSearch =
      booking.customerName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      booking.id.toLowerCase().includes(searchTerm.toLowerCase()) ||
      booking.address.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesStatus = filterStatus === 'all' || booking.status === filterStatus;
    const matchesDate = !filterDate || booking.date === filterDate;
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

  const formatDuration = (minutes: number) => {
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    return hours > 0 ? `${hours}h ${mins > 0 ? `${mins}m` : ''}` : `${mins}m`;
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
                  ? 'ring-2 ring-sky-500 border-sky-500'
                  : 'hover:border-gray-300'
              } bg-white`}
            >
              <p className="text-sm text-gray-500">{config.label}</p>
              <p className={`text-2xl font-bold ${config.color}`}>
                {bookings.filter(b => b.status === status).length}
              </p>
            </button>
          ))}
        </div>

        {/* Filters */}
        <div className="flex flex-col sm:flex-row gap-4">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-gray-400" />
            <input
              type="search"
              placeholder="Buscar por nombre, ID o dirección..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sky-500 focus:border-transparent"
            />
          </div>
          <input
            type="date"
            value={filterDate}
            onChange={(e) => setFilterDate(e.target.value)}
            className="px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sky-500 focus:border-transparent"
          />
          <select
            value={filterStatus}
            onChange={(e) => setFilterStatus(e.target.value)}
            className="px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sky-500 focus:border-transparent"
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
                  <th className="px-6 py-3 font-medium">ID</th>
                  <th className="px-6 py-3 font-medium">Cliente</th>
                  <th className="px-6 py-3 font-medium">Servicio</th>
                  <th className="px-6 py-3 font-medium">Fecha/Hora</th>
                  <th className="px-6 py-3 font-medium">Estado</th>
                  <th className="px-6 py-3 font-medium">Empleado</th>
                  <th className="px-6 py-3 font-medium text-right">Monto</th>
                  <th className="px-6 py-3 font-medium text-right">Acciones</th>
                </tr>
              </thead>
              <tbody>
                {paginatedBookings.map((booking) => (
                  <tr key={booking.id} className="border-b last:border-0 hover:bg-gray-50">
                    <td className="px-6 py-4 text-sm font-medium text-gray-900">{booking.id}</td>
                    <td className="px-6 py-4">
                      <p className="text-sm font-medium text-gray-900">{booking.customerName}</p>
                      <p className="text-xs text-gray-500">{booking.address}</p>
                    </td>
                    <td className="px-6 py-4">
                      <p className="text-sm text-gray-900">{booking.serviceName}</p>
                      <p className="text-xs text-gray-500">{formatDuration(booking.duration)}</p>
                    </td>
                    <td className="px-6 py-4">
                      <p className="text-sm text-gray-900">{formatDate(booking.date)}</p>
                      <p className="text-xs text-gray-500">{booking.time}</p>
                    </td>
                    <td className="px-6 py-4">
                      <span
                        className={`px-2 py-1 rounded-full text-xs font-medium ${
                          statusConfig[booking.status].bgColor
                        } ${statusConfig[booking.status].color}`}
                      >
                        {statusConfig[booking.status].label}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-600">
                      {booking.employeeName || (
                        <span className="text-yellow-600">Sin asignar</span>
                      )}
                    </td>
                    <td className="px-6 py-4 text-sm font-medium text-gray-900 text-right">
                      {formatCurrency(booking.amount)}
                    </td>
                    <td className="px-6 py-4 text-right">
                      <div className="flex items-center justify-end gap-1">
                        <button
                          onClick={() => setSelectedBooking(booking)}
                          className="p-2 text-gray-400 hover:text-sky-600 hover:bg-sky-50 rounded-lg"
                          title="Ver detalles"
                        >
                          <Eye className="h-4 w-4" />
                        </button>
                        {booking.status === 'pending' && (
                          <>
                            <button
                              onClick={() => updateBookingStatus(booking.id, 'confirmed')}
                              className="p-2 text-gray-400 hover:text-green-600 hover:bg-green-50 rounded-lg"
                              title="Confirmar"
                            >
                              <CheckCircle className="h-4 w-4" />
                            </button>
                            <button
                              onClick={() => updateBookingStatus(booking.id, 'cancelled')}
                              className="p-2 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded-lg"
                              title="Cancelar"
                            >
                              <XCircle className="h-4 w-4" />
                            </button>
                          </>
                        )}
                        {booking.status === 'confirmed' && (
                          <button
                            onClick={() => updateBookingStatus(booking.id, 'in-progress')}
                            className="p-2 text-gray-400 hover:text-purple-600 hover:bg-purple-50 rounded-lg"
                            title="Iniciar"
                          >
                            <AlertCircle className="h-4 w-4" />
                          </button>
                        )}
                        {booking.status === 'in-progress' && (
                          <button
                            onClick={() => updateBookingStatus(booking.id, 'completed')}
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
                  Reserva {selectedBooking.id}
                </h2>
                <span
                  className={`inline-block mt-1 px-2 py-1 rounded-full text-xs font-medium ${
                    statusConfig[selectedBooking.status].bgColor
                  } ${statusConfig[selectedBooking.status].color}`}
                >
                  {statusConfig[selectedBooking.status].label}
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
                    <span className="text-gray-900">{selectedBooking.customerName}</span>
                  </div>
                  <div className="flex items-center gap-3">
                    <Mail className="h-5 w-5 text-gray-400" />
                    <span className="text-gray-900">{selectedBooking.customerEmail}</span>
                  </div>
                  <div className="flex items-center gap-3">
                    <Phone className="h-5 w-5 text-gray-400" />
                    <span className="text-gray-900">{selectedBooking.customerPhone}</span>
                  </div>
                  <div className="flex items-start gap-3">
                    <MapPin className="h-5 w-5 text-gray-400 mt-0.5" />
                    <div>
                      <p className="text-gray-900">{selectedBooking.address}</p>
                      <p className="text-sm text-gray-500">ZIP: {selectedBooking.zipCode}</p>
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
                    <p className="font-medium text-gray-900">{selectedBooking.serviceName}</p>
                  </div>
                  <div>
                    <p className="text-xs text-gray-500">Duración Estimada</p>
                    <p className="font-medium text-gray-900">{formatDuration(selectedBooking.duration)}</p>
                  </div>
                  <div>
                    <p className="text-xs text-gray-500">Habitaciones</p>
                    <p className="font-medium text-gray-900">{selectedBooking.rooms}</p>
                  </div>
                  <div>
                    <p className="text-xs text-gray-500">Baños</p>
                    <p className="font-medium text-gray-900">{selectedBooking.bathrooms}</p>
                  </div>
                  <div>
                    <p className="text-xs text-gray-500">Área</p>
                    <p className="font-medium text-gray-900">{selectedBooking.squareFeet} sq ft</p>
                  </div>
                  <div>
                    <p className="text-xs text-gray-500">Monto Total</p>
                    <p className="font-medium text-gray-900 text-lg">{formatCurrency(selectedBooking.amount)}</p>
                  </div>
                </div>
              </div>

              {/* Schedule */}
              <div>
                <h3 className="text-sm font-medium text-gray-500 mb-3">Programación</h3>
                <div className="bg-gray-50 rounded-lg p-4 grid grid-cols-2 gap-4">
                  <div className="flex items-center gap-3">
                    <Calendar className="h-5 w-5 text-gray-400" />
                    <div>
                      <p className="text-xs text-gray-500">Fecha</p>
                      <p className="font-medium text-gray-900">{formatDate(selectedBooking.date)}</p>
                    </div>
                  </div>
                  <div className="flex items-center gap-3">
                    <Clock className="h-5 w-5 text-gray-400" />
                    <div>
                      <p className="text-xs text-gray-500">Hora</p>
                      <p className="font-medium text-gray-900">{selectedBooking.time}</p>
                    </div>
                  </div>
                </div>
              </div>

              {/* Employee */}
              <div>
                <h3 className="text-sm font-medium text-gray-500 mb-3">Empleado Asignado</h3>
                <div className="bg-gray-50 rounded-lg p-4">
                  {selectedBooking.employeeName ? (
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 bg-sky-100 rounded-full flex items-center justify-center">
                        <User className="h-5 w-5 text-sky-600" />
                      </div>
                      <span className="font-medium text-gray-900">{selectedBooking.employeeName}</span>
                    </div>
                  ) : (
                    <div className="flex items-center justify-between">
                      <span className="text-yellow-600">Sin asignar</span>
                      <button className="px-3 py-1 text-sm bg-sky-500 text-white rounded-lg hover:bg-sky-600">
                        Asignar Empleado
                      </button>
                    </div>
                  )}
                </div>
              </div>

              {/* Notes */}
              {selectedBooking.notes && (
                <div>
                  <h3 className="text-sm font-medium text-gray-500 mb-3">Notas</h3>
                  <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
                    <p className="text-yellow-800">{selectedBooking.notes}</p>
                  </div>
                </div>
              )}

              {/* Actions */}
              <div className="flex gap-3 pt-4 border-t">
                {selectedBooking.status === 'pending' && (
                  <>
                    <button
                      onClick={() => updateBookingStatus(selectedBooking.id, 'confirmed')}
                      className="flex-1 px-4 py-2 bg-green-500 text-white rounded-lg hover:bg-green-600 transition-colors"
                    >
                      Confirmar Reserva
                    </button>
                    <button
                      onClick={() => updateBookingStatus(selectedBooking.id, 'cancelled')}
                      className="flex-1 px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600 transition-colors"
                    >
                      Cancelar Reserva
                    </button>
                  </>
                )}
                {selectedBooking.status === 'confirmed' && (
                  <button
                    onClick={() => updateBookingStatus(selectedBooking.id, 'in-progress')}
                    className="flex-1 px-4 py-2 bg-purple-500 text-white rounded-lg hover:bg-purple-600 transition-colors"
                  >
                    Iniciar Servicio
                  </button>
                )}
                {selectedBooking.status === 'in-progress' && (
                  <button
                    onClick={() => updateBookingStatus(selectedBooking.id, 'completed')}
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
