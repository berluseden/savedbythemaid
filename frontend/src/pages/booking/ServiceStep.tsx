import React, { useCallback } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Sparkles } from 'lucide-react';
import { Button, Spinner } from '@/components/ui';
import { bookingApi, type ServiceType } from '@/lib/api';
import { cn, formatCurrency } from '@/lib/utils';

interface ServiceStepProps {
  selectedId: number;
  onChange: (id: number) => void;
  onNext: () => void;
  onBack: () => void;
}

export const ServiceStep = React.memo(function ServiceStep({
  selectedId,
  onChange,
  onNext,
  onBack,
}: ServiceStepProps) {
  const { data: serviceTypes, isLoading } = useQuery({
    queryKey: ['serviceTypes'],
    queryFn: () => bookingApi.getServiceTypes(),
  });

  const handleSelect = useCallback((id: number) => {
    onChange(id);
  }, [onChange]);

  if (isLoading) {
    return (
      <div className="flex justify-center py-12">
        <Spinner size="lg" />
      </div>
    );
  }

  return (
    <div>
      <h2 className="text-2xl font-bold text-gray-900 mb-2">Choose your service</h2>
      <p className="text-gray-600 mb-8">Select the type of cleaning you need.</p>

      <div className="grid gap-4 sm:grid-cols-2">
        {serviceTypes?.data.map((service: ServiceType) => (
          <button
            key={service.id}
            onClick={() => handleSelect(service.id)}
            aria-label={`Select ${service.name} service`}
            className={cn(
              'p-6 rounded-xl border-2 text-left transition-all',
              selectedId === service.id
                ? 'border-[#2196f3] bg-[#b8e07c]/10 ring-2 ring-[#2196f3]/20'
                : 'border-gray-200 hover:border-gray-300'
            )}
          >
            <Sparkles className={cn('h-8 w-8 mb-3', selectedId === service.id ? 'text-[#2196f3]' : 'text-gray-400')} />
            <h3 className="font-semibold text-gray-900">{service.name}</h3>
            <p className="text-sm text-gray-600 mt-1">{service.description}</p>
            <p className="text-lg font-bold text-[#2196f3] mt-3">From {formatCurrency(service.price)}</p>
          </button>
        ))}
      </div>

      <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-between sm:gap-0 mt-8">
        <Button variant="outline" onClick={onBack} aria-label="Go back" className="w-full sm:w-auto">Back</Button>
        <Button onClick={onNext} disabled={!selectedId} aria-label="Continue to next step" className="w-full sm:w-auto">Continue</Button>
      </div>
    </div>
  );
});
