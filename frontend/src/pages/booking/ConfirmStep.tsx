import React, { useState, useCallback, useEffect, useRef } from 'react';
import { useMutation } from '@tanstack/react-query';
import { Calendar, CreditCard } from 'lucide-react';
import { Button } from '@/components/ui';
import { Alert } from '@/components/ui/Alert';
import { bookingApi, type EstimateResponse, type BookingConfirmation } from '@/lib/api';
import { formatCurrency } from '@/lib/utils';
import type { BookingData } from './types';

interface ConfirmStepProps {
  data: BookingData;
  estimate: EstimateResponse | null;
  onBack: () => void;
  onSuccess: (confirmation: BookingConfirmation) => void;
  onGoToSchedule: () => void;
  onGoToContact: () => void;
}

export const ConfirmStep = React.memo(function ConfirmStep({
  data,
  estimate,
  onBack,
  onSuccess,
  onGoToSchedule,
  onGoToContact,
}: ConfirmStepProps) {
  const [error, setError] = useState<string | null>(null);
  const redirectTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    return () => {
      if (redirectTimerRef.current !== null) {
        clearTimeout(redirectTimerRef.current);
      }
    };
  }, []);

  const confirmBooking = useMutation({
    mutationFn: () =>
      bookingApi.confirmBooking({
        softReserveId: data.softReserveId || 0,
        sessionId: data.sessionId || '',
        zipCode: data.zipCode,
        address: data.address,
        city: data.city,
        state: data.state,
        serviceTypeId: data.serviceTypeId,
        cleaningPlaceId: data.cleaningPlaceId,
        bedrooms: data.bedrooms,
        bathrooms: data.bathrooms,
        squareFootage: data.squareFootage,
        // Pricing modifiers — must match what was sent to /estimate, otherwise
        // the backend recalculation diverges from what the customer saw.
        dirtLevel: data.dirtLevel,
        hasPets: data.hasPets,
        floorLevel: data.floorLevel,
        hasElevator: data.hasElevator,
        isFirstTime: data.isFirstTime,
        additionalServiceIds: data.additionalServiceIds,
        rooms: data.rooms.length > 0 ? data.rooms : undefined,
        subtotal: estimate?.subtotal || 0,
        tax: 0,
        discount: estimate?.discount || 0,
        total: estimate?.total || 0,
        // Recurrence — drives discount AND triggers creation of recurring meets
        recurrenceType: data.recurrenceType,
        recurrenceEndDate: data.recurrenceEndDate,
        contactName: `${data.firstName} ${data.lastName}`,
        contactPhone: data.phone,
        contactEmail: data.email,
        password: data.password || undefined,
        specialInstructions: data.specialInstructions,
      }),
    onSuccess: (response) => {
      setError(null);
      // Backend sets HttpOnly cookies for the (possibly new) user on confirm —
      // nothing to store on the client.
      onSuccess(response.data);
    },
    onError: (err: unknown) => {
      const maybe = err as {
        response?: { status?: number; data?: { code?: string; message?: string; details?: string; errors?: Record<string, string[]> } };
        message?: string;
      };
      if (maybe.response?.status === 409 && maybe.response?.data?.code === 'login_required') {
        setError('An account with this email already exists. Please go back and sign in to continue.');
        return;
      }
      if (maybe.response?.status === 400) {
        setError('Your time slot expired. Taking you back to pick a new time…');
        redirectTimerRef.current = setTimeout(() => {
          onGoToSchedule();
        }, 1500);
        return;
      }
      const message = maybe.response?.data?.message || maybe.response?.data?.details || maybe.message || 'Something went wrong. Please try again.';
      setError(message);
    },
  });

  const handleConfirm = useCallback(() => {
    if (!data.softReserveId || !data.sessionId) {
      setError('Your time slot expired. Taking you back to pick a new time…');
      redirectTimerRef.current = setTimeout(() => {
        onGoToSchedule();
      }, 1500);
      return;
    }
    setError(null);
    confirmBooking.mutate();
  }, [confirmBooking, data.softReserveId, data.sessionId, onGoToSchedule]);

  const scheduledDate = new Date(data.date);

  return (
    <div>
      <h2 className="text-2xl font-bold text-gray-900 mb-2">Review & Confirm</h2>
      <p className="text-gray-600 mb-8">Please review your booking details before confirming.</p>

      {error && (
        <Alert variant="error" className="mb-6" title="Booking Error">
          {error}
        </Alert>
      )}

      <div className="space-y-6">
        {/* Schedule Summary */}
        <div className="bg-secondary/10 rounded-xl p-6">
          <div className="flex items-start justify-between gap-4">
            <div className="flex items-center gap-4">
              <div className="flex h-14 w-14 items-center justify-center rounded-full bg-secondary/20">
                <Calendar className="h-7 w-7 text-brand" />
              </div>
              <div>
                <p className="text-lg font-semibold text-gray-900">
                  {scheduledDate.toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric' })}
                </p>
                <p className="text-brand font-medium">
                  {new Date(`2000-01-01T${data.timeSlot}`).toLocaleTimeString('en-US', {
                    hour: 'numeric',
                    minute: '2-digit',
                    hour12: true,
                  })}
                </p>
              </div>
            </div>
            <button
              type="button"
              onClick={onGoToSchedule}
              className="text-sm text-brand hover:underline shrink-0"
            >
              Edit
            </button>
          </div>
        </div>

        {/* Address */}
        <div className="border rounded-xl p-4">
          <div className="flex items-center justify-between mb-2">
            <h3 className="font-semibold text-gray-900">Service Address</h3>
            <button
              type="button"
              onClick={onGoToContact}
              className="text-sm text-brand hover:underline"
            >
              Edit
            </button>
          </div>
          <p className="text-gray-600">
            {data.address}<br />
            {data.city}, {data.state} {data.zipCode}
          </p>
        </div>

        {/* Price Breakdown */}
        {estimate && (
          <div className="border rounded-xl p-4">
            <h3 className="font-semibold text-gray-900 mb-4">Price Summary</h3>
            <div className="space-y-2">
              <div className="flex justify-between text-sm">
                <span className="text-gray-600">Subtotal</span>
                <span className="font-medium">{formatCurrency(estimate.subtotal)}</span>
              </div>
              {estimate.discount > 0 && (
                <div className="flex justify-between text-sm text-green-600">
                  <span>Discount ({estimate.discountPercent}%)</span>
                  <span className="font-medium">-{formatCurrency(estimate.discount)}</span>
                </div>
              )}
              <div className="flex justify-between text-sm">
                <span className="text-gray-600">Duration</span>
                <span className="font-medium">{estimate.formattedDuration}</span>
              </div>
              <div className="border-t pt-2 mt-2 flex justify-between">
                <span className="font-semibold text-gray-900">Total</span>
                <span className="text-xl font-bold text-brand">{formatCurrency(estimate.total)}</span>
              </div>
            </div>
          </div>
        )}

        {/* Payment notice */}
        <div className="flex items-center gap-3 text-sm text-gray-600 bg-gray-50 rounded-lg p-4">
          <CreditCard className="h-5 w-5 text-gray-400" />
          <span>Payment will be collected after service completion</span>
        </div>
      </div>

      <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-between sm:gap-0 mt-8">
        <Button variant="outline" onClick={onBack} aria-label="Go back" className="w-full sm:w-auto">Back</Button>
        <Button onClick={handleConfirm} loading={confirmBooking.isPending} aria-label="Confirm booking" className="w-full sm:w-auto">
          Confirm Booking
        </Button>
      </div>
    </div>
  );
});
