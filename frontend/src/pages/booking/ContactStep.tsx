import React, { useState, useCallback } from 'react';
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
  const { login } = useAuth();
  const [errors, setErrors] = useState<Record<string, string>>({});
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

    // If already has password, continue
    if (data.password) {
      onNext();
      return;
    }

    // Check if email exists
    setCheckingEmail(true);
    try {
      const response = await api.get<{ email: string; exists: boolean }>(`/auth/check-email?email=${encodeURIComponent(data.email)}`);

      if (response.data.exists) {
        // Email exists - show modal with options
        setShowLoginModal(true);
      } else {
        // New email, show modal to create password
        setShowPasswordModal(true);
      }
    } catch (err) {
      // If email verification fails, continue without password (backend will handle it)
      onNext();
    } finally {
      setCheckingEmail(false);
    }
  }, [data, onNext]);

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

  const handleContinueAsGuest = useCallback(() => {
    setShowLoginModal(false);
    setLoginPassword('');
    setLoginError('');
    // Continue without password - backend will associate booking with existing account
    onNext();
  }, [onNext]);

  return (
    <div>
      <h2 className="text-2xl font-bold text-gray-900 mb-2">Contact Information</h2>
      <p className="text-gray-600 mb-8">Tell us where to send the cleaning crew.</p>

      {/* Modal to create password */}
      <Modal
        isOpen={showPasswordModal}
        onClose={() => setShowPasswordModal(false)}
        title="Create Your Account"
        showCloseButton={false}
      >
        <div className="space-y-4">
          <p className="text-gray-600 text-sm">
            Create a password to track your bookings, manage appointments, and get exclusive offers.
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
            <Button variant="outline" onClick={() => setShowPasswordModal(false)} className="flex-1" aria-label="Cancel account creation">
              Cancel
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
        title="Welcome Back!"
        showCloseButton={true}
      >
        <div className="space-y-4">
          <p className="text-gray-600">
            The email <strong>{data.email}</strong> is already registered. Please enter your password to continue.
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

          <a href="/forgot-password" className="text-sm text-[#2196f3] hover:text-[#29338c]">
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
              onClick={handleContinueAsGuest}
              className="w-full"
              disabled={isLoggingIn}
              aria-label="Continue as guest"
            >
              Continue as Guest
            </Button>
          </div>

          <p className="text-xs text-gray-500 text-center">
            If you continue as guest, a confirmation email will be sent to {data.email}
          </p>
        </div>
      </Modal>


      <div className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <Input
            label="First Name"
            value={data.firstName}
            onChange={(e) => onChange({ firstName: e.target.value })}
            error={errors.firstName}
            aria-label="First name"
          />
          <Input
            label="Last Name"
            value={data.lastName}
            onChange={(e) => onChange({ lastName: e.target.value })}
            error={errors.lastName}
            aria-label="Last name"
          />
        </div>
        <div>
          <Input
            label="Email"
            type="email"
            value={data.email}
            onChange={(e) => {
              onChange({ email: e.target.value, password: undefined });
              setErrors(prev => ({ ...prev, email: '' }));
            }}
            error={errors.email}
            aria-label="Email address"
          />
          {checkingEmail && <p className="text-sm text-gray-500 mt-1">Checking email...</p>}
          {data.password && (
            <p className="text-sm text-green-600 mt-1">✓ Account will be created with this email</p>
          )}
        </div>

        <Input
          label="Phone"
          type="tel"
          value={data.phone}
          onChange={(e) => onChange({ phone: e.target.value.replace(/\D/g, '').slice(0, 10) })}
          error={errors.phone}
          aria-label="Phone number"
        />
        <Input
          label="Street Address"
          value={data.address}
          onChange={(e) => onChange({ address: e.target.value })}
          error={errors.address}
          aria-label="Street address"
        />
        <div className="grid grid-cols-2 gap-4">
          <Input
            label="City"
            value={data.city}
            onChange={(e) => onChange({ city: e.target.value })}
            error={errors.city}
            aria-label="City"
          />
          <Input
            label="State"
            value={data.state}
            onChange={(e) => onChange({ state: e.target.value.toUpperCase().slice(0, 2) })}
            error={errors.state}
            maxLength={2}
            aria-label="State"
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Special Instructions (optional)</label>
          <textarea
            value={data.specialInstructions}
            onChange={(e) => onChange({ specialInstructions: e.target.value })}
            rows={3}
            className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-[#2196f3]"
            placeholder="Gate code, parking instructions, pet info, etc."
            aria-label="Special instructions"
          />
        </div>
      </div>

      <div className="flex justify-between mt-8">
        <Button variant="outline" onClick={onBack} aria-label="Go back">Back</Button>
        <Button onClick={checkEmailAndProceed} loading={checkingEmail} aria-label="Review booking">
          Review Booking
        </Button>
      </div>
    </div>
  );
});
