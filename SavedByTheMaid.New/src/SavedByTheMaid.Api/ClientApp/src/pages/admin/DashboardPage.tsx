import { useState, useEffect } from 'react';
import {
  Calendar,
  Users,
  DollarSign,
  TrendingUp,
  Clock,
  CheckCircle,
  AlertCircle,
  ArrowUpRight,
  ArrowDownRight,
} from 'lucide-react';
import { AdminLayout } from '../../components/admin/AdminLayout';

interface DashboardStats {
  totalBookings: number;
  pendingBookings: number;
  completedBookings: number;
  totalRevenue: number;
  totalEmployees: number;
  activeEmployees: number;
  bookingsThisWeek: number;
  revenueThisWeek: number;
  bookingsGrowth: number;
  revenueGrowth: number;
}

interface RecentBooking {
  id: string;
  customerName: string;
  serviceName: string;
  date: string;
  time: string;
  status: 'pending' | 'confirmed' | 'in-progress' | 'completed' | 'cancelled';
  amount: number;
}

export function AdminDashboardPage() {
  const [stats, setStats] = useState<DashboardStats>({
    totalBookings: 0,
    pendingBookings: 0,
    completedBookings: 0,
    totalRevenue: 0,
    totalEmployees: 0,
    activeEmployees: 0,
    bookingsThisWeek: 0,
    revenueThisWeek: 0,
    bookingsGrowth: 0,
    revenueGrowth: 0,
  });

  const [recentBookings, setRecentBookings] = useState<RecentBooking[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    // Simulated data - replace with actual API call
    const fetchDashboardData = async () => {
      setIsLoading(true);
      // Simulate API delay
      await new Promise(resolve => setTimeout(resolve, 500));
      
      setStats({
        totalBookings: 1247,
        pendingBookings: 23,
        completedBookings: 1189,
        totalRevenue: 89450.00,
        totalEmployees: 15,
        activeEmployees: 12,
        bookingsThisWeek: 47,
        revenueThisWeek: 3520.00,
        bookingsGrowth: 12.5,
        revenueGrowth: 8.3,
      });

      setRecentBookings([
        {
          id: 'BK-001',
          customerName: 'María García',
          serviceName: 'Limpieza Profunda',
          date: '2026-01-06',
          time: '09:00',
          status: 'confirmed',
          amount: 150.00,
        },
        {
          id: 'BK-002',
          customerName: 'Carlos López',
          serviceName: 'Limpieza Regular',
          date: '2026-01-06',
          time: '11:00',
          status: 'in-progress',
          amount: 85.00,
        },
        {
          id: 'BK-003',
          customerName: 'Ana Martínez',
          serviceName: 'Mudanza',
          date: '2026-01-06',
          time: '14:00',
          status: 'pending',
          amount: 250.00,
        },
        {
          id: 'BK-004',
          customerName: 'José Rodríguez',
          serviceName: 'Limpieza Regular',
          date: '2026-01-05',
          time: '10:00',
          status: 'completed',
          amount: 95.00,
        },
        {
          id: 'BK-005',
          customerName: 'Laura Sánchez',
          serviceName: 'Limpieza Profunda',
          date: '2026-01-05',
          time: '13:00',
          status: 'completed',
          amount: 175.00,
        },
      ]);

      setIsLoading(false);
    };

    fetchDashboardData();
  }, []);

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(amount);
  };

  const getStatusBadge = (status: RecentBooking['status']) => {
    const styles = {
      pending: 'bg-yellow-100 text-yellow-700',
      confirmed: 'bg-blue-100 text-blue-700',
      'in-progress': 'bg-purple-100 text-purple-700',
      completed: 'bg-green-100 text-green-700',
      cancelled: 'bg-red-100 text-red-700',
    };
    const labels = {
      pending: 'Pendiente',
      confirmed: 'Confirmada',
      'in-progress': 'En Progreso',
      completed: 'Completada',
      cancelled: 'Cancelada',
    };
    return (
      <span className={`px-2 py-1 rounded-full text-xs font-medium ${styles[status]}`}>
        {labels[status]}
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
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>
          <p className="text-gray-600">Resumen de tu negocio</p>
        </div>

        {/* Stats Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          {/* Total Bookings */}
          <div className="bg-white rounded-xl p-6 shadow-sm border">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-gray-500">Reservas Totales</p>
                <p className="text-2xl font-bold text-gray-900">{stats.totalBookings}</p>
              </div>
              <div className="w-12 h-12 bg-sky-100 rounded-lg flex items-center justify-center">
                <Calendar className="h-6 w-6 text-sky-600" />
              </div>
            </div>
            <div className="mt-4 flex items-center gap-2">
              <span className={`flex items-center text-sm ${stats.bookingsGrowth >= 0 ? 'text-green-600' : 'text-red-600'}`}>
                {stats.bookingsGrowth >= 0 ? (
                  <ArrowUpRight className="h-4 w-4" />
                ) : (
                  <ArrowDownRight className="h-4 w-4" />
                )}
                {Math.abs(stats.bookingsGrowth)}%
              </span>
              <span className="text-sm text-gray-500">vs semana pasada</span>
            </div>
          </div>

          {/* Revenue */}
          <div className="bg-white rounded-xl p-6 shadow-sm border">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-gray-500">Ingresos Totales</p>
                <p className="text-2xl font-bold text-gray-900">{formatCurrency(stats.totalRevenue)}</p>
              </div>
              <div className="w-12 h-12 bg-green-100 rounded-lg flex items-center justify-center">
                <DollarSign className="h-6 w-6 text-green-600" />
              </div>
            </div>
            <div className="mt-4 flex items-center gap-2">
              <span className={`flex items-center text-sm ${stats.revenueGrowth >= 0 ? 'text-green-600' : 'text-red-600'}`}>
                {stats.revenueGrowth >= 0 ? (
                  <ArrowUpRight className="h-4 w-4" />
                ) : (
                  <ArrowDownRight className="h-4 w-4" />
                )}
                {Math.abs(stats.revenueGrowth)}%
              </span>
              <span className="text-sm text-gray-500">vs semana pasada</span>
            </div>
          </div>

          {/* Employees */}
          <div className="bg-white rounded-xl p-6 shadow-sm border">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-gray-500">Empleados Activos</p>
                <p className="text-2xl font-bold text-gray-900">{stats.activeEmployees}/{stats.totalEmployees}</p>
              </div>
              <div className="w-12 h-12 bg-purple-100 rounded-lg flex items-center justify-center">
                <Users className="h-6 w-6 text-purple-600" />
              </div>
            </div>
            <div className="mt-4">
              <div className="w-full bg-gray-200 rounded-full h-2">
                <div
                  className="bg-purple-600 h-2 rounded-full"
                  style={{ width: `${(stats.activeEmployees / stats.totalEmployees) * 100}%` }}
                />
              </div>
            </div>
          </div>

          {/* Pending */}
          <div className="bg-white rounded-xl p-6 shadow-sm border">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-gray-500">Pendientes</p>
                <p className="text-2xl font-bold text-gray-900">{stats.pendingBookings}</p>
              </div>
              <div className="w-12 h-12 bg-yellow-100 rounded-lg flex items-center justify-center">
                <Clock className="h-6 w-6 text-yellow-600" />
              </div>
            </div>
            <div className="mt-4 flex items-center gap-4">
              <span className="flex items-center gap-1 text-sm text-green-600">
                <CheckCircle className="h-4 w-4" />
                {stats.completedBookings} completadas
              </span>
            </div>
          </div>
        </div>

        {/* Recent Bookings & Quick Actions */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Recent Bookings */}
          <div className="lg:col-span-2 bg-white rounded-xl shadow-sm border">
            <div className="p-6 border-b">
              <div className="flex items-center justify-between">
                <h2 className="text-lg font-semibold text-gray-900">Reservas Recientes</h2>
                <a href="/admin/bookings" className="text-sm text-sky-600 hover:text-sky-700">
                  Ver todas →
                </a>
              </div>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="text-left text-sm text-gray-500 border-b">
                    <th className="px-6 py-3 font-medium">ID</th>
                    <th className="px-6 py-3 font-medium">Cliente</th>
                    <th className="px-6 py-3 font-medium">Servicio</th>
                    <th className="px-6 py-3 font-medium">Fecha</th>
                    <th className="px-6 py-3 font-medium">Estado</th>
                    <th className="px-6 py-3 font-medium text-right">Monto</th>
                  </tr>
                </thead>
                <tbody>
                  {recentBookings.map((booking) => (
                    <tr key={booking.id} className="border-b last:border-0 hover:bg-gray-50">
                      <td className="px-6 py-4 text-sm font-medium text-gray-900">
                        {booking.id}
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-600">
                        {booking.customerName}
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-600">
                        {booking.serviceName}
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-600">
                        {new Date(booking.date).toLocaleDateString('es-ES')} {booking.time}
                      </td>
                      <td className="px-6 py-4">
                        {getStatusBadge(booking.status)}
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-900 text-right font-medium">
                        {formatCurrency(booking.amount)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          {/* Quick Actions & Alerts */}
          <div className="space-y-6">
            {/* Quick Actions */}
            <div className="bg-white rounded-xl shadow-sm border p-6">
              <h2 className="text-lg font-semibold text-gray-900 mb-4">Acciones Rápidas</h2>
              <div className="space-y-3">
                <a
                  href="/admin/bookings/new"
                  className="flex items-center gap-3 p-3 bg-sky-50 text-sky-700 rounded-lg hover:bg-sky-100 transition-colors"
                >
                  <Calendar className="h-5 w-5" />
                  <span className="font-medium">Nueva Reserva</span>
                </a>
                <a
                  href="/admin/employees/new"
                  className="flex items-center gap-3 p-3 bg-purple-50 text-purple-700 rounded-lg hover:bg-purple-100 transition-colors"
                >
                  <Users className="h-5 w-5" />
                  <span className="font-medium">Agregar Empleado</span>
                </a>
                <a
                  href="/admin/services"
                  className="flex items-center gap-3 p-3 bg-green-50 text-green-700 rounded-lg hover:bg-green-100 transition-colors"
                >
                  <TrendingUp className="h-5 w-5" />
                  <span className="font-medium">Gestionar Servicios</span>
                </a>
              </div>
            </div>

            {/* Alerts */}
            <div className="bg-white rounded-xl shadow-sm border p-6">
              <h2 className="text-lg font-semibold text-gray-900 mb-4">Alertas</h2>
              <div className="space-y-3">
                <div className="flex items-start gap-3 p-3 bg-yellow-50 rounded-lg">
                  <AlertCircle className="h-5 w-5 text-yellow-600 mt-0.5" />
                  <div>
                    <p className="text-sm font-medium text-yellow-800">
                      {stats.pendingBookings} reservas pendientes
                    </p>
                    <p className="text-xs text-yellow-600 mt-1">
                      Requieren confirmación
                    </p>
                  </div>
                </div>
                <div className="flex items-start gap-3 p-3 bg-blue-50 rounded-lg">
                  <Clock className="h-5 w-5 text-blue-600 mt-0.5" />
                  <div>
                    <p className="text-sm font-medium text-blue-800">
                      3 servicios hoy
                    </p>
                    <p className="text-xs text-blue-600 mt-1">
                      Próximo a las 14:00
                    </p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </AdminLayout>
  );
}
