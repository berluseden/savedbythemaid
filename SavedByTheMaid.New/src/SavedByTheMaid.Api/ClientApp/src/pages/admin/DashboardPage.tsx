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
} from 'lucide-react';
import { AdminLayout } from '../../components/admin/AdminLayout';
import api from '../../lib/api';

interface DashboardStats {
  totalBookings: number;
  pendingBookings: number;
  completedBookings: number;
  totalRevenue: number;
  totalEmployees: number;
  activeEmployees: number;
}

interface OrderSummary {
  id: number;
  confirmationNumber: string;
  contactName: string | null;
  serviceTypeName: string | null;
  total: number;
  orderStatus: string;
  createdAt: string;
}

interface EmployeeDto {
  id: number;
  firstName: string;
  lastName: string;
  isActive: boolean;
}

export function AdminDashboardPage() {
  const [stats, setStats] = useState<DashboardStats>({
    totalBookings: 0,
    pendingBookings: 0,
    completedBookings: 0,
    totalRevenue: 0,
    totalEmployees: 0,
    activeEmployees: 0,
  });

  const [recentBookings, setRecentBookings] = useState<OrderSummary[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const fetchDashboardData = async () => {
      setIsLoading(true);
      try {
        // Fetch orders and employees in parallel
        const [ordersRes, employeesRes] = await Promise.all([
          api.get<OrderSummary[]>('/admin/orders', { params: { pageSize: '100' } }),
          api.get<EmployeeDto[]>('/admin/employees'),
        ]);

        const orders = ordersRes.data;
        const employees = employeesRes.data;

        // Calculate stats from orders
        const totalRevenue = orders.reduce((acc, o) => acc + o.total, 0);
        const pendingBookings = orders.filter(o => o.orderStatus === 'Pending').length;
        const completedBookings = orders.filter(o => o.orderStatus === 'Completed').length;

        setStats({
          totalBookings: orders.length,
          pendingBookings,
          completedBookings,
          totalRevenue,
          totalEmployees: employees.length,
          activeEmployees: employees.filter(e => e.isActive).length,
        });

        // Recent bookings (last 5)
        setRecentBookings(orders.slice(0, 5));
      } catch (err) {
        setError('Error al cargar datos del dashboard');
        console.error(err);
      } finally {
        setIsLoading(false);
      }
    };

    fetchDashboardData();
  }, []);

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(amount);
  };

  const getStatusBadge = (status: string) => {
    const styles: Record<string, string> = {
      Pending: 'bg-yellow-100 text-yellow-700',
      Confirmed: 'bg-blue-100 text-blue-700',
      InProgress: 'bg-purple-100 text-purple-700',
      Completed: 'bg-green-100 text-green-700',
      Cancelled: 'bg-red-100 text-red-700',
    };
    const labels: Record<string, string> = {
      Pending: 'Pendiente',
      Confirmed: 'Confirmada',
      InProgress: 'En Progreso',
      Completed: 'Completada',
      Cancelled: 'Cancelada',
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
              <span className="flex items-center text-sm text-green-600">
                <ArrowUpRight className="h-4 w-4" />
                Datos reales
              </span>
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
              <span className="flex items-center text-sm text-green-600">
                <TrendingUp className="h-4 w-4" />
                Desde inicio
              </span>
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
                  style={{ width: stats.totalEmployees > 0 ? `${(stats.activeEmployees / stats.totalEmployees) * 100}%` : '0%' }}
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

        {error && (
          <div className="bg-red-50 text-red-600 px-4 py-3 rounded-lg">
            {error}
            <button onClick={() => setError('')} className="ml-2 underline">Cerrar</button>
          </div>
        )}

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
                        {booking.confirmationNumber}
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-600">
                        {booking.contactName || '-'}
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-600">
                        {booking.serviceTypeName || '-'}
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-600">
                        {new Date(booking.createdAt).toLocaleDateString('es-ES')}
                      </td>
                      <td className="px-6 py-4">
                        {getStatusBadge(booking.orderStatus)}
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-900 text-right font-medium">
                        {formatCurrency(booking.total)}
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
