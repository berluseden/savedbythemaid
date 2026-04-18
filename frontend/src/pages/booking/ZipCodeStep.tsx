import React, { useState, useCallback } from 'react';
import { useMutation } from '@tanstack/react-query';
import { MapPin } from 'lucide-react';
import { Button, Input } from '@/components/ui';
import { bookingApi } from '@/lib/api';

interface ZipCodeStepProps {
  value: string;
  onChange: (value: string) => void;
  onNext: () => void;
}

export const ZipCodeStep = React.memo(function ZipCodeStep({
  value,
  onChange,
  onNext,
}: ZipCodeStepProps) {
  const [zipCode, setZipCode] = useState(value);
  const [error, setError] = useState('');
  const [coverageInfo, setCoverageInfo] = useState<{ city?: string; state?: string } | null>(null);

  const checkCoverage = useMutation({
    mutationFn: (zip: string) => bookingApi.checkCoverage(zip),
    onSuccess: (response) => {
      if (response.data.isCovered) {
        setCoverageInfo({ city: response.data.city, state: response.data.state });
        onChange(zipCode);
        // Auto-advance after showing success message
        setTimeout(() => onNext(), 1500);
      } else {
        setError(response.data.message || 'Sorry, we do not service this area yet.');
      }
    },
    onError: () => {
      setError('Unable to check coverage. Please try again.');
    },
  });

  const handleSubmit = useCallback((e: React.FormEvent) => {
    e.preventDefault();
    if (!/^\d{5}$/.test(zipCode)) {
      setError('Please enter a valid 5-digit ZIP code');
      return;
    }
    setError('');
    setCoverageInfo(null);
    checkCoverage.mutate(zipCode);
  }, [zipCode, checkCoverage]);

  const handleZipChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    setZipCode(e.target.value.replace(/\D/g, '').slice(0, 5));
  }, []);

  return (
    <div className="text-center max-w-md mx-auto">
      <MapPin className="mx-auto h-16 w-16 text-[#2196f3] mb-6" />
      <h2 className="text-2xl font-bold text-gray-900 mb-2">Where do you need cleaning?</h2>
      <p className="text-gray-600 mb-8">Enter your ZIP code to check if we service your area.</p>

      <form onSubmit={handleSubmit} className="space-y-4">
        <Input
          type="text"
          placeholder="Enter ZIP code"
          value={zipCode}
          onChange={handleZipChange}
          className="text-center text-2xl tracking-widest"
          maxLength={5}
          error={error}
          disabled={!!coverageInfo}
          aria-label="ZIP code"
        />

        {coverageInfo && (
          <div className="p-4 bg-green-50 border border-green-200 rounded-lg">
            <p className="text-green-800 font-medium">
              ✓ Great news! We service {coverageInfo.city || 'your area'}
              {coverageInfo.state ? `, ${coverageInfo.state}` : ''}.
            </p>
            <p className="text-green-600 text-sm mt-1">Continuing to next step...</p>
          </div>
        )}

        {!coverageInfo && (
          <Button type="submit" className="w-full" loading={checkCoverage.isPending} aria-label="Check availability">
            Check Availability
          </Button>
        )}
      </form>
    </div>
  );
});
