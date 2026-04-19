import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  Calendar,
  Users,
  DollarSign,
  Clock,
  CheckCircle,
  AlertCircle,
} from 'lucide-react';
import { AdminLayout } from '../../components/admin/AdminLayout';
import api from '../../lib/api';
import { getOrderStatusConfig } from '@/shared/lib/status-config';
import { Spinner } from '@/shared/components/ui/spinner';

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

interface PaginatedOrders {
  items: OrderSummary[];
  totalCount: number;
  page: number;
  pageSize: number;
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
    queryFn: () => api.get<PaginatedOrders>('/admin/orders', { params: { pageSize: '100' } }).then(r => r.data.items),
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
          <Spinner size="md" />
        </div>
      </AdminLayout>
    );
  }

  return (
    <AdminLayout>
      <div className="space-y-6">
        {/* Header */}
        <div>
          <h1 className="text-2xl font-bold text-gray-900 tracking-tight">Dashboard</h1>
          <p className="text-sm text-gray-500 mt-0.5">Business overview</p>
        </div>

        {/* Stats Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {/* Total Bookings */}
          <div className="bg-white rounded-xl p-5 border border-gray-100 shadow-sm">
            <div className="flex items-center justify-between mb-4">
              <p className="text-sm text-gray-500 font-medium">Total Bookings</p>
              <div className="w-9 h-9 bg-brand/10 rounded-lg flex items-center justify-center">
                <Calendar className="h-4 w-4 text-brand" aria-hidden="true" />
              </div>
            </div>
            <p className="text-3xl font-bold text-gray-900">{stats.totalBookings}</p>
          </div>

          {/* Revenue */}
          <div className="bg-white rounded-xl p-5 border border-gray-100 shadow-sm">
            <div className="flex items-center justify-between mb-4">
              <p className="text-sm text-gray-500 font-medium">Total Revenue</p>
              <div className="w-9 h-9 bg-brand/10 rounded-lg flex items-center justify-center">
                <DollarSign className="h-4 w-4 text-brand" aria-hidden="true" />
              </div>
            </div>
            <p className="text-3xl font-bold text-gray-900">{formatCurrency(stats.totalRevenue)}</p>
          </div>

          {/* Employees */}
          <div className="bg-white rounded-xl p-5 border border-gray-100 shadow-sm">
            <div className="flex items-center justify-between mb-4">
              <p className="text-sm text-gray-500 font-medium">Active Employees</p>
              <div className="w-9 h-9 bg-brand/10 rounded-lg flex items-center justify-center">
                <Users className="h-4 w-4 text-brand" aria-hidden="true" />
              </div>
            </div>
            <p className="text-3xl font-bold text-gray-900">{stats.activeEmployees}<span className="text-lg font-medium text-gray-400">/{stats.totalEmployees}</span></p>
          </div>

          {/* Pending */}
          <div className="bg-white rounded-xl p-5 border border-gray-100 shadow-sm">
            <div className="flex items-center justify-between mb-4">
              <p className="text-sm text-gray-500 font-medium">Pending</p>
              <div className="w-9 h-9 bg-brand/10 rounded-lg flex items-center justify-center">
                <Clock className="h-4 w-4 text-brand" aria-hidden="true" />
              </div>
            </div>
            <p className="text-3xl font-bold text-gray-900">{stats.pendingBookings}</p>
            <p className="text-xs text-gray-400 mt-1.5 flex items-center gap-1">
              <CheckCircle className="h-3.5 w-3.5 text-success" aria-hidden="true" />
              {stats.completedBookings} completed
            </p>
          </div>
        </div>

        {(error || ordersQuery.isError || employeesQuery.isError) && (
          <div role="alert" className="bg-red-50 text-red-600 px-4 py-3 rounded-lg">
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
                    <th scope="col" className="px-6 py-3 font-medium">ID</th>
                    <th scope="col" className="px-6 py-3 font-medium">Customer</th>
                    <th scope="col" className="px-6 py-3 font-medium">Service</th>
                    <th scope="col" className="px-6 py-3 font-medium">Date</th>
                    <th scope="col" className="px-6 py-3 font-medium">Status</th>
                    <th scope="col" className="px-6 py-3 font-medium text-right">Amount</th>
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
