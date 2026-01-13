import { useState, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { User, Mail, Phone, MapPin, Save, X, AlertCircle, CheckCircle } from 'lucide-react';
import { Link } from 'react-router-dom';
import api from '@/lib/api';

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
  
  const [editForm, setEditForm] = useState<ProfileData>(profile);

  useEffect(() => {
    fetchProfile();
  }, []);

  const fetchProfile = async () => {
    try {
      setIsLoading(true);
      const response = await api.get<ProfileData>('/customer/profile');
      setProfile(response.data);
      setEditForm(response.data);
    } catch {
      // Fallback to auth context user
      setProfile({
        firstName: user?.firstName || '',
        lastName: user?.lastName || '',
        email: user?.email || '',
        phone: user?.phone || '',
        address: '',
        city: '',
        state: '',
        zipCode: '',
      });
      setEditForm(profile);
    } finally {
      setIsLoading(false);
    }
  };

  const handleSave = async () => {
    try {
      setIsSaving(true);
      setError('');
      
      await api.put('/customer/profile', {
        firstName: editForm.firstName,
        lastName: editForm.lastName,
        phone: editForm.phone,
        address: editForm.address,
        city: editForm.city,
        state: editForm.state,
        zipCode: editForm.zipCode,
      });
      
      setProfile(editForm);
      setIsEditing(false);
      setSuccess('Profile updated successfully!');
      
      // Refresh auth context user
      if (refreshUser) {
        refreshUser();
      }
      
      setTimeout(() => setSuccess(''), 3000);
    } catch {
      setError('Failed to update profile. Please try again.');
    } finally {
      setIsSaving(false);
    }
  };

  const handleCancel = () => {
    setEditForm(profile);
    setIsEditing(false);
    setError('');
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <div className="w-8 h-8 border-4 border-[#00205B] border-t-transparent rounded-full animate-spin mx-auto mb-4" />
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
          <Link to="/dashboard" className="text-[#00205B] hover:text-[#001440] flex items-center gap-1">
            ← Back to Dashboard
          </Link>
        </div>

        {/* Success Message */}
        {success && (
          <div className="mb-6 bg-green-50 border border-green-200 rounded-lg p-4 flex items-center gap-3">
            <CheckCircle className="w-5 h-5 text-green-500" />
            <p className="text-green-700">{success}</p>
          </div>
        )}

        {/* Error Message */}
        {error && (
          <div className="mb-6 bg-red-50 border border-red-200 rounded-lg p-4 flex items-center gap-3">
            <AlertCircle className="w-5 h-5 text-red-500" />
            <p className="text-red-700">{error}</p>
            <button onClick={() => setError('')} className="ml-auto text-red-500">
              <X className="w-4 h-4" />
            </button>
          </div>
        )}

        <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
          {/* Header */}
          <div className="p-6 border-b border-gray-200 bg-gradient-to-r from-[#FFE44D]/50 to-sky-600">
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
                  className="ml-auto px-4 py-2 bg-white text-[#00205B] rounded-lg hover:bg-[#FFE44D]/10 transition-colors font-medium"
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
                <User className="w-5 h-5 text-[#00205B]" />
                Personal Information
              </h2>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    First Name
                  </label>
                  {isEditing ? (
                    <input
                      type="text"
                      value={editForm.firstName}
                      onChange={(e) => setEditForm({ ...editForm, firstName: e.target.value })}
                      className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
                    />
                  ) : (
                    <p className="text-gray-900 py-2">{profile.firstName || '-'}</p>
                  )}
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Last Name
                  </label>
                  {isEditing ? (
                    <input
                      type="text"
                      value={editForm.lastName}
                      onChange={(e) => setEditForm({ ...editForm, lastName: e.target.value })}
                      className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
                    />
                  ) : (
                    <p className="text-gray-900 py-2">{profile.lastName || '-'}</p>
                  )}
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1 flex items-center gap-1">
                    <Mail className="w-4 h-4" /> Email
                  </label>
                  <p className="text-gray-900 py-2">{profile.email}</p>
                  {isEditing && (
                    <p className="text-xs text-gray-500">Email cannot be changed</p>
                  )}
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1 flex items-center gap-1">
                    <Phone className="w-4 h-4" /> Phone
                  </label>
                  {isEditing ? (
                    <input
                      type="tel"
                      value={editForm.phone}
                      onChange={(e) => setEditForm({ ...editForm, phone: e.target.value })}
                      placeholder="(555) 123-4567"
                      className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
                    />
                  ) : (
                    <p className="text-gray-900 py-2">{profile.phone || 'Not specified'}</p>
                  )}
                </div>
              </div>
            </section>

            {/* Address */}
            <section>
              <h2 className="text-lg font-semibold text-gray-900 mb-4 flex items-center gap-2">
                <MapPin className="w-5 h-5 text-[#00205B]" />
                Address
              </h2>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="md:col-span-2">
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Street Address
                  </label>
                  {isEditing ? (
                    <input
                      type="text"
                      value={editForm.address}
                      onChange={(e) => setEditForm({ ...editForm, address: e.target.value })}
                      placeholder="123 Main St, Apt 4B"
                      className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
                    />
                  ) : (
                    <p className="text-gray-900 py-2">{profile.address || 'Not specified'}</p>
                  )}
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    City
                  </label>
                  {isEditing ? (
                    <input
                      type="text"
                      value={editForm.city}
                      onChange={(e) => setEditForm({ ...editForm, city: e.target.value })}
                      className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
                    />
                  ) : (
                    <p className="text-gray-900 py-2">{profile.city || 'Not specified'}</p>
                  )}
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    State
                  </label>
                  {isEditing ? (
                    <input
                      type="text"
                      value={editForm.state}
                      onChange={(e) => setEditForm({ ...editForm, state: e.target.value })}
                      className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
                    />
                  ) : (
                    <p className="text-gray-900 py-2">{profile.state || 'Not specified'}</p>
                  )}
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    ZIP Code
                  </label>
                  {isEditing ? (
                    <input
                      type="text"
                      value={editForm.zipCode}
                      onChange={(e) => setEditForm({ ...editForm, zipCode: e.target.value })}
                      className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00205B] focus:border-transparent"
                    />
                  ) : (
                    <p className="text-gray-900 py-2">{profile.zipCode || 'Not specified'}</p>
                  )}
                </div>
              </div>
            </section>

            {/* Actions */}
            {isEditing && (
              <div className="flex justify-end gap-3 pt-4 border-t border-gray-200">
                <button
                  onClick={handleCancel}
                  disabled={isSaving}
                  className="px-6 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 transition-colors flex items-center gap-2"
                >
                  <X className="w-4 h-4" />
                  Cancel
                </button>
                <button
                  onClick={handleSave}
                  disabled={isSaving}
                  className="px-6 py-2 bg-[#00205B] text-white rounded-lg hover:bg-[#001440] transition-colors flex items-center gap-2 disabled:opacity-50"
                >
                  <Save className="w-4 h-4" />
                  {isSaving ? 'Saving...' : 'Save Changes'}
                </button>
              </div>
            )}

            {/* Security Section */}
            {!isEditing && (
              <section className="pt-4 border-t border-gray-200">
                <h2 className="text-lg font-semibold text-gray-900 mb-4">Security</h2>
                <Link
                  to="/forgot-password"
                  className="inline-flex items-center px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 transition-colors"
                >
                  Change Password
                </Link>
              </section>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

export default ProfilePage;
