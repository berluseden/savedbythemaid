import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
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
import { getOrderStatusConfig } from '@/shared/lib/status-config';

interface OrderSummary {
  id: number;
  confirmationNumber: string;
  contactName: string | null;
  serviceTypeName: string | null;
  total: number;
  orderStatus: string;
  createdAt: string;
  scheduledDate?: string | null;
}

interface EmployeeDto {
  id: number;
  firstName: string;
  lastName: string;
  isActive: boolean;
}

export function AdminDashboardPage() {
  const [error, setError] = useState('');

  const ordersQuery = useQuery({
    queryKey: ['admin', 'orders'],
    queryFn: () => api.get<OrderSummary[]>('/admin/orders', { params: { pageSize: '100' } }).then(r => r.data),
  });

  const employeesQuery = useQuery({
    queryKey: ['admin', 'employees'],
    queryFn: () => api.get<EmployeeDto[]>('/admin/employees').then(r => r.data),
  });

  const isLoading = ordersQuery.isLoading || employeesQuery.isLoading;
  const orders = ordersQuery.data ?? [];
  const employees = employeesQuery.data ?? [];

  // Derive error state from queries
  if ((ordersQuery.isError || employeesQuery.isError) && !error) {
    // Intentionally not setting error in render; the error banner below handles it
  }

  // Calculate stats from orders
  const totalRevenue = orders.reduce((acc, o) => acc + o.total, 0);
  const pendingBookings = orders.filter(o => o.orderStatus === 'Pending').length;
  const completedBookings = orders.filter(o => o.orderStatus === 'Completed').length;

  // Logic for today/next services
  const now = new Date();
  const startOfToday = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const endOfToday = new Date(startOfToday);
  endOfToday.setDate(endOfToday.getDate() + 1);

  // Filter active orders that have a scheduled date
  const activeOrdersWithDate = orders
    .filter(o => o.scheduledDate && ['Confirmed', 'InProgress', 'Pending'].includes(o.orderStatus))
    .map(o => ({ ...o, dateObj: new Date(o.scheduledDate!) }));

  const todayServices = activeOrdersWithDate.filter(o =>
    o.dateObj >= startOfToday && o.dateObj < endOfToday
  );

  // Find next future service
  const futureServices = activeOrdersWithDate
    .filter(o => o.dateObj > now)
    .sort((a, b) => a.dateObj.getTime() - b.dateObj.getTime());

  let nextServiceText = "No upcoming services";
  if (futureServices.length > 0) {
    const next = futureServices[0];
    const timeStr = next.dateObj.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });

    // If it's today
    if (next.dateObj >= startOfToday && next.dateObj < endOfToday) {
      nextServiceText = `Next at ${timeStr}`;
    } else {
      const dateStr = next.dateObj.toLocaleDateString('en-US', { day: 'numeric', month: 'short' });
      nextServiceText = `Next: ${dateStr} ${timeStr}`;
    }
  }

  const stats = {
    totalBookings: orders.length,
    pendingBookings,
    completedBookings,
    totalRevenue,
    totalEmployees: employees.length,
    activeEmployees: employees.filter(e => e.isActive).length,
    todayServiceCount: todayServices.length,
    nextServiceText,
  };

  const recentBookings = orders.slice(0, 5);

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(amount);
  };

  const getStatusBadge = (status: string) => {
    const config = getOrderStatusConfig(status);
    return (
      <span className={`px-2 py-1 rounded-full text-xs font-medium ${config.bgColor} ${config.color}`}>
        {config.label}
      </span>
    );
  };

  if (isLoading) {
    return (
      <AdminLayout>
        <div className="flex items-center justify-center h-64">
          <div className="w-8 h-8 border-4 border-brand border-t-transparent rounded-full animate-spin" />
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
          <p className="text-gray-600">Business overview</p>
        </div>

        {/* Stats Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          {/* Total Bookings */}
          <div className="bg-white rounded-xl p-6 shadow-sm border">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-gray-500">Total Bookings</p>
                <p className="text-2xl font-bold text-gray-900">{stats.totalBookings}</p>
              </div>
              <div className="w-12 h-12 bg-accent-light/20 rounded-lg flex items-center justify-center">
                <Calendar className="h-6 w-6 text-brand" />
              </div>
            </div>
            <div className="mt-4 flex items-center gap-2">
              <span className="flex items-center text-sm text-green-600">
                <ArrowUpRight className="h-4 w-4" />
                Real data
              </span>
            </div>
          </div>

          {/* Revenue */}
          <div className="bg-white rounded-xl p-6 shadow-sm border">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-gray-500">Total Revenue</p>
                <p className="text-2xl font-bold text-gray-900">{formatCurrency(stats.totalRevenue)}</p>
              </div>
              <div className="w-12 h-12 bg-green-100 rounded-lg flex items-center justify-center">
                <DollarSign className="h-6 w-6 text-green-600" />
              </div>
            </div>
            <div className="mt-4 flex items-center gap-2">
              <span className="flex items-center text-sm text-green-600">
                <TrendingUp className="h-4 w-4" />
                All time
              </span>
            </div>
          </div>

          {/* Employees */}
          <div className="bg-white rounded-xl p-6 shadow-sm border">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-gray-500">Active Employees</p>
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
                <p className="text-sm text-gray-500">Pending</p>
                <p className="text-2xl font-bold text-gray-900">{stats.pendingBookings}</p>
              </div>
              <div className="w-12 h-12 bg-yellow-100 rounded-lg flex items-center justify-center">
                <Clock className="h-6 w-6 text-yellow-600" />
              </div>
            </div>
            <div className="mt-4 flex items-center gap-4">
              <span className="flex items-center gap-1 text-sm text-green-600">
                <CheckCircle className="h-4 w-4" />
                {stats.completedBookings} completed
              </span>
            </div>
          </div>
        </div>

        {(error || ordersQuery.isError || employeesQuery.isError) && (
          <div className="bg-red-50 text-red-600 px-4 py-3 rounded-lg">
            {error || 'Error loading dashboard data'}
            <button onClick={() => setError('')} className="ml-2 underline">Close</button>
          </div>
        )}

        {/* Recent Bookings & Alerts */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Recent Bookings */}
          <div className="lg:col-span-2 bg-white rounded-xl shadow-sm border">
            <div className="p-6 border-b">
              <div className="flex items-center justify-between">
                <h2 className="text-lg font-semibold text-gray-900">Recent Bookings</h2>
                <a href="/admin/bookings" className="text-sm text-brand hover:text-brand-dark">
                  View all →
                </a>
              </div>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="text-left text-sm text-gray-500 border-b">
                    <th className="px-6 py-3 font-medium">ID</th>
                    <th className="px-6 py-3 font-medium">Customer</th>
                    <th className="px-6 py-3 font-medium">Service</th>
                    <th className="px-6 py-3 font-medium">Date</th>
                    <th className="px-6 py-3 font-medium">Status</th>
                    <th className="px-6 py-3 font-medium text-right">Amount</th>
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
                        {new Date(booking.createdAt).toLocaleDateString('en-US')}
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

          {/* Alerts */}
          <div className="space-y-6">
            <div className="bg-white rounded-xl shadow-sm border p-6">
              <h2 className="text-lg font-semibold text-gray-900 mb-4">Alerts</h2>
              <div className="space-y-3">
                <div className="flex items-start gap-3 p-3 bg-yellow-50 rounded-lg">
                  <AlertCircle className="h-5 w-5 text-yellow-600 mt-0.5" />
                  <div>
                    <p className="text-sm font-medium text-yellow-800">
                      {stats.pendingBookings} pending bookings
                    </p>
                    <p className="text-xs text-yellow-600 mt-1">
                      Require confirmation
                    </p>
                  </div>
                </div>
                <div className="flex items-start gap-3 p-3 bg-blue-50 rounded-lg">
                  <Clock className="h-5 w-5 text-blue-600 mt-0.5" />
                  <div>
                    <p className="text-sm font-medium text-blue-800">
                      {stats.todayServiceCount} services today
                    </p>
                    <p className="text-xs text-blue-600 mt-1">
                      {stats.nextServiceText}
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
