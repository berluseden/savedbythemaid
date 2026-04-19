import React, { useState, useCallback } from 'react';
import { useQuery, useMutation } from '@tanstack/react-query';
import { Home } from 'lucide-react';
import { Button, Spinner } from '@/components/ui';
import { bookingApi, type ServiceType, type CleaningPlace, type EstimateResponse } from '@/lib/api';
import { cn, formatCurrency } from '@/lib/utils';
import type { BookingData } from './types';

interface DetailsStepProps {
  data: BookingData;
  onChange: (data: Partial<BookingData>) => void;
  onEstimate: (estimate: EstimateResponse) => void;
  onNext: () => void;
  onBack: () => void;
}

export const DetailsStep = React.memo(function DetailsStep({
  data,
  onChange,
  onEstimate,
  onNext,
  onBack,
}: DetailsStepProps) {
  const [estimateError, setEstimateError] = useState('');

  const { data: cleaningPlaces, isLoading: loadingPlaces } = useQuery({
    queryKey: ['cleaningPlaces'],
    queryFn: () => bookingApi.getCleaningPlaces(),
  });

  const { data: serviceTypes } = useQuery({
    queryKey: ['serviceTypes'],
    queryFn: () => bookingApi.getServiceTypes(),
  });

  const { data: additionalServices } = useQuery({
    queryKey: ['additionalServices'],
    queryFn: () => bookingApi.getAdditionalServices(),
  });

  const selectedService = serviceTypes?.data.find((s: ServiceType) => s.id === data.serviceTypeId);

  // Quick client-side preview (just base + extras, no multipliers)
  const previewTotal = (() => {
    if (!selectedService) return 0;
    const base = selectedService.price;
    const bedroomExtra = Math.max(0, data.bedrooms - 1) * selectedService.pricePerBedroom;
    const bathroomExtra = Math.max(0, data.bathrooms - 1) * selectedService.pricePerBathroom;
    const extrasTotal = data.additionalServiceIds.reduce((sum, id) => {
      const extra = additionalServices?.data.find((s: { id: number; price: number }) => s.id === id);
      return sum + (extra?.price || 0);
    }, 0);
    return base + bedroomExtra + bathroomExtra + extrasTotal;
  })();

  const fetchEstimate = useMutation({
    mutationFn: () =>
      bookingApi.getEstimate({
        serviceTypeId: data.serviceTypeId,
        cleaningPlaceId: data.cleaningPlaceId || undefined,
        rooms: data.rooms.length > 0 ? data.rooms : undefined,
        additionalServiceIds: data.additionalServiceIds,
        bedrooms: data.bedrooms,
        bathrooms: data.bathrooms,
        squareFootage: data.squareFootage || undefined,
        dirtLevel: data.dirtLevel,
        hasPets: data.hasPets,
        hasElevator: data.hasElevator,
        isFirstTime: data.isFirstTime,
        recurrenceType: data.recurrenceType,
      }),
    onSuccess: (response) => {
      setEstimateError('');
      onEstimate(response.data);
      onNext();
    },
    onError: () => {
      setEstimateError('Could not calculate estimate. Please try again.');
    },
  });

  const toggleExtra = useCallback((id: number) => {
    const current = data.additionalServiceIds;
    if (current.includes(id)) {
      onChange({ additionalServiceIds: current.filter(x => x !== id) });
    } else {
      onChange({ additionalServiceIds: [...current, id] });
    }
  }, [data.additionalServiceIds, onChange]);

  const handleDecrementBedrooms = useCallback(() => {
    onChange({ bedrooms: Math.max(1, data.bedrooms - 1) });
  }, [data.bedrooms, onChange]);

  const handleIncrementBedrooms = useCallback(() => {
    onChange({ bedrooms: Math.min(10, data.bedrooms + 1) });
  }, [data.bedrooms, onChange]);

  const handleDecrementBathrooms = useCallback(() => {
    onChange({ bathrooms: Math.max(1, data.bathrooms - 1) });
  }, [data.bathrooms, onChange]);

  const handleIncrementBathrooms = useCallback(() => {
    onChange({ bathrooms: Math.min(10, data.bathrooms + 1) });
  }, [data.bathrooms, onChange]);

  const handleContinue = useCallback(() => {
    fetchEstimate.mutate();
  }, [fetchEstimate]);

  if (loadingPlaces) {
    return (
      <div className="flex justify-center py-12">
        <Spinner size="lg" />
      </div>
    );
  }

  return (
    <div>
      <h2 className="text-2xl font-bold text-gray-900 mb-2">Tell us about your space</h2>
      <p className="text-gray-600 mb-8">Help us give you an accurate estimate.</p>

      <div className="space-y-6">
        {/* Property Type */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-3">Property Type</label>
          <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
            {cleaningPlaces?.data.map((place: CleaningPlace) => (
              <button
                key={place.id}
                onClick={() => onChange({ cleaningPlaceId: place.id })}
                aria-label={`Select ${place.name}`}
                className={cn(
                  'p-4 rounded-lg border-2 text-center transition-all',
                  data.cleaningPlaceId === place.id
                    ? 'border-brand bg-secondary/10'
                    : 'border-gray-200 hover:border-gray-300'
                )}
              >
                <Home className={cn('h-6 w-6 mx-auto mb-2', data.cleaningPlaceId === place.id ? 'text-brand' : 'text-gray-400')} />
                <span className="text-sm font-medium">{place.name}</span>
              </button>
            ))}
          </div>
        </div>

        {/* Bedrooms & Bathrooms */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 sm:gap-6">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Bedrooms</label>
            <div className="flex items-center gap-3">
              <Button
                variant="outline"
                size="sm"
                onClick={handleDecrementBedrooms}
                aria-label="Decrease bedrooms"
              >
                -
              </Button>
              <span className="text-xl font-semibold w-8 text-center">{data.bedrooms}</span>
              <Button
                variant="outline"
                size="sm"
                onClick={handleIncrementBedrooms}
                aria-label="Increase bedrooms"
              >
                +
              </Button>
            </div>
            {data.bedrooms > 1 && selectedService && (
              <p className="text-xs text-gray-500 mt-1">
                +{formatCurrency(selectedService.pricePerBedroom)} each extra
              </p>
            )}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Bathrooms</label>
            <div className="flex items-center gap-3">
              <Button
                variant="outline"
                size="sm"
                onClick={handleDecrementBathrooms}
                aria-label="Decrease bathrooms"
              >
                -
              </Button>
              <span className="text-xl font-semibold w-8 text-center">{data.bathrooms}</span>
              <Button
                variant="outline"
                size="sm"
                onClick={handleIncrementBathrooms}
                aria-label="Increase bathrooms"
              >
                +
              </Button>
            </div>
            {data.bathrooms > 1 && selectedService && (
              <p className="text-xs text-gray-500 mt-1">
                +{formatCurrency(selectedService.pricePerBathroom)} each extra
              </p>
            )}
          </div>
        </div>

        {/* Additional Services */}
        {additionalServices?.data && additionalServices.data.length > 0 && (
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-3">
              Additional Services <span className="text-gray-400 font-normal">(Optional)</span>
            </label>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              {additionalServices.data.map((extra: { id: number; title: string; description?: string; price: number }) => (
                <button
                  key={extra.id}
                  onClick={() => toggleExtra(extra.id)}
                  aria-label={`Toggle ${extra.title}`}
                  className={cn(
                    'p-4 rounded-lg border-2 text-left transition-all',
                    data.additionalServiceIds.includes(extra.id)
                      ? 'border-brand bg-secondary/10'
                      : 'border-gray-200 hover:border-gray-300'
                  )}
                >
                  <div className="flex justify-between items-start">
                    <div>
                      <p className="font-medium text-gray-900">{extra.title}</p>
                      {extra.description && (
                        <p className="text-sm text-gray-500 mt-1">{extra.description}</p>
                      )}
                    </div>
                    <span className="text-brand font-semibold">+{formatCurrency(extra.price)}</span>
                  </div>
                </button>
              ))}
            </div>
          </div>
        )}

        {/* Property modifiers — feed the pricing engine */}
        <fieldset>
          <legend className="block text-sm font-medium text-gray-700 mb-3">Property details</legend>

          {/* Dirt level */}
          <div className="mb-4">
            <p className="text-xs text-gray-500 mb-2">How dirty is the space?</p>
            <div role="radiogroup" aria-label="Dirt level" className="grid grid-cols-3 gap-2">
              {([
                { value: 0 as const, label: 'Light' },
                { value: 1 as const, label: 'Normal' },
                { value: 2 as const, label: 'Heavy' },
              ]).map((opt) => {
                const active = data.dirtLevel === opt.value;
                return (
                  <button
                    key={opt.value}
                    type="button"
                    role="radio"
                    aria-checked={active}
                    onClick={() => onChange({ dirtLevel: opt.value })}
                    className={cn(
                      'rounded-lg border-2 px-3 py-2 text-sm font-medium transition-all',
                      active ? 'border-brand bg-accent-light/10 text-brand' : 'border-gray-200 hover:border-gray-300 text-gray-700'
                    )}
                  >
                    {opt.label}
                  </button>
                );
              })}
            </div>
            <p className="text-xs text-gray-500 mt-1">Affects final price calculation</p>
          </div>

          {/* Floor level + elevator */}
          <div className="mb-4 grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label htmlFor="floor-level" className="block text-xs text-gray-500 mb-1">Floor level</label>
              <input
                id="floor-level"
                type="number"
                min={0}
                max={50}
                inputMode="numeric"
                value={data.floorLevel}
                onChange={(e) => onChange({ floorLevel: Math.max(0, Math.min(50, Number(e.target.value) || 0)) })}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand"
              />
              <p className="text-xs text-gray-400 mt-1">0 = ground floor</p>
            </div>
            <div className="self-end pb-2">
              <label className="flex items-center gap-3 cursor-pointer">
                <input
                  type="checkbox"
                  checked={data.hasElevator}
                  onChange={(e) => onChange({ hasElevator: e.target.checked })}
                  className="h-4 w-4 rounded border-gray-300 text-brand focus:ring-brand"
                />
                <span className="text-sm text-gray-700">Building has elevator</span>
              </label>
              <p className="text-xs text-gray-500 mt-1">Affects final price calculation</p>
            </div>
          </div>

          {/* Pets + first time */}
          <div className="mb-2 grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label className="flex items-center gap-3 cursor-pointer">
                <input
                  type="checkbox"
                  checked={data.hasPets}
                  onChange={(e) => onChange({ hasPets: e.target.checked })}
                  className="h-4 w-4 rounded border-gray-300 text-brand focus:ring-brand"
                />
                <span className="text-sm text-gray-700">Pets at home</span>
              </label>
              <p className="text-xs text-gray-500 mt-1">Affects final price calculation</p>
            </div>
            <label className="flex items-center gap-3 cursor-pointer">
              <input
                type="checkbox"
                checked={data.isFirstTime}
                onChange={(e) => onChange({ isFirstTime: e.target.checked })}
                className="h-4 w-4 rounded border-gray-300 text-brand focus:ring-brand"
              />
              <span className="text-sm text-gray-700">First-time cleaning</span>
            </label>
          </div>
        </fieldset>

        {/* Recurrence — discount for repeat customers */}
        <fieldset>
          <legend className="block text-sm font-medium text-gray-700 mb-3">How often?</legend>
          <div role="radiogroup" aria-label="Recurrence" className="grid grid-cols-2 sm:grid-cols-4 gap-2">
            {([
              { value: 'None' as const, label: 'One-time' },
              { value: 'Weekly' as const, label: 'Weekly' },
              { value: 'BiWeekly' as const, label: 'Bi-weekly' },
              { value: 'Monthly' as const, label: 'Monthly' },
            ]).map((opt) => {
              const active = data.recurrenceType === opt.value;
              return (
                <button
                  key={opt.value}
                  type="button"
                  role="radio"
                  aria-checked={active}
                  onClick={() => onChange({
                    recurrenceType: opt.value,
                    // clear end date when going back to one-time
                    recurrenceEndDate: opt.value === 'None' ? undefined : data.recurrenceEndDate,
                  })}
                  className={cn(
                    'rounded-lg border-2 px-3 py-2 text-sm font-medium transition-all',
                    active ? 'border-brand bg-accent-light/10 text-brand' : 'border-gray-200 hover:border-gray-300 text-gray-700'
                  )}
                >
                  {opt.label}
                </button>
              );
            })}
          </div>
          {data.recurrenceType !== 'None' && (
            <p className="text-xs text-gray-500 mt-2">
              Recurring bookings include a discount — shown in next step.
            </p>
          )}
          {data.recurrenceType !== 'None' && (
            <div className="mt-3">
              <label htmlFor="recurrence-end-date" className="block text-xs text-gray-500 mb-1">
                End date <span className="text-gray-400">(optional — leave empty for ongoing)</span>
              </label>
              <input
                id="recurrence-end-date"
                type="date"
                value={data.recurrenceEndDate ?? ''}
                onChange={(e) => onChange({ recurrenceEndDate: e.target.value || undefined })}
                className="w-full sm:w-auto rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand"
              />
            </div>
          )}
        </fieldset>

        {/* Price Preview */}
        <div className="bg-gray-50 rounded-xl p-6 border border-gray-200">
          <h3 className="font-semibold text-gray-900 mb-2">Estimated Total <span className="font-normal text-gray-400 text-sm">(~)</span></h3>
          <p className="text-2xl font-bold text-brand">{formatCurrency(previewTotal)}</p>
          <p className="text-xs text-gray-500 mt-1">Approximation — modifiers (dirt level, pets, elevator) not yet applied. Final price calculated on next step.</p>
        </div>

        {estimateError && (
          <p className="text-sm text-red-500">{estimateError}</p>
        )}
      </div>

      <p className="text-xs text-gray-500 text-center mt-6">
        Final price will be calculated based on your selections above.
      </p>

      <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-between sm:gap-0 mt-4">
        <Button variant="outline" onClick={onBack} aria-label="Go back" className="w-full sm:w-auto">Back</Button>
        <Button
          onClick={handleContinue}
          loading={fetchEstimate.isPending}
          disabled={!data.cleaningPlaceId}
          aria-label="Continue to next step"
        >
          Continue
        </Button>
      </div>
    </div>
  );
});
