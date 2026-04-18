import { useState, useCallback } from 'react';
import { bookingApi, type EstimateResponse } from '@/lib/api';
import type { BookingData } from './types';

export function useReservationTimer(
  data: BookingData,
  estimate: EstimateResponse | null,
  updateData: (updates: Partial<BookingData>) => void,
  goToStep: (step: 'schedule') => void,
) {
  const [showExpireModal, setShowExpireModal] = useState(false);
  const [isRenewing, setIsRenewing] = useState(false);
  const [renewalError, setRenewalError] = useState<string | null>(null);

  const handleExpire = useCallback(() => {
    setShowExpireModal(true);
    setRenewalError(null);
  }, []);

  const handleResetAfterExpiry = useCallback(() => {
    setShowExpireModal(false);
    updateData({
      softReserveId: undefined,
      expiresAt: undefined,
      timeSlot: '',
      employeeId: 0,
    });
    setRenewalError(null);
    goToStep('schedule');
  }, [updateData, goToStep]);

  const handleTryRenew = useCallback(async () => {
    setIsRenewing(true);
    setRenewalError(null);
    try {
      const response = await bookingApi.createSoftReserve({
        date: new Date(data.date),
        startTime: data.timeSlot,
        estimatedMinutes: estimate?.estimatedMinutes || 120,
        zipCode: data.zipCode,
        employeeId: data.employeeId,
        sessionId: data.sessionId,
      });

      // Success
      updateData({
        softReserveId: response.data.softReserveId,
        sessionId: response.data.sessionId,
        expiresAt: response.data.expiresAt,
      });
      setShowExpireModal(false);
    } catch {
      setRenewalError('The selected time slot is unfortunately no longer available.');
    } finally {
      setIsRenewing(false);
    }
  }, [data.date, data.timeSlot, data.zipCode, data.employeeId, data.sessionId, estimate?.estimatedMinutes, updateData]);

  return {
    showExpireModal,
    isRenewing,
    renewalError,
    handleExpire,
    handleResetAfterExpiry,
    handleTryRenew,
  };
}
