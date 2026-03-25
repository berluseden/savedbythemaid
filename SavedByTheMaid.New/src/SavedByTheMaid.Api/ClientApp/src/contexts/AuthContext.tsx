/* eslint-disable react-refresh/only-export-components */
import { createContext, useContext, useState, useEffect, type ReactNode } from 'react';
import { authApi, type User } from '../lib/api';
import { authStorage } from '@/shared/lib/auth-storage';
import { getErrorMessage } from '@/shared/lib/error-utils';

interface AuthContextType {
  user: User | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  login: (email: string, password: string, rememberMe?: boolean) => Promise<string>;
  register: (data: RegisterData) => Promise<string>;
  logout: () => Promise<void>;
  refreshUser: () => Promise<void>;
}

interface RegisterData {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  password: string;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // Check if user is authenticated on mount
  // With HttpOnly cookies, the token is sent automatically by the browser.
  // We always call /auth/me to verify — the cookie handles authentication.
  useEffect(() => {
    refreshUser().finally(() => setIsLoading(false));
  }, []);

  const refreshUser = async () => {
    try {
      const response = await authApi.me();
      setUser(response.data);
    } catch {
      // Token invalid or expired
      authStorage.clear();
      setUser(null);
    }
  };

  // Returns redirect path based on user role
  const getRedirectPath = (roles: string[]): string => {
    if (roles.includes('Admin')) return '/admin';
    if (roles.includes('Employee')) return '/employee';
    return '/dashboard';
  };

  const login = async (email: string, password: string, rememberMe: boolean = false): Promise<string> => {
    setIsLoading(true);
    try {
      const response = await authApi.login({ email, password });
      // Backend sets HttpOnly cookies automatically.
      // Also store in localStorage as fallback for backward compatibility.
      if (response.data.accessToken) {
        authStorage.setToken(response.data.accessToken, rememberMe);
      }
      setUser(response.data.user);
      return getRedirectPath(response.data.user.roles);
    } catch (err: unknown) {
      const msg = getErrorMessage(err, 'Invalid email or password');
      throw new Error(msg);
    } finally {
      setIsLoading(false);
    }
  };

  const register = async (data: RegisterData): Promise<string> => {
    setIsLoading(true);
    try {
      const response = await authApi.register(data);
      // Backend sets HttpOnly cookies automatically.
      if (response.data.accessToken) {
        authStorage.setToken(response.data.accessToken, true);
      }
      setUser(response.data.user);
      return getRedirectPath(response.data.user.roles);
    } finally {
      setIsLoading(false);
    }
  };

  const logout = async () => {
    setIsLoading(true);
    try {
      await authApi.logout();
    } catch {
      // Ignore errors on logout
    } finally {
      authStorage.clear();
      setUser(null);
      setIsLoading(false);
    }
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        isLoading,
        isAuthenticated: !!user,
        login,
        register,
        logout,
        refreshUser,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
