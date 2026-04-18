import * as AlertDialog from '@radix-ui/react-alert-dialog';
import { cn } from '@/lib/utils';

export interface ConfirmDialogProps {
  open: boolean;
  onConfirm: () => void;
  onCancel: () => void;
  title: string;
  description?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  variant?: 'danger' | 'warning';
  loading?: boolean;
}

export function ConfirmDialog({
  open,
  onConfirm,
  onCancel,
  title,
  description,
  confirmLabel = 'Confirm',
  cancelLabel = 'Cancel',
  variant = 'danger',
  loading = false,
}: ConfirmDialogProps) {
  return (
    <AlertDialog.Root open={open} onOpenChange={(isOpen) => !isOpen && onCancel()}>
      <AlertDialog.Portal>
        <AlertDialog.Overlay className="fixed inset-0 z-50 bg-black/50 data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0" />
        <AlertDialog.Content
          className={cn(
            'fixed left-1/2 top-1/2 z-50 w-full max-w-md -translate-x-1/2 -translate-y-1/2',
            'rounded-xl bg-surface p-6 shadow-xl',
            'focus:outline-none'
          )}
        >
          <AlertDialog.Title className="text-lg font-semibold text-text-primary">
            {title}
          </AlertDialog.Title>

          {description && (
            <AlertDialog.Description className="mt-2 text-sm text-text-secondary">
              {description}
            </AlertDialog.Description>
          )}

          <div className="mt-6 flex justify-end gap-3">
            <AlertDialog.Cancel asChild>
              <button
                type="button"
                className="rounded-lg border border-border bg-surface px-4 py-2 text-sm font-medium text-text-primary hover:bg-surface-secondary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2"
                disabled={loading}
              >
                {cancelLabel}
              </button>
            </AlertDialog.Cancel>

            <AlertDialog.Action asChild>
              <button
                type="button"
                className={cn(
                  'rounded-lg px-4 py-2 text-sm font-medium text-text-inverse focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:opacity-50',
                  variant === 'danger'
                    ? 'bg-danger hover:bg-danger-hover focus-visible:ring-danger'
                    : 'bg-warning hover:bg-amber-600 focus-visible:ring-warning'
                )}
                onClick={onConfirm}
                disabled={loading}
                aria-busy={loading || undefined}
              >
                {loading ? 'Processing...' : confirmLabel}
              </button>
            </AlertDialog.Action>
          </div>
        </AlertDialog.Content>
      </AlertDialog.Portal>
    </AlertDialog.Root>
  );
}
