import React, { useState, useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import { ChevronUp, ChevronDown } from 'lucide-react';
import { bookingApi, type ServiceType, type EstimateResponse } from '@/lib/api';
import { cn, formatCurrency } from '@/lib/utils';
import type { BookingData, BookingStep } from '@/pages/booking/types';

interface PriceSummaryBarProps {
  data: BookingData;
  estimate: EstimateResponse | null;
  currentStep: BookingStep;
}

type PriceTier = 'base' | 'preview' | 'server';

export const PriceSummaryBar = React.memo(function PriceSummaryBar({
  data,
  estimate,
  currentStep,
}: PriceSummaryBarProps) {
  const [expanded, setExpanded] = useState(false);

  const { data: serviceTypes } = useQuery({
    queryKey: ['serviceTypes'],
    queryFn: () => bookingApi.getServiceTypes(),
  });

  const { data: additionalServices } = useQuery({
    queryKey: ['additionalServices'],
    queryFn: () => bookingApi.getAdditionalServices(),
  });

  const selectedService = serviceTypes?.data.find(
    (s: ServiceType) => s.id === data.serviceTypeId,
  );

  // Determine which pricing tier we can show
  const tier: PriceTier = useMemo(() => {
    if (estimate) return 'server';
    if (selectedService && currentStep !== 'service') return 'preview';
    return 'base';
  }, [estimate, selectedService, currentStep]);

  // Client-side preview calculation (mirrors DetailsStep logic)
  const preview = useMemo(() => {
    if (!selectedService) return { base: 0, bedroomExtra: 0, bathroomExtra: 0, extrasTotal: 0, total: 0 };
    const base = selectedService.price;
    const bedroomExtra = Math.max(0, data.bedrooms - 1) * selectedService.pricePerBedroom;
    const bathroomExtra = Math.max(0, data.bathrooms - 1) * selectedService.pricePerBathroom;
    const extrasTotal = data.additionalServiceIds.reduce((sum, id) => {
      const extra = additionalServices?.data.find((s: { id: number; price: number }) => s.id === id);
      return sum + (extra?.price || 0);
    }, 0);
    return {
      base,
      bedroomExtra,
      bathroomExtra,
      extrasTotal,
      total: base + bedroomExtra + bathroomExtra + extrasTotal,
    };
  }, [selectedService, data.bedrooms, data.bathrooms, data.additionalServiceIds, additionalServices]);

  // Don't show on zipcode step or if no service selected yet
  const hidden = currentStep === 'zipcode' || !data.serviceTypeId || !selectedService;
  if (hidden) return null;

  const displayPrice = tier === 'server' ? estimate!.total : tier === 'preview' ? preview.total : selectedService!.price;

  const label =
    tier === 'server'
      ? 'Total'
      : tier === 'preview'
        ? 'Estimated'
        : 'From';

  const hasBreakdown = tier === 'preview' || tier === 'server';

  return (
    <>
      {/* Desktop sidebar-style card (visible lg+) */}
      <div className="mt-6 hidden lg:block">
        <div className="rounded-xl border border-gray-200 bg-white shadow-sm overflow-hidden">
          <div className="px-5 py-4">
            <div className="flex items-baseline justify-between">
              <span className="text-sm font-medium text-gray-500">{label}</span>
              <span
                className="text-2xl font-bold text-[#2196f3] transition-all duration-300 ease-in-out"
              >
                {formatCurrency(displayPrice)}
              </span>
            </div>
            {tier === 'server' && estimate!.formattedDuration && (
              <p className="text-xs text-gray-500 mt-1 text-right">
                ~{estimate!.formattedDuration}
              </p>
            )}
            {tier === 'base' && (
              <p className="text-xs text-gray-500 mt-1 text-right">
                Base price for {selectedService!.name}
              </p>
            )}
          </div>

          {hasBreakdown && (
            <div className="border-t border-gray-100">
              <button
                onClick={() => setExpanded((v) => !v)}
                className="flex w-full items-center justify-between px-5 py-3 text-sm text-gray-600 hover:bg-gray-50 transition-colors"
                aria-label={expanded ? 'Hide price breakdown' : 'Show price breakdown'}
              >
                <span>Price breakdown</span>
                {expanded ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
              </button>
              <div
                className={cn(
                  'overflow-hidden transition-all duration-300 ease-in-out',
                  expanded ? 'max-h-64 opacity-100' : 'max-h-0 opacity-0',
                )}
              >
                <Breakdown
                  tier={tier}
                  preview={preview}
                  estimate={estimate}
                  serviceName={selectedService!.name}
                />
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Mobile sticky bottom bar (visible below lg) */}
      <div className="fixed inset-x-0 bottom-0 z-40 lg:hidden pb-safe">
        <div className="border-t border-gray-200 bg-white shadow-[0_-2px_10px_rgba(0,0,0,0.08)]">
          {/* Expandable breakdown panel */}
          <div
            className={cn(
              'overflow-hidden transition-all duration-300 ease-in-out',
              expanded ? 'max-h-64 opacity-100' : 'max-h-0 opacity-0',
            )}
          >
            {hasBreakdown && (
              <Breakdown
                tier={tier}
                preview={preview}
                estimate={estimate}
                serviceName={selectedService!.name}
              />
            )}
          </div>

          {/* Main bar */}
          <button
            onClick={() => hasBreakdown && setExpanded((v) => !v)}
            className={cn(
              'flex w-full items-center justify-between px-4 py-3',
              hasBreakdown && 'cursor-pointer',
            )}
            aria-label={expanded ? 'Hide price breakdown' : 'Show price breakdown'}
            disabled={!hasBreakdown}
          >
            <div className="flex items-center gap-2">
              <span className="text-sm font-medium text-gray-500">{label}</span>
              <span
                className="text-xl font-bold text-[#2196f3] transition-all duration-300 ease-in-out"
              >
                {formatCurrency(displayPrice)}
              </span>
            </div>
            {hasBreakdown && (
              expanded ? <ChevronDown className="h-4 w-4 text-gray-400" /> : <ChevronUp className="h-4 w-4 text-gray-400" />
            )}
          </button>
        </div>
      </div>

      {/* Spacer so mobile content is not hidden behind sticky bar */}
      <div className="h-16 lg:hidden" />
    </>
  );
});

/** Shared breakdown content for desktop and mobile */
function Breakdown({
  tier,
  preview,
  estimate,
  serviceName,
}: {
  tier: PriceTier;
  preview: { base: number; bedroomExtra: number; bathroomExtra: number; extrasTotal: number; total: number };
  estimate: EstimateResponse | null;
  serviceName: string;
}) {
  if (tier === 'server' && estimate) {
    return (
      <div className="px-5 py-3 space-y-2 text-sm">
        <div className="flex justify-between">
          <span className="text-gray-600">Subtotal</span>
          <span className="font-medium">{formatCurrency(estimate.subtotal)}</span>
        </div>
        {estimate.discount > 0 && (
          <div className="flex justify-between text-green-600">
            <span>Discount ({estimate.discountPercent}%)</span>
            <span className="font-medium">-{formatCurrency(estimate.discount)}</span>
          </div>
        )}
        <div className="flex justify-between border-t border-gray-100 pt-2">
          <span className="font-semibold text-gray-900">Total</span>
          <span className="font-bold text-[#2196f3]">{formatCurrency(estimate.total)}</span>
        </div>
      </div>
    );
  }

  // Preview tier breakdown
  return (
    <div className="px-5 py-3 space-y-2 text-sm">
      <div className="flex justify-between">
        <span className="text-gray-600">{serviceName} (base)</span>
        <span className="font-medium">{formatCurrency(preview.base)}</span>
      </div>
      {preview.bedroomExtra > 0 && (
        <div className="flex justify-between">
          <span className="text-gray-600">Extra bedrooms</span>
          <span className="font-medium">+{formatCurrency(preview.bedroomExtra)}</span>
        </div>
      )}
      {preview.bathroomExtra > 0 && (
        <div className="flex justify-between">
          <span className="text-gray-600">Extra bathrooms</span>
          <span className="font-medium">+{formatCurrency(preview.bathroomExtra)}</span>
        </div>
      )}
      {preview.extrasTotal > 0 && (
        <div className="flex justify-between">
          <span className="text-gray-600">Add-ons</span>
          <span className="font-medium">+{formatCurrency(preview.extrasTotal)}</span>
        </div>
      )}
      <div className="flex justify-between border-t border-gray-100 pt-2">
        <span className="font-semibold text-gray-900">Estimated</span>
        <span className="font-bold text-[#2196f3]">{formatCurrency(preview.total)}</span>
      </div>
      <p className="text-xs text-gray-400">Final price may vary</p>
    </div>
  );
}
