export type ToastVariant = 'info' | 'success' | 'warning' | 'error';

export interface ToastMessage {
  id: string;
  message: string;
  variant?: ToastVariant;
}

type Listener = (t: ToastMessage) => void;

const listeners: Listener[] = [];

export function onToast(listener: Listener) {
  listeners.push(listener);
  return () => {
    const idx = listeners.indexOf(listener);
    if (idx !== -1) listeners.splice(idx, 1);
  };
}

export function pushToast(message: string, variant: ToastVariant = 'info') {
  const toast: ToastMessage = { id: String(Date.now()) + Math.random().toString(16).slice(2), message, variant };
  listeners.slice().forEach((l) => l(toast));
  return toast.id;
}

export function clearAllToasts() {
  // Not implemented; consumers can track by id
}
