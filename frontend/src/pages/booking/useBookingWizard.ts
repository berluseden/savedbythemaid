import { useShallow } from 'zustand/react/shallow';
import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';
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

const STORAGE_KEY = 'booking-wizard-state';

interface BookingState {
  currentStep: BookingStep;
  data: BookingData;
  // Actions
  updateData: (patch: Partial<BookingData>) => void;
  goNext: () => void;
  goBack: () => void;
  goToStep: (step: BookingStep) => void;
  reset: () => void;
}

/**
 * Zustand store for the booking wizard.
 *
 * Persisted in sessionStorage so the user can refresh the tab without losing
 * progress, but the data is scoped to the tab (multiple booking flows in
 * different tabs do not collide).
 *
 * `partialize` excludes:
 *   - `password` (never persist credentials)
 *   - `softReserveId` / `sessionId` / `expiresAt` (server-issued, expire fast)
 *
 * Following the 2026 slice/persist pattern from the Zustand docs.
 */
export const useBookingStore = create<BookingState>()(
  persist(
    (set, get) => ({
      currentStep: 'zipcode',
      data: initialBookingData,

      updateData: (patch) =>
        set((state) => ({ data: { ...state.data, ...patch } })),

      goNext: () => {
        const idx = steps.findIndex((s) => s.id === get().currentStep);
        const next = steps[idx + 1];
        if (next) set({ currentStep: next.id });
      },

      goBack: () => {
        const idx = steps.findIndex((s) => s.id === get().currentStep);
        const prev = steps[idx - 1];
        if (prev) set({ currentStep: prev.id });
      },

      goToStep: (step) => set({ currentStep: step }),

      reset: () => set({ currentStep: 'zipcode', data: initialBookingData }),
    }),
    {
      name: STORAGE_KEY,
      storage: createJSONStorage(() => sessionStorage),
      version: 2,
      partialize: (state) => ({
        currentStep: state.currentStep,
        data: {
          ...state.data,
          // Strip volatile / sensitive fields from the snapshot.
          password: '',
          softReserveId: undefined,
          sessionId: undefined,
          expiresAt: undefined,
        },
      }),
    }
  )
);

/** Wipes the wizard state — call after a successful booking confirm. */
export function clearWizardState() {
  useBookingStore.getState().reset();
  // Belt + suspenders: also drop the persisted snapshot.
  try {
    sessionStorage.removeItem(STORAGE_KEY);
  } catch {
    /* storage disabled — ignore */
  }
}

/**
 * Compatibility hook — keeps the existing call sites working.
 * Components stay shielded from the underlying store implementation.
 *
 * Uses `useShallow` so consumers re-render only when one of the picked
 * fields changes (Zustand 2026 best practice for multi-field selectors).
 */
export function useBookingWizard() {
  const { currentStep, data, updateData, goNext, goBack, goToStep } =
    useBookingStore(
      useShallow((s) => ({
        currentStep: s.currentStep,
        data: s.data,
        updateData: s.updateData,
        goNext: s.goNext,
        goBack: s.goBack,
        goToStep: s.goToStep,
      }))
    );

  const currentStepIndex = steps.findIndex((s) => s.id === currentStep);

  return {
    currentStep,
    currentStepIndex,
    data,
    updateData,
    goNext,
    goBack,
    goToStep,
    resetWizard: clearWizardState,
  };
}
