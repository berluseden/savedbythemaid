import React from 'react';
import { Check } from 'lucide-react';
import { cn } from '@/lib/utils';
import { steps } from './useBookingWizard';
import type { BookingStep } from './types';

interface StepProgressProps {
  currentStep: BookingStep;
}

export const StepProgress = React.memo(function StepProgress({ currentStep }: StepProgressProps) {
  const currentIndex = steps.findIndex((s) => s.id === currentStep);

  return (
    <nav aria-label="Booking progress" className="mb-8">
      {/* Visible mobile label + SR-only live announcement so assistive tech
       * users hear the step change (WCAG 4.1.3 Status Messages). */}
      <p
        role="status"
        aria-live="polite"
        aria-atomic="true"
        className="mb-3 text-center text-sm font-medium text-text-secondary sm:hidden"
      >
        Step {currentIndex + 1} of {steps.length} &mdash; {steps[currentIndex].title}
      </p>
      <p role="status" aria-live="polite" aria-atomic="true" className="sr-only">
        Step {currentIndex + 1} of {steps.length}: {steps[currentIndex].title}
      </p>

      <ol className="flex items-center justify-between">
        {steps.map((step, index) => {
          const isCompleted = index < currentIndex;
          const isCurrent = index === currentIndex;
          const Icon = step.icon;

          return (
            <li
              key={step.id}
              className="flex items-center last:flex-none flex-1"
              aria-current={isCurrent ? 'step' : undefined}
            >
              <div className="flex flex-col items-center">
                <div
                  className={cn(
                    'flex items-center justify-center rounded-full border-2 transition-all duration-200',
                    isCompleted &&
                      'h-10 w-10 border-success bg-success text-white',
                    isCurrent &&
                      'h-12 w-12 border-primary bg-primary-light text-primary shadow-md shadow-primary/20',
                    !isCompleted && !isCurrent &&
                      'h-10 w-10 border-border bg-surface text-muted'
                  )}
                >
                  {isCompleted ? (
                    <Check className="h-5 w-5" strokeWidth={3} />
                  ) : (
                    <Icon className={cn('h-5 w-5', isCurrent && 'h-6 w-6')} />
                  )}
                </div>

                <span
                  className={cn(
                    'mt-2 hidden text-xs font-medium sm:block',
                    isCompleted && 'text-success',
                    isCurrent && 'text-primary',
                    !isCompleted && !isCurrent && 'text-muted'
                  )}
                >
                  {step.title}
                </span>
              </div>

              {index < steps.length - 1 && (
                <div className="mx-1 h-0.5 flex-1 sm:mx-2">
                  <div
                    className={cn(
                      'h-full rounded-full transition-colors duration-200',
                      index < currentIndex ? 'bg-success' : 'bg-border'
                    )}
                  />
                </div>
              )}
            </li>
          );
        })}
      </ol>
    </nav>
  );
});
