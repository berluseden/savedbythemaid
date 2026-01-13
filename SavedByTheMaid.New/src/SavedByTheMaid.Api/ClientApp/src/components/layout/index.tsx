import { Link, useLocation } from 'react-router-dom';
import { Home, Calendar, Sparkles, Phone, User, LogOut, ChevronDown } from 'lucide-react';
import { cn } from '@/lib/utils';
import { useAuth } from '@/contexts/AuthContext';
import { useState } from 'react';

export function Header() {
  const location = useLocation();
  const { user, isAuthenticated, logout } = useAuth();
  const [showUserMenu, setShowUserMenu] = useState(false);

  const navigation = [
    { name: 'Home', href: '/', icon: Home },
    { name: 'Book Now', href: '/booking', icon: Calendar },
    { name: 'Services', href: '/services', icon: Sparkles },
    { name: 'Contact', href: '/contact', icon: Phone },
  ];

  const handleLogout = async () => {
    await logout();
    setShowUserMenu(false);
  };

  return (
    <header className="sticky top-0 z-50 border-b border-gray-200 bg-white/80 backdrop-blur-md">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="flex h-16 items-center justify-between">
          {/* Logo */}
          <Link to="/" className="flex items-center space-x-2">
            <img src="/logo-ecomaid.svg" alt="ecoMaid" className="h-12 w-auto" />
          </Link>

          {/* Navigation */}
          <nav className="hidden md:flex items-center space-x-1">
            {navigation.map((item) => {
              const isActive = location.pathname === item.href;
              return (
                <Link
                  key={item.name}
                  to={item.href}
                  className={cn(
                    'flex items-center space-x-1 rounded-lg px-3 py-2 text-sm font-medium transition-colors',
                    isActive
                      ? 'bg-[#FFE44D]/10 text-[#00205B]'
                      : 'text-gray-600 hover:bg-gray-100 hover:text-gray-900'
                  )}
                >
                  <item.icon className="h-4 w-4" />
                  <span>{item.name}</span>
                </Link>
              );
            })}
          </nav>

          {/* Auth/CTA */}
          <div className="flex items-center space-x-4">
            {isAuthenticated && user ? (
              <div className="relative">
                <button
                  onClick={() => setShowUserMenu(!showUserMenu)}
                  className="flex items-center gap-2 px-3 py-2 rounded-lg text-sm font-medium text-gray-700 hover:bg-gray-100 transition-colors"
                >
                  <div className="w-8 h-8 bg-[#00205B] rounded-full flex items-center justify-center text-white font-medium">
                    {user.firstName?.[0]?.toUpperCase() || user.email?.[0]?.toUpperCase() || 'U'}
                  </div>
                  <span className="hidden sm:inline">
                    {user.firstName || user.email?.split('@')[0]}
                  </span>
                  <ChevronDown className="h-4 w-4" />
                </button>

                {showUserMenu && (
                  <>
                    <div
                      className="fixed inset-0 z-40"
                      onClick={() => setShowUserMenu(false)}
                    />
                    <div className="absolute right-0 mt-2 w-48 bg-white rounded-lg shadow-lg border z-50 py-1">
                      <Link
                        to="/dashboard"
                        onClick={() => setShowUserMenu(false)}
                        className="flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                      >
                        <User className="h-4 w-4" />
                        My Dashboard
                      </Link>
                      {user.roles?.includes('Admin') && (
                        <Link
                          to="/admin"
                          onClick={() => setShowUserMenu(false)}
                          className="flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                        >
                          <Sparkles className="h-4 w-4" />
                          Admin Panel
                        </Link>
                      )}
                      <hr className="my-1" />
                      <button
                        onClick={handleLogout}
                        className="flex items-center gap-2 w-full px-4 py-2 text-sm text-red-600 hover:bg-red-50"
                      >
                        <LogOut className="h-4 w-4" />
                        Sign Out
                      </button>
                    </div>
                  </>
                )}
              </div>
            ) : (
              <>
                <Link
                  to="/login"
                  className="flex items-center space-x-1 text-sm font-medium text-gray-600 hover:text-gray-900"
                >
                  <User className="h-4 w-4" />
                  <span className="hidden sm:inline">Login</span>
                </Link>
                <Link
                  to="/booking"
                  className="rounded-lg bg-[#00205B] px-4 py-2 text-sm font-medium text-white hover:bg-[#001440] transition-colors"
                >
                  Book Now
                </Link>
              </>
            )}
          </div>
        </div>
      </div>
    </header>
  );
}

export function Footer() {
  return (
    <footer className="border-t border-gray-200 bg-white">
      <div className="mx-auto max-w-7xl px-4 py-12 sm:px-6 lg:px-8">
        <div className="grid grid-cols-1 gap-8 md:grid-cols-4">
          {/* Brand */}
          <div className="space-y-4">
            <div className="flex items-center space-x-2">
              <img src="/logo-ecomaid.svg" alt="ecoMaid" className="h-8" />
            </div>
            <p className="text-sm text-gray-600">
              Servicios profesionales de limpieza para tu hogar y oficina. Reserva en línea en minutos.
            </p>
          </div>

          {/* Services */}
          <div>
            <h3 className="text-sm font-semibold text-gray-900">Services</h3>
            <ul className="mt-4 space-y-2">
              <li><Link to="/services" className="text-sm text-gray-600 hover:text-gray-900">Standard Cleaning</Link></li>
              <li><Link to="/services" className="text-sm text-gray-600 hover:text-gray-900">Deep Cleaning</Link></li>
              <li><Link to="/services" className="text-sm text-gray-600 hover:text-gray-900">Move In/Out</Link></li>
              <li><Link to="/services" className="text-sm text-gray-600 hover:text-gray-900">Office Cleaning</Link></li>
            </ul>
          </div>

          {/* Company */}
          <div>
            <h3 className="text-sm font-semibold text-gray-900">Company</h3>
            <ul className="mt-4 space-y-2">
              <li><Link to="/about" className="text-sm text-gray-600 hover:text-gray-900">About Us</Link></li>
              <li><Link to="/contact" className="text-sm text-gray-600 hover:text-gray-900">Contact</Link></li>
            </ul>
          </div>

          {/* Contact */}
          <div>
            <h3 className="text-sm font-semibold text-gray-900">Contact</h3>
            <ul className="mt-4 space-y-2">
              <li className="text-sm text-gray-600">📞 (555) 123-4567</li>
              <li className="text-sm text-gray-600">📧 hello@ecomaid.com</li>
              <li className="text-sm text-gray-600">📍 New York, NY</li>
            </ul>
          </div>
        </div>

        <div className="mt-8 border-t border-gray-200 pt-8">
          <p className="text-center text-sm text-gray-500">
            © {new Date().getFullYear()} ecoMaid. Todos los derechos reservados.
          </p>
        </div>
      </div>
    </footer>
  );
}
