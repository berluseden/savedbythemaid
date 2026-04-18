import { useState, useCallback, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useAuth } from '@/contexts/AuthContext';
import { Calendar, Clock, MapPin, CreditCard, X, AlertCircle, Sparkles, ChevronRight, History, CalendarDays, Home, FileText, Zap } from 'lucide-react';
import { Link } from 'react-router-dom';
import api from '@/lib/api';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from '@/shared/components/ui/dialog';
import { AlertDialog, AlertDialogContent, AlertDialogHeader, AlertDialogTitle, AlertDialogDescription, AlertDialogFooter, AlertDialogAction, AlertDialogCancel } from '@/shared/components/ui/alert-dialog';

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
  city?: string;
  zipCode?: string;
  specialInstructions?: string;
  recurrenceType?: string;
  createdAt?: string;
}

interface CustomerStats {
  completedBookings: number;
  totalSpent: number;
  nextBooking: string | null;
}

interface AvailableSlot {
  time: string;
  available: boolean;
  formattedTime: string;
}

interface SuggestedSlot {
  date: string;
  time: string;
  label: string;
  dateLabel: string;
}

const statusConfig: Record<string, { label: string; color: string; bgColor: string }> = {
  PendingReview: { label: 'Pending Review', color: 'text-yellow-700', bgColor: 'bg-yellow-100' },
  Confirmed: { label: 'Confirmed', color: 'text-blue-700', bgColor: 'bg-blue-100' },
  InProgress: { label: 'In Progress', color: 'text-purple-700', bgColor: 'bg-purple-100' },
  Completed: { label: 'Completed', color: 'text-green-700', bgColor: 'bg-green-100' },
  Cancelled: { label: 'Cancelled', color: 'text-red-700', bgColor: 'bg-red-100' },
  NoShow: { label: 'No Show', color: 'text-orange-700', bgColor: 'bg-orange-100' },
};

type TabType = 'upcoming' | 'history';

