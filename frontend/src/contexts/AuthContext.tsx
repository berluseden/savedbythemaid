/* eslint-disable react-refresh/only-export-components */
import { createContext, useContext, useState, useEffect, type ReactNode } from 'react';
import { authApi, type User } from '../lib/api';
import { ensureCsrfToken, clearCsrfToken } from '@/lib/csrf';
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

  // Bootstrap sequence on mount:
  //   1. Seed the XSRF-TOKEN cookie so the very first POST (login/register)
  //      can carry the X-XSRF-TOKEN header the backend expects.
  //   2. Ping /auth/me to rehydrate the session from the HttpOnly cookie.
  // Both are fire-and-forget — failures leave the user logged out but the
  // app usable.
  useEffect(() => {
    void ensureCsrfToken();
    refreshUser().finally(() => setIsLoading(false));
  }, []);

  const refreshUser = async () => {
    try {
      const response = await authApi.me();
      setUser(response.data);
    } catch {
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
      const response = await authApi.login({ email, password, rememberMe });
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
      setUser(response.data.user);
      return getRedirectPath(response.data.user.roles);
    } catch (err: unknown) {
      const msg = getErrorMessage(err, 'Could not create account');
      throw new Error(msg);
    } finally {
      setIsLoading(false);
    }
  };

  const logout = async () => {
    setIsLoading(true);
    try {
      await authApi.logout();
    } catch {
      // Ignore errors on logout — the cookie will still be cleared by the server
      // or will expire naturally.
    } finally {
      setUser(null);
      setIsLoading(false);
      // Clear and re-seed the CSRF token — the backend rotates it on logout.
      clearCsrfToken();
      void ensureCsrfToken();
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
