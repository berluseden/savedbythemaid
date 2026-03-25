import React from 'react';
import { CheckCircle } from 'lucide-react';
import { cn } from '@/lib/utils';
import { steps } from './useBookingWizard';
import type { BookingStep } from './types';

interface StepProgressProps {
  currentStep: BookingStep;
}

export const StepProgress = React.memo(function StepProgress({ currentStep }: StepProgressProps) {
  const currentStepIndex = steps.findIndex((s) => s.id === currentStep);

  return (
    <div className="mb-8">
      <div className="flex items-center justify-between">
        {steps.map((step, index) => (
          <div key={step.id} className="flex items-center">
            <div
              className={cn(
                'flex h-10 w-10 items-center justify-center rounded-full border-2 transition-colors',
                index < currentStepIndex
                  ? 'border-[#2196f3] bg-[#2196f3] text-white'
                  : index === currentStepIndex
                  ? 'border-[#2196f3] bg-white text-[#2196f3]'
                  : 'border-gray-300 bg-white text-gray-400'
              )}
              aria-label={`Step ${index + 1}: ${step.title}${index < currentStepIndex ? ' (completed)' : index === currentStepIndex ? ' (current)' : ''}`}
            >
              {index < currentStepIndex ? (
                <CheckCircle className="h-5 w-5" />
              ) : (
                <step.icon className="h-5 w-5" />
              )}
            </div>
            {index < steps.length - 1 && (
              <div
                className={cn(
                  'hidden sm:block h-0.5 w-12 lg:w-24',
                  index < currentStepIndex ? 'bg-[#2196f3]' : 'bg-gray-300'
                )}
              />
            )}
          </div>
        ))}
      </div>
      <div className="mt-4 flex justify-between">
        {steps.map((step, index) => (
          <span
            key={step.id}
            className={cn(
              'text-xs font-medium',
              index <= currentStepIndex ? 'text-[#2196f3]' : 'text-gray-400'
            )}
          >
            {step.title}
          </span>
        ))}
      </div>
    </div>
  );
});
