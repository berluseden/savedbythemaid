// Centralized API endpoint constants
// All endpoint strings used across the frontend in one place.

export const API = {
  AUTH: {
    LOGIN: '/auth/login',
    REGISTER: '/auth/register',
    ME: '/auth/me',
    CHECK_EMAIL: '/auth/check-email',
    FORGOT_PASSWORD: '/auth/forgot-password',
    CHANGE_PASSWORD: '/auth/change-password',
  },
  BOOKING: {
    COVERAGE: '/booking/coverage',
    SERVICE_TYPES: '/booking/service-types',
    CLEANING_PLACES: '/booking/cleaning-places',
    ADDITIONAL_SERVICES: '/booking/additional-services',
    RECURRENCE_DISCOUNTS: '/booking/recurrence-discounts',
    ESTIMATE: '/booking/estimate',
    AVAILABILITY: '/booking/availability',
    SOFT_RESERVE: '/booking/soft-reserve',
    CONFIRM: '/booking/confirm',
  },
  CUSTOMER: {
    PROFILE: '/customer/profile',
    MY_ORDERS: '/customer/my-orders',
    STATS: '/customer/stats',
  },
  ADMIN: {
    ORDERS: '/admin/orders',
    ORDERS_MEETINGS: '/admin/orders/meetings',
    EMPLOYEES: '/admin/employees',
    EQUIPMENT: '/admin/equipment',
    USERS: '/admin/users',
    USERS_ROLES: '/admin/users/roles',
    SERVICE_TYPES: '/admin/service-types',
    CLEANING_PLACES: '/admin/cleaningplaces',
    ADDITIONAL_SERVICES: '/admin/additionalservices',
    PRICE_MULTIPLIERS: '/admin/pricemultipliers',
    RECURRENCE_DISCOUNTS: '/admin/pricemultipliers/recurrence-discounts',
  },
  SERVICE_AREAS: '/serviceareas',
  CONTACT: '/contact',
} as const;
