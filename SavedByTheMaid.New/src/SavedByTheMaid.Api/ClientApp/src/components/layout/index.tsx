import { Link, useLocation } from 'react-router-dom';
import { Home, Calendar, Sparkles, Phone, User } from 'lucide-react';
import { cn } from '@/lib/utils';

export function Header() {
  const location = useLocation();

  const navigation = [
    { name: 'Home', href: '/', icon: Home },
    { name: 'Book Now', href: '/book', icon: Calendar },
    { name: 'Services', href: '/services', icon: Sparkles },
    { name: 'Contact', href: '/contact', icon: Phone },
  ];

  return (
    <header className="sticky top-0 z-50 border-b border-gray-200 bg-white/80 backdrop-blur-md">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="flex h-16 items-center justify-between">
          {/* Logo */}
          <Link to="/" className="flex items-center space-x-2">
            <Sparkles className="h-8 w-8 text-sky-500" />
            <span className="text-xl font-bold text-gray-900">SavedByTheMaid</span>
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
                      ? 'bg-sky-50 text-sky-600'
                      : 'text-gray-600 hover:bg-gray-100 hover:text-gray-900'
                  )}
                >
                  <item.icon className="h-4 w-4" />
                  <span>{item.name}</span>
                </Link>
              );
            })}
          </nav>

          {/* CTA */}
          <div className="flex items-center space-x-4">
            <Link
              to="/login"
              className="flex items-center space-x-1 text-sm font-medium text-gray-600 hover:text-gray-900"
            >
              <User className="h-4 w-4" />
              <span className="hidden sm:inline">Login</span>
            </Link>
            <Link
              to="/book"
              className="rounded-lg bg-sky-500 px-4 py-2 text-sm font-medium text-white hover:bg-sky-600 transition-colors"
            >
              Book Now
            </Link>
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
              <Sparkles className="h-6 w-6 text-sky-500" />
              <span className="text-lg font-bold text-gray-900">SavedByTheMaid</span>
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
              <li><Link to="/about" className="text-sm text-gray-600 hover:text-gray-900">About Us</Link></li>
              <li><Link to="/careers" className="text-sm text-gray-600 hover:text-gray-900">Careers</Link></li>
              <li><Link to="/contact" className="text-sm text-gray-600 hover:text-gray-900">Contact</Link></li>
              <li><Link to="/faq" className="text-sm text-gray-600 hover:text-gray-900">FAQ</Link></li>
            </ul>
          </div>

          {/* Contact */}
          <div>
            <h3 className="text-sm font-semibold text-gray-900">Contact</h3>
            <ul className="mt-4 space-y-2">
              <li className="text-sm text-gray-600">📞 (555) 123-4567</li>
              <li className="text-sm text-gray-600">📧 hello@savedbythemaid.com</li>
              <li className="text-sm text-gray-600">📍 New York, NY</li>
            </ul>
          </div>
        </div>

        <div className="mt-8 border-t border-gray-200 pt-8">
          <p className="text-center text-sm text-gray-500">
            © {new Date().getFullYear()} SavedByTheMaid. All rights reserved.
          </p>
        </div>
      </div>
    </footer>
  );
}
