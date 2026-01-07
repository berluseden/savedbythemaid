import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Header, Footer } from '@/components/layout';
import { AuthProvider, ProtectedRoute } from '@/contexts/AuthContext';
import HomePage from '@/pages/HomePage';
import BookingPage from '@/pages/BookingPage';
import BookingSuccessPage from '@/pages/BookingSuccessPage';
import { LoginPage } from '@/pages/LoginPage';
import { RegisterPage } from '@/pages/RegisterPage';
import { ForgotPasswordPage } from '@/pages/ForgotPasswordPage';
import { UserDashboardPage } from '@/pages/UserDashboardPage';
import { AdminDashboardPage } from '@/pages/admin/DashboardPage';
import { AdminBookingsPage } from '@/pages/admin/BookingsPage';
import { AdminEmployeesPage } from '@/pages/admin/EmployeesPage';
import { AdminServicesPage } from '@/pages/admin/ServicesPage';
import { AdminServiceAreasPage } from '@/pages/admin/ServiceAreasPage';
import { AdminUsersPage } from '@/pages/admin/UsersPage';
import { AdminCleaningPlacesPage } from '@/pages/admin/CleaningPlacesPage';
import { AdminAdditionalServicesPage } from '@/pages/admin/AdditionalServicesPage';
import ServicesPage from '@/pages/ServicesPage';
import ContactPage from '@/pages/ContactPage';
import AboutPage from '@/pages/AboutPage';

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

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <BrowserRouter>
          <Routes>
            {/* Public routes with header/footer */}
            <Route path="/" element={<PublicLayout><HomePage /></PublicLayout>} />
            <Route path="/book" element={<PublicLayout><BookingPage /></PublicLayout>} />
            <Route path="/booking/success" element={<PublicLayout><BookingSuccessPage /></PublicLayout>} />
            <Route path="/services" element={<PublicLayout><ServicesPage /></PublicLayout>} />
            <Route path="/contact" element={<PublicLayout><ContactPage /></PublicLayout>} />
            <Route path="/about" element={<PublicLayout><AboutPage /></PublicLayout>} />
            
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
            
            {/* Admin routes (protected) */}
            <Route
              path="/admin"
              element={
                <ProtectedRoute requiredRoles={['Admin']}>
                  <AdminDashboardPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/admin/bookings"
              element={
                <ProtectedRoute requiredRoles={['Admin']}>
                  <AdminBookingsPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/admin/employees"
              element={
                <ProtectedRoute requiredRoles={['Admin']}>
                  <AdminEmployeesPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/admin/services"
              element={
                <ProtectedRoute requiredRoles={['Admin']}>
                  <AdminServicesPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/admin/service-areas"
              element={
                <ProtectedRoute requiredRoles={['Admin']}>
                  <AdminServiceAreasPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/admin/users"
              element={
                <ProtectedRoute requiredRoles={['Admin']}>
                  <AdminUsersPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/admin/cleaning-places"
              element={
                <ProtectedRoute requiredRoles={['Admin']}>
                  <AdminCleaningPlacesPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/admin/additional-services"
              element={
                <ProtectedRoute requiredRoles={['Admin']}>
                  <AdminAdditionalServicesPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/admin/settings"
              element={
                <ProtectedRoute requiredRoles={['Admin']}>
                  <PlaceholderPage title="Configuración" />
                </ProtectedRoute>
              }
            />
            
            {/* 404 */}
            <Route path="*" element={<PublicLayout><NotFoundPage /></PublicLayout>} />
          </Routes>
        </BrowserRouter>
      </AuthProvider>
    </QueryClientProvider>
  );
}

function PlaceholderPage({ title }: { title: string }) {
  return (
    <div className="flex items-center justify-center min-h-[60vh]">
      <div className="text-center">
        <h1 className="text-3xl font-bold text-gray-900 mb-4">{title}</h1>
        <p className="text-gray-600">Esta página estará disponible pronto.</p>
      </div>
    </div>
  );
}

function NotFoundPage() {
  return (
    <div className="flex items-center justify-center min-h-[60vh]">
      <div className="text-center">
        <h1 className="text-6xl font-bold text-gray-300 mb-4">404</h1>
        <h2 className="text-2xl font-bold text-gray-900 mb-2">Página no encontrada</h2>
        <p className="text-gray-600 mb-6">La página que buscas no existe o fue movida.</p>
        <a href="/" className="inline-flex items-center px-4 py-2 bg-sky-500 text-white rounded-lg hover:bg-sky-600 transition-colors">
          Volver al inicio
        </a>
      </div>
    </div>
  );
}

export default App;