export function UserDashboardPage() {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState<TabType>('upcoming');
  
  const [cancellingId, setCancellingId] = useState<number | null>(null);
  const [cancelReason, setCancelReason] = useState('');
  const [showCancelModal, setShowCancelModal] = useState(false);
  
  const [selectedBooking, setSelectedBooking] = useState<CustomerOrder | null>(null);
  const [showDetailModal, setShowDetailModal] = useState(false);
  
  const [showRescheduleModal, setShowRescheduleModal] = useState(false);
  const [rescheduleBookingId, setRescheduleBookingId] = useState<number | null>(null);
  const [rescheduleDate, setRescheduleDate] = useState('');
  const [rescheduleTime, setRescheduleTime] = useState('');
  const [availableSlots, setAvailableSlots] = useState<AvailableSlot[]>([]);
  const [isLoadingSlots, setIsLoadingSlots] = useState(false);
  const [isRescheduling, setIsRescheduling] = useState(false);
  const [dismissedError, setDismissedError] = useState(false);
  const [suggestedSlots, setSuggestedSlots] = useState<SuggestedSlot[]>([]);
  const [isLoadingSuggestions, setIsLoadingSuggestions] = useState(false);
  const [showFullDatePicker, setShowFullDatePicker] = useState(false);
  const [confirmingSuggestion, setConfirmingSuggestion] = useState<SuggestedSlot | null>(null);

  const [page, setPage] = useState(1);
  const PAGE_SIZE = 20;

  const { data: paginatedData, isLoading: loadingBookings, error: bookingsError } = useQuery({
    queryKey: ['customer', 'orders', page],
    queryFn: async () => {
      const res = await api.get<{ items: CustomerOrder[]; totalCount: number }>(
        `/customer/my-orders?pageSize=${PAGE_SIZE}&page=${page}`
      );
      return res.data;
    },
  });

  // Accumulate items across pages for load-more behavior
  const [accumulatedBookings, setAccumulatedBookings] = useState<CustomerOrder[]>([]);
  useEffect(() => {
    if (paginatedData?.items) {
      if (page === 1) {
        setAccumulatedBookings(paginatedData.items);
      } else {
        setAccumulatedBookings((prev) => {
          const existingIds = new Set(prev.map((b) => b.id));
          const newItems = paginatedData.items.filter((b) => !existingIds.has(b.id));
          return [...prev, ...newItems];
        });
      }
    }
  }, [paginatedData, page]);

  const allBookings = accumulatedBookings;
  const hasMore = paginatedData ? allBookings.length < paginatedData.totalCount : false;

  const { data: stats = null, isLoading: loadingStats } = useQuery({
    queryKey: ['customer', 'stats'],
    queryFn: async () => {
      const res = await api.get<CustomerStats>('/customer/stats');
      return res.data;
    },
  });

  const isLoading = loadingBookings || loadingStats;
  const error = bookingsError ? 'Failed to load dashboard data' : '';

  // Include PendingReview so a newly created booking is visible in the Upcoming tab
  const upcomingBookings = allBookings.filter(
    o => o.status === 'Confirmed' || o.status === 'InProgress' || o.status === 'PendingReview'
  );
  const pastBookings = allBookings.filter(o => o.status === 'Completed' || o.status === 'Cancelled' || o.status === 'NoShow');

  const cancelMutation = useMutation({
    mutationFn: async ({ id, reason }: { id: number; reason: string }) => {
      await api.post(`/customer/my-orders/${id}/cancel`, { reason });
    },
    onSuccess: () => {
      setShowCancelModal(false);
      setCancellingId(null);
      setCancelReason('');
      queryClient.invalidateQueries({ queryKey: ['customer', 'orders'] });
      queryClient.invalidateQueries({ queryKey: ['customer', 'stats'] });
    },
  });

  const handleCancelOrder = async () => {
    if (!cancellingId) return;
    cancelMutation.mutate({ id: cancellingId, reason: cancelReason });
  };

  const getNextDays = (count: number): string[] => {
    const days: string[] = [];
    for (let i = 1; i <= count; i++) {
      const d = new Date();
      d.setDate(d.getDate() + i);
      days.push(d.toISOString().split('T')[0]);
    }
    return days;
  };

  const formatSuggestionDateLabel = (dateStr: string): string => {
    const date = new Date(dateStr + 'T12:00:00');
    const today = new Date();
    const tomorrow = new Date();
    tomorrow.setDate(today.getDate() + 1);
    if (date.toDateString() === tomorrow.toDateString()) return 'Tomorrow';
    return date.toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' });
  };

  const formatSlotTimeShort = (time: string): string => {
    const [hours, minutes] = time.split(':');
    const hour = parseInt(hours);
    const suffix = hour >= 12 ? 'PM' : 'AM';
    const displayHour = hour % 12 || 12;
    return minutes === '00' ? `${displayHour} ${suffix}` : `${displayHour}:${minutes} ${suffix}`;
  };

  const fetchSuggestedSlots = useCallback(async (booking: CustomerOrder) => {
    if (!booking.zipCode) return;
    setIsLoadingSuggestions(true);
    setSuggestedSlots([]);

    const originalHour = booking.scheduledTime ? parseInt(booking.scheduledTime.split(':')[0]) : 10;
    const nextDays = getNextDays(3);

    try {
      const results = await Promise.all(
        nextDays.map(date =>
          api.post<{ slots: AvailableSlot[] }>('/booking/availability', {
            zipCode: booking.zipCode,
            date,
            estimatedMinutes: booking.estimatedDuration || 120,
          }).then(res => ({ date, slots: res.data.slots || [] }))
           .catch(() => ({ date, slots: [] as AvailableSlot[] }))
        )
      );

      const suggestions: SuggestedSlot[] = [];
      for (const { date, slots } of results) {
        const available = slots.filter(s => s.available);
        if (available.length === 0) continue;

        // Sort by closeness to original booking time
        const sorted = [...available].sort((a, b) => {
          const aHour = parseInt(a.time.split(':')[0]);
          const bHour = parseInt(b.time.split(':')[0]);
          return Math.abs(aHour - originalHour) - Math.abs(bHour - originalHour);
        });

        // Take top 2 from each day (closest to original time)
        const picks = sorted.slice(0, 2);
        for (const slot of picks) {
          const dateLabel = formatSuggestionDateLabel(date);
          suggestions.push({
            date,
            time: slot.time,
            label: `${dateLabel} ${formatSlotTimeShort(slot.time)}`,
            dateLabel,
          });
        }
      }

      // Limit to 5 suggestions, prioritizing variety across days
      const finalSuggestions: SuggestedSlot[] = [];
      const usedDays = new Set<string>();

      // First pass: one per day (closest to original time)
      for (const s of suggestions) {
        if (!usedDays.has(s.date) && finalSuggestions.length < 5) {
          finalSuggestions.push(s);
          usedDays.add(s.date);
        }
      }
      // Second pass: fill remaining from leftover
      for (const s of suggestions) {
        if (finalSuggestions.length >= 5) break;
        if (!finalSuggestions.includes(s)) {
          finalSuggestions.push(s);
        }
      }

      setSuggestedSlots(finalSuggestions);
    } catch {
      setSuggestedSlots([]);
    } finally {
      setIsLoadingSuggestions(false);
    }
  }, []);

  const openRescheduleModal = (booking: CustomerOrder) => {
    setRescheduleBookingId(booking.id);
    setRescheduleDate('');
    setRescheduleTime('');
    setAvailableSlots([]);
    setSuggestedSlots([]);
    setShowFullDatePicker(false);
    setConfirmingSuggestion(null);
    setShowRescheduleModal(true);
    fetchSuggestedSlots(booking);
  };

  const handleSuggestionClick = (suggestion: SuggestedSlot) => {
    setConfirmingSuggestion(suggestion);
  };

  const confirmSuggestion = async () => {
    if (!confirmingSuggestion || !rescheduleBookingId) return;
    setIsRescheduling(true);
    try {
      await api.post(`/customer/my-orders/${rescheduleBookingId}/reschedule`, {
        newDate: confirmingSuggestion.date,
        newTime: confirmingSuggestion.time,
      });
      setShowRescheduleModal(false);
      setRescheduleBookingId(null);
      setConfirmingSuggestion(null);
      queryClient.invalidateQueries({ queryKey: ['customer', 'orders'] });
      queryClient.invalidateQueries({ queryKey: ['customer', 'stats'] });
    } catch {
      // Error handled by API interceptor
    } finally {
      setIsRescheduling(false);
    }
  };

  const fetchAvailableSlots = async (date: string) => {
    if (!date || !rescheduleBookingId) return;
    setIsLoadingSlots(true);
    try {
      const booking = allBookings.find(b => b.id === rescheduleBookingId);
      if (!booking) return;
      if (!booking.zipCode) {
        setAvailableSlots([]);
        return;
      }
      const response = await api.post<{ slots: AvailableSlot[] }>('/booking/availability', {
        zipCode: booking.zipCode,
        date,
        estimatedMinutes: booking.estimatedDuration || 120,
      });
      setAvailableSlots(response.data.slots || []);
    } catch { setAvailableSlots([]); }
    finally { setIsLoadingSlots(false); }
  };

  const handleReschedule = async () => {
    if (!rescheduleBookingId || !rescheduleDate || !rescheduleTime) return;
    setIsRescheduling(true);
    try {
      await api.post(`/customer/my-orders/${rescheduleBookingId}/reschedule`, { newDate: rescheduleDate, newTime: rescheduleTime });
      setShowRescheduleModal(false);
      setRescheduleBookingId(null);
      queryClient.invalidateQueries({ queryKey: ['customer', 'orders'] });
      queryClient.invalidateQueries({ queryKey: ['customer', 'stats'] });
    } catch (err: unknown) {
      // Error handled by API interceptor
    } finally { setIsRescheduling(false); }
  };

  const formatDate = (dateStr: string) => {
    if (!dateStr) return 'N/A';
    return new Date(dateStr).toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' });
  };
  const formatFullDate = (dateStr: string) => {
    if (!dateStr) return 'N/A';
    return new Date(dateStr).toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' });
  };
  const formatTime = (timeStr: string) => {
    if (!timeStr) return 'N/A';
    const [hours, minutes] = timeStr.split(':');
    const hour = parseInt(hours);
    return `${hour % 12 || 12}:${minutes} ${hour >= 12 ? 'PM' : 'AM'}`;
  };
  const openCancelModal = (id: number) => { setCancellingId(id); setCancelReason(''); setShowCancelModal(true); };
  const openDetailModal = (booking: CustomerOrder) => { setSelectedBooking(booking); setShowDetailModal(true); };
  const getMinDate = () => { const d = new Date(); d.setDate(d.getDate() + 1); return d.toISOString().split('T')[0]; };

  if (isLoading) return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center">
      <div className="text-center">
        <div className="w-8 h-8 border-4 border-[#2196f3] border-t-transparent rounded-full animate-spin mx-auto mb-4" />
        <p className="text-gray-600">Loading your dashboard...</p>
      </div>
    </div>
  );

  const renderBookingCard = (booking: CustomerOrder, showActions: boolean = true) => {
    const status = statusConfig[booking.status] || statusConfig.Confirmed;
    return (
      <div key={booking.id} className="p-6 hover:bg-gray-50 transition-colors cursor-pointer" onClick={() => openDetailModal(booking)}>
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div className="flex-1">
            <div className="flex items-center gap-3 mb-2">
              <h3 className="font-semibold text-gray-900">{booking.serviceTypeName}</h3>
              <span className={`px-2 py-1 rounded-full text-xs font-medium ${status.color} ${status.bgColor}`}>{status.label}</span>
            </div>
            <div className="flex flex-wrap items-center gap-4 text-sm text-gray-600">
              <div className="flex items-center gap-1"><Calendar className="w-4 h-4" />{formatDate(booking.scheduledDate)}</div>
              <div className="flex items-center gap-1"><Clock className="w-4 h-4" />{formatTime(booking.scheduledTime)}</div>
              <div className="flex items-center gap-1"><MapPin className="w-4 h-4" />{booking.address || booking.cleaningPlaceName}</div>
            </div>
          </div>
          <div className="flex items-center gap-4">
            <div className="text-right">
              <p className="text-lg font-bold text-gray-900">${booking.totalAmount?.toFixed(2) || '0.00'}</p>
              <p className="text-sm text-gray-500">{booking.estimatedDuration} min</p>
            </div>
            {showActions && booking.status === 'Confirmed' && (
              <div className="flex gap-2" onClick={(e) => e.stopPropagation()}>
                <button onClick={() => openRescheduleModal(booking)} className="px-3 py-1 text-sm text-[#2196f3] border border-[#b8e07c] rounded-lg hover:bg-[#b8e07c]/10">Reschedule</button>
                <button onClick={() => openCancelModal(booking.id)} className="px-3 py-1 text-sm text-red-600 border border-red-300 rounded-lg hover:bg-red-50">Cancel</button>
              </div>
            )}
            <ChevronRight className="w-5 h-5 text-gray-400" />
          </div>
        </div>
      </div>
    );
  };

  return (
    <div className="min-h-screen bg-gray-50 py-8">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900">Welcome back{user?.firstName ? `, ${user.firstName}` : ''}!</h1>
          <p className="text-gray-600 mt-1">Manage your bookings and account</p>
        </div>

        {error && !dismissedError && (
          <div className="mb-6 bg-red-50 border border-red-200 rounded-lg p-4 flex items-center gap-3">
            <AlertCircle className="w-5 h-5 text-red-500" /><p className="text-red-700">{error}</p>
            <button onClick={() => setDismissedError(true)} className="ml-auto text-red-500"><X className="w-4 h-4" /></button>
          </div>
        )}

        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6 mb-8">
          <div className="bg-white rounded-xl shadow-sm p-6 border border-gray-200">
            <div className="flex items-center gap-4">
              <div className="w-12 h-12 bg-green-100 rounded-xl flex items-center justify-center"><Calendar className="w-6 h-6 text-green-600" /></div>
              <div><p className="text-sm text-gray-500">Completed</p><p className="text-2xl font-bold text-gray-900">{stats?.completedBookings ?? 0}</p></div>
            </div>
          </div>
          <div className="bg-white rounded-xl shadow-sm p-6 border border-gray-200">
            <div className="flex items-center gap-4">
              <div className="w-12 h-12 bg-[#b8e07c]/20 rounded-xl flex items-center justify-center"><CreditCard className="w-6 h-6 text-[#2196f3]" /></div>
              <div><p className="text-sm text-gray-500">Total Spent</p><p className="text-2xl font-bold text-gray-900">${stats?.totalSpent?.toFixed(2) ?? '0.00'}</p></div>
            </div>
          </div>
          <div className="bg-white rounded-xl shadow-sm p-6 border border-gray-200">
            <div className="flex items-center gap-4">
              <div className="w-12 h-12 bg-purple-100 rounded-xl flex items-center justify-center"><Clock className="w-6 h-6 text-purple-600" /></div>
              <div><p className="text-sm text-gray-500">Next Booking</p><p className="text-lg font-bold text-gray-900">{stats?.nextBooking ? formatDate(stats.nextBooking) : 'None'}</p></div>
            </div>
          </div>
        </div>

        <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
          <div className="border-b border-gray-200 flex">
            <button onClick={() => setActiveTab('upcoming')} className={`flex-1 px-6 py-4 text-sm font-medium flex items-center justify-center gap-2 ${activeTab === 'upcoming' ? 'text-[#2196f3] border-b-2 border-[#2196f3] bg-[#b8e07c]/10' : 'text-gray-500 hover:text-gray-700'}`}>
              <CalendarDays className="w-4 h-4" />Upcoming ({upcomingBookings.length})
            </button>
            <button onClick={() => setActiveTab('history')} className={`flex-1 px-6 py-4 text-sm font-medium flex items-center justify-center gap-2 ${activeTab === 'history' ? 'text-[#2196f3] border-b-2 border-[#2196f3] bg-[#b8e07c]/10' : 'text-gray-500 hover:text-gray-700'}`}>
              <History className="w-4 h-4" />History ({pastBookings.length})
            </button>
          </div>

          {activeTab === 'upcoming' && (upcomingBookings.length === 0 ? (
            <div className="p-8 text-center">
              <Calendar className="w-12 h-12 text-gray-300 mx-auto mb-4" /><p className="text-gray-500 mb-4">No upcoming bookings</p>
              <Link to="/booking" className="inline-flex items-center px-4 py-2 bg-[#2196f3] text-white rounded-lg hover:bg-[#29338c]">Book Your First Cleaning</Link>
            </div>
          ) : <div className="divide-y divide-gray-200">{upcomingBookings.map(b => renderBookingCard(b, true))}</div>)}

          {activeTab === 'history' && (pastBookings.length === 0 ? (
            <div className="p-8 text-center"><History className="w-12 h-12 text-gray-300 mx-auto mb-4" /><p className="text-gray-500">No booking history yet</p></div>
          ) : <div className="divide-y divide-gray-200">{pastBookings.map(b => renderBookingCard(b, false))}</div>)}

          {/* Load more button */}
          {hasMore && (
            <div className="p-4 text-center border-t border-gray-200">
              <button
                onClick={() => setPage((p) => p + 1)}
                disabled={loadingBookings}
                className="px-6 py-2 text-sm font-medium text-brand border border-brand rounded-lg hover:bg-brand/5 transition-colors disabled:opacity-50"
              >
                {loadingBookings ? 'Loading...' : 'Load more'}
              </button>
            </div>
          )}
        </div>

        <div className="mt-8 grid grid-cols-1 sm:grid-cols-3 gap-4">
          <Link to="/booking" className="bg-[#2196f3] text-white rounded-xl p-6 text-center hover:bg-[#29338c]"><Calendar className="w-8 h-8 mx-auto mb-2" /><p className="font-semibold">Book Cleaning</p></Link>
          <Link to="/profile" className="bg-white border border-gray-200 rounded-xl p-6 text-center hover:bg-gray-50"><Sparkles className="w-8 h-8 mx-auto mb-2 text-gray-600" /><p className="font-semibold text-gray-900">My Profile</p></Link>
          <Link to="/contact" className="bg-white border border-gray-200 rounded-xl p-6 text-center hover:bg-gray-50"><MapPin className="w-8 h-8 mx-auto mb-2 text-gray-600" /><p className="font-semibold text-gray-900">Contact Support</p></Link>
        </div>
      </div>

      <Dialog open={showDetailModal && !!selectedBooking} onOpenChange={(open) => !open && setShowDetailModal(false)}>
        <DialogContent className="max-w-lg">
          <DialogHeader>
            <DialogTitle>Booking Details</DialogTitle>
          </DialogHeader>
          {selectedBooking && (
            <div className="space-y-6">
              <div className="flex items-center justify-between">
                <span className="text-sm text-gray-500">Status</span>
                <span className={`px-3 py-1 rounded-full text-sm font-medium ${statusConfig[selectedBooking.status]?.color} ${statusConfig[selectedBooking.status]?.bgColor}`}>{statusConfig[selectedBooking.status]?.label}</span>
              </div>
              <div className="bg-gray-50 rounded-lg p-4 space-y-3">
                <div className="flex items-center gap-3"><Sparkles className="w-5 h-5 text-[#2196f3]" /><div><p className="text-sm text-gray-500">Service</p><p className="font-medium text-gray-900">{selectedBooking.serviceTypeName}</p></div></div>
                <div className="flex items-center gap-3"><Home className="w-5 h-5 text-[#2196f3]" /><div><p className="text-sm text-gray-500">Property</p><p className="font-medium text-gray-900">{selectedBooking.cleaningPlaceName}</p></div></div>
              </div>
              <div className="bg-gray-50 rounded-lg p-4 space-y-3">
                <div className="flex items-center gap-3"><Calendar className="w-5 h-5 text-[#2196f3]" /><div><p className="text-sm text-gray-500">Date</p><p className="font-medium text-gray-900">{formatFullDate(selectedBooking.scheduledDate)}</p></div></div>
                <div className="flex items-center gap-3"><Clock className="w-5 h-5 text-[#2196f3]" /><div><p className="text-sm text-gray-500">Time</p><p className="font-medium text-gray-900">{formatTime(selectedBooking.scheduledTime)} ({selectedBooking.estimatedDuration} min)</p></div></div>
              </div>
              <div className="bg-gray-50 rounded-lg p-4">
                <div className="flex items-start gap-3"><MapPin className="w-5 h-5 text-[#2196f3] mt-0.5" /><div><p className="text-sm text-gray-500">Address</p><p className="font-medium text-gray-900">{selectedBooking.address}</p>{selectedBooking.city && <p className="text-gray-600">{selectedBooking.city}, {selectedBooking.zipCode}</p>}</div></div>
              </div>
              {selectedBooking.specialInstructions && (
                <div className="bg-gray-50 rounded-lg p-4"><div className="flex items-start gap-3"><FileText className="w-5 h-5 text-[#2196f3] mt-0.5" /><div><p className="text-sm text-gray-500">Special Instructions</p><p className="font-medium text-gray-900">{selectedBooking.specialInstructions}</p></div></div></div>
              )}
              <div className="border-t border-gray-200 pt-4 flex items-center justify-between">
                <span className="text-lg font-semibold text-gray-900">Total</span>
                <span className="text-2xl font-bold text-[#2196f3]">${selectedBooking.totalAmount?.toFixed(2)}</span>
              </div>
              {selectedBooking.status === 'Confirmed' && (
                <div className="flex gap-3">
                  <button onClick={() => { setShowDetailModal(false); openRescheduleModal(selectedBooking); }} className="flex-1 px-4 py-2 bg-[#2196f3] text-white rounded-lg hover:bg-[#29338c]">Reschedule</button>
                  <button onClick={() => { setShowDetailModal(false); openCancelModal(selectedBooking.id); }} className="flex-1 px-4 py-2 border border-red-300 text-red-600 rounded-lg hover:bg-red-50">Cancel</button>
                </div>
              )}
            </div>
          )}
        </DialogContent>
      </Dialog>

      <Dialog open={showRescheduleModal} onOpenChange={(open) => { if (!open) { setShowRescheduleModal(false); setConfirmingSuggestion(null); } }}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Reschedule Booking</DialogTitle>
            <DialogDescription>
              {confirmingSuggestion
                ? 'Confirm your new time'
                : 'Pick a suggested time or choose your own.'}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            {/* Confirmation view for a suggested slot */}
            {confirmingSuggestion ? (
              <div className="space-y-4">
                <div className="bg-blue-50 border border-blue-200 rounded-xl p-4 text-center">
                  <Calendar className="w-8 h-8 text-[#2196f3] mx-auto mb-2" />
                  <p className="text-lg font-semibold text-gray-900">
                    {confirmingSuggestion.label}
                  </p>
                  <p className="text-sm text-gray-500 mt-1">
                    {formatFullDate(confirmingSuggestion.date)}
                  </p>
                </div>
                <div className="flex gap-3">
                  <button
                    onClick={() => setConfirmingSuggestion(null)}
                    className="flex-1 px-4 py-2 border border-gray-300 rounded-lg text-gray-700 hover:bg-gray-50"
                  >
                    Back
                  </button>
                  <button
                    onClick={confirmSuggestion}
                    disabled={isRescheduling}
                    className="flex-1 px-4 py-2 bg-[#2196f3] text-white rounded-lg hover:bg-[#29338c] disabled:opacity-50"
                  >
                    {isRescheduling ? 'Rescheduling...' : 'Confirm Reschedule'}
                  </button>
                </div>
              </div>
            ) : (
              <>
                {/* Suggested quick-pick slots */}
                {!showFullDatePicker && (
                  <div>
                    <div className="flex items-center gap-2 mb-3">
                      <Zap className="w-4 h-4 text-amber-500" />
                      <span className="text-sm font-medium text-gray-700">Quick reschedule</span>
                    </div>
                    {isLoadingSuggestions ? (
                      <div className="text-center py-6">
                        <div className="w-6 h-6 border-2 border-[#2196f3] border-t-transparent rounded-full animate-spin mx-auto mb-2" />
                        <p className="text-xs text-gray-400">Finding available times...</p>
                      </div>
                    ) : suggestedSlots.length === 0 ? (
                      <p className="text-sm text-gray-500 py-4 text-center">No quick suggestions available. Pick a date below.</p>
                    ) : (
                      <div className="flex flex-wrap gap-2">
                        {suggestedSlots.map((suggestion, idx) => (
                          <button
                            key={`${suggestion.date}-${suggestion.time}-${idx}`}
                            onClick={() => handleSuggestionClick(suggestion)}
                            className="inline-flex items-center gap-1.5 px-3 py-2 text-sm font-medium rounded-full border border-[#b8e07c] bg-white text-gray-800 hover:bg-[#b8e07c]/20 hover:border-[#2196f3] transition-colors"
                          >
                            <Clock className="w-3.5 h-3.5 text-[#2196f3]" />
                            {suggestion.label}
                          </button>
                        ))}
                      </div>
                    )}
                  </div>
                )}

                {/* Divider */}
                {!showFullDatePicker && (
                  <div className="relative py-2">
                    <div className="absolute inset-0 flex items-center">
                      <div className="w-full border-t border-gray-200" />
                    </div>
                    <div className="relative flex justify-center">
                      <button
                        onClick={() => setShowFullDatePicker(true)}
                        className="bg-white px-3 py-1 text-sm text-[#2196f3] hover:text-[#29338c] font-medium flex items-center gap-1"
                      >
                        <CalendarDays className="w-4 h-4" />
                        Pick a different date
                      </button>
                    </div>
                  </div>
                )}

                {/* Full date picker (fallback) */}
                {showFullDatePicker && (
                  <>
                    {suggestedSlots.length > 0 && (
                      <button
                        onClick={() => setShowFullDatePicker(false)}
                        className="text-sm text-[#2196f3] hover:text-[#29338c] font-medium flex items-center gap-1"
                      >
                        <Zap className="w-3.5 h-3.5" />
                        Back to suggestions
                      </button>
                    )}
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-2">New Date</label>
                      <input
                        type="date"
                        value={rescheduleDate}
                        min={getMinDate()}
                        onChange={(e) => { setRescheduleDate(e.target.value); setRescheduleTime(''); fetchAvailableSlots(e.target.value); }}
                        className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#2196f3]"
                      />
                    </div>
                    {rescheduleDate && (
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-2">New Time</label>
                        {isLoadingSlots ? (
                          <div className="text-center py-4">
                            <div className="w-6 h-6 border-2 border-[#2196f3] border-t-transparent rounded-full animate-spin mx-auto" />
                          </div>
                        ) : availableSlots.length === 0 ? (
                          <p className="text-sm text-gray-500 py-4 text-center">No times available. Try another date.</p>
                        ) : (
                          <div className="grid grid-cols-3 gap-2">
                            {availableSlots.filter(s => s.available).map(slot => (
                              <button
                                key={slot.time}
                                onClick={() => setRescheduleTime(slot.time)}
                                className={`p-2 text-sm rounded-lg border ${rescheduleTime === slot.time ? 'border-[#2196f3] bg-[#b8e07c]/10 text-[#29338c]' : 'border-gray-200 hover:border-gray-300'}`}
                              >
                                {slot.formattedTime}
                              </button>
                            ))}
                          </div>
                        )}
                      </div>
                    )}
                    <div className="flex gap-3 pt-2">
                      <button onClick={() => setShowRescheduleModal(false)} className="flex-1 px-4 py-2 border border-gray-300 rounded-lg text-gray-700 hover:bg-gray-50">Cancel</button>
                      <button onClick={handleReschedule} disabled={!rescheduleDate || !rescheduleTime || isRescheduling} className="flex-1 px-4 py-2 bg-[#2196f3] text-white rounded-lg hover:bg-[#29338c] disabled:opacity-50">{isRescheduling ? 'Rescheduling...' : 'Confirm'}</button>
                    </div>
                  </>
                )}

                {/* Cancel button when in suggestions view */}
                {!showFullDatePicker && (
                  <div className="pt-1">
                    <button onClick={() => setShowRescheduleModal(false)} className="w-full px-4 py-2 border border-gray-300 rounded-lg text-gray-700 hover:bg-gray-50 text-sm">Cancel</button>
                  </div>
                )}
              </>
            )}
          </div>
        </DialogContent>
      </Dialog>

      <AlertDialog open={showCancelModal} onOpenChange={(open) => !open && setShowCancelModal(false)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Cancel Booking</AlertDialogTitle>
            <AlertDialogDescription>Are you sure? This cannot be undone.</AlertDialogDescription>
          </AlertDialogHeader>
          <div className="mb-4">
            <label className="block text-sm font-medium text-gray-700 mb-2">Reason (optional)</label>
            <textarea value={cancelReason} onChange={(e) => setCancelReason(e.target.value)} className="w-full px-3 py-2 border border-gray-300 rounded-lg" rows={3} placeholder="Tell us why..." />
          </div>
          <AlertDialogFooter className="flex gap-3 sm:space-x-0">
            <AlertDialogCancel className="flex-1" onClick={() => setShowCancelModal(false)}>Keep Booking</AlertDialogCancel>
            <AlertDialogAction className="flex-1" onClick={handleCancelOrder}>Cancel Booking</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}

export default UserDashboardPage;
