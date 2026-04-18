import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Eye, EyeOff, Mail, Lock, User, Phone, Check } from 'lucide-react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { registerSchema, type RegisterFormData } from '@/shared/schemas/auth.schema';
import { Logo } from '@/shared/components/ui/logo';
import { getErrorMessage } from '@/shared/lib/error-utils';
import { useAuth } from '../contexts/AuthContext';

export function RegisterPage() {
  const navigate = useNavigate();
  const { register: authRegister, isLoading } = useAuth();

  const { register, handleSubmit, watch, formState: { errors } } = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      firstName: '',
      lastName: '',
      email: '',
      phone: '',
      password: '',
      confirmPassword: '',
      acceptTerms: false as unknown as true,
    },
  });
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState('');

  const password = watch('password', '');
  const confirmPassword = watch('confirmPassword', '');

  const passwordRequirements = [
    { text: 'At least 8 characters', met: password.length >= 8 },
    { text: 'One uppercase letter', met: /[A-Z]/.test(password) },
    { text: 'One lowercase letter', met: /[a-z]/.test(password) },
    { text: 'One number', met: /\d/.test(password) },
    { text: 'One special character', met: /[!@#$%^&*(),.?":{}|<>]/.test(password) },
  ];

  const allRequirementsMet = passwordRequirements.every(req => req.met);
  const passwordsMatch = password === confirmPassword && confirmPassword.length > 0;

  const onSubmit = async (data: RegisterFormData) => {
    setError('');

    try {
      await authRegister({
        firstName: data.firstName,
        lastName: data.lastName,
        email: data.email,
        phone: data.phone ?? '',
        password: data.password,
      });
      navigate('/');
    } catch (err) {
      setError(getErrorMessage(err, 'Failed to create account'));
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-accent-light/5 to-blue-100 flex items-center justify-center p-4 py-12">
      <div className="w-full max-w-md">
        {/* Logo */}
        <div className="text-center mb-8">
          <Link to="/" className="inline-flex items-center gap-2">
            <Logo size="lg" className="h-10 w-10" />
            <span className="text-2xl font-bold text-gray-900">ecoMaid</span>
          </Link>
          <p className="mt-2 text-gray-600">Create your free account</p>
        </div>

        {/* Card */}
        <div className="bg-white rounded-2xl shadow-xl p-8">
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
            {error && (
              <div role="alert" className="bg-red-50 text-red-600 px-4 py-3 rounded-lg text-sm">
                {error}
              </div>
            )}

            {/* Name Fields */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <label htmlFor="firstName" className="block text-sm font-medium text-gray-700 mb-2">
                  First Name
                </label>
                <div className="relative">
                  <User className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-gray-400" aria-hidden="true" />
                  <input
                    type="text"
                    id="firstName"
                    autoComplete="given-name"
                    aria-invalid={errors.firstName ? 'true' : 'false'}
                    aria-describedby={errors.firstName ? 'firstName-error' : undefined}
                    {...register('firstName')}
                    className="w-full pl-10 pr-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent transition-all"
                    placeholder="John"
                  />
                </div>
                {errors.firstName && (
                  <p id="firstName-error" className="mt-1 text-sm text-red-600">{errors.firstName.message}</p>
                )}
              </div>
              <div>
                <label htmlFor="lastName" className="block text-sm font-medium text-gray-700 mb-2">
                  Last Name
                </label>
                <input
                  type="text"
                  id="lastName"
                  autoComplete="family-name"
                  aria-invalid={errors.lastName ? 'true' : 'false'}
                  aria-describedby={errors.lastName ? 'lastName-error' : undefined}
                  {...register('lastName')}
                  className="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent transition-all"
                  placeholder="Doe"
                />
                {errors.lastName && (
                  <p id="lastName-error" className="mt-1 text-sm text-red-600">{errors.lastName.message}</p>
                )}
              </div>
            </div>

            {/* Email */}
            <div>
              <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-2">
                Email
              </label>
              <div className="relative">
                <Mail className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-gray-400" aria-hidden="true" />
                <input
                  type="email"
                  id="email"
                  autoComplete="email"
                  inputMode="email"
                  spellCheck={false}
                  autoCapitalize="off"
                  aria-invalid={errors.email ? 'true' : 'false'}
                  aria-describedby={errors.email ? 'email-error' : undefined}
                  {...register('email')}
                  className="w-full pl-10 pr-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent transition-all"
                  placeholder="you@email.com"
                />
              </div>
              {errors.email && (
                <p id="email-error" className="mt-1 text-sm text-red-600">{errors.email.message}</p>
              )}
            </div>

            {/* Phone */}
            <div>
              <label htmlFor="phone" className="block text-sm font-medium text-gray-700 mb-2">
                Phone
              </label>
              <div className="relative">
                <Phone className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-gray-400" aria-hidden="true" />
                <input
                  type="tel"
                  id="phone"
                  autoComplete="tel"
                  inputMode="tel"
                  aria-invalid={errors.phone ? 'true' : 'false'}
                  aria-describedby={errors.phone ? 'phone-error' : undefined}
                  {...register('phone')}
                  className="w-full pl-10 pr-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent transition-all"
                  placeholder="(555) 123-4567"
                />
              </div>
              {errors.phone && (
                <p id="phone-error" className="mt-1 text-sm text-red-600">{errors.phone.message}</p>
              )}
            </div>

            {/* Password */}
            <div>
              <label htmlFor="password" className="block text-sm font-medium text-gray-700 mb-2">
                Password
              </label>
              <div className="relative">
                <Lock className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-gray-400" aria-hidden="true" />
                <input
                  type={showPassword ? 'text' : 'password'}
                  id="password"
                  autoComplete="new-password"
                  aria-invalid={errors.password ? 'true' : 'false'}
                  aria-describedby="password-requirements"
                  {...register('password')}
                  className="w-full pl-10 pr-12 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent transition-all"
                  placeholder="••••••••"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600"
                  aria-label={showPassword ? 'Hide password' : 'Show password'}
                  aria-pressed={showPassword}
                >
                  {showPassword ? <EyeOff className="h-5 w-5" aria-hidden="true" /> : <Eye className="h-5 w-5" aria-hidden="true" />}
                </button>
              </div>
              {errors.password && (
                <p className="mt-1 text-sm text-red-600">{errors.password.message}</p>
              )}

              {/* Password Requirements */}
              <div
                id="password-requirements"
                role="status"
                aria-live="polite"
                className={password.length > 0 ? 'mt-3 space-y-1' : 'sr-only'}
              >
                {password.length > 0 && passwordRequirements.map((req, i) => (
                  <div key={i} className={`flex items-center gap-2 text-xs ${req.met ? 'text-green-600' : 'text-gray-400'}`}>
                    <Check className={`h-3 w-3 ${req.met ? 'opacity-100' : 'opacity-0'}`} aria-hidden="true" />
                    <span>{req.text}</span>
                  </div>
                ))}
              </div>
            </div>

            {/* Confirm Password */}
            <div>
              <label htmlFor="confirmPassword" className="block text-sm font-medium text-gray-700 mb-2">
                Confirm Password
              </label>
              <div className="relative">
                <Lock className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-gray-400" aria-hidden="true" />
                <input
                  type={showPassword ? 'text' : 'password'}
                  id="confirmPassword"
                  autoComplete="new-password"
                  aria-invalid={errors.confirmPassword || (confirmPassword.length > 0 && !passwordsMatch) ? 'true' : 'false'}
                  aria-describedby={errors.confirmPassword ? 'confirmPassword-error' : undefined}
                  {...register('confirmPassword')}
                  className={`w-full pl-10 pr-12 py-3 border rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent transition-all ${
                    confirmPassword.length > 0
                      ? passwordsMatch
                        ? 'border-green-500'
                        : 'border-red-500'
                      : 'border-gray-300'
                  }`}
                  placeholder="••••••••"
                />
                {confirmPassword.length > 0 && (
                  <div
                    aria-hidden="true"
                    className={`absolute right-3 top-1/2 -translate-y-1/2 ${passwordsMatch ? 'text-green-500' : 'text-red-500'}`}
                  >
                    {passwordsMatch ? <Check className="h-5 w-5" /> : <span className="text-xs">&#10007;</span>}
                  </div>
                )}
              </div>
              {errors.confirmPassword && (
                <p id="confirmPassword-error" className="mt-1 text-sm text-red-600">{errors.confirmPassword.message}</p>
              )}
            </div>

            {/* Terms */}
            <label className="flex items-start gap-3 cursor-pointer">
              <input
                type="checkbox"
                {...register('acceptTerms')}
                className="w-4 h-4 mt-0.5 text-brand border-gray-300 rounded focus:ring-brand"
              />
              <span className="text-sm text-gray-600">
                I agree to the{' '}
                <Link to="/terms" className="text-brand hover:underline">
                  terms of service
                </Link>{' '}
                and{' '}
                <Link to="/privacy" className="text-brand hover:underline">
                  privacy policy
                </Link>
              </span>
            </label>
            {errors.acceptTerms && (
              <p className="text-sm text-red-600">{errors.acceptTerms.message}</p>
            )}

            {/* Submit */}
            <button
              type="submit"
              disabled={isLoading || !allRequirementsMet || !passwordsMatch}
              className="w-full py-3 px-4 bg-brand hover:bg-brand-dark text-white font-medium rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
            >
              {isLoading ? (
                <>
                  <div className="w-5 h-5 border-2 border-white border-t-transparent rounded-full animate-spin" />
                  Creating account...
                </>
              ) : (
                'Create Account'
              )}
            </button>
          </form>

          {/* Login Link */}
          <p className="mt-6 text-center text-sm text-gray-600">
            Already have an account?{' '}
            <Link to="/login" className="text-brand hover:text-brand-dark font-medium">
              Sign in
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}
