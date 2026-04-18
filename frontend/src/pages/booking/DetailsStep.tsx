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
        additionalServiceIds: data.additionalServiceIds,
        bedrooms: data.bedrooms,
        bathrooms: data.bathrooms,
        squareFootage: data.squareFootage || undefined,
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
                    ? 'border-[#2196f3] bg-[#b8e07c]/10'
                    : 'border-gray-200 hover:border-gray-300'
                )}
              >
                <Home className={cn('h-6 w-6 mx-auto mb-2', data.cleaningPlaceId === place.id ? 'text-[#2196f3]' : 'text-gray-400')} />
                <span className="text-sm font-medium">{place.name}</span>
              </button>
            ))}
          </div>
        </div>

        {/* Bedrooms & Bathrooms */}
        <div className="grid grid-cols-2 gap-6">
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
                      ? 'border-[#2196f3] bg-[#b8e07c]/10'
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
                    <span className="text-[#2196f3] font-semibold">+{formatCurrency(extra.price)}</span>
                  </div>
                </button>
              ))}
            </div>
          </div>
        )}

        {/* Price Preview */}
        <div className="bg-gray-50 rounded-xl p-6 border border-gray-200">
          <h3 className="font-semibold text-gray-900 mb-2">Estimated Total</h3>
          <p className="text-2xl font-bold text-[#2196f3]">{formatCurrency(previewTotal)}</p>
          <p className="text-xs text-gray-500 mt-1">Final price calculated on next step</p>
        </div>

        {estimateError && (
          <p className="text-sm text-red-500">{estimateError}</p>
        )}
      </div>

      <div className="flex justify-between mt-8">
        <Button variant="outline" onClick={onBack} aria-label="Go back">Back</Button>
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
