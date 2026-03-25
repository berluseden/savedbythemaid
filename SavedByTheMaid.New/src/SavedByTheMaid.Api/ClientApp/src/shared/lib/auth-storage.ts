// Centralized token management
// Replaces duplicate getToken/setToken/clearToken implementations across
// AuthContext.tsx, lib/api.ts interceptor, and authApi.logout().

const TOKEN_KEY = 'token';
const REMEMBER_KEY = 'rememberMe';

export const authStorage = {
  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY) || sessionStorage.getItem(TOKEN_KEY);
  },

  setToken(token: string, rememberMe: boolean): void {
    localStorage.setItem(REMEMBER_KEY, rememberMe ? 'true' : 'false');
    const storage = rememberMe ? localStorage : sessionStorage;
    storage.setItem(TOKEN_KEY, token);
  },

  clear(): void {
    [TOKEN_KEY, 'accessToken', 'refreshToken', REMEMBER_KEY].forEach((key) => {
      localStorage.removeItem(key);
      sessionStorage.removeItem(key);
    });
  },

  isRemembered(): boolean {
    return localStorage.getItem(REMEMBER_KEY) === 'true';
  },
};
