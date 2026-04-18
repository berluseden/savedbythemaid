import { useQuery, useMutation, useQueryClient, queryOptions } from '@tanstack/react-query';
import api from '@/lib/api';
import { queryKeys } from '@/shared/lib/query-keys';
import type { OrderSummary, MeetingSummary, Employee } from './types';

type OrderStatus = OrderSummary['orderStatus'];
type MeetStatus = MeetingSummary['status'];

// ----- Query option factories -----
// Following the 2026 TanStack Query pattern: queryOptions() lets the same
// definition be reused across useQuery/useSuspenseQuery/prefetch/ensureQueryData.

export const adminBookingsQuery = () =>
  queryOptions({
    queryKey: queryKeys.admin.bookings.list({ pageSize: 100 }),
    queryFn: async () => {
      const res = await api.get<OrderSummary[]>('/admin/orders', {
        params: { pageSize: '100' },
      });
      return res.data;
    },
  });

export const adminEmployeesQuery = () =>
  queryOptions({
    queryKey: queryKeys.admin.employees.list(),
    queryFn: async () => {
      const res = await api.get<Employee[]>('/admin/employees');
      return res.data.filter((e) => e.isActive);
    },
  });

export const orderMeetingsQuery = (orderId: number | null) =>
  queryOptions({
    queryKey: queryKeys.admin.bookings.meetings(orderId ?? -1),
    queryFn: async () => {
      const res = await api.get<MeetingSummary[]>(
        `/admin/orders/meetings?orderId=${orderId}`
      );
      return res.data;
    },
    enabled: orderId !== null,
  });

// ----- Hooks -----

export function useAdminBookings() {
  return useQuery(adminBookingsQuery());
}

export function useActiveEmployees() {
  return useQuery(adminEmployeesQuery());
}

export function useOrderMeetings(orderId: number | null) {
  return useQuery(orderMeetingsQuery(orderId));
}

// ----- Mutations -----

export function useUpdateBookingStatus() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ bookingId, status }: { bookingId: number; status: OrderStatus }) =>
      api.put(`/admin/orders/${bookingId}/status`, { orderStatus: status }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.admin.bookings.all });
    },
  });
}

export function useCancelOrder() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (bookingId: number) =>
      api.post(`/admin/orders/${bookingId}/cancel`, {
        reason: 'Cancelled by administrator',
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.admin.bookings.all });
    },
  });
}

export function useAssignEmployee() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ meetingId, employeeId }: { meetingId: number; employeeId: number }) =>
      api.put(`/admin/orders/meetings/${meetingId}/assign`, { employeeId }),
    onSuccess: (_data, variables, context) => {
      void variables;
      void context;
      queryClient.invalidateQueries({ queryKey: queryKeys.admin.bookings.all });
    },
  });
}

export function useUpdateMeetingStatus() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ meetingId, status }: { meetingId: number; status: MeetStatus }) =>
      api.put(`/admin/orders/meetings/${meetingId}/status`, { status }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.admin.bookings.all });
    },
  });
}
