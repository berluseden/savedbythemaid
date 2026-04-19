import { useState, useEffect } from 'react';
import api from '../../../lib/api';
import { getErrorMessage } from '@/shared/lib/error-utils';
import { useFormModal } from '@/shared/hooks/use-form-modal';
import type { UserDto } from './types';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/shared/components/ui/dialog';

interface PasswordResetDialogProps {
  modal: ReturnType<typeof useFormModal<UserDto>>;
}

export function PasswordResetDialog({ modal }: PasswordResetDialogProps) {
  const [newPassword, setNewPassword] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);

  useEffect(() => {
    if (modal.isOpen) {
      setNewPassword('');
      setError('');
      setSuccess(false);
    }
  }, [modal.isOpen]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!modal.editingItem) return;

    try {
      await api.put(`/admin/users/${modal.editingItem.id}/password`, {
        newPassword,
      });
      setSuccess(true);
      setError('');
    } catch (err: unknown) {
      setError(getErrorMessage(err, 'Error changing password'));
    }
  };

  return (
    <Dialog open={modal.isOpen} onOpenChange={(open) => !open && modal.close()}>
      <DialogContent className="max-w-sm">
        <DialogHeader>
          <DialogTitle>Change Password</DialogTitle>
        </DialogHeader>

        {success ? (
          <div className="space-y-4">
            <div className="rounded-lg bg-green-50 p-3 text-sm text-green-700">
              Password updated successfully.
            </div>
            <div className="flex justify-end">
              <button
                type="button"
                onClick={() => modal.close()}
                className="rounded-lg bg-brand px-4 py-2 text-sm font-medium text-white hover:bg-brand-dark"
              >
                Close
              </button>
            </div>
          </div>
        ) : (
          <>
            {error && (
              <div className="mb-4 rounded-lg bg-red-50 p-3 text-sm text-red-600">
                {error}
              </div>
            )}

            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  New Password *
                </label>
                <input
                  type="password"
                  required
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  placeholder="Minimum 8 characters"
                  className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-brand focus:outline-none focus:ring-1 focus:ring-brand"
                />
                <p className="mt-1 text-xs text-gray-500">
                  Must contain uppercase, lowercase, number and symbol
                </p>
              </div>

              <div className="flex justify-end gap-3 pt-2">
                <button
                  type="button"
                  onClick={() => modal.close()}
                  className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="rounded-lg bg-brand px-4 py-2 text-sm font-medium text-white hover:bg-brand-dark"
                >
                  Change
                </button>
              </div>
            </form>
          </>
        )}
      </DialogContent>
    </Dialog>
  );
}
