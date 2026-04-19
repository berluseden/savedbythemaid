import React, { useState, useCallback, useEffect } from 'react';
import { Button, Input, Modal } from '@/components/ui';
import api from '@/lib/api';
import { useAuth } from '@/contexts/AuthContext';
import type { BookingData } from './types';

interface ContactStepProps {
  data: BookingData;
  onChange: (data: Partial<BookingData>) => void;
  onNext: () => void;
  onBack: () => void;
}

export const ContactStep = React.memo(function ContactStep({
  data,
  onChange,
  onNext,
  onBack,
}: ContactStepProps) {
  const { login, user, isAuthenticated } = useAuth();
  const [errors, setErrors] = useState<Record<string, string>>({});

  // Pre-fill contact fields from the logged-in user's profile.
  // Only fills empty fields — never overwrites something the user already typed.
  useEffect(() => {
    if (!user) return;
    const updates: Partial<BookingData> = {};
    if (!data.firstName && user.firstName) updates.firstName = user.firstName;
    if (!data.lastName && user.lastName) updates.lastName = user.lastName;
    if (!data.email && user.email) updates.email = user.email;
    if (!data.phone && user.phone) updates.phone = user.phone;
    if (Object.keys(updates).length > 0) onChange(updates);
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [user]);
  const [showPasswordModal, setShowPasswordModal] = useState(false);
  const [showLoginModal, setShowLoginModal] = useState(false);
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [passwordError, setPasswordError] = useState('');
  const [checkingEmail, setCheckingEmail] = useState(false);
  const [loginPassword, setLoginPassword] = useState('');
  const [loginError, setLoginError] = useState('');
  const [isLoggingIn, setIsLoggingIn] = useState(false);

  const checkEmailAndProceed = useCallback(async () => {
    // Validate basic fields first
    const newErrors: Record<string, string> = {};
    if (!data.firstName) newErrors.firstName = 'First name is required';
    if (!data.lastName) newErrors.lastName = 'Last name is required';
    if (!data.email || !/\S+@\S+\.\S+/.test(data.email)) newErrors.email = 'Valid email is required';
    if (!data.phone || data.phone.length < 10) newErrors.phone = 'Valid phone is required';
    if (!data.address) newErrors.address = 'Address is required';
    if (!data.city) newErrors.city = 'City is required';
    if (!data.state) newErrors.state = 'State is required';

    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }
    setErrors({});

    // Already authenticated — skip email/password check entirely
    if (isAuthenticated) {
      onNext();
      return;
    }

    // If already has password, continue
    if (data.password) {
      onNext();
      return;
    }

    // Check if email exists and whether it has a password set
    setCheckingEmail(true);
    try {
      const response = await api.get<{ email: string; exists: boolean; hasPassword: boolean }>(
        `/auth/check-email?email=${encodeURIComponent(data.email)}`
      );

      if (response.data.exists && response.data.hasPassword) {
        // Account exists with a password — must authenticate before booking
        // can be associated. No "guest fallback" available (anti-fraud).
        setShowLoginModal(true);
      } else if (response.data.exists) {
        // Legacy account without a password (created via prior guest checkout)
        // — continue, backend will associate the booking by email.
        onNext();
      } else {
        // Brand new email — offer optional account creation
        setShowPasswordModal(true);
      }
    } catch {
      // If verification fails (network), let the user retry rather than
      // silently associating the booking to an account they may not own.
      setErrors({ email: 'Could not verify your email. Please try again.' });
    } finally {
      setCheckingEmail(false);
    }
  }, [data, onNext]);

  const handleSkipPassword = useCallback(() => {
    setShowPasswordModal(false);
    onNext();
  }, [onNext]);

  const handleCreatePassword = useCallback(() => {
    if (password.length < 8) {
      setPasswordError('Password must be at least 8 characters');
      return;
    }
    if (password !== confirmPassword) {
      setPasswordError('Passwords do not match');
      return;
    }

    // Save password and continue
    onChange({ password });
    setShowPasswordModal(false);
    onNext();
  }, [password, confirmPassword, onChange, onNext]);

  const handleLogin = useCallback(async () => {
    if (!loginPassword) {
      setLoginError('Please enter your password');
      return;
    }

    setIsLoggingIn(true);
    setLoginError('');

    try {
      await login(data.email, loginPassword);
      setShowLoginModal(false);
      setLoginPassword('');
      // User authenticated, continue to next step
      onNext();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Invalid password. Please try again.';
      setLoginError(errorMessage);
    } finally {
      setIsLoggingIn(false);
    }
  }, [loginPassword, data.email, login, onNext]);

  const handleLoginKeyDown = useCallback((e: React.KeyboardEvent) => {
    if (e.key === 'Enter') handleLogin();
  }, [handleLogin]);

  // "Continue as guest" is intentionally removed when the account already
  // has a password — the user must authenticate to bind the booking. To
  // book under a different identity, they must use a different email.
  const handleUseDifferentEmail = useCallback(() => {
    setShowLoginModal(false);
    setLoginPassword('');
    setLoginError('');
  }, []);

  return (
    <div>
      <h2 className="text-2xl font-bold text-gray-900 mb-2">Contact Information</h2>
      <p className="text-gray-600 mb-4">Tell us where to send the cleaning crew.</p>

      {/* Soft-reserve reminder — visible only while the server hold is active.
          The main countdown lives in the BookingPage header; this banner
          keeps the context visible while the user fills out the form. */}
      {data.expiresAt && (
        <div className="flex items-center gap-2 bg-amber-50 border border-amber-200 rounded-lg px-4 py-2 mb-4 text-sm text-amber-800">
          <span>⏱</span>
          <span>Your time slot is held — please complete this step before your reservation expires.</span>
        </div>
      )}

      {/* Modal to create password */}
      <Modal
        isOpen={showPasswordModal}
        onClose={handleSkipPassword}
        title="Create Your Account"
        showCloseButton={false}
      >
        <div className="space-y-4">
          <p className="text-gray-600 text-sm">
            Create a password to manage your bookings online — or skip and we'll email your confirmation.
          </p>

          <Input
            label="Password"
            type="password"
            value={password}
            onChange={(e) => { setPassword(e.target.value); setPasswordError(''); }}
            placeholder="Minimum 8 characters"
            error={passwordError && password.length < 8 ? passwordError : undefined}
            aria-label="Create password"
          />

          <Input
            label="Confirm Password"
            type="password"
            value={confirmPassword}
            onChange={(e) => { setConfirmPassword(e.target.value); setPasswordError(''); }}
            placeholder="Re-enter your password"
            error={passwordError && password !== confirmPassword ? passwordError : undefined}
            aria-label="Confirm password"
          />

          {passwordError && (
            <p className="text-sm text-red-500">{passwordError}</p>
          )}

          <div className="flex gap-3 pt-2">
            <Button variant="outline" onClick={handleSkipPassword} className="flex-1" aria-label="Skip account creation and continue as guest">
              Skip, continue as guest
            </Button>
            <Button onClick={handleCreatePassword} className="flex-1" aria-label="Create account and continue">
              Create Account & Continue
            </Button>
          </div>
        </div>
      </Modal>

      {/* Modal when email is already registered */}
      <Modal
        isOpen={showLoginModal}
        onClose={() => { setShowLoginModal(false); setLoginPassword(''); setLoginError(''); }}
        title="Sign In to Your Account"
        showCloseButton={true}
      >
        <div className="space-y-4">
          <p className="text-gray-600">
            The email <strong>{data.email}</strong> already has an account. Sign in to link this booking to your profile.
          </p>

          <Input
            label="Password"
            type="password"
            value={loginPassword}
            onChange={(e) => { setLoginPassword(e.target.value); setLoginError(''); }}
            placeholder="Enter your password"
            error={loginError}
            onKeyDown={handleLoginKeyDown}
            aria-label="Login password"
          />

          <a href="/forgot-password" className="text-sm text-brand hover:text-brand-dark">
            Forgot your password?
          </a>

          {loginError && (
            <p className="text-sm text-red-500">{loginError}</p>
          )}

          <div className="flex flex-col gap-3 pt-2">
            <Button
              onClick={handleLogin}
              loading={isLoggingIn}
              className="w-full"
              aria-label="Login and continue"
            >
              Login & Continue
            </Button>
            <Button
              variant="outline"
              onClick={handleUseDifferentEmail}
              className="w-full"
              disabled={isLoggingIn}
              aria-label="Continue with a different email"
            >
              Continue with a different email
            </Button>
          </div>
        </div>
      </Modal>


      <div className="space-y-4">
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <Input
            label="First Name"
            autoComplete="given-name"
            value={data.firstName}
            onChange={(e) => onChange({ firstName: e.target.value })}
            error={errors.firstName}
          />
          <Input
            label="Last Name"
            autoComplete="family-name"
            value={data.lastName}
            onChange={(e) => onChange({ lastName: e.target.value })}
            error={errors.lastName}
          />
        </div>
        <div>
          <Input
            label="Email"
            type="email"
            autoComplete="email"
            inputMode="email"
            spellCheck={false}
            autoCapitalize="off"
            value={data.email}
            onChange={(e) => {
              onChange({ email: e.target.value, password: undefined });
              setErrors(prev => ({ ...prev, email: '' }));
            }}
            error={errors.email}
          />
          <div aria-live="polite" className="min-h-[1.25rem]">
            {checkingEmail && <p className="text-sm text-gray-500 mt-1">Checking email…</p>}
            {data.password && (
              <p className="text-sm text-green-600 mt-1">✓ Account will be created with this email</p>
            )}
          </div>
        </div>

        <Input
          label="Phone"
          type="tel"
          autoComplete="tel"
          inputMode="tel"
          value={data.phone}
          onChange={(e) => onChange({ phone: e.target.value.replace(/\D/g, '').slice(0, 10) })}
          error={errors.phone}
        />
        <Input
          label="Street Address"
          autoComplete="street-address"
          value={data.address}
          onChange={(e) => onChange({ address: e.target.value })}
          error={errors.address}
        />
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <Input
            label="City"
            autoComplete="address-level2"
            value={data.city}
            onChange={(e) => onChange({ city: e.target.value })}
            error={errors.city}
          />
          <Input
            label="State"
            autoComplete="address-level1"
            value={data.state}
            onChange={(e) => onChange({ state: e.target.value.toUpperCase().slice(0, 2) })}
            error={errors.state}
            maxLength={2}
          />
        </div>
        <div>
          <label htmlFor="special-instructions" className="block text-sm font-medium text-gray-700 mb-1">
            Special Instructions (optional)
          </label>
          <textarea
            id="special-instructions"
            value={data.specialInstructions}
            onChange={(e) => onChange({ specialInstructions: e.target.value })}
            rows={3}
            className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand"
            placeholder="Gate code, parking instructions, pet info, etc."
          />
        </div>
      </div>

      <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-between sm:gap-0 mt-8">
        <Button variant="outline" onClick={onBack} aria-label="Go back" className="w-full sm:w-auto">Back</Button>
        <Button onClick={checkEmailAndProceed} loading={checkingEmail} aria-label="Review booking" className="w-full sm:w-auto">
          Review Booking
        </Button>
      </div>
    </div>
  );
});
