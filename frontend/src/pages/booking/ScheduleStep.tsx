import React, { useState, useCallback } from 'react';
import { useQuery, useMutation } from '@tanstack/react-query';
import { Button, Spinner } from '@/components/ui';
import { bookingApi, type EstimateResponse } from '@/lib/api';
import { cn } from '@/lib/utils';
import type { BookingData } from './types';

interface ScheduleStepProps {
  data: BookingData;
  estimate: EstimateResponse | null;
  onChange: (data: Partial<BookingData>) => void;
  onNext: () => void;
  onBack: () => void;
}

export const ScheduleStep = React.memo(function ScheduleStep({
  data,
  estimate,
  onChange,
  onNext,
  onBack,
}: ScheduleStepProps) {
  const [selectedDate, setSelectedDate] = useState(data.date);

  // Generate next 14 days
  const dates = Array.from({ length: 14 }, (_, i) => {
    const date = new Date();
    date.setDate(date.getDate() + i + 1);
    return date.toISOString().split('T')[0];
  });

  const { data: availability, isLoading } = useQuery({
    queryKey: ['availability', selectedDate, data.zipCode, estimate?.estimatedMinutes],
    queryFn: () =>
      bookingApi.getAvailability(data.zipCode, selectedDate, estimate?.estimatedMinutes || 120),
    enabled: !!selectedDate && !!estimate,
  });

  const createReservation = useMutation({
    mutationFn: () =>
      bookingApi.createSoftReserve({
        date: new Date(data.date),
        startTime: data.timeSlot,
        estimatedMinutes: estimate?.estimatedMinutes || 120,
        zipCode: data.zipCode,
        employeeId: data.employeeId,
      }),
    onSuccess: (response) => {
      onChange({
        softReserveId: response.data.softReserveId,
        sessionId: response.data.sessionId,
        expiresAt: response.data.expiresAt,
      });
      onNext();
    },
  });

  const handleDateSelect = useCallback((date: string) => {
    setSelectedDate(date);
    onChange({ date, timeSlot: '' });
  }, [onChange]);

  const handleTimeSelect = useCallback((timeSlot: string) => {
    onChange({ timeSlot });
  }, [onChange]);

  const handleContinue = useCallback(() => {
    createReservation.mutate();
  }, [createReservation]);

  return (
    <div>
      <h2 className="text-2xl font-bold text-gray-900 mb-2">Pick a date & time</h2>
      <p className="text-gray-600 mb-8">Choose when you'd like us to come.</p>

      {/* Date Selection */}
      <div className="mb-8">
        <label className="block text-sm font-medium text-gray-700 mb-3">Select Date</label>
        <div className="flex gap-2 overflow-x-auto pb-2">
          {dates.map((date) => {
            const d = new Date(date);
            const isSelected = selectedDate === date;
            return (
              <button
                key={date}
                onClick={() => handleDateSelect(date)}
                aria-label={`Select date ${d.toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' })}`}
                className={cn(
                  'flex-shrink-0 w-16 p-3 rounded-lg border-2 text-center transition-all',
                  isSelected
                    ? 'border-[#2196f3] bg-[#b8e07c]/10'
                    : 'border-gray-200 hover:border-gray-300'
                )}
              >
                <p className="text-xs text-gray-500">{d.toLocaleDateString('en-US', { weekday: 'short' })}</p>
                <p className="text-lg font-bold">{d.getDate()}</p>
                <p className="text-xs text-gray-500">{d.toLocaleDateString('en-US', { month: 'short' })}</p>
              </button>
            );
          })}
        </div>
      </div>

      {/* Time Selection */}
      {selectedDate && (
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-3">Select Time</label>
          {isLoading ? (
            <div className="flex justify-center py-8">
              <Spinner />
            </div>
          ) : (
            <div className="grid grid-cols-3 sm:grid-cols-4 gap-3">
              {availability?.data.slots?.map((slot) => {
                const isAvailable = slot.isAvailable ?? slot.availableEmployeeIds.length > 0;
                const isSelected = data.timeSlot === slot.startTime;

                return (
                  <button
                    key={slot.startTime}
                    onClick={() => {
                      if (isAvailable) {
                        handleTimeSelect(slot.startTime);
                        onChange({ employeeId: slot.availableEmployeeIds[0] ?? null });
                      }
                    }}
                    disabled={!isAvailable}
                    aria-label={`Select time ${slot.formattedTime}${!isAvailable ? ' (unavailable)' : ''}`}
                    className={cn(
                      'p-3 rounded-lg border-2 text-sm font-medium transition-all',
                      !isAvailable && 'opacity-50 cursor-not-allowed bg-gray-100',
                      isSelected
                        ? 'border-[#2196f3] bg-[#b8e07c]/10 text-[#29338c]'
                        : 'border-gray-200 hover:border-gray-300'
                    )}
                  >
                    {slot.formattedTime}
                  </button>
                );
              })}
            </div>
          )}
        </div>
      )}

      <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-between sm:gap-0 mt-8">
        <Button variant="outline" onClick={onBack} aria-label="Go back" className="w-full sm:w-auto">Back</Button>
        <Button
          onClick={handleContinue}
          loading={createReservation.isPending}
          disabled={!data.date || !data.timeSlot}
          aria-label="Continue to next step"
          className="w-full sm:w-auto"
        >
          Continue
        </Button>
      </div>
    </div>
  );
});
