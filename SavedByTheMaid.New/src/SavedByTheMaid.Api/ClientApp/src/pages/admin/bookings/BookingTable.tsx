import {
  Search,
  Eye,
  CheckCircle,
  XCircle,
  AlertCircle,
  ChevronLeft,
  ChevronRight,
} from 'lucide-react';
import { orderStatusConfig } from '@/shared/lib/status-config';
import type { OrderStatus } from '@/shared/lib/status-config';
import type { OrderSummary } from './types';
import { formatCurrency, formatDate } from './types';

interface BookingTableProps {
  paginatedBookings: OrderSummary[];
  filteredCount: number;
  searchTerm: string;
  onSearchChange: (term: string) => void;
  filterStatus: string;
  onFilterStatusChange: (status: string) => void;
  filterDate: string;
  onFilterDateChange: (date: string) => void;
  currentPage: number;
  totalPages: number;
  pageSize: number;
  hasNextPage: boolean;
  hasPrevPage: boolean;
  onNextPage: () => void;
  onPrevPage: () => void;
  onViewBooking: (booking: OrderSummary) => void;
  onUpdateStatus: (bookingId: number, status: OrderStatus) => void;
  onCancelOrder: (bookingId: number) => void;
}

export function BookingTable({
  paginatedBookings,
  filteredCount,
  searchTerm,
  onSearchChange,
  filterStatus,
  onFilterStatusChange,
  filterDate,
  onFilterDateChange,
  currentPage,
  totalPages,
  pageSize,
  hasNextPage,
  hasPrevPage,
  onNextPage,
  onPrevPage,
  onViewBooking,
  onUpdateStatus,
  onCancelOrder,
}: BookingTableProps) {
  return (
    <>
      {/* Filters */}
      <div className="flex flex-col sm:flex-row gap-4">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-gray-400" />
          <input
            type="search"
            placeholder="Search by name, ID or address..."
            value={searchTerm}
            onChange={(e) => onSearchChange(e.target.value)}
            className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
          />
        </div>
        <input
          type="date"
          value={filterDate}
          onChange={(e) => onFilterDateChange(e.target.value)}
          className="px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
        />
        <select
          value={filterStatus}
          onChange={(e) => onFilterStatusChange(e.target.value)}
          className="px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
        >
          <option value="all">All statuses</option>
          {Object.entries(orderStatusConfig).map(([status, config]) => (
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
                <th className="px-6 py-3 font-medium">Confirmation</th>
                <th className="px-6 py-3 font-medium">Customer</th>
                <th className="px-6 py-3 font-medium">Service</th>
                <th className="px-6 py-3 font-medium">Date</th>
                <th className="px-6 py-3 font-medium">Status</th>
                <th className="px-6 py-3 font-medium">Phone</th>
                <th className="px-6 py-3 font-medium text-right">Total</th>
                <th className="px-6 py-3 font-medium text-right">Actions</th>
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
                        orderStatusConfig[booking.orderStatus]?.bgColor || 'bg-gray-100'
                      } ${orderStatusConfig[booking.orderStatus]?.color || 'text-gray-700'}`}
                    >
                      {orderStatusConfig[booking.orderStatus]?.label || booking.orderStatus}
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
                        onClick={() => onViewBooking(booking)}
                        className="p-2 text-gray-400 hover:text-brand hover:bg-accent-light/10 rounded-lg"
                        title="View details"
                      >
                        <Eye className="h-4 w-4" />
                      </button>
                      {(booking.orderStatus === 'PendingReview' || booking.orderStatus === 'Draft') && (
                        <>
                          <button
                            onClick={() => onUpdateStatus(booking.id, 'Confirmed')}
                            className="p-2 text-gray-400 hover:text-green-600 hover:bg-green-50 rounded-lg"
                            title="Approve and Confirm"
                          >
                            <CheckCircle className="h-4 w-4" />
                          </button>
                          <button
                            onClick={() => onCancelOrder(booking.id)}
                            className="p-2 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded-lg"
                            title="Reject"
                          >
                            <XCircle className="h-4 w-4" />
                          </button>
                        </>
                      )}
                      {booking.orderStatus === 'Confirmed' && (
                        <button
                          onClick={() => onUpdateStatus(booking.id, 'InProgress')}
                          className="p-2 text-gray-400 hover:text-purple-600 hover:bg-purple-50 rounded-lg"
                          title="Start"
                        >
                          <AlertCircle className="h-4 w-4" />
                        </button>
                      )}
                      {booking.orderStatus === 'InProgress' && (
                        <button
                          onClick={() => onUpdateStatus(booking.id, 'Completed')}
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
              Showing {(currentPage - 1) * pageSize + 1} -{' '}
              {Math.min(currentPage * pageSize, filteredCount)} of{' '}
              {filteredCount}
            </p>
            <div className="flex gap-2">
              <button
                onClick={onPrevPage}
                disabled={!hasPrevPage}
                className="p-2 border rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
              >
                <ChevronLeft className="h-5 w-5" />
              </button>
              <button
                onClick={onNextPage}
                disabled={!hasNextPage}
                className="p-2 border rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
              >
                <ChevronRight className="h-5 w-5" />
              </button>
            </div>
          </div>
        )}
      </div>

      {filteredCount === 0 && (
        <div className="text-center py-12">
          <p className="text-gray-500">No bookings found</p>
        </div>
      )}
    </>
  );
}
