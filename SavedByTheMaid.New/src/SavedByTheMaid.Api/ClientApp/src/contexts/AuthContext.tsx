import { createContext, useContext, useState, useEffect, type ReactNode } from 'react';
import { authApi, type User } from '../lib/api';

const setToken = (token: string, rememberMe: boolean) => {
  localStorage.setItem('rememberMe', rememberMe ? 'true' : 'false');
  const storage = rememberMe ? localStorage : sessionStorage;
  storage.setItem('token', token);
};

const getToken = () => {
  return localStorage.getItem('token') || sessionStorage.getItem('token');
};

const clearToken = () => {
  localStorage.removeItem('token');
  localStorage.removeItem('rememberMe');
  sessionStorage.removeItem('token');
};

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
  useEffect(() => {
    const token = getToken();
    if (token) {
      refreshUser().finally(() => setIsLoading(false));
    } else {
      setIsLoading(false);
    }
  }, []);

  const refreshUser = async () => {
    try {
      const response = await authApi.me();
      setUser(response.data);
    } catch {
      // Token invalid or expired
      clearToken();
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
      setToken(response.data.accessToken, rememberMe);
      setUser(response.data.user);
      return getRedirectPath(response.data.user.roles);
    } finally {
      setIsLoading(false);
    }
  };

  const register = async (data: RegisterData): Promise<string> => {
    setIsLoading(true);
    try {
      const response = await authApi.register(data);
      setToken(response.data.accessToken, true); // Always remember on register
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
      clearToken();
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

// Protected Route Component
import { Navigate, useLocation } from 'react-router-dom';

interface ProtectedRouteProps {
  children: ReactNode;
  requiredRoles?: string[];
}

export function ProtectedRoute({ children, requiredRoles }: ProtectedRouteProps) {
  const { user, isLoading, isAuthenticated } = useAuth();
  const location = useLocation();

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="w-8 h-8 border-4 border-[#00205B] border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  if (requiredRoles && requiredRoles.length > 0) {
    const hasRequiredRole = user?.roles?.some((role: string) => requiredRoles.includes(role));
    if (!hasRequiredRole) {
      return <Navigate to="/" replace />;
    }
  }

  return <>{children}</>;
}
