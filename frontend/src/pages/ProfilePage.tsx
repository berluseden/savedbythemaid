import { useState, useEffect, useCallback } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { User, Mail, Phone, MapPin, Save, X, AlertCircle, CheckCircle } from 'lucide-react';
import { Link } from 'react-router-dom';
import api from '@/lib/api';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { profileSchema, type ProfileFormData } from '@/shared/schemas/profile.schema';
import { changePasswordSchema, type ChangePasswordFormData } from '@/shared/schemas/auth.schema';
import { getErrorMessage } from '@/shared/lib/error-utils';

interface ProfileData {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  address: string;
  city: string;
  state: string;
  zipCode: string;
}

export function ProfilePage() {
  const { user, refreshUser } = useAuth();
  const [isEditing, setIsEditing] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [showChangePassword, setShowChangePassword] = useState(false);
  const [isChangingPassword, setIsChangingPassword] = useState(false);
  const [passwordError, setPasswordError] = useState('');
  const [passwordSuccess, setPasswordSuccess] = useState('');

  // Read-only display copy of the profile
  const [profile, setProfile] = useState<ProfileData>({
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    address: '',
    city: '',
    state: '',
    zipCode: '',
  });

  const profileForm = useForm<ProfileFormData>({
    resolver: zodResolver(profileSchema),
    defaultValues: {
      firstName: '',
      lastName: '',
      phone: '',
      address: '',
      city: '',
      state: '',
      zipCode: '',
    },
  });

  const passwordForm = useForm<ChangePasswordFormData>({
    resolver: zodResolver(changePasswordSchema),
  });

  const fetchProfile = useCallback(async () => {
    try {
      setIsLoading(true);
      const response = await api.get<ProfileData>('/customer/profile');
      setProfile(response.data);
      profileForm.reset({
        firstName: response.data.firstName,
        lastName: response.data.lastName,
        phone: response.data.phone,
        address: response.data.address,
        city: response.data.city,
        state: response.data.state,
        zipCode: response.data.zipCode,
      });
    } catch {
      // Fallback to auth context user
      const fallback: ProfileData = {
        firstName: user?.firstName || '',
        lastName: user?.lastName || '',
        email: user?.email || '',
        phone: user?.phone || '',
        address: '',
        city: '',
        state: '',
        zipCode: '',
      };
      setProfile(fallback);
      profileForm.reset({
        firstName: fallback.firstName,
        lastName: fallback.lastName,
        phone: fallback.phone,
        address: fallback.address,
        city: fallback.city,
        state: fallback.state,
        zipCode: fallback.zipCode,
      });
    } finally {
      setIsLoading(false);
    }
  }, [user, profileForm]);

  useEffect(() => {
    fetchProfile();
  }, [fetchProfile]);

  const handleSave = profileForm.handleSubmit(async (formData: ProfileFormData) => {
    try {
      setIsSaving(true);
      setError('');

      await api.put('/customer/profile', {
        firstName: formData.firstName,
        lastName: formData.lastName,
        phone: formData.phone,
        address: formData.address,
        city: formData.city,
        state: formData.state,
        zipCode: formData.zipCode,
      });

      setProfile((prev) => ({
        ...prev,
        firstName: formData.firstName,
        lastName: formData.lastName,
        phone: formData.phone ?? '',
        address: formData.address ?? '',
        city: formData.city ?? '',
        state: formData.state ?? '',
        zipCode: formData.zipCode ?? '',
      }));
      setIsEditing(false);
      setSuccess('Profile updated successfully!');

      if (refreshUser) {
        refreshUser();
      }

      setTimeout(() => setSuccess(''), 3000);
    } catch (err) {
      setError(getErrorMessage(err, 'Failed to update profile. Please try again.'));
    } finally {
      setIsSaving(false);
    }
  });

  const handleCancel = () => {
    profileForm.reset({
      firstName: profile.firstName,
      lastName: profile.lastName,
      phone: profile.phone,
      address: profile.address,
      city: profile.city,
      state: profile.state,
      zipCode: profile.zipCode,
    });
    setIsEditing(false);
    setError('');
  };

  const handleChangePassword = async (data: ChangePasswordFormData) => {
    setPasswordError('');
    setPasswordSuccess('');
    setIsChangingPassword(true);
    try {
      await api.post('/auth/change-password', {
        currentPassword: data.currentPassword,
        newPassword: data.newPassword,
      });
      setPasswordSuccess('Password updated successfully');
      passwordForm.reset();
      setTimeout(() => setShowChangePassword(false), 2000);
    } catch (err) {
      setPasswordError(getErrorMessage(err, 'Failed to update password'));
    } finally {
      setIsChangingPassword(false);
    }
  };

  const pf = profileForm.register;
  const pfErrors = profileForm.formState.errors;

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <div className="w-8 h-8 border-4 border-brand border-t-transparent rounded-full animate-spin mx-auto mb-4" />
          <p className="text-gray-600">Loading profile...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 py-8">
      <div className="max-w-3xl mx-auto px-4">
        {/* Back to Dashboard */}
        <div className="mb-6">
          <Link to="/dashboard" className="text-brand hover:text-brand-dark flex items-center gap-1">
            ← Back to Dashboard
          </Link>
        </div>

        {/* Success Message */}
        {success && (
          <div role="status" className="mb-6 bg-green-50 border border-green-200 rounded-lg p-4 flex items-center gap-3">
            <CheckCircle className="w-5 h-5 text-green-500" />
            <p className="text-green-700">{success}</p>
          </div>
        )}

        {/* Error Message */}
        {error && (
          <div role="alert" className="mb-6 bg-red-50 border border-red-200 rounded-lg p-4 flex items-center gap-3">
            <AlertCircle className="w-5 h-5 text-red-500" />
            <p className="text-red-700">{error}</p>
            <button onClick={() => setError('')} className="ml-auto text-red-500" aria-label="Dismiss">
              <X className="w-4 h-4" />
            </button>
          </div>
        )}

        <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
          {/* Header */}
          <div className="p-6 border-b border-gray-200 bg-gradient-to-r from-accent-light/50 to-sky-600">
            <div className="flex items-center gap-4">
              <div className="w-16 h-16 bg-white/20 rounded-full flex items-center justify-center">
                <User className="w-8 h-8 text-white" />
              </div>
              <div>
                <h1 className="text-2xl font-bold text-white">
                  {profile.firstName} {profile.lastName}
                </h1>
                <p className="text-sky-100">{profile.email}</p>
              </div>
              {!isEditing && (
                <button
                  onClick={() => setIsEditing(true)}
                  className="ml-auto px-4 py-2 bg-white text-brand rounded-lg hover:bg-accent-light/10 transition-colors font-medium"
                >
                  Edit Profile
                </button>
              )}
            </div>
          </div>

          <div className="p-6 space-y-8">
            {/* Personal Information */}
            <section>
              <h2 className="text-lg font-semibold text-gray-900 mb-4 flex items-center gap-2">
                <User className="w-5 h-5 text-brand" aria-hidden="true" />
                Personal Information
              </h2>
              <form id="profile-form" onSubmit={handleSave} noValidate>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label htmlFor="firstName" className="block text-sm font-medium text-gray-700 mb-1">
                      First Name
                    </label>
                    {isEditing ? (
                      <>
                        <input
                          id="firstName"
                          type="text"
                          aria-invalid={pfErrors.firstName ? 'true' : 'false'}
                          aria-describedby={pfErrors.firstName ? 'firstName-error' : undefined}
                          {...pf('firstName')}
                          className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
                        />
                        {pfErrors.firstName && (
                          <p id="firstName-error" className="mt-1 text-sm text-red-600">{pfErrors.firstName.message}</p>
                        )}
                      </>
                    ) : (
                      <p className="text-gray-900 py-2">{profile.firstName || '-'}</p>
                    )}
                  </div>

                  <div>
                    <label htmlFor="lastName" className="block text-sm font-medium text-gray-700 mb-1">
                      Last Name
                    </label>
                    {isEditing ? (
                      <>
                        <input
                          id="lastName"
                          type="text"
                          aria-invalid={pfErrors.lastName ? 'true' : 'false'}
                          aria-describedby={pfErrors.lastName ? 'lastName-error' : undefined}
                          {...pf('lastName')}
                          className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
                        />
                        {pfErrors.lastName && (
                          <p id="lastName-error" className="mt-1 text-sm text-red-600">{pfErrors.lastName.message}</p>
                        )}
                      </>
                    ) : (
                      <p className="text-gray-900 py-2">{profile.lastName || '-'}</p>
                    )}
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1 flex items-center gap-1">
                      <Mail className="w-4 h-4" aria-hidden="true" /> Email
                    </label>
                    <p className="text-gray-900 py-2">{profile.email}</p>
                    {isEditing && (
                      <p className="text-xs text-gray-500">Email cannot be changed</p>
                    )}
                  </div>

                  <div>
                    <label htmlFor="phone" className="block text-sm font-medium text-gray-700 mb-1 flex items-center gap-1">
                      <Phone className="w-4 h-4" aria-hidden="true" /> Phone
                    </label>
                    {isEditing ? (
                      <>
                        <input
                          id="phone"
                          type="tel"
                          aria-invalid={pfErrors.phone ? 'true' : 'false'}
                          aria-describedby={pfErrors.phone ? 'phone-error' : undefined}
                          {...pf('phone')}
                          placeholder="(555) 123-4567"
                          className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
                        />
                        {pfErrors.phone && (
                          <p id="phone-error" className="mt-1 text-sm text-red-600">{pfErrors.phone.message}</p>
                        )}
                      </>
                    ) : (
                      <p className="text-gray-900 py-2">{profile.phone || 'Not specified'}</p>
                    )}
                  </div>
                </div>

                {/* Address */}
                <section className="mt-6">
                  <h2 className="text-lg font-semibold text-gray-900 mb-4 flex items-center gap-2">
                    <MapPin className="w-5 h-5 text-brand" aria-hidden="true" />
                    Address
                  </h2>
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div className="md:col-span-2">
                      <label htmlFor="address" className="block text-sm font-medium text-gray-700 mb-1">
                        Street Address
                      </label>
                      {isEditing ? (
                        <>
                          <input
                            id="address"
                            type="text"
                            aria-invalid={pfErrors.address ? 'true' : 'false'}
                            aria-describedby={pfErrors.address ? 'address-error' : undefined}
                            {...pf('address')}
                            placeholder="123 Main St, Apt 4B"
                            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
                          />
                          {pfErrors.address && (
                            <p id="address-error" className="mt-1 text-sm text-red-600">{pfErrors.address.message}</p>
                          )}
                        </>
                      ) : (
                        <p className="text-gray-900 py-2">{profile.address || 'Not specified'}</p>
                      )}
                    </div>

                    <div>
                      <label htmlFor="city" className="block text-sm font-medium text-gray-700 mb-1">
                        City
                      </label>
                      {isEditing ? (
                        <>
                          <input
                            id="city"
                            type="text"
                            aria-invalid={pfErrors.city ? 'true' : 'false'}
                            aria-describedby={pfErrors.city ? 'city-error' : undefined}
                            {...pf('city')}
                            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
                          />
                          {pfErrors.city && (
                            <p id="city-error" className="mt-1 text-sm text-red-600">{pfErrors.city.message}</p>
                          )}
                        </>
                      ) : (
                        <p className="text-gray-900 py-2">{profile.city || 'Not specified'}</p>
                      )}
                    </div>

                    <div>
                      <label htmlFor="state" className="block text-sm font-medium text-gray-700 mb-1">
                        State
                      </label>
                      {isEditing ? (
                        <>
                          <input
                            id="state"
                            type="text"
                            aria-invalid={pfErrors.state ? 'true' : 'false'}
                            aria-describedby={pfErrors.state ? 'state-error' : undefined}
                            {...pf('state')}
                            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
                          />
                          {pfErrors.state && (
                            <p id="state-error" className="mt-1 text-sm text-red-600">{pfErrors.state.message}</p>
                          )}
                        </>
                      ) : (
                        <p className="text-gray-900 py-2">{profile.state || 'Not specified'}</p>
                      )}
                    </div>

                    <div>
                      <label htmlFor="zipCode" className="block text-sm font-medium text-gray-700 mb-1">
                        ZIP Code
                      </label>
                      {isEditing ? (
                        <>
                          <input
                            id="zipCode"
                            type="text"
                            aria-invalid={pfErrors.zipCode ? 'true' : 'false'}
                            aria-describedby={pfErrors.zipCode ? 'zipCode-error' : undefined}
                            {...pf('zipCode')}
                            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
                          />
                          {pfErrors.zipCode && (
                            <p id="zipCode-error" className="mt-1 text-sm text-red-600">{pfErrors.zipCode.message}</p>
                          )}
                        </>
                      ) : (
                        <p className="text-gray-900 py-2">{profile.zipCode || 'Not specified'}</p>
                      )}
                    </div>
                  </div>
                </section>

                {/* Actions */}
                {isEditing && (
                  <div className="flex justify-end gap-3 pt-4 border-t border-gray-200 mt-6">
                    <button
                      type="button"
                      onClick={handleCancel}
                      disabled={isSaving}
                      className="px-6 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 transition-colors flex items-center gap-2"
                    >
                      <X className="w-4 h-4" />
                      Cancel
                    </button>
                    <button
                      type="submit"
                      disabled={isSaving}
                      className="px-6 py-2 bg-brand text-white rounded-lg hover:bg-brand-dark transition-colors flex items-center gap-2 disabled:opacity-50"
                    >
                      <Save className="w-4 h-4" aria-hidden="true" />
                      {isSaving ? 'Saving...' : 'Save Changes'}
                    </button>
                  </div>
                )}
              </form>
            </section>

            {/* Security Section */}
            {!isEditing && (
              <section className="pt-4 border-t border-gray-200">
                <h2 className="text-lg font-semibold text-gray-900 mb-4">Security</h2>
                {showChangePassword ? (
                  <form onSubmit={passwordForm.handleSubmit(handleChangePassword)} className="space-y-4 max-w-md">
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">Current Password</label>
                      <input
                        type="password"
                        {...passwordForm.register('currentPassword')}
                        className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
                      />
                      {passwordForm.formState.errors.currentPassword?.message && (
                        <p className="mt-1 text-sm text-red-500">{passwordForm.formState.errors.currentPassword.message}</p>
                      )}
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">New Password</label>
                      <input
                        type="password"
                        {...passwordForm.register('newPassword')}
                        className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand focus:border-transparent"
                        placeholder="Minimum 8 characters"
                      />
                      {passwordForm.formState.errors.newPassword?.message && (
                        <p className="mt-1 text-sm text-red-500">{passwordForm.formState.errors.newPassword.message}</p>
                      )}
                    </div>
                    {passwordError && <p className="text-sm text-red-500">{passwordError}</p>}
                    {passwordSuccess && <p className="text-sm text-green-600">{passwordSuccess}</p>}
                    <div className="flex gap-3">
                      <button
                        type="submit"
                        disabled={isChangingPassword}
                        className="px-4 py-2 bg-brand text-white rounded-lg hover:bg-brand-dark transition-colors disabled:opacity-50"
                      >
                        {isChangingPassword ? 'Updating...' : 'Update Password'}
                      </button>
                      <button
                        type="button"
                        onClick={() => { setShowChangePassword(false); passwordForm.reset(); setPasswordError(''); setPasswordSuccess(''); }}
                        className="px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 transition-colors"
                      >
                        Cancel
                      </button>
                    </div>
                  </form>
                ) : (
                  <button
                    onClick={() => setShowChangePassword(true)}
                    className="inline-flex items-center px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 transition-colors"
                  >
                    Change Password
                  </button>
                )}
              </section>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

export default ProfilePage;
