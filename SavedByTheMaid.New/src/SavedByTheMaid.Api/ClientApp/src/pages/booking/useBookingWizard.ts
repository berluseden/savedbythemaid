import { useState, useCallback } from 'react';
import { MapPin, Sparkles, Home, Calendar, User, CreditCard } from 'lucide-react';
import type { BookingStep, BookingData, StepDefinition } from './types';
import { initialBookingData } from './types';

export const steps: StepDefinition[] = [
  { id: 'zipcode', title: 'Location', icon: MapPin },
  { id: 'service', title: 'Service', icon: Sparkles },
  { id: 'details', title: 'Details', icon: Home },
  { id: 'schedule', title: 'Schedule', icon: Calendar },
  { id: 'contact', title: 'Contact', icon: User },
  { id: 'confirm', title: 'Confirm', icon: CreditCard },
];

const WIZARD_STORAGE_KEY = 'booking-wizard-state';

function loadWizardState(): { step: BookingStep; data: BookingData } | null {
  try {
    const raw = sessionStorage.getItem(WIZARD_STORAGE_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw);
    if (parsed?.step && parsed?.data) return parsed;
  } catch { /* ignore corrupt data */ }
  return null;
}

function saveWizardState(step: BookingStep, data: BookingData) {
  try {
    // Never persist password to storage - strip it before saving
    const { password: _, ...safeData } = data;
    sessionStorage.setItem(WIZARD_STORAGE_KEY, JSON.stringify({ step, data: { ...safeData, password: '' } }));
  } catch { /* storage full - ignore */ }
}

export function clearWizardState() {
  sessionStorage.removeItem(WIZARD_STORAGE_KEY);
}

export function useBookingWizard() {
  const saved = loadWizardState();
  const [currentStep, setCurrentStep] = useState<BookingStep>(saved?.step ?? 'zipcode');
  const [bookingData, setBookingData] = useState<BookingData>(saved?.data ?? initialBookingData);

  const currentStepIndex = steps.findIndex((s) => s.id === currentStep);

  const updateData = useCallback((data: Partial<BookingData>) => {
    setBookingData((prev) => {
      const updated = { ...prev, ...data };
      saveWizardState(currentStep, updated);
      return updated;
    });
  }, [currentStep]);

  const goNext = useCallback(() => {
    const idx = steps.findIndex((s) => s.id === currentStep);
    const nextIndex = idx + 1;
    if (nextIndex < steps.length) {
      const nextStep = steps[nextIndex].id;
      setCurrentStep(nextStep);
      setBookingData((prev) => {
        saveWizardState(nextStep, prev);
        return prev;
      });
    }
  }, [currentStep]);

  const goBack = useCallback(() => {
    const idx = steps.findIndex((s) => s.id === currentStep);
    const prevIndex = idx - 1;
    if (prevIndex >= 0) {
      const prevStep = steps[prevIndex].id;
      setCurrentStep(prevStep);
      setBookingData((prev) => {
        saveWizardState(prevStep, prev);
        return prev;
      });
    }
  }, [currentStep]);

  const goToStep = useCallback((step: BookingStep) => {
    setCurrentStep(step);
    setBookingData((prev) => {
      saveWizardState(step, prev);
      return prev;
    });
  }, []);

  const resetWizard = useCallback(() => {
    clearWizardState();
    setCurrentStep('zipcode');
    setBookingData(initialBookingData);
  }, []);

  return {
    currentStep,
    currentStepIndex,
    data: bookingData,
    updateData,
    goNext,
    goBack,
    goToStep,
    resetWizard,
  };
}
