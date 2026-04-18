import { useState, useEffect } from 'react';
import { Shield, Trash2, X } from 'lucide-react';
import api from '../../../lib/api';
import { getErrorMessage } from '@/shared/lib/error-utils';
import { useFormModal } from '@/shared/hooks/use-form-modal';
import type { RoleDto } from './types';

interface RoleManageDialogProps {
  modal: ReturnType<typeof useFormModal>;
  roles: RoleDto[];
  onChanged: () => void;
}

export function RoleManageDialog({ modal, roles, onChanged }: RoleManageDialogProps) {
  const [newRoleName, setNewRoleName] = useState('');
  const [error, setError] = useState('');

  useEffect(() => {
    if (modal.isOpen) {
      setNewRoleName('');
      setError('');
    }
  }, [modal.isOpen]);

  const handleCreateRole = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newRoleName.trim()) return;

    try {
      await api.post('/admin/users/roles', { name: newRoleName.trim() });
      modal.close();
      setNewRoleName('');
      onChanged();
    } catch (err: unknown) {
      setError(getErrorMessage(err, 'Error creating role'));
    }
  };

  const handleDeleteRole = async (role: RoleDto) => {
    if (!confirm(`Delete role "${role.name}"?`)) return;

    try {
      await api.delete(`/admin/users/roles/${role.id}`);
      onChanged();
    } catch (err: unknown) {
      alert(getErrorMessage(err, 'Error deleting role'));
    }
  };

  if (!modal.isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="w-full max-w-md rounded-lg bg-white p-6 shadow-xl">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-gray-900">
            Manage Roles
          </h2>
          <button
            onClick={() => modal.close()}
            className="rounded-lg p-1 text-gray-400 hover:bg-gray-100 hover:text-gray-600"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        {/* Existing Roles */}
        <div className="mb-4">
          <h3 className="text-sm font-medium text-gray-700 mb-2">
            Existing roles
          </h3>
          <div className="space-y-2">
            {roles.map((role) => (
              <div
                key={role.id}
                className="flex items-center justify-between rounded-lg border border-gray-200 p-3"
              >
                <div className="flex items-center gap-2">
                  <Shield className="h-4 w-4 text-gray-400" />
                  <span className="font-medium">{role.name}</span>
                </div>
                {!['Admin', 'Employee', 'Customer'].includes(role.name) && (
                  <button
                    onClick={() => handleDeleteRole(role)}
                    className="rounded-lg p-1 text-gray-400 hover:bg-red-50 hover:text-red-600"
                  >
                    <Trash2 className="h-4 w-4" />
                  </button>
                )}
                {['Admin', 'Employee', 'Customer'].includes(role.name) && (
                  <span className="text-xs text-gray-400">System</span>
                )}
              </div>
            ))}
          </div>
        </div>

        {/* Create New Role */}
        <form onSubmit={handleCreateRole} className="border-t border-gray-200 pt-4">
          <h3 className="text-sm font-medium text-gray-700 mb-2">
            Create new role
          </h3>
          {error && (
            <div className="mb-2 text-sm text-red-600">{error}</div>
          )}
          <div className="flex gap-2">
            <input
              type="text"
              value={newRoleName}
              onChange={(e) => {
                setNewRoleName(e.target.value);
                setError('');
              }}
              placeholder="Role name"
              className="flex-1 rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-brand focus:outline-none focus:ring-1 focus:ring-brand"
            />
            <button
              type="submit"
              className="rounded-lg bg-brand px-4 py-2 text-sm font-medium text-white hover:bg-brand-dark"
            >
              Create
            </button>
          </div>
        </form>

        <div className="flex justify-end pt-4">
          <button
            type="button"
            onClick={() => modal.close()}
            className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
          >
            Close
          </button>
        </div>
      </div>
    </div>
  );
}
