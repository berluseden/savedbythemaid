import { useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { Eye, EyeOff, Lock, CheckCircle, AlertTriangle, ArrowLeft } from 'lucide-react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { resetPasswordSchema, type ResetPasswordFormData } from '@/shared/schemas/auth.schema';
import { Logo } from '@/shared/components/ui/logo';
import { getErrorMessage } from '@/shared/lib/error-utils';
import api from '../lib/api';

export function ResetPasswordPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token') ?? '';

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ResetPasswordFormData>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: { newPassword: '', confirmPassword: '' },
  });

  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);

  const hasToken = token.length > 0;

  const onSubmit = async (data: ResetPasswordFormData) => {
    setError('');
    setIsLoading(true);
    try {
      await api.post('/auth/reset-password', {
        token,
        newPassword: data.newPassword,
      });
      setSuccess(true);
      // Give the user a moment to read the success state before redirecting
      setTimeout(() => navigate('/login', { replace: true }), 2500);
    } catch (err) {
      setError(getErrorMessage(err, 'This reset link is invalid or has expired. Please request a new one.'));
    } finally {
      setIsLoading(false);
    }
  };

  // --- Invalid / missing token ---
  if (!hasToken) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-accent-light/5 to-blue-100 flex items-center justify-center p-4">
        <div className="w-full max-w-md">
          <div className="text-center mb-8">
            <Link to="/" className="inline-flex items-center gap-2">
              <Logo size="lg" className="h-10 w-10" />
              <span className="text-2xl font-bold text-gray-900">ecoMaid</span>
            </Link>
          </div>
          <div className="bg-white rounded-2xl shadow-xl p-8 text-center">
            <AlertTriangle className="w-14 h-14 text-amber-500 mx-auto mb-4" aria-hidden="true" />
            <h1 className="text-2xl font-bold text-gray-900 mb-2">Invalid reset link</h1>
            <p className="text-gray-600 mb-6">
              This password reset link is missing a token. It may have been copied incorrectly or expired.
            </p>
            <Link
              to="/forgot-password"
              className="inline-flex w-full items-center justify-center rounded-lg bg-brand px-4 py-3 text-white font-medium hover:bg-brand-dark transition-colors"
            >
              Request a new link
            </Link>
          </div>
        </div>
      </div>
    );
  }

  // --- Success ---
  if (success) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-accent-light/5 to-blue-100 flex items-center justify-center p-4">
        <div className="w-full max-w-md">
          <div className="text-center mb-8">
            <Link to="/" className="inline-flex items-center gap-2">
              <Logo size="lg" className="h-10 w-10" />
              <span className="text-2xl font-bold text-gray-900">ecoMaid</span>
            </Link>
          </div>
          <div
            role="status"
            aria-live="polite"
            className="bg-white rounded-2xl shadow-xl p-8 text-center"
          >
            <CheckCircle className="w-16 h-16 text-green-500 mx-auto mb-4" aria-hidden="true" />
            <h1 className="text-2xl font-bold text-gray-900 mb-2">Password updated</h1>
            <p className="text-gray-600 mb-6">
              You can now sign in with your new password. Redirecting you to the login page…
            </p>
            <Link
              to="/login"
              className="inline-flex w-full items-center justify-center gap-2 rounded-lg bg-brand px-4 py-3 text-white font-medium hover:bg-brand-dark transition-colors"
            >
              Go to login
            </Link>
          </div>
        </div>
      </div>
    );
  }

  // --- Form ---
  return (
    <div className="min-h-screen bg-gradient-to-br from-accent-light/5 to-blue-100 flex items-center justify-center p-4">
      <div className="w-full max-w-md">
        <div className="text-center mb-8">
          <Link to="/" className="inline-flex items-center gap-2">
            <Logo size="lg" className="h-10 w-10" />
            <span className="text-2xl font-bold text-gray-900">ecoMaid</span>
          </Link>
          <p className="mt-2 text-gray-600">Set a new password</p>
        </div>

        <div className="bg-white rounded-2xl shadow-xl p-8">
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-6" noValidate>
            {error && (
              <div role="alert" className="bg-red-50 text-red-600 px-4 py-3 rounded-lg text-sm">
                {error}
              </div>
            )}

            <div>
              <label htmlFor="newPassword" className="block text-sm font-medium text-gray-700 mb-2">
                New password
              </label>
              <div className="relative">
                <Lock
                  className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-gray-400"
                  aria-hidden="true"
                />
                <input
                  id="newPassword"
                  type={showPassword ? 'text' : 'password'}
                  autoComplete="new-password"
                  aria-invalid={errors.newPassword ? 'true' : 'false'}
                  aria-describedby={errors.newPassword ? 'newPassword-error' : undefined}
                  {...register('newPassword')}
                  className="w-full pl-10 pr-12 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
                  placeholder="••••••••"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((v) => !v)}
                  aria-label={showPassword ? 'Hide password' : 'Show password'}
                  aria-pressed={showPassword}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600"
                >
                  {showPassword ? (
                    <EyeOff className="h-5 w-5" aria-hidden="true" />
                  ) : (
                    <Eye className="h-5 w-5" aria-hidden="true" />
                  )}
                </button>
              </div>
              {errors.newPassword && (
                <p id="newPassword-error" className="mt-1 text-sm text-red-600">
                  {errors.newPassword.message}
                </p>
              )}
            </div>

            <div>
              <label htmlFor="confirmPassword" className="block text-sm font-medium text-gray-700 mb-2">
                Confirm new password
              </label>
              <div className="relative">
                <Lock
                  className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-gray-400"
                  aria-hidden="true"
                />
                <input
                  id="confirmPassword"
                  type={showPassword ? 'text' : 'password'}
                  autoComplete="new-password"
                  aria-invalid={errors.confirmPassword ? 'true' : 'false'}
                  aria-describedby={errors.confirmPassword ? 'confirmPassword-error' : undefined}
                  {...register('confirmPassword')}
                  className="w-full pl-10 pr-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
                  placeholder="••••••••"
                />
              </div>
              {errors.confirmPassword && (
                <p id="confirmPassword-error" className="mt-1 text-sm text-red-600">
                  {errors.confirmPassword.message}
                </p>
              )}
            </div>

            <button
              type="submit"
              disabled={isLoading}
              className="w-full py-3 px-4 bg-brand hover:bg-brand-dark text-white font-medium rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
            >
              {isLoading ? 'Updating…' : 'Update password'}
            </button>
          </form>

          <Link
            to="/login"
            className="mt-6 inline-flex items-center gap-2 text-sm text-gray-600 hover:text-gray-900"
          >
            <ArrowLeft className="h-4 w-4" aria-hidden="true" />
            Back to sign in
          </Link>
        </div>
      </div>
    </div>
  );
}
