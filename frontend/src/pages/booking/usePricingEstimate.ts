import { useState, useCallback } from 'react';
import type { EstimateResponse } from '@/lib/api';

export function usePricingEstimate() {
  const [estimate, setEstimate] = useState<EstimateResponse | null>(null);

  const updateEstimate = useCallback((newEstimate: EstimateResponse) => {
    setEstimate(newEstimate);
  }, []);

  return {
    estimate,
    setEstimate: updateEstimate,
  };
}
