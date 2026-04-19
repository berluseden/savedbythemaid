import { useState } from 'react';
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
  Users,
  PlayCircle,
} from 'lucide-react';
import { AdminLayout } from '../../components/admin/AdminLayout';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/shared/components/ui/dialog';
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
import { Skeleton, TableRowSkeleton } from '@/shared/components/ui/skeleton';
import { pushToast } from '@/lib/toast';
import {
  useAdminBookings,
  useActiveEmployees,
  useOrderMeetings,
  useUpdateBookingStatus,
  useCancelOrder,
  useAssignEmployee,
  useUpdateMeetingStatus,
} from './bookings/hooks';
import { formatCurrency, formatDate, formatDateTime } from './bookings/types';
import type { OrderSummary, MeetingSummary } from './bookings/types';
import type { OrderStatus, MeetStatus } from '@/shared/lib/status-config';

const statusConfig: Record<OrderStatus, { label: string; color: string; bgColor: string }> = {
  PendingReview: { label: 'Pending Review', color: 'text-orange-700', bgColor: 'bg-orange-100' },
  Draft: { label: 'Draft', color: 'text-gray-700', bgColor: 'bg-gray-100' },
  Confirmed: { label: 'Confirmed', color: 'text-blue-700', bgColor: 'bg-blue-100' },
  InProgress: { label: 'In Progress', color: 'text-purple-700', bgColor: 'bg-purple-100' },
  Completed: { label: 'Completed', color: 'text-green-700', bgColor: 'bg-green-100' },
  Cancelled: { label: 'Cancelled', color: 'text-red-700', bgColor: 'bg-red-100' },
  NoShow: { label: 'No Show', color: 'text-red-700', bgColor: 'bg-red-200' },
};

const meetStatusConfig: Record<MeetStatus, { label: string; color: string; bgColor: string }> = {
  Scheduled: { label: 'Scheduled', color: 'text-gray-700', bgColor: 'bg-gray-100' },
  Assigned: { label: 'Assigned', color: 'text-blue-700', bgColor: 'bg-blue-100' },
  OnTheWay: { label: 'On The Way', color: 'text-cyan-700', bgColor: 'bg-cyan-100' },
  InProgress: { label: 'In Progress', color: 'text-purple-700', bgColor: 'bg-purple-100' },
  Completed: { label: 'Completed', color: 'text-green-700', bgColor: 'bg-green-100' },
  Cancelled: { label: 'Cancelled', color: 'text-red-700', bgColor: 'bg-red-100' },
  Rescheduled: { label: 'Rescheduled', color: 'text-orange-700', bgColor: 'bg-orange-100' },
  NoShow: { label: 'No Show', color: 'text-red-700', bgColor: 'bg-red-100' },
};

