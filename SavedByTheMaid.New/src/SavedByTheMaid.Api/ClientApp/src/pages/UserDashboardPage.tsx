import { useState, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { Calendar, Clock, MapPin, CreditCard, X, AlertCircle, Sparkles } from 'lucide-react';
import { Link } from 'react-router-dom';
import api from '@/lib/api';

interface CustomerOrder {
  id: number;
  serviceTypeName: string;
  cleaningPlaceName: string;
  scheduledDate: string;
  scheduledTime: string;
  estimatedDuration: number;
  totalAmount: number;
  status: string;
  address: string;
}

interface CustomerStats {
  completedBookings: number;
  totalSpent: number;
  nextBooking: string | null;
}

const statusConfig: Record<string, { label: string; color: string; bgColor: string }> = {
  Confirmed: { label: 'Confirmed', color: 'text-blue-700', bgColor: 'bg-blue-100' },
  InProgress: { label: 'In Progress', color: 'text-purple-700', bgColor: 'bg-purple-100' },
  Completed: { label: 'Completed', color: 'text-green-700', bgColor: 'bg-green-100' },
  Cancelled: { label: 'Cancelled', color: 'text-red-700', bgColor: 'bg-red-100' },
};

export function UserDashboardPage() {
  const { user } = useAuth();
  const [upcomingBookings, setUpcomingBookings] = useState<CustomerOrder[]>([]);
  const [stats, setStats] = useState<CustomerStats | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [cancellingId, setCancellingId] = useState<number | null>(null);
  const [cancelReason, setCancelReason] = useState('');
  const [showCancelModal, setShowCancelModal] = useState(false);

  useEffect(() => {
    fetchDashboardData();
  }, []);

  const fetchDashboardData = async () => {
    try {
      setIsLoading(true);
      setError('');

      const [ordersRes, statsRes] = await Promise.all([
        api.get<{ items: CustomerOrder[] }>('/customer/my-orders?pageSize=10'),
        api.get<CustomerStats>('/customer/stats'),
      ]);

      const upcoming = ordersRes.data.items.filter(
        (o) => o.status === 'Confirmed' || o.status === 'InProgress'
      );
      setUpcomingBookings(upcoming);
      setStats(statsRes.data);
    } catch (err) {
      setError('Failed to load dashboard data');
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  const handleCancelOrder = async () => {
    if (!cancellingId) return;
    try {
      await api.post(`/customer/my-orders/${cancellingId}/cancel`, { reason: cancelReason });
      setShowCancelModal(false);
      setCancellingId(null);
      setCancelReason('');
      await fetchDashboardData();
    } catch (err) {
      setError('Failed to cancel booking');
      console.error(err);
    }
  };

  const formatDate = (dateStr: string) => {
    const date = new Date(dateStr);
    return date.toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' });
  };

  const formatTime = (timeStr: string) => {
    const [hours, minutes] = timeStr.split(':');
    const hour = parseInt(hours);
    const ampm = hour >= 12 ? 'PM' : 'AM';
    const hour12 = hour % 12 || 12;
    return `${hour12}:${minutes} ${ampm}`;
  };

  const openCancelModal = (id: number) => {
    setCancellingId(id);
    setCancelReason('');
    setShowCancelModal(true);
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <div className="w-8 h-8 border-4 border-sky-500 border-t-transparent rounded-full animate-spin mx-auto mb-4" />
          <p className="text-gray-600">Loading your dashboard...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 py-8">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900">
            Welcome back{user?.firstName ? `, ${user.firstName}` : ''}!
          </h1>
          <p className="text-gray-600 mt-1">Manage your bookings and account</p>
        </div>

        {error && (
          <div className="mb-6 bg-red-50 border border-red-200 rounded-lg p-4 flex items-center gap-3">
            <AlertCircle className="w-5 h-5 text-red-500" />
            <p className="text-red-700">{error}</p>
          </div>
        )}

        {/* Stats Cards */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
          <div className="bg-white rounded-xl shadow-sm p-6 border border-gray-200">
            <div className="flex items-center gap-4">
              <div className="w-12 h-12 bg-green-100 rounded-xl flex items-center justify-center">
                <Calendar className="w-6 h-6 text-green-600" />
              </div>
              <div>
                <p className="text-sm text-gray-500">Completed</p>
                <p className="text-2xl font-bold text-gray-900">{stats?.completedBookings ?? 0}</p>
              </div>
            </div>
          </div>

          <div className="bg-white rounded-xl shadow-sm p-6 border border-gray-200">
            <div className="flex items-center gap-4">
              <div className="w-12 h-12 bg-sky-100 rounded-xl flex items-center justify-center">
                <CreditCard className="w-6 h-6 text-sky-600" />
              </div>
              <div>
                <p className="text-sm text-gray-500">Total Spent</p>
                <p className="text-2xl font-bold text-gray-900">${stats?.totalSpent?.toFixed(2) ?? '0.00'}</p>
              </div>
            </div>
          </div>

          <div className="bg-white rounded-xl shadow-sm p-6 border border-gray-200">
            <div className="flex items-center gap-4">
              <div className="w-12 h-12 bg-purple-100 rounded-xl flex items-center justify-center">
                <Clock className="w-6 h-6 text-purple-600" />
              </div>
              <div>
                <p className="text-sm text-gray-500">Next Booking</p>
                <p className="text-lg font-bold text-gray-900">
                  {stats?.nextBooking ? formatDate(stats.nextBooking) : 'None'}
                </p>
              </div>
            </div>
          </div>

        </div>

        {/* Upcoming Bookings */}
        <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
          <div className="px-6 py-4 border-b border-gray-200 flex items-center justify-between">
            <h2 className="text-lg font-semibold text-gray-900">Upcoming Bookings</h2>
            <Link
              to="/booking"
              className="text-sm text-sky-600 hover:text-sky-700 font-medium"
            >
              Book New Cleaning
            </Link>
          </div>

          {upcomingBookings.length === 0 ? (
            <div className="p-8 text-center">
              <Calendar className="w-12 h-12 text-gray-300 mx-auto mb-4" />
              <p className="text-gray-500 mb-4">No upcoming bookings</p>
              <Link
                to="/booking"
                className="inline-flex items-center px-4 py-2 bg-sky-500 text-white rounded-lg hover:bg-sky-600 transition-colors"
              >
                Book Your First Cleaning
              </Link>
            </div>
          ) : (
            <div className="divide-y divide-gray-200">
              {upcomingBookings.map((booking) => {
                const status = statusConfig[booking.status] || statusConfig.Pending;
                return (
                  <div key={booking.id} className="p-6 hover:bg-gray-50 transition-colors">
                    <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                      <div className="flex-1">
                        <div className="flex items-center gap-3 mb-2">
                          <h3 className="font-semibold text-gray-900">{booking.serviceTypeName}</h3>
                          <span className={`px-2 py-1 rounded-full text-xs font-medium ${status.color} ${status.bgColor}`}>
                            {status.label}
                          </span>
                        </div>
                        <div className="flex flex-wrap items-center gap-4 text-sm text-gray-600">
                          <div className="flex items-center gap-1">
                            <Calendar className="w-4 h-4" />
                            {formatDate(booking.scheduledDate)}
                          </div>
                          <div className="flex items-center gap-1">
                            <Clock className="w-4 h-4" />
                            {formatTime(booking.scheduledTime)}
                          </div>
                          <div className="flex items-center gap-1">
                            <MapPin className="w-4 h-4" />
                            {booking.address || booking.cleaningPlaceName}
                          </div>
                        </div>
                      </div>
                      <div className="flex items-center gap-4">
                        <div className="text-right">
                          <p className="text-lg font-bold text-gray-900">${booking.totalAmount.toFixed(2)}</p>
                          <p className="text-sm text-gray-500">{booking.estimatedDuration} min</p>
                        </div>
                        {(booking.status === 'Pending' || booking.status === 'Confirmed') && (
                          <button
                            onClick={() => openCancelModal(booking.id)}
                            className="px-3 py-1 text-sm text-red-600 border border-red-300 rounded-lg hover:bg-red-50 transition-colors"
                          >
                            Cancel
                          </button>
                        )}
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>

        {/* Quick Actions */}
        <div className="mt-8 grid grid-cols-1 sm:grid-cols-3 gap-4">
          <Link
            to="/booking"
            className="bg-sky-500 text-white rounded-xl p-6 text-center hover:bg-sky-600 transition-colors"
          >
            <Calendar className="w-8 h-8 mx-auto mb-2" />
            <p className="font-semibold">Book Cleaning</p>
          </Link>
          <Link
            to="/services"
            className="bg-white border border-gray-200 rounded-xl p-6 text-center hover:bg-gray-50 transition-colors"
          >
            <Sparkles className="w-8 h-8 mx-auto mb-2 text-gray-600" />
            <p className="font-semibold text-gray-900">View Services</p>
          </Link>
          <Link
            to="/contact"
            className="bg-white border border-gray-200 rounded-xl p-6 text-center hover:bg-gray-50 transition-colors"
          >
            <MapPin className="w-8 h-8 mx-auto mb-2 text-gray-600" />
            <p className="font-semibold text-gray-900">Contact Support</p>
          </Link>
        </div>
      </div>

      {/* Cancel Modal */}
      {showCancelModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-white rounded-xl shadow-xl max-w-md w-full mx-4 p-6">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-semibold text-gray-900">Cancel Booking</h3>
              <button
                onClick={() => setShowCancelModal(false)}
                className="text-gray-400 hover:text-gray-600"
              >
                <X className="w-5 h-5" />
              </button>
            </div>
            <p className="text-gray-600 mb-4">
              Are you sure you want to cancel this booking? This action cannot be undone.
            </p>
            <div className="mb-4">
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Reason for cancellation (optional)
              </label>
              <textarea
                value={cancelReason}
                onChange={(e) => setCancelReason(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-sky-500 focus:border-sky-500"
                rows={3}
                placeholder="Tell us why you're cancelling..."
              />
            </div>
            <div className="flex gap-3">
              <button
                onClick={() => setShowCancelModal(false)}
                className="flex-1 px-4 py-2 border border-gray-300 rounded-lg text-gray-700 hover:bg-gray-50 transition-colors"
              >
                Keep Booking
              </button>
              <button
                onClick={handleCancelOrder}
                className="flex-1 px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600 transition-colors"
              >
                Cancel Booking
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default UserDashboardPage;
