import React, { useState, useCallback } from 'react';
import { useMutation } from '@tanstack/react-query';
import { Calendar, CreditCard } from 'lucide-react';
import { Button } from '@/components/ui';
import { Alert } from '@/components/ui/Alert';
import { bookingApi, type EstimateResponse, type BookingConfirmation } from '@/lib/api';
import { authStorage } from '@/shared/lib/auth-storage';
import { formatCurrency } from '@/lib/utils';
import type { BookingData } from './types';

interface ConfirmStepProps {
  data: BookingData;
  estimate: EstimateResponse | null;
  onBack: () => void;
  onSuccess: (confirmation: BookingConfirmation) => void;
}

export const ConfirmStep = React.memo(function ConfirmStep({
  data,
  estimate,
  onBack,
  onSuccess,
}: ConfirmStepProps) {
  const [error, setError] = useState<string | null>(null);

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
        additionalServiceIds: data.additionalServiceIds,
        subtotal: estimate?.subtotal || 0,
        tax: 0,
        discount: estimate?.discount || 0,
        total: estimate?.total || 0,
        contactName: `${data.firstName} ${data.lastName}`,
        contactPhone: data.phone,
        contactEmail: data.email,
        password: data.password || undefined,
        specialInstructions: data.specialInstructions,
      }),
    onSuccess: (response) => {
      setError(null);
      // If user was created, save tokens via centralized auth storage
      if (response.data.authToken) {
        authStorage.setToken(response.data.authToken.accessToken, true);
      }
      onSuccess(response.data);
    },
    onError: (err: unknown) => {
      // Try to extract detailed error message
      const maybe = err as { response?: { data?: { message?: string; details?: string } }; message?: string };
      const message = maybe.response?.data?.message || maybe.response?.data?.details || maybe.message || 'Something went wrong. Please try again.';
      setError(message);
    },
  });

  const handleConfirm = useCallback(() => {
    setError(null);
    confirmBooking.mutate();
  }, [confirmBooking]);

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
        <div className="bg-[#b8e07c]/10 rounded-xl p-6">
          <div className="flex items-center gap-4">
            <div className="flex h-14 w-14 items-center justify-center rounded-full bg-[#b8e07c]/20">
              <Calendar className="h-7 w-7 text-[#2196f3]" />
            </div>
            <div>
              <p className="text-lg font-semibold text-gray-900">
                {scheduledDate.toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric' })}
              </p>
              <p className="text-[#2196f3] font-medium">
                {new Date(`2000-01-01T${data.timeSlot}`).toLocaleTimeString('en-US', {
                  hour: 'numeric',
                  minute: '2-digit',
                  hour12: true,
                })}
              </p>
            </div>
          </div>
        </div>

        {/* Address */}
        <div className="border rounded-xl p-4">
          <h3 className="font-semibold text-gray-900 mb-2">Service Address</h3>
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
                <span className="text-xl font-bold text-[#2196f3]">{formatCurrency(estimate.total)}</span>
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

      <div className="flex justify-between mt-8">
        <Button variant="outline" onClick={onBack} aria-label="Go back">Back</Button>
        <Button onClick={handleConfirm} loading={confirmBooking.isPending} aria-label="Confirm booking">
          Confirm Booking
        </Button>
      </div>
    </div>
  );
});
