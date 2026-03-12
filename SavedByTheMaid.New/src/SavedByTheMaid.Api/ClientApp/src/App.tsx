import { lazy, Suspense } from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Header, Footer } from '@/components/layout';
import { AuthProvider, ProtectedRoute } from '@/contexts/AuthContext';
import { ToastProvider } from '@/contexts/ToastProvider';
import { ErrorBoundary } from '@/components/ui/ErrorBoundary';
import HomePage from '@/pages/HomePage';
import BookingPage from '@/pages/BookingPage';
import BookingSuccessPage from '@/pages/BookingSuccessPage';
import { LoginPage } from '@/pages/LoginPage';
import { RegisterPage } from '@/pages/RegisterPage';
import { ForgotPasswordPage } from '@/pages/ForgotPasswordPage';
import { UserDashboardPage } from '@/pages/UserDashboardPage';
import { ProfilePage } from '@/pages/ProfilePage';
import { NotFoundPage, ServerErrorPage } from '@/pages/ErrorPage';
import ServicesPage from '@/pages/ServicesPage';
import ContactPage from '@/pages/ContactPage';

// Lazy load admin pages - only downloaded when admin navigates to them
const AdminDashboardPage = lazy(() => import('@/pages/admin/DashboardPage').then(m => ({ default: m.AdminDashboardPage })));
const AdminBookingsPage = lazy(() => import('@/pages/admin/BookingsPage').then(m => ({ default: m.AdminBookingsPage })));
const AdminEmployeesPage = lazy(() => import('@/pages/admin/EmployeesPage').then(m => ({ default: m.AdminEmployeesPage })));
const AdminServicesPage = lazy(() => import('@/pages/admin/ServicesPage').then(m => ({ default: m.AdminServicesPage })));
const AdminServiceAreasPage = lazy(() => import('@/pages/admin/ServiceAreasPage').then(m => ({ default: m.AdminServiceAreasPage })));
const AdminUsersPage = lazy(() => import('@/pages/admin/UsersPage').then(m => ({ default: m.AdminUsersPage })));
const AdminCleaningPlacesPage = lazy(() => import('@/pages/admin/CleaningPlacesPage').then(m => ({ default: m.AdminCleaningPlacesPage })));
const AdminAdditionalServicesPage = lazy(() => import('@/pages/admin/AdditionalServicesPage').then(m => ({ default: m.AdminAdditionalServicesPage })));
const AdminPricingPage = lazy(() => import('@/pages/admin/PricingPage').then(m => ({ default: m.AdminPricingPage })));

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 60 * 5, // 5 minutes
      retry: 1,
    },
  },
});

// Layout wrapper for public pages (with header/footer)
function PublicLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-screen flex-col">
      <Header />
      <main className="flex-1">{children}</main>
      <Footer />
    </div>
  );
}

function AdminSuspense({ children }: { children: React.ReactNode }) {
  return (
    <Suspense fallback={
      <div className="flex min-h-screen items-center justify-center">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-[#00205B] border-t-transparent" role="status" aria-label="Loading" />
      </div>
    }>
      {children}
    </Suspense>
  );
}

function App() {
  return (
    <ErrorBoundary>
      <QueryClientProvider client={queryClient}>
        <ToastProvider>
          <AuthProvider>
          <BrowserRouter>
            <Routes>
              {/* Public routes with header/footer */}
              <Route path="/" element={<PublicLayout><HomePage /></PublicLayout>} />
              <Route path="/booking" element={<PublicLayout><BookingPage /></PublicLayout>} />
              <Route path="/booking/success" element={<PublicLayout><BookingSuccessPage /></PublicLayout>} />
              <Route path="/services" element={<PublicLayout><ServicesPage /></PublicLayout>} />
              <Route path="/contact" element={<PublicLayout><ContactPage /></PublicLayout>} />

              {/* Auth routes (no header/footer) */}
              <Route path="/login" element={<LoginPage />} />
              <Route path="/register" element={<RegisterPage />} />
              <Route path="/forgot-password" element={<ForgotPasswordPage />} />

              {/* User dashboard (protected) */}
              <Route
                path="/dashboard"
                element={
                  <ProtectedRoute>
                    <PublicLayout><UserDashboardPage /></PublicLayout>
                  </ProtectedRoute>
                }
              />

              {/* Profile (protected) */}
              <Route
                path="/profile"
                element={
                  <ProtectedRoute>
                    <PublicLayout><ProfilePage /></PublicLayout>
                  </ProtectedRoute>
                }
              />

              {/* Admin routes (protected + lazy loaded) */}
              <Route
                path="/admin"
                element={
                  <ProtectedRoute requiredRoles={['Admin']}>
                    <AdminSuspense><AdminDashboardPage /></AdminSuspense>
                  </ProtectedRoute>
                }
              />
              <Route
                path="/admin/bookings"
                element={
                  <ProtectedRoute requiredRoles={['Admin']}>
                    <AdminSuspense><AdminBookingsPage /></AdminSuspense>
                  </ProtectedRoute>
                }
              />
              <Route
                path="/admin/employees"
                element={
                  <ProtectedRoute requiredRoles={['Admin']}>
                    <AdminSuspense><AdminEmployeesPage /></AdminSuspense>
                  </ProtectedRoute>
                }
              />
              <Route
                path="/admin/services"
                element={
                  <ProtectedRoute requiredRoles={['Admin']}>
                    <AdminSuspense><AdminServicesPage /></AdminSuspense>
                  </ProtectedRoute>
                }
              />
              <Route
                path="/admin/service-areas"
                element={
                  <ProtectedRoute requiredRoles={['Admin']}>
                    <AdminSuspense><AdminServiceAreasPage /></AdminSuspense>
                  </ProtectedRoute>
                }
              />
              <Route
                path="/admin/users"
                element={
                  <ProtectedRoute requiredRoles={['Admin']}>
                    <AdminSuspense><AdminUsersPage /></AdminSuspense>
                  </ProtectedRoute>
                }
              />
              <Route
                path="/admin/cleaning-places"
                element={
                  <ProtectedRoute requiredRoles={['Admin']}>
                    <AdminSuspense><AdminCleaningPlacesPage /></AdminSuspense>
                  </ProtectedRoute>
                }
              />
              <Route
                path="/admin/additional-services"
                element={
                  <ProtectedRoute requiredRoles={['Admin']}>
                    <AdminSuspense><AdminAdditionalServicesPage /></AdminSuspense>
                  </ProtectedRoute>
                }
              />
              <Route
                path="/admin/pricing"
                element={
                  <ProtectedRoute requiredRoles={['Admin']}>
                    <AdminSuspense><AdminPricingPage /></AdminSuspense>
                  </ProtectedRoute>
                }
              />

              {/* Error pages */}
              <Route path="/error" element={<ServerErrorPage />} />

              {/* 404 */}
              <Route path="*" element={<PublicLayout><NotFoundPage /></PublicLayout>} />
            </Routes>
          </BrowserRouter>
          </AuthProvider>
        </ToastProvider>
      </QueryClientProvider>
    </ErrorBoundary>
  );
}

export default App;
