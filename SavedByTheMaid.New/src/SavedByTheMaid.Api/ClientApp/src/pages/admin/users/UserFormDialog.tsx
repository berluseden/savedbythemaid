import { useState, useEffect } from 'react';
import { X } from 'lucide-react';
import api from '../../../lib/api';
import { getErrorMessage } from '@/shared/lib/error-utils';
import { useFormModal } from '@/shared/hooks/use-form-modal';
import type { UserDto, RoleDto, UserFormData } from './types';
import { getRoleColor } from './types';

interface UserFormDialogProps {
  modal: ReturnType<typeof useFormModal<UserDto>>;
  roles: RoleDto[];
  onSaved: () => void;
}

const emptyForm: UserFormData = {
  email: '',
  password: '',
  firstName: '',
  lastName: '',
  phoneNumber: '',
  isActive: true,
  roles: [],
};

export function UserFormDialog({ modal, roles, onSaved }: UserFormDialogProps) {
  const [formData, setFormData] = useState<UserFormData>(emptyForm);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!modal.isOpen) return;
    if (modal.editingItem) {
      setFormData({
        email: modal.editingItem.email,
        password: '',
        firstName: modal.editingItem.firstName || '',
        lastName: modal.editingItem.lastName || '',
        phoneNumber: modal.editingItem.phoneNumber || '',
        isActive: modal.editingItem.isActive,
        roles: modal.editingItem.roles,
      });
    } else {
      setFormData(emptyForm);
    }
    setError('');
  }, [modal.isOpen, modal.editingItem]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    try {
      if (modal.editingItem) {
        await api.put(`/admin/users/${modal.editingItem.id}`, {
          firstName: formData.firstName,
          lastName: formData.lastName,
          phoneNumber: formData.phoneNumber,
          isActive: formData.isActive,
          roles: formData.roles,
        });
      } else {
        if (!formData.password) {
          setError('Password is required for new users');
          return;
        }
        await api.post('/admin/users', formData);
      }
      modal.close();
      onSaved();
    } catch (err: unknown) {
      setError(getErrorMessage(err, 'Error saving user'));
    }
  };

  const handleToggleRole = (role: string) => {
    setFormData((prev) => ({
      ...prev,
      roles: prev.roles.includes(role)
        ? prev.roles.filter((r) => r !== role)
        : [...prev.roles, role],
    }));
  };

  if (!modal.isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="w-full max-w-lg rounded-lg bg-white p-6 shadow-xl max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-gray-900">
            {modal.isEditing ? 'Edit User' : 'New User'}
          </h2>
          <button
            onClick={() => modal.close()}
            className="rounded-lg p-1 text-gray-400 hover:bg-gray-100 hover:text-gray-600"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        {error && (
          <div className="mb-4 rounded-lg bg-red-50 p-3 text-sm text-red-600">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                First Name
              </label>
              <input
                type="text"
                value={formData.firstName}
                onChange={(e) =>
                  setFormData({
                    ...formData,
                    firstName: e.target.value,
                  })
                }
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-brand focus:outline-none focus:ring-1 focus:ring-brand"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Last Name
              </label>
              <input
                type="text"
                value={formData.lastName}
                onChange={(e) =>
                  setFormData({
                    ...formData,
                    lastName: e.target.value,
                  })
                }
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-brand focus:outline-none focus:ring-1 focus:ring-brand"
              />
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Email *
            </label>
            <input
              type="email"
              required
              value={formData.email}
              onChange={(e) =>
                setFormData({
                  ...formData,
                  email: e.target.value,
                })
              }
              disabled={modal.isEditing}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-brand focus:outline-none focus:ring-1 focus:ring-brand disabled:bg-gray-100"
            />
          </div>

          {!modal.isEditing && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Password *
              </label>
              <input
                type="password"
                value={formData.password}
                onChange={(e) =>
                  setFormData({
                    ...formData,
                    password: e.target.value,
                  })
                }
                placeholder="Minimum 8 characters"
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-brand focus:outline-none focus:ring-1 focus:ring-brand"
              />
              <p className="mt-1 text-xs text-gray-500">
                Must contain uppercase, lowercase, number and symbol
              </p>
            </div>
          )}

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Phone
            </label>
            <input
              type="tel"
              value={formData.phoneNumber}
              onChange={(e) =>
                setFormData({
                  ...formData,
                  phoneNumber: e.target.value,
                })
              }
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-brand focus:outline-none focus:ring-1 focus:ring-brand"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Roles
            </label>
            <div className="flex flex-wrap gap-2">
              {roles.map((role) => (
                <button
                  key={role.id}
                  type="button"
                  onClick={() => handleToggleRole(role.name)}
                  className={`rounded-full px-3 py-1.5 text-sm font-medium transition-colors ${
                    formData.roles.includes(role.name)
                      ? getRoleColor(role.name)
                      : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
                  }`}
                >
                  {role.name}
                </button>
              ))}
            </div>
          </div>

          {modal.isEditing && (
            <div className="flex items-center gap-2">
              <input
                type="checkbox"
                id="isActive"
                checked={formData.isActive}
                onChange={(e) =>
                  setFormData({
                    ...formData,
                    isActive: e.target.checked,
                  })
                }
                className="h-4 w-4 rounded border-gray-300 text-brand focus:ring-brand"
              />
              <label htmlFor="isActive" className="text-sm text-gray-700">
                User active
              </label>
            </div>
          )}

          <div className="flex justify-end gap-3 pt-4">
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
              {modal.isEditing ? 'Save Changes' : 'Create User'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
