import { lazy } from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { Header, Footer } from '@/components/layout';
import { AuthProvider } from '@/contexts/AuthContext';
import { ToastProvider } from '@/contexts/ToastProvider';
import { AppErrorBoundary, QueryProvider, SuspenseBoundary, ProtectedRoute } from '@/shared/components';
import HomePage from '@/pages/HomePage';
import { NotFoundPage, ServerErrorPage } from '@/pages/ErrorPage';

// Lazy load secondary public/auth/protected routes to keep initial bundle lean
const BookingPage = lazy(() => import('@/pages/BookingPage'));
const BookingSuccessPage = lazy(() => import('@/pages/BookingSuccessPage'));
const LoginPage = lazy(() => import('@/pages/LoginPage').then(m => ({ default: m.LoginPage })));
const RegisterPage = lazy(() => import('@/pages/RegisterPage').then(m => ({ default: m.RegisterPage })));
const ForgotPasswordPage = lazy(() => import('@/pages/ForgotPasswordPage').then(m => ({ default: m.ForgotPasswordPage })));
const UserDashboardPage = lazy(() => import('@/pages/UserDashboardPage').then(m => ({ default: m.UserDashboardPage })));
const ProfilePage = lazy(() => import('@/pages/ProfilePage').then(m => ({ default: m.ProfilePage })));
const ServicesPage = lazy(() => import('@/pages/ServicesPage'));
const ContactPage = lazy(() => import('@/pages/ContactPage'));

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

// Layout wrapper for public pages (with header/footer)
function PublicLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-screen flex-col">
      <Header />
      <main id="main-content" className="flex-1">
        <SuspenseBoundary variant="page">{children}</SuspenseBoundary>
      </main>
      <Footer />
    </div>
  );
}

function AuthLayout({ children }: { children: React.ReactNode }) {
  return (
    <main id="main-content">
      <SuspenseBoundary variant="page">{children}</SuspenseBoundary>
    </main>
  );
}

function App() {
  return (
    <AppErrorBoundary variant="page">
      <QueryProvider>
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
              <Route path="/login" element={<AuthLayout><LoginPage /></AuthLayout>} />
              <Route path="/register" element={<AuthLayout><RegisterPage /></AuthLayout>} />
              <Route path="/forgot-password" element={<AuthLayout><ForgotPasswordPage /></AuthLayout>} />

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
                    <SuspenseBoundary variant="page"><AdminDashboardPage /></SuspenseBoundary>
                  </ProtectedRoute>
                }
              />
              <Route
                path="/admin/bookings"
                element={
                  <ProtectedRoute requiredRoles={['Admin']}>
                    <SuspenseBoundary variant="page"><AdminBookingsPage /></SuspenseBoundary>
                  </ProtectedRoute>
                }
              />
              <Route
                path="/admin/employees"
                element={
                  <ProtectedRoute requiredRoles={['Admin']}>
                    <SuspenseBoundary variant="page"><AdminEmployeesPage /></SuspenseBoundary>
                  </ProtectedRoute>
                }
              />
              <Route
                path="/admin/services"
                element={
                  <ProtectedRoute requiredRoles={['Admin']}>
                    <SuspenseBoundary variant="page"><AdminServicesPage /></SuspenseBoundary>
                  </ProtectedRoute>
                }
              />
              <Route
                path="/admin/service-areas"
                element={
                  <ProtectedRoute requiredRoles={['Admin']}>
                    <SuspenseBoundary variant="page"><AdminServiceAreasPage /></SuspenseBoundary>
                  </ProtectedRoute>
                }
              />
              <Route
                path="/admin/users"
                element={
                  <ProtectedRoute requiredRoles={['Admin']}>
                    <SuspenseBoundary variant="page"><AdminUsersPage /></SuspenseBoundary>
                  </ProtectedRoute>
                }
              />
              <Route
                path="/admin/cleaning-places"
                element={
                  <ProtectedRoute requiredRoles={['Admin']}>
                    <SuspenseBoundary variant="page"><AdminCleaningPlacesPage /></SuspenseBoundary>
                  </ProtectedRoute>
                }
              />
              <Route
                path="/admin/additional-services"
                element={
                  <ProtectedRoute requiredRoles={['Admin']}>
                    <SuspenseBoundary variant="page"><AdminAdditionalServicesPage /></SuspenseBoundary>
                  </ProtectedRoute>
                }
              />
              <Route
                path="/admin/pricing"
                element={
                  <ProtectedRoute requiredRoles={['Admin']}>
                    <SuspenseBoundary variant="page"><AdminPricingPage /></SuspenseBoundary>
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
      </QueryProvider>
    </AppErrorBoundary>
  );
}

export default App;
