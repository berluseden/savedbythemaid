import { create } from 'zustand';
import { useShallow } from 'zustand/react/shallow';

export type ToastVariant = 'info' | 'success' | 'warning' | 'error';

export interface ToastMessage {
  id: string;
  message: string;
  variant: ToastVariant;
}

interface ToastState {
  toasts: ToastMessage[];
  push: (message: string, variant?: ToastVariant) => string;
  dismiss: (id: string) => void;
  clear: () => void;
}

const AUTO_DISMISS_MS = 5_000;

/**
 * Toast store (Zustand 2026 pattern).
 *
 * Why Zustand here: the previous module-level `listeners[]` array required
 * the consumer to subscribe via `useEffect` and mirror state into local
 * `useState`. The store owns the array directly — components subscribe to
 * the slice they need via selectors and re-render only when toasts change.
 *
 * `push` returns the id so callers can dismiss programmatically; the store
 * also schedules an auto-dismiss after AUTO_DISMISS_MS so the visual UI
 * doesn't have to manage timers itself (single source of truth).
 */
export const useToastStore = create<ToastState>((set, get) => ({
  toasts: [],
  push: (message, variant = 'info') => {
    const id = `${Date.now()}-${Math.random().toString(16).slice(2)}`;
    const toast: ToastMessage = { id, message, variant };
    set((state) => ({ toasts: [...state.toasts, toast] }));
    if (typeof window !== 'undefined') {
      setTimeout(() => get().dismiss(id), AUTO_DISMISS_MS);
    }
    return id;
  },
  dismiss: (id) =>
    set((state) => ({ toasts: state.toasts.filter((t) => t.id !== id) })),
  clear: () => set({ toasts: [] }),
}));

/**
 * Imperative push usable from any module (axios interceptors, hooks, etc.)
 * without needing a React hook/context.
 */
export function pushToast(message: string, variant: ToastVariant = 'info'): string {
  return useToastStore.getState().push(message, variant);
}

export function dismissToast(id: string): void {
  useToastStore.getState().dismiss(id);
}

export function clearAllToasts(): void {
  useToastStore.getState().clear();
}

/** React hook for the renderer — selects only the slice it needs. */
export function useToasts() {
  return useToastStore(
    useShallow((s) => ({ toasts: s.toasts, dismiss: s.dismiss }))
  );
}