export function AdminBookingsPage() {
  const [searchTerm, setSearchTerm] = useState('');
  const [filterStatus, setFilterStatus] = useState<string>('all');
  const [filterDate, setFilterDate] = useState<string>('');
  const [selectedBooking, setSelectedBooking] = useState<OrderSummary | null>(null);
  const [currentPage, setCurrentPage] = useState(1);
  const [assigningMeetingId, setAssigningMeetingId] = useState<number | null>(null);
  const [cancelConfirmId, setCancelConfirmId] = useState<number | null>(null);
  const itemsPerPage = 10;

  // Server state via TanStack Query — cached, deduped, refetched as needed
  const bookingsQuery = useAdminBookings();
  const employeesQuery = useActiveEmployees();
  const meetingsQuery = useOrderMeetings(selectedBooking?.id ?? null);

  const bookings = bookingsQuery.data ?? [];
  const employees = employeesQuery.data ?? [];
  const meetings: MeetingSummary[] = meetingsQuery.data ?? [];
  const isLoading = bookingsQuery.isLoading;
  const error = bookingsQuery.isError ? 'Error loading bookings' : '';

  const updateStatusMutation = useUpdateBookingStatus();
  const cancelOrderMutation = useCancelOrder();
  const assignEmployeeMutation = useAssignEmployee();
  const updateMeetingMutation = useUpdateMeetingStatus();

  const updateBookingStatus = (bookingId: number, newStatus: OrderStatus) => {
    updateStatusMutation.mutate(
      { bookingId, status: newStatus },
      {
        onSuccess: () => {
          pushToast(`Booking updated to ${newStatus}`, 'success');
          if (selectedBooking?.id === bookingId) {
            setSelectedBooking((prev) => (prev ? { ...prev, orderStatus: newStatus } : null));
          }
        },
        onError: () => pushToast('Could not update booking status. Please try again.', 'error'),
      }
    );
  };

  const requestCancel = (bookingId: number) => setCancelConfirmId(bookingId);

  const confirmCancel = () => {
    if (cancelConfirmId == null) return;
    const id = cancelConfirmId;
    cancelOrderMutation.mutate(id, {
      onSuccess: () => {
        pushToast('Booking cancelled', 'success');
        setCancelConfirmId(null);
        if (selectedBooking?.id === id) setSelectedBooking(null);
      },
      onError: () => {
        pushToast('Could not cancel booking. Please try again.', 'error');
        setCancelConfirmId(null);
      },
    });
  };

  const assignEmployee = (meetingId: number, employeeId: number) => {
    assignEmployeeMutation.mutate(
      { meetingId, employeeId },
      {
        onSuccess: () => {
          pushToast('Employee assigned', 'success');
          setAssigningMeetingId(null);
        },
        onError: () => pushToast('Could not assign employee. The slot may conflict.', 'error'),
      }
    );
  };

  const updateMeetingStatus = (meetingId: number, status: MeetStatus) => {
    updateMeetingMutation.mutate(
      { meetingId, status },
      {
        onSuccess: () => pushToast(`Appointment ${status.toLowerCase()}`, 'success'),
        onError: () => pushToast('Could not update appointment status.', 'error'),
      }
    );
  };

  const filteredBookings = bookings.filter((booking: OrderSummary) => {
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

  if (isLoading) {
    return (
      <AdminLayout>
        <div role="status" aria-live="polite" aria-busy="true" className="space-y-6">
          <span className="sr-only">Loading bookings…</span>

          {/* Header skeleton */}
          <div className="space-y-2">
            <Skeleton className="h-7 w-40" />
            <Skeleton className="h-4 w-64" />
          </div>

          {/* Stats grid skeleton */}
          <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
            {Array.from({ length: 5 }).map((_, i) => (
              <div key={i} className="p-4 rounded-lg border bg-white space-y-2">
                <Skeleton className="h-3 w-20" />
                <Skeleton className="h-7 w-12" />
              </div>
            ))}
          </div>

          {/* Table skeleton */}
          <div className="bg-white rounded-xl shadow-sm border overflow-hidden">
            <table className="w-full">
              <tbody>
                {Array.from({ length: 6 }).map((_, i) => (
                  <TableRowSkeleton key={i} columns={8} />
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </AdminLayout>
    );
  }

  return (
    <AdminLayout>
      <div className="space-y-6">
        {/* Page header */}
        <div className="mb-8">
          <h1 className="text-2xl font-bold text-gray-900">Bookings</h1>
          <p className="mt-1 text-sm text-gray-500">Manage all customer bookings</p>
        </div>

        {/* Stats */}
        <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
          {Object.entries(statusConfig).map(([status, config]) => (
            <button
              key={status}
              onClick={() => setFilterStatus(filterStatus === status ? 'all' : status)}
              className={`p-4 rounded-lg border transition-all ${
                filterStatus === status
                  ? 'ring-2 ring-brand border-brand'
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
          <div role="alert" className="bg-red-50 text-red-600 px-4 py-3 rounded-lg">
            {error}
            <button
              type="button"
              onClick={() => bookingsQuery.refetch()}
              className="ml-2 underline"
            >
              Retry
            </button>
          </div>
        )}

        {/* Filters */}
        <div className="flex flex-col sm:flex-row gap-4" role="search" aria-label="Filter bookings">
          <div className="relative flex-1">
            <label htmlFor="bookings-page-search" className="sr-only">Search bookings</label>
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-gray-400" aria-hidden="true" />
            <input
              id="bookings-page-search"
              type="search"
              placeholder="Search by name, ID or address..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
            />
          </div>
          <div>
            <label htmlFor="bookings-page-date" className="sr-only">Filter by date</label>
            <input
              id="bookings-page-date"
              type="date"
              value={filterDate}
              onChange={(e) => setFilterDate(e.target.value)}
              className="px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
            />
          </div>
          <div>
            <label htmlFor="bookings-page-status" className="sr-only">Filter by status</label>
            <select
              id="bookings-page-status"
              value={filterStatus}
              onChange={(e) => setFilterStatus(e.target.value)}
              className="px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
            >
              <option value="all">All statuses</option>
              {Object.entries(statusConfig).map(([status, config]) => (
                <option key={status} value={status}>
                  {config.label}
                </option>
              ))}
            </select>
          </div>
        </div>

        {/* Table */}
        <div className="bg-white rounded-xl shadow-sm border overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full">
              <caption className="sr-only">
                Bookings list — {filteredBookings.length} result{filteredBookings.length === 1 ? '' : 's'}
              </caption>
              <thead>
                <tr className="text-left text-sm text-gray-500 bg-gray-50 border-b">
                  <th scope="col" className="px-6 py-3 font-medium">Confirmation</th>
                  <th scope="col" className="px-6 py-3 font-medium">Customer</th>
                  <th scope="col" className="px-6 py-3 font-medium">Service</th>
                  <th scope="col" className="px-6 py-3 font-medium">Date</th>
                  <th scope="col" className="px-6 py-3 font-medium">Status</th>
                  <th scope="col" className="px-6 py-3 font-medium">Phone</th>
                  <th scope="col" className="px-6 py-3 font-medium text-right">Total</th>
                  <th scope="col" className="px-6 py-3 font-medium text-right">Actions</th>
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
                          className="p-2 text-gray-400 hover:text-brand hover:bg-brand/10 rounded-lg"
                          title="View details"
                        >
                          <Eye className="h-4 w-4" />
                        </button>
                        {(booking.orderStatus === 'PendingReview' || booking.orderStatus === 'Draft') && (
                          <>
                            <button
                              onClick={() => updateBookingStatus(booking.id, 'Confirmed')}
                              className="p-2 text-gray-400 hover:text-green-600 hover:bg-green-50 rounded-lg"
                              title="Approve and Confirm"
                            >
                              <CheckCircle className="h-4 w-4" />
                            </button>
                            <button
                              onClick={() => requestCancel(booking.id)}
                              className="p-2 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded-lg"
                              title="Reject"
                            >
                              <XCircle className="h-4 w-4" />
                            </button>
                          </>
                        )}
                        {booking.orderStatus === 'Confirmed' && (
                          <button
                            onClick={() => updateBookingStatus(booking.id, 'InProgress')}
                            className="p-2 text-gray-400 hover:text-purple-600 hover:bg-purple-50 rounded-lg"
                            title="Start"
                          >
                            <AlertCircle className="h-4 w-4" />
                          </button>
                        )}
                        {booking.orderStatus === 'InProgress' && (
                          <button
                            onClick={() => updateBookingStatus(booking.id, 'Completed')}
                            className="p-2 text-gray-400 hover:text-green-600 hover:bg-green-50 rounded-lg"
                            title="Complete"
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
                Showing {(currentPage - 1) * itemsPerPage + 1} -{' '}
                {Math.min(currentPage * itemsPerPage, filteredBookings.length)} of{' '}
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
            <p className="text-gray-500">No bookings found</p>
          </div>
        )}
      </div>

      {/* Detail Modal */}
      <Dialog open={!!selectedBooking} onOpenChange={(open) => !open && setSelectedBooking(null)}>
        <DialogContent className="max-w-2xl">
          {selectedBooking && (
            <>
              <DialogHeader>
                <DialogTitle className="flex items-center gap-3">
                  {selectedBooking.confirmationNumber}
                  <span
                    className={`inline-block px-2 py-1 rounded-full text-xs font-medium ${
                      statusConfig[selectedBooking.orderStatus]?.bgColor || 'bg-gray-100'
                    } ${statusConfig[selectedBooking.orderStatus]?.color || 'text-gray-700'}`}
                  >
                    {statusConfig[selectedBooking.orderStatus]?.label || selectedBooking.orderStatus}
                  </span>
                </DialogTitle>
              </DialogHeader>

              <div className="space-y-6">
                {/* Customer Info */}
                <div>
                  <h3 className="text-sm font-medium text-gray-500 mb-3">Customer Information</h3>
                  <div className="bg-gray-50 rounded-lg p-4 space-y-3">
                    <div className="flex items-center gap-3">
                      <User className="h-5 w-5 text-gray-400" />
                      <span className="text-gray-900">{selectedBooking.contactName || 'No name'}</span>
                    </div>
                    <div className="flex items-center gap-3">
                      <Phone className="h-5 w-5 text-gray-400" />
                      <span className="text-gray-900">{selectedBooking.contactPhone || 'No phone'}</span>
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
                  <h3 className="text-sm font-medium text-gray-500 mb-3">Service Details</h3>
                  <div className="bg-gray-50 rounded-lg p-4 grid grid-cols-2 gap-4">
                    <div>
                      <p className="text-xs text-gray-500">Service Type</p>
                      <p className="font-medium text-gray-900">{selectedBooking.serviceTypeName || '-'}</p>
                    </div>
                    <div>
                      <p className="text-xs text-gray-500">Service Area</p>
                      <p className="font-medium text-gray-900">{selectedBooking.serviceAreaName || '-'}</p>
                    </div>
                    <div>
                      <p className="text-xs text-gray-500">Recurrence</p>
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
                  <h3 className="text-sm font-medium text-gray-500 mb-3">Order Information</h3>
                  <div className="bg-gray-50 rounded-lg p-4 grid grid-cols-2 gap-4">
                    <div className="flex items-center gap-3">
                      <Calendar className="h-5 w-5 text-gray-400" />
                      <div>
                        <p className="text-xs text-gray-500">Created</p>
                        <p className="font-medium text-gray-900">{formatDate(selectedBooking.createdAt)}</p>
                      </div>
                    </div>
                    <div className="flex items-center gap-3">
                      <Clock className="h-5 w-5 text-gray-400" />
                      <div>
                        <p className="text-xs text-gray-500">Payment Status</p>
                        <p className="font-medium text-gray-900">{selectedBooking.paymentStatus}</p>
                      </div>
                    </div>
                  </div>
                </div>

                {/* Meetings Section */}
                <div>
                  <h3 className="text-sm font-medium text-gray-500 mb-3 flex items-center gap-2">
                    <Users className="h-4 w-4" />
                    Scheduled Appointments
                  </h3>
                  {meetings.length === 0 ? (
                    <div className="bg-gray-50 rounded-lg p-4 text-center text-gray-500 text-sm">
                      No scheduled appointments for this order
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
                                  <p className="text-xs text-gray-500">Scheduled Start</p>
                                  <p className="font-medium text-gray-900">{formatDateTime(meeting.scheduledStart)}</p>
                                </div>
                                <div>
                                  <p className="text-xs text-gray-500">Scheduled End</p>
                                  <p className="font-medium text-gray-900">{formatDateTime(meeting.scheduledEnd)}</p>
                                </div>
                                {meeting.actualStart && (
                                  <div>
                                    <p className="text-xs text-gray-500">Actual Start</p>
                                    <p className="font-medium text-green-700">{formatDateTime(meeting.actualStart)}</p>
                                  </div>
                                )}
                                {meeting.actualEnd && (
                                  <div>
                                    <p className="text-xs text-gray-500">Actual End</p>
                                    <p className="font-medium text-green-700">{formatDateTime(meeting.actualEnd)}</p>
                                  </div>
                                )}
                              </div>
                            </div>
                          </div>

                          {/* Employee Assignment */}
                          <div className="pt-3 border-t border-gray-200">
                            <p className="text-xs text-gray-500 mb-2">Assigned Employee</p>
                            {assigningMeetingId === meeting.id ? (
                              <div className="flex gap-2">
                                <select
                                  onChange={(e) => assignEmployee(meeting.id, parseInt(e.target.value))}
                                  className="flex-1 px-3 py-2 border rounded-lg text-sm focus:ring-2 focus:ring-brand"
                                  defaultValue=""
                                >
                                  <option value="" disabled>Select employee...</option>
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
                                  Cancel
                                </button>
                              </div>
                            ) : (
                              <div className="flex items-center justify-between">
                                <div className="flex items-center gap-2">
                                  <User className="h-4 w-4 text-gray-400" />
                                  <span className="font-medium text-gray-900">
                                    {meeting.employeeName || 'Unassigned'}
                                  </span>
                                </div>
                                <button
                                  onClick={() => setAssigningMeetingId(meeting.id)}
                                  className="text-sm text-brand hover:text-brand-dark font-medium"
                                >
                                  {meeting.employeeId ? 'Change' : 'Assign'}
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
                                  Start
                                </button>
                              )}
                              {meeting.status === 'InProgress' && (
                                <button
                                  onClick={() => updateMeetingStatus(meeting.id, 'Completed')}
                                  className="flex-1 px-3 py-1.5 bg-green-500 text-white rounded text-sm hover:bg-green-600 flex items-center justify-center gap-1"
                                >
                                  <CheckCircle className="h-4 w-4" />
                                  Complete
                                </button>
                              )}
                              {(meeting.status === 'Scheduled' || meeting.status === 'Assigned') && (
                                <button
                                  onClick={() => updateMeetingStatus(meeting.id, 'Cancelled')}
                                  className="px-3 py-1.5 bg-red-500 text-white rounded text-sm hover:bg-red-600 flex items-center justify-center gap-1"
                                >
                                  <XCircle className="h-4 w-4" />
                                  Cancel
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
                        Confirm Booking
                      </button>
                      <button
                        onClick={() => requestCancel(selectedBooking.id)}
                        className="flex-1 px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600 transition-colors"
                      >
                        Cancel Booking
                      </button>
                    </>
                  )}
                  {selectedBooking.orderStatus === 'Confirmed' && (
                    <button
                      onClick={() => updateBookingStatus(selectedBooking.id, 'InProgress')}
                      className="flex-1 px-4 py-2 bg-purple-500 text-white rounded-lg hover:bg-purple-600 transition-colors"
                    >
                      Start Service
                    </button>
                  )}
                  {selectedBooking.orderStatus === 'InProgress' && (
                    <button
                      onClick={() => updateBookingStatus(selectedBooking.id, 'Completed')}
                      className="flex-1 px-4 py-2 bg-green-500 text-white rounded-lg hover:bg-green-600 transition-colors"
                    >
                      Mark as Completed
                    </button>
                  )}
                  <button
                    onClick={() => setSelectedBooking(null)}
                    className="px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 transition-colors"
                  >
                    Close
                  </button>
                </div>
              </div>
            </>
          )}
        </DialogContent>
      </Dialog>

      {/* Destructive-action confirmation for cancel/reject */}
      <AlertDialog
        open={cancelConfirmId !== null}
        onOpenChange={(open) => !open && setCancelConfirmId(null)}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Cancel this booking?</AlertDialogTitle>
            <AlertDialogDescription>
              This will cancel the booking and notify the customer. This action
              cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={cancelOrderMutation.isPending}>
              Keep booking
            </AlertDialogCancel>
            <AlertDialogAction
              onClick={confirmCancel}
              disabled={cancelOrderMutation.isPending}
              className="bg-red-600 text-white hover:bg-red-700"
            >
              {cancelOrderMutation.isPending ? 'Cancelling…' : 'Yes, cancel'}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </AdminLayout>
  );
}
