import { useState, useEffect } from 'react';
import {
  Plus,
  Search,
  Edit2,
  Trash2,
  Mail,
  Phone,
  Shield,
  X,
  Key,
  UserCheck,
  UserX,
  User,
} from 'lucide-react';
import { AdminLayout } from '../../components/admin/AdminLayout';
import api from '../../lib/api';

interface UserDto {
  id: string;
  email: string;
  firstName: string | null;
  lastName: string | null;
  phoneNumber: string | null;
  isActive: boolean;
  emailConfirmed: boolean;
  roles: string[];
  createdAt: string;
}

interface RoleDto {
  id: string;
  name: string;
}

export function AdminUsersPage() {
  const [users, setUsers] = useState<UserDto[]>([]);
  const [roles, setRoles] = useState<RoleDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [filterRole, setFilterRole] = useState<string>('all');
  const [filterStatus, setFilterStatus] = useState<string>('all');

  // User modal
  const [showUserModal, setShowUserModal] = useState(false);
  const [editingUser, setEditingUser] = useState<UserDto | null>(null);
  const [userFormData, setUserFormData] = useState({
    email: '',
    password: '',
    firstName: '',
    lastName: '',
    phoneNumber: '',
    isActive: true,
    roles: [] as string[],
  });
  const [userFormError, setUserFormError] = useState('');

  // Password reset modal
  const [showPasswordModal, setShowPasswordModal] = useState(false);
  const [passwordUserId, setPasswordUserId] = useState<string | null>(null);
  const [newPassword, setNewPassword] = useState('');
  const [passwordError, setPasswordError] = useState('');

  // Role modal
  const [showRoleModal, setShowRoleModal] = useState(false);
  const [newRoleName, setNewRoleName] = useState('');
  const [roleError, setRoleError] = useState('');

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    setIsLoading(true);
    try {
      const [usersRes, rolesRes] = await Promise.all([
        api.get<UserDto[]>('/admin/users'),
        api.get<RoleDto[]>('/admin/users/roles'),
      ]);
      setUsers(usersRes.data);
      setRoles(rolesRes.data);
    } catch (error) {
      console.error('Error fetching data:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleOpenUserModal = (user?: UserDto) => {
    if (user) {
      setEditingUser(user);
      setUserFormData({
        email: user.email,
        password: '',
        firstName: user.firstName || '',
        lastName: user.lastName || '',
        phoneNumber: user.phoneNumber || '',
        isActive: user.isActive,
        roles: user.roles,
      });
    } else {
      setEditingUser(null);
      setUserFormData({
        email: '',
        password: '',
        firstName: '',
        lastName: '',
        phoneNumber: '',
        isActive: true,
        roles: [],
      });
    }
    setUserFormError('');
    setShowUserModal(true);
  };

  const handleCloseUserModal = () => {
    setShowUserModal(false);
    setEditingUser(null);
  };

  const handleSubmitUser = async (e: React.FormEvent) => {
    e.preventDefault();
    setUserFormError('');

    try {
      if (editingUser) {
        await api.put(`/admin/users/${editingUser.id}`, {
          firstName: userFormData.firstName,
          lastName: userFormData.lastName,
          phoneNumber: userFormData.phoneNumber,
          isActive: userFormData.isActive,
          roles: userFormData.roles,
        });
      } else {
        if (!userFormData.password) {
          setUserFormError('Password is required for new users');
          return;
        }
        await api.post('/admin/users', userFormData);
      }
      handleCloseUserModal();
      fetchData();
    } catch (error: unknown) {
      const maybe = error as { response?: { data?: { message?: string } } };
      setUserFormError(maybe.response?.data?.message || 'Error saving user');
    }
  };

  const handleDeleteUser = async (user: UserDto) => {
    if (!confirm(`Are you sure you want to deactivate user ${user.email}?`)) {
      return;
    }
    try {
      await api.delete(`/admin/users/${user.id}`);
      fetchData();
    } catch (error: unknown) {
      const maybe = error as { response?: { data?: { message?: string } } };
      alert(maybe.response?.data?.message || 'Error deleting user');
    }
  };

  const handleOpenPasswordModal = (userId: string) => {
    setPasswordUserId(userId);
    setNewPassword('');
    setPasswordError('');
    setShowPasswordModal(true);
  };

  const handleResetPassword = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!passwordUserId) return;

    try {
      await api.post(`/admin/users/${passwordUserId}/reset-password`, {
        newPassword,
      });
      setShowPasswordModal(false);
      alert('Password updated successfully');
    } catch (error: unknown) {
      const maybe = error as { response?: { data?: { message?: string } } };
      setPasswordError(maybe.response?.data?.message || 'Error changing password');
    }
  };

  const handleToggleRole = (role: string) => {
    setUserFormData((prev) => ({
      ...prev,
      roles: prev.roles.includes(role)
        ? prev.roles.filter((r) => r !== role)
        : [...prev.roles, role],
    }));
  };

  const handleCreateRole = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newRoleName.trim()) return;

    try {
      await api.post('/admin/users/roles', { name: newRoleName.trim() });
      setShowRoleModal(false);
      setNewRoleName('');
      fetchData();
    } catch (error: unknown) {
      const maybe = error as { response?: { data?: { message?: string } } };
      setRoleError(maybe.response?.data?.message || 'Error creating role');
    }
  };

  const handleDeleteRole = async (role: RoleDto) => {
    if (!confirm(`Delete role "${role.name}"?`)) return;

    try {
      await api.delete(`/admin/users/roles/${role.id}`);
      fetchData();
    } catch (error: unknown) {
      const maybe = error as { response?: { data?: { message?: string } } };
      alert(maybe.response?.data?.message || 'Error deleting role');
    }
  };

  const filteredUsers = users.filter((user) => {
    const matchesSearch =
      user.email.toLowerCase().includes(searchTerm.toLowerCase()) ||
      user.firstName?.toLowerCase().includes(searchTerm.toLowerCase()) ||
      user.lastName?.toLowerCase().includes(searchTerm.toLowerCase());

    const matchesRole =
      filterRole === 'all' || user.roles.includes(filterRole);

    const matchesStatus =
      filterStatus === 'all' ||
      (filterStatus === 'active' && user.isActive) ||
      (filterStatus === 'inactive' && !user.isActive);

    return matchesSearch && matchesRole && matchesStatus;
  });

  const getRoleColor = (role: string) => {
    switch (role) {
      case 'Admin':
        return 'bg-red-100 text-red-700';
      case 'Employee':
        return 'bg-blue-100 text-blue-700';
      case 'Customer':
        return 'bg-green-100 text-green-700';
      default:
        return 'bg-gray-100 text-gray-700';
    }
  };

  return (
    <AdminLayout>
      <div className="space-y-6">
        {/* Header */}
        <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">
              Users and Roles
            </h1>
            <p className="mt-1 text-sm text-gray-500">
              Manage system users and their permissions
            </p>
          </div>
          <div className="flex gap-2">
            <button
              onClick={() => setShowRoleModal(true)}
              className="inline-flex items-center gap-2 rounded-lg border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
            >
              <Shield className="h-4 w-4" />
              Manage Roles
            </button>
            <button
              onClick={() => handleOpenUserModal()}
              className="inline-flex items-center gap-2 rounded-lg bg-[#00205B] px-4 py-2 text-sm font-medium text-white hover:bg-[#001440]"
            >
              <Plus className="h-4 w-4" />
              New User
            </button>
          </div>
        </div>

        {/* Stats */}
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-4">
          <div className="rounded-lg border border-gray-200 bg-white p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-[#FFE44D]/20 p-2">
                <User className="h-5 w-5 text-[#00205B]" />
              </div>
              <div>
                <p className="text-2xl font-bold text-gray-900">{users.length}</p>
                <p className="text-sm text-gray-500">Total users</p>
              </div>
            </div>
          </div>
          <div className="rounded-lg border border-gray-200 bg-white p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-green-100 p-2">
                <UserCheck className="h-5 w-5 text-green-600" />
              </div>
              <div>
                <p className="text-2xl font-bold text-gray-900">
                  {users.filter((u) => u.isActive).length}
                </p>
                <p className="text-sm text-gray-500">Active</p>
              </div>
            </div>
          </div>
          <div className="rounded-lg border border-gray-200 bg-white p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-red-100 p-2">
                <Shield className="h-5 w-5 text-red-600" />
              </div>
              <div>
                <p className="text-2xl font-bold text-gray-900">
                  {users.filter((u) => u.roles.includes('Admin')).length}
                </p>
                <p className="text-sm text-gray-500">Administrators</p>
              </div>
            </div>
          </div>
          <div className="rounded-lg border border-gray-200 bg-white p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-purple-100 p-2">
                <Shield className="h-5 w-5 text-purple-600" />
              </div>
              <div>
                <p className="text-2xl font-bold text-gray-900">{roles.length}</p>
                <p className="text-sm text-gray-500">Roles</p>
              </div>
            </div>
          </div>
        </div>

        {/* Filters */}
        <div className="flex flex-col gap-4 sm:flex-row">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 h-5 w-5 -translate-y-1/2 text-gray-400" />
            <input
              type="text"
              placeholder="Search by name or email..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="w-full rounded-lg border border-gray-300 bg-white py-2 pl-10 pr-4 text-sm focus:border-[#00205B] focus:outline-none focus:ring-1 focus:ring-[#00205B]"
            />
          </div>
          <select
            value={filterRole}
            onChange={(e) => setFilterRole(e.target.value)}
            className="rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm focus:border-[#00205B] focus:outline-none focus:ring-1 focus:ring-[#00205B]"
          >
            <option value="all">All roles</option>
            {roles.map((role) => (
              <option key={role.id} value={role.name}>
                {role.name}
              </option>
            ))}
          </select>
          <select
            value={filterStatus}
            onChange={(e) => setFilterStatus(e.target.value)}
            className="rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm focus:border-[#00205B] focus:outline-none focus:ring-1 focus:ring-[#00205B]"
          >
            <option value="all">All statuses</option>
            <option value="active">Active</option>
            <option value="inactive">Inactive</option>
          </select>
        </div>

        {/* Users Table */}
        {isLoading ? (
          <div className="flex items-center justify-center py-12">
            <div className="h-8 w-8 animate-spin rounded-full border-4 border-[#00205B] border-t-transparent" />
          </div>
        ) : filteredUsers.length === 0 ? (
          <div className="rounded-lg border border-gray-200 bg-white p-12 text-center">
            <User className="mx-auto h-12 w-12 text-gray-400" />
            <h3 className="mt-4 text-lg font-medium text-gray-900">
              No users
            </h3>
            <p className="mt-2 text-sm text-gray-500">
              {searchTerm || filterRole !== 'all' || filterStatus !== 'all'
                ? 'No se encontraron usuarios con los filtros aplicados'
                : 'Comienza creando tu primer usuario'}
            </p>
          </div>
        ) : (
          <div className="overflow-hidden rounded-lg border border-gray-200 bg-white">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500">
                    Usuario
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500">
                    Contacto
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500">
                    Roles
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500">
                    Estado
                  </th>
                  <th className="px-6 py-3 text-right text-xs font-medium uppercase tracking-wider text-gray-500">
                    Acciones
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200 bg-white">
                {filteredUsers.map((user) => (
                  <tr key={user.id} className="hover:bg-gray-50">
                    <td className="whitespace-nowrap px-6 py-4">
                      <div className="flex items-center gap-3">
                        <div className="flex h-10 w-10 items-center justify-center rounded-full bg-[#FFE44D]/20 text-[#00205B] font-semibold">
                          {(user.firstName?.[0] || user.email[0]).toUpperCase()}
                        </div>
                        <div>
                          <div className="font-medium text-gray-900">
                            {user.firstName && user.lastName
                              ? `${user.firstName} ${user.lastName}`
                              : user.email}
                          </div>
                          {user.firstName && (
                            <div className="text-sm text-gray-500">
                              {user.email}
                            </div>
                          )}
                        </div>
                      </div>
                    </td>
                    <td className="whitespace-nowrap px-6 py-4">
                      <div className="flex flex-col gap-1">
                        <div className="flex items-center gap-1 text-sm text-gray-500">
                          <Mail className="h-3 w-3" />
                          {user.email}
                        </div>
                        {user.phoneNumber && (
                          <div className="flex items-center gap-1 text-sm text-gray-500">
                            <Phone className="h-3 w-3" />
                            {user.phoneNumber}
                          </div>
                        )}
                      </div>
                    </td>
                    <td className="whitespace-nowrap px-6 py-4">
                      <div className="flex flex-wrap gap-1">
                        {user.roles.map((role) => (
                          <span
                            key={role}
                            className={`inline-flex rounded-full px-2 py-0.5 text-xs font-medium ${getRoleColor(
                              role
                            )}`}
                          >
                            {role}
                          </span>
                        ))}
                        {user.roles.length === 0 && (
                          <span className="text-sm text-gray-400 italic">
                            Sin roles
                          </span>
                        )}
                      </div>
                    </td>
                    <td className="whitespace-nowrap px-6 py-4">
                      <span
                        className={`inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-medium ${
                          user.isActive
                            ? 'bg-green-100 text-green-700'
                            : 'bg-red-100 text-red-700'
                        }`}
                      >
                        {user.isActive ? (
                          <>
                            <UserCheck className="h-3 w-3" />
                            Activo
                          </>
                        ) : (
                          <>
                            <UserX className="h-3 w-3" />
                            Inactivo
                          </>
                        )}
                      </span>
                    </td>
                    <td className="whitespace-nowrap px-6 py-4 text-right">
                      <div className="flex items-center justify-end gap-2">
                        <button
                          onClick={() => handleOpenPasswordModal(user.id)}
                          className="rounded-lg p-2 text-gray-500 hover:bg-gray-100 hover:text-gray-700"
                          title="Change password"
                        >
                          <Key className="h-4 w-4" />
                        </button>
                        <button
                          onClick={() => handleOpenUserModal(user)}
                          className="rounded-lg p-2 text-gray-500 hover:bg-gray-100 hover:text-gray-700"
                          title="Edit user"
                        >
                          <Edit2 className="h-4 w-4" />
                        </button>
                        <button
                          onClick={() => handleDeleteUser(user)}
                          className="rounded-lg p-2 text-gray-500 hover:bg-red-50 hover:text-red-600"
                          title="Desactivar usuario"
                        >
                          <Trash2 className="h-4 w-4" />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {/* User Modal */}
        {showUserModal && (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
            <div className="w-full max-w-lg rounded-lg bg-white p-6 shadow-xl max-h-[90vh] overflow-y-auto">
              <div className="flex items-center justify-between mb-4">
                <h2 className="text-lg font-semibold text-gray-900">
                  {editingUser ? 'Edit User' : 'New User'}
                </h2>
                <button
                  onClick={handleCloseUserModal}
                  className="rounded-lg p-1 text-gray-400 hover:bg-gray-100 hover:text-gray-600"
                >
                  <X className="h-5 w-5" />
                </button>
              </div>

              {userFormError && (
                <div className="mb-4 rounded-lg bg-red-50 p-3 text-sm text-red-600">
                  {userFormError}
                </div>
              )}

              <form onSubmit={handleSubmitUser} className="space-y-4">
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Nombre
                    </label>
                    <input
                      type="text"
                      value={userFormData.firstName}
                      onChange={(e) =>
                        setUserFormData({
                          ...userFormData,
                          firstName: e.target.value,
                        })
                      }
                      className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-[#00205B] focus:outline-none focus:ring-1 focus:ring-[#00205B]"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Apellido
                    </label>
                    <input
                      type="text"
                      value={userFormData.lastName}
                      onChange={(e) =>
                        setUserFormData({
                          ...userFormData,
                          lastName: e.target.value,
                        })
                      }
                      className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-[#00205B] focus:outline-none focus:ring-1 focus:ring-[#00205B]"
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
                    value={userFormData.email}
                    onChange={(e) =>
                      setUserFormData({
                        ...userFormData,
                        email: e.target.value,
                      })
                    }
                    disabled={!!editingUser}
                    className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-[#00205B] focus:outline-none focus:ring-1 focus:ring-[#00205B] disabled:bg-gray-100"
                  />
                </div>

                {!editingUser && (
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Password *
                    </label>
                    <input
                      type="password"
                      value={userFormData.password}
                      onChange={(e) =>
                        setUserFormData({
                          ...userFormData,
                          password: e.target.value,
                        })
                      }
                      placeholder="Minimum 8 characters"
                      className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-[#00205B] focus:outline-none focus:ring-1 focus:ring-[#00205B]"
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
                    value={userFormData.phoneNumber}
                    onChange={(e) =>
                      setUserFormData({
                        ...userFormData,
                        phoneNumber: e.target.value,
                      })
                    }
                    className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-[#00205B] focus:outline-none focus:ring-1 focus:ring-[#00205B]"
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
                          userFormData.roles.includes(role.name)
                            ? getRoleColor(role.name)
                            : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
                        }`}
                      >
                        {role.name}
                      </button>
                    ))}
                  </div>
                </div>

                {editingUser && (
                  <div className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      id="isActive"
                      checked={userFormData.isActive}
                      onChange={(e) =>
                        setUserFormData({
                          ...userFormData,
                          isActive: e.target.checked,
                        })
                      }
                      className="h-4 w-4 rounded border-gray-300 text-[#00205B] focus:ring-[#00205B]"
                    />
                    <label htmlFor="isActive" className="text-sm text-gray-700">
                      User active
                    </label>
                  </div>
                )}

                <div className="flex justify-end gap-3 pt-4">
                  <button
                    type="button"
                    onClick={handleCloseUserModal}
                    className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    className="rounded-lg bg-[#00205B] px-4 py-2 text-sm font-medium text-white hover:bg-[#001440]"
                  >
                    {editingUser ? 'Save Changes' : 'Create User'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}

        {/* Password Reset Modal */}
        {showPasswordModal && (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
            <div className="w-full max-w-sm rounded-lg bg-white p-6 shadow-xl">
              <div className="flex items-center justify-between mb-4">
                <h2 className="text-lg font-semibold text-gray-900">
                  Change Password
                </h2>
                <button
                  onClick={() => setShowPasswordModal(false)}
                  className="rounded-lg p-1 text-gray-400 hover:bg-gray-100 hover:text-gray-600"
                >
                  <X className="h-5 w-5" />
                </button>
              </div>

              {passwordError && (
                <div className="mb-4 rounded-lg bg-red-50 p-3 text-sm text-red-600">
                  {passwordError}
                </div>
              )}

              <form onSubmit={handleResetPassword} className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Nueva Password *
                  </label>
                  <input
                    type="password"
                    required
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    placeholder="Minimum 8 characters"
                    className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-[#00205B] focus:outline-none focus:ring-1 focus:ring-[#00205B]"
                  />
                  <p className="mt-1 text-xs text-gray-500">
                    Must contain uppercase, lowercase, number and symbol
                  </p>
                </div>

                <div className="flex justify-end gap-3 pt-2">
                  <button
                    type="button"
                    onClick={() => setShowPasswordModal(false)}
                    className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    className="rounded-lg bg-[#00205B] px-4 py-2 text-sm font-medium text-white hover:bg-[#001440]"
                  >
                    Cambiar
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}

        {/* Role Management Modal */}
        {showRoleModal && (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
            <div className="w-full max-w-md rounded-lg bg-white p-6 shadow-xl">
              <div className="flex items-center justify-between mb-4">
                <h2 className="text-lg font-semibold text-gray-900">
                  Manage Roles
                </h2>
                <button
                  onClick={() => setShowRoleModal(false)}
                  className="rounded-lg p-1 text-gray-400 hover:bg-gray-100 hover:text-gray-600"
                >
                  <X className="h-5 w-5" />
                </button>
              </div>

              {/* Existing Roles */}
              <div className="mb-4">
                <h3 className="text-sm font-medium text-gray-700 mb-2">
                  Roles existentes
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
                        <span className="text-xs text-gray-400">Sistema</span>
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
                {roleError && (
                  <div className="mb-2 text-sm text-red-600">{roleError}</div>
                )}
                <div className="flex gap-2">
                  <input
                    type="text"
                    value={newRoleName}
                    onChange={(e) => {
                      setNewRoleName(e.target.value);
                      setRoleError('');
                    }}
                    placeholder="Role name"
                    className="flex-1 rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-[#00205B] focus:outline-none focus:ring-1 focus:ring-[#00205B]"
                  />
                  <button
                    type="submit"
                    className="rounded-lg bg-[#00205B] px-4 py-2 text-sm font-medium text-white hover:bg-[#001440]"
                  >
                    Crear
                  </button>
                </div>
              </form>

              <div className="flex justify-end pt-4">
                <button
                  type="button"
                  onClick={() => setShowRoleModal(false)}
                  className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
                >
                  Cerrar
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </AdminLayout>
  );
}
