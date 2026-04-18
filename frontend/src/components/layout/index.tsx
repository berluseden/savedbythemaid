import { Link, useLocation } from 'react-router-dom';
import { Home, Calendar, Sparkles, Phone, User, LogOut, ChevronDown, Menu, X } from 'lucide-react';
import { cn } from '@/lib/utils';
import { useAuth } from '@/contexts/AuthContext';
import { useEffect, useState } from 'react';

export function Header() {
  const location = useLocation();
  const { user, isAuthenticated, logout } = useAuth();
  const [showUserMenu, setShowUserMenu] = useState(false);
  const [showMobileMenu, setShowMobileMenu] = useState(false);

  const navigation = [
    { name: 'Home', href: '/', icon: Home },
    { name: 'Book Now', href: '/booking', icon: Calendar },
    { name: 'Services', href: '/services', icon: Sparkles },
    { name: 'Contact', href: '/contact', icon: Phone },
  ];

  const handleLogout = async () => {
    await logout();
    setShowUserMenu(false);
    setShowMobileMenu(false);
  };

  // Close mobile menu on route change
  useEffect(() => {
    setShowMobileMenu(false);
  }, [location.pathname]);

  // Lock body scroll while drawer is open (native-feel)
  useEffect(() => {
    if (showMobileMenu) {
      document.body.style.overflow = 'hidden';
      return () => {
        document.body.style.overflow = '';
      };
    }
  }, [showMobileMenu]);

  return (
    <header className="sticky top-0 z-50 border-b border-gray-200 bg-white/80 backdrop-blur-md">
      <a href="#main-content" className="sr-only focus:not-sr-only focus:absolute focus:z-50 focus:p-4 focus:bg-white focus:text-brand">
        Skip to main content
      </a>
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="flex h-16 items-center justify-between">
          {/* Logo */}
          <Link to="/" className="flex items-center space-x-2 transition-transform hover:scale-105">
            <img src="/logo-ecomaid.svg" alt="ecoMaid" width={140} height={56} className="h-14 w-auto drop-shadow-sm hover:drop-shadow-md transition-all" />
          </Link>

          {/* Navigation */}
          <nav aria-label="Main navigation" className="hidden md:flex items-center space-x-1">
            {navigation.map((item) => {
              const isActive = location.pathname === item.href;
              return (
                <Link
                  key={item.name}
                  to={item.href}
                  className={cn(
                    'flex items-center space-x-1 rounded-lg px-3 py-2 text-sm font-medium transition-colors',
                    isActive
                      ? 'bg-accent-light/10 text-brand'
                      : 'text-gray-600 hover:bg-gray-100 hover:text-gray-900'
                  )}
                >
                  <item.icon className="h-4 w-4" aria-hidden="true" />
                  <span>{item.name}</span>
                </Link>
              );
            })}
          </nav>

          {/* Mobile hamburger — visible below md */}
          <button
            type="button"
            onClick={() => setShowMobileMenu(true)}
            aria-label="Open navigation menu"
            aria-expanded={showMobileMenu}
            aria-controls="mobile-nav-drawer"
            className="md:hidden inline-flex items-center justify-center rounded-lg p-2 text-gray-700 hover:bg-gray-100 touch-target"
          >
            <Menu className="h-6 w-6" aria-hidden="true" />
          </button>

          {/* Auth/CTA */}
          <div className="hidden md:flex items-center space-x-4">
            {isAuthenticated && user ? (
              <div className="relative">
                <button
                  onClick={() => setShowUserMenu(!showUserMenu)}
                  aria-expanded={showUserMenu}
                  aria-haspopup="true"
                  aria-label="User menu"
                  className="flex items-center gap-2 px-3 py-2 rounded-lg text-sm font-medium text-gray-700 hover:bg-gray-100 transition-colors"
                >
                  <div className="w-8 h-8 bg-brand rounded-full flex items-center justify-center text-white font-medium">
                    {user.firstName?.[0]?.toUpperCase() || user.email?.[0]?.toUpperCase() || 'U'}
                  </div>
                  <span className="hidden sm:inline">
                    {user.firstName || user.email?.split('@')[0]}
                  </span>
                  <ChevronDown className="h-4 w-4" aria-hidden="true" />
                </button>

                {showUserMenu && (
                  <>
                    <div
                      className="fixed inset-0 z-40"
                      onClick={() => setShowUserMenu(false)}
                      aria-hidden="true"
                    />
                    <div role="menu" className="absolute right-0 mt-2 w-48 bg-white rounded-lg shadow-lg border z-50 py-1">
                      <Link
                        to="/dashboard"
                        onClick={() => setShowUserMenu(false)}
                        className="flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                      >
                        <User className="h-4 w-4" aria-hidden="true" />
                        My Dashboard
                      </Link>
                      {user.roles?.includes('Admin') && (
                        <Link
                          to="/admin"
                          onClick={() => setShowUserMenu(false)}
                          className="flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                        >
                          <Sparkles className="h-4 w-4" aria-hidden="true" />
                          Admin Panel
                        </Link>
                      )}
                      <hr className="my-1" />
                      <button
                        onClick={handleLogout}
                        className="flex items-center gap-2 w-full px-4 py-2 text-sm text-red-600 hover:bg-red-50"
                      >
                        <LogOut className="h-4 w-4" aria-hidden="true" />
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
                  <User className="h-4 w-4" aria-hidden="true" />
                  <span className="hidden sm:inline">Login</span>
                </Link>
                <Link
                  to="/booking"
                  className="rounded-lg bg-brand px-4 py-2 text-sm font-medium text-white hover:bg-brand-dark transition-colors"
                >
                  Book Now
                </Link>
              </>
            )}
          </div>
        </div>
      </div>

      {/* Mobile drawer — slide-in from right, native-app feel */}
      {showMobileMenu && (
        <div
          id="mobile-nav-drawer"
          role="dialog"
          aria-modal="true"
          aria-label="Navigation menu"
          className="md:hidden fixed inset-0 z-50"
        >
          {/* Scrim */}
          <button
            type="button"
            aria-label="Close menu"
            onClick={() => setShowMobileMenu(false)}
            className="absolute inset-0 bg-gray-900/40 backdrop-blur-sm animate-in fade-in duration-200"
          />
          {/* Panel */}
          <div
            className="absolute right-0 top-0 bottom-0 w-[85%] max-w-sm bg-white shadow-xl flex flex-col animate-in slide-in-from-right duration-200"
            style={{ paddingTop: 'env(safe-area-inset-top)' }}
          >
            <div className="flex items-center justify-between px-4 h-16 border-b">
              <span className="font-semibold text-gray-900">Menu</span>
              <button
                type="button"
                onClick={() => setShowMobileMenu(false)}
                aria-label="Close menu"
                className="inline-flex items-center justify-center rounded-lg p-2 text-gray-700 hover:bg-gray-100 touch-target"
              >
                <X className="h-6 w-6" aria-hidden="true" />
              </button>
            </div>

            <nav aria-label="Mobile navigation" className="flex-1 overflow-y-auto py-2">
              {navigation.map((item) => {
                const isActive = location.pathname === item.href;
                return (
                  <Link
                    key={item.name}
                    to={item.href}
                    className={cn(
                      'flex items-center gap-3 px-4 py-3 text-base font-medium touch-target',
                      isActive
                        ? 'bg-accent-light/10 text-brand'
                        : 'text-gray-700 hover:bg-gray-50'
                    )}
                  >
                    <item.icon className="h-5 w-5 shrink-0" aria-hidden="true" />
                    <span>{item.name}</span>
                  </Link>
                );
              })}
            </nav>

            <div
              className="border-t px-4 py-4 space-y-2"
              style={{ paddingBottom: 'max(env(safe-area-inset-bottom), 1rem)' }}
            >
              {isAuthenticated && user ? (
                <>
                  <Link
                    to="/dashboard"
                    className="flex items-center gap-3 px-3 py-3 rounded-lg text-base text-gray-700 hover:bg-gray-50 touch-target"
                  >
                    <User className="h-5 w-5" aria-hidden="true" />
                    My Dashboard
                  </Link>
                  {user.roles?.includes('Admin') && (
                    <Link
                      to="/admin"
                      className="flex items-center gap-3 px-3 py-3 rounded-lg text-base text-gray-700 hover:bg-gray-50 touch-target"
                    >
                      <Sparkles className="h-5 w-5" aria-hidden="true" />
                      Admin Panel
                    </Link>
                  )}
                  <button
                    type="button"
                    onClick={handleLogout}
                    className="flex w-full items-center gap-3 px-3 py-3 rounded-lg text-base text-red-600 hover:bg-red-50 touch-target"
                  >
                    <LogOut className="h-5 w-5" aria-hidden="true" />
                    Sign Out
                  </button>
                </>
              ) : (
                <>
                  <Link
                    to="/login"
                    className="flex w-full items-center justify-center gap-2 rounded-lg border border-gray-300 px-4 py-3 text-base font-medium text-gray-700 hover:bg-gray-50 touch-target"
                  >
                    <User className="h-5 w-5" aria-hidden="true" />
                    Login
                  </Link>
                  <Link
                    to="/booking"
                    className="flex w-full items-center justify-center rounded-lg bg-brand px-4 py-3 text-base font-medium text-white hover:bg-brand-dark touch-target"
                  >
                    Book Now
                  </Link>
                </>
              )}
            </div>
          </div>
        </div>
      )}
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
              <img src="/logo-ecomaid.svg" alt="ecoMaid" width={80} height={32} className="h-8 w-auto" />
            </div>
            <p className="text-sm text-gray-600">
              Professional cleaning services for your home and office. Book online in minutes.
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
            © {new Date().getFullYear()} ecoMaid. All rights reserved.
          </p>
        </div>
      </div>
    </footer>
  );
}
