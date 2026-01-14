import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Mail, ArrowLeft, CheckCircle } from 'lucide-react';
import api from '../lib/api';

export function ForgotPasswordPage() {
  const [email, setEmail] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setIsLoading(true);

    try {
      await api.post('/auth/forgot-password', { email });
      setSuccess(true);
    } catch (err) {
      // Don't reveal if email exists or not for security
      setSuccess(true);
    } finally {
      setIsLoading(false);
    }
  };

  if (success) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-[#FFE44D]/5 to-blue-100 flex items-center justify-center p-4">
        <div className="w-full max-w-md">
          <div className="text-center mb-8">
            <Link to="/" className="inline-flex items-center gap-2">
              {/* 3 Bubbles icon */}
              <svg viewBox="0 0 100 100" fill="none" className="h-10 w-10">
                <circle cx="58" cy="65" r="24" fill="#00205B"/>
                <ellipse cx="50" cy="55" rx="7" ry="10" fill="white" opacity="0.35"/>
                <circle cx="72" cy="32" r="16" fill="#00205B"/>
                <ellipse cx="66" cy="26" rx="5" ry="7" fill="white" opacity="0.35"/>
                <circle cx="38" cy="28" r="12" fill="#00205B"/>
                <ellipse cx="34" cy="24" rx="4" ry="5" fill="white" opacity="0.35"/>
                <g transform="translate(18, 50)">
                  <path d="M0 -6 L0 6 M-6 0 L6 0" stroke="#F7C52D" strokeWidth="2.5" strokeLinecap="round"/>
                </g>
              </svg>
              <span className="text-2xl font-bold text-gray-900">ecoMaid</span>
            </Link>
          </div>

          <div className="bg-white rounded-2xl shadow-xl p-8 text-center">
            <CheckCircle className="w-16 h-16 text-green-500 mx-auto mb-4" />
            <h2 className="text-2xl font-bold text-gray-900 mb-2">Email Sent!</h2>
            <p className="text-gray-600 mb-6">
              If an account exists with <strong>{email}</strong>, you will receive a link to reset your password.
            </p>
            <p className="text-sm text-gray-500 mb-6">
              Check your inbox and spam folder.
            </p>
            <Link
              to="/login"
              className="inline-flex items-center gap-2 text-[#00205B] hover:text-[#001440] font-medium"
            >
              <ArrowLeft className="w-4 h-4" />
              Back to Login
            </Link>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-[#FFE44D]/5 to-blue-100 flex items-center justify-center p-4">
      <div className="w-full max-w-md">
        {/* Logo */}
        <div className="text-center mb-8">
          <Link to="/" className="inline-flex items-center gap-2">
            {/* 3 Bubbles icon */}
            <svg viewBox="0 0 100 100" fill="none" className="h-10 w-10">
              <circle cx="58" cy="65" r="24" fill="#00205B"/>
              <ellipse cx="50" cy="55" rx="7" ry="10" fill="white" opacity="0.35"/>
              <circle cx="72" cy="32" r="16" fill="#00205B"/>
              <ellipse cx="66" cy="26" rx="5" ry="7" fill="white" opacity="0.35"/>
              <circle cx="38" cy="28" r="12" fill="#00205B"/>
              <ellipse cx="34" cy="24" rx="4" ry="5" fill="white" opacity="0.35"/>
              <g transform="translate(18, 50)">
                <path d="M0 -6 L0 6 M-6 0 L6 0" stroke="#F7C52D" strokeWidth="2.5" strokeLinecap="round"/>
              </g>
            </svg>
            <span className="text-2xl font-bold text-gray-900">ecoMaid</span>
          </Link>
          <p className="mt-2 text-gray-600">Reset your password</p>
        </div>

        {/* Card */}
        <div className="bg-white rounded-2xl shadow-xl p-8">
          <div className="text-center mb-6">
            <h2 className="text-xl font-semibold text-gray-900 mb-2">
              Forgot your password?
            </h2>
            <p className="text-gray-600 text-sm">
              Enter your email address and we'll send you a link to reset it.
            </p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-6">
            {error && (
              <div className="bg-red-50 text-red-600 px-4 py-3 rounded-lg text-sm">
                {error}
              </div>
            )}

            <div>
              <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-2">
                Email Address
              </label>
              <div className="relative">
                <Mail className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-gray-400" />
                <input
                  type="email"
                  id="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="w-full pl-10 pr-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent transition-all"
                  placeholder="you@example.com"
                  required
                />
              </div>
            </div>

            <button
              type="submit"
              disabled={isLoading}
              className="w-full py-3 px-4 bg-[#00205B] hover:bg-[#001440] text-white font-medium rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
            >
              {isLoading ? (
                <>
                  <div className="w-5 h-5 border-2 border-white border-t-transparent rounded-full animate-spin" />
                  Sending...
                </>
              ) : (
                'Send Reset Link'
              )}
            </button>
          </form>

          <div className="mt-6 text-center">
            <Link
              to="/login"
              className="inline-flex items-center gap-2 text-sm text-[#00205B] hover:text-[#001440] font-medium"
            >
              <ArrowLeft className="w-4 h-4" />
              Back to Login
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
