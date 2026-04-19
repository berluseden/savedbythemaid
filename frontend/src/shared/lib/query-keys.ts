/**
 * Centralized TanStack Query key factory.
 *
 * Pattern follows the 2026 community standard (TkDodo "Effective Query Keys"):
 *  - Hierarchical: each scope exposes `all` + parameterized variants
 *  - Always arrays
 *  - Factory functions return readonly tuples for type-safe invalidation
 *
 * Usage:
 *   useQuery({ queryKey: queryKeys.admin.bookings.list({ status: 'Confirmed' }), ... })
 *   queryClient.invalidateQueries({ queryKey: queryKeys.admin.bookings.all })
 */

export const queryKeys = {
  admin: {
    bookings: {
      all: ['admin', 'bookings'] as const,
      list: (filters?: Record<string, unknown>) =>
        [...queryKeys.admin.bookings.all, 'list', filters ?? {}] as const,
      detail: (id: number) =>
        [...queryKeys.admin.bookings.all, 'detail', id] as const,
      meetings: (orderId: number) =>
        [...queryKeys.admin.bookings.all, 'meetings', orderId] as const,
    },
    employees: {
      all: ['admin', 'employees'] as const,
      list: () => [...queryKeys.admin.employees.all, 'list'] as const,
      detail: (id: number) =>
        [...queryKeys.admin.employees.all, 'detail', id] as const,
    },
    users: {
      all: ['admin', 'users'] as const,
      list: () => [...queryKeys.admin.users.all, 'list'] as const,
    },
    services: {
      all: ['admin', 'services'] as const,
      list: () => [...queryKeys.admin.services.all, 'list'] as const,
    },
    pricing: {
      all: ['admin', 'pricing'] as const,
      multipliers: () => [...queryKeys.admin.pricing.all, 'multipliers'] as const,
      discounts: () => [...queryKeys.admin.pricing.all, 'discounts'] as const,
    },
  },
  customer: {
    orders: {
      all: ['customer', 'orders'] as const,
      list: (filters?: Record<string, unknown>) =>
        [...queryKeys.customer.orders.all, 'list', filters ?? {}] as const,
      detail: (id: number) =>
        [...queryKeys.customer.orders.all, 'detail', id] as const,
    },
  },
  auth: {
    me: ['auth', 'me'] as const,
  },
  booking: {
    serviceTypes: ['booking', 'service-types'] as const,
    cleaningPlaces: ['booking', 'cleaning-places'] as const,
    additionalServices: ['booking', 'additional-services'] as const,
    recurrenceDiscounts: ['booking', 'recurrence-discounts'] as const,
  },
} as const;
