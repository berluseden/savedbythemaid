import {
  Calendar,
  Clock,
  MapPin,
  User,
  Phone,
  X,
  Users,
  CheckCircle,
  XCircle,
  PlayCircle,
} from 'lucide-react';
import { orderStatusConfig, meetStatusConfig } from '@/shared/lib/status-config';
import type { OrderStatus, MeetStatus } from '@/shared/lib/status-config';
import type { OrderSummary, MeetingSummary, Employee } from './types';
import { formatCurrency, formatDate, formatDateTime } from './types';
import { AssignEmployeeDialog, EmployeeDisplay } from './AssignEmployeeDialog';

interface BookingDetailPanelProps {
  booking: OrderSummary;
  meetings: MeetingSummary[];
  employees: Employee[];
  assigningMeetingId: number | null;
  onClose: () => void;
  onUpdateStatus: (bookingId: number, status: OrderStatus) => void;
  onCancelOrder: (bookingId: number) => void;
  onAssignEmployee: (meetingId: number, employeeId: number) => void;
  onUpdateMeetingStatus: (meetingId: number, status: MeetStatus) => void;
  onStartAssigning: (meetingId: number) => void;
  onCancelAssigning: () => void;
}

export function BookingDetailPanel({
  booking,
  meetings,
  employees,
  assigningMeetingId,
  onClose,
  onUpdateStatus,
  onCancelOrder,
  onAssignEmployee,
  onUpdateMeetingStatus,
  onStartAssigning,
  onCancelAssigning,
}: BookingDetailPanelProps) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-2xl max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between p-6 border-b">
          <div>
            <h2 className="text-xl font-semibold text-gray-900">
              {booking.confirmationNumber}
            </h2>
            <span
              className={`inline-block mt-1 px-2 py-1 rounded-full text-xs font-medium ${
                orderStatusConfig[booking.orderStatus]?.bgColor || 'bg-gray-100'
              } ${orderStatusConfig[booking.orderStatus]?.color || 'text-gray-700'}`}
            >
              {orderStatusConfig[booking.orderStatus]?.label || booking.orderStatus}
            </span>
          </div>
          <button
            onClick={onClose}
            className="p-2 text-gray-400 hover:text-gray-600"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        <div className="p-6 space-y-6">
          {/* Customer Info */}
          <div>
            <h3 className="text-sm font-medium text-gray-500 mb-3">Customer Information</h3>
            <div className="bg-gray-50 rounded-lg p-4 space-y-3">
              <div className="flex items-center gap-3">
                <User className="h-5 w-5 text-gray-400" />
                <span className="text-gray-900">{booking.contactName || 'No name'}</span>
              </div>
              <div className="flex items-center gap-3">
                <Phone className="h-5 w-5 text-gray-400" />
                <span className="text-gray-900">{booking.contactPhone || 'No phone'}</span>
              </div>
              <div className="flex items-start gap-3">
                <MapPin className="h-5 w-5 text-gray-400 mt-0.5" />
                <div>
                  <p className="text-gray-900">{booking.address}</p>
                  <p className="text-sm text-gray-500">{booking.city} - ZIP: {booking.zipCode}</p>
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
                <p className="font-medium text-gray-900">{booking.serviceTypeName || '-'}</p>
              </div>
              <div>
                <p className="text-xs text-gray-500">Service Area</p>
                <p className="font-medium text-gray-900">{booking.serviceAreaName || '-'}</p>
              </div>
              <div>
                <p className="text-xs text-gray-500">Recurrence</p>
                <p className="font-medium text-gray-900">{booking.recurrenceType}</p>
              </div>
              <div>
                <p className="text-xs text-gray-500">Total</p>
                <p className="font-medium text-gray-900 text-lg">{formatCurrency(booking.total)}</p>
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
                  <p className="font-medium text-gray-900">{formatDate(booking.createdAt)}</p>
                </div>
              </div>
              <div className="flex items-center gap-3">
                <Clock className="h-5 w-5 text-gray-400" />
                <div>
                  <p className="text-xs text-gray-500">Payment Status</p>
                  <p className="font-medium text-gray-900">{booking.paymentStatus}</p>
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
                    {assigningMeetingId === meeting.id ? (
                      <AssignEmployeeDialog
                        meeting={meeting}
                        employees={employees}
                        onAssign={onAssignEmployee}
                        onCancel={onCancelAssigning}
                      />
                    ) : (
                      <EmployeeDisplay
                        meeting={meeting}
                        onStartAssign={() => onStartAssigning(meeting.id)}
                      />
                    )}

                    {/* Meeting Actions */}
                    {meeting.status !== 'Completed' && meeting.status !== 'Cancelled' && (
                      <div className="pt-3 border-t border-gray-200 flex gap-2">
                        {meeting.status === 'Assigned' && (
                          <button
                            onClick={() => onUpdateMeetingStatus(meeting.id, 'InProgress')}
                            className="flex-1 px-3 py-1.5 bg-purple-500 text-white rounded text-sm hover:bg-purple-600 flex items-center justify-center gap-1"
                          >
                            <PlayCircle className="h-4 w-4" />
                            Start
                          </button>
                        )}
                        {meeting.status === 'InProgress' && (
                          <button
                            onClick={() => onUpdateMeetingStatus(meeting.id, 'Completed')}
                            className="flex-1 px-3 py-1.5 bg-green-500 text-white rounded text-sm hover:bg-green-600 flex items-center justify-center gap-1"
                          >
                            <CheckCircle className="h-4 w-4" />
                            Complete
                          </button>
                        )}
                        {(meeting.status === 'Scheduled' || meeting.status === 'Assigned') && (
                          <button
                            onClick={() => onUpdateMeetingStatus(meeting.id, 'Cancelled')}
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
            {booking.orderStatus === 'PendingReview' && (
              <>
                <button
                  onClick={() => onUpdateStatus(booking.id, 'Confirmed')}
                  className="flex-1 px-4 py-2 bg-green-500 text-white rounded-lg hover:bg-green-600 transition-colors"
                >
                  Confirm Booking
                </button>
                <button
                  onClick={() => onCancelOrder(booking.id)}
                  className="flex-1 px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600 transition-colors"
                >
                  Cancel Booking
                </button>
              </>
            )}
            {booking.orderStatus === 'Confirmed' && (
              <button
                onClick={() => onUpdateStatus(booking.id, 'InProgress')}
                className="flex-1 px-4 py-2 bg-purple-500 text-white rounded-lg hover:bg-purple-600 transition-colors"
              >
                Start Service
              </button>
            )}
            {booking.orderStatus === 'InProgress' && (
              <button
                onClick={() => onUpdateStatus(booking.id, 'Completed')}
                className="flex-1 px-4 py-2 bg-green-500 text-white rounded-lg hover:bg-green-600 transition-colors"
              >
                Mark as Completed
              </button>
            )}
            <button
              onClick={onClose}
              className="px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 transition-colors"
            >
              Close
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
