import React from 'react';
import { Card } from '@/components/ui';
import { formatCurrency } from '@/lib/utils';
import type { EstimateResponse } from '@/lib/api';
import type { BookingStep } from './types';

interface PriceSummaryProps {
  estimate: EstimateResponse | null;
  currentStep: BookingStep;
}

export const PriceSummary = React.memo(function PriceSummary({ estimate, currentStep }: PriceSummaryProps) {
  if (!estimate || currentStep === 'zipcode' || currentStep === 'service') {
    return null;
  }

  return (
    <Card className="mt-6 p-4">
      <div className="flex items-center justify-between">
        <span className="text-sm text-gray-600">Estimated Total</span>
        <span className="text-2xl font-bold text-gray-900">{formatCurrency(estimate.total)}</span>
      </div>
      <p className="text-xs text-gray-500 mt-1">
        ~{Math.round(estimate.estimatedMinutes / 60)} hours estimated
      </p>
    </Card>
  );
});
