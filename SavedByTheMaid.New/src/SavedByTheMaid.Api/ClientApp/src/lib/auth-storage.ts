const TOKEN_KEY = 'token';
const REFRESH_KEY = 'refresh_token';
const REMEMBER_KEY = 'rememberMe';

function getStorage(): Storage {
  return localStorage.getItem(REMEMBER_KEY) === 'true' ? localStorage : sessionStorage;
}

export const authStorage = {
  getToken: (): string | null => getStorage().getItem(TOKEN_KEY),

  setToken: (token: string): void => {
    getStorage().setItem(TOKEN_KEY, token);
  },

  getRefreshToken: (): string | null => getStorage().getItem(REFRESH_KEY),

  setRefreshToken: (token: string): void => {
    getStorage().setItem(REFRESH_KEY, token);
  },

  setRememberMe: (remember: boolean): void => {
    localStorage.setItem(REMEMBER_KEY, String(remember));
  },

  clear: (): void => {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_KEY);
    localStorage.removeItem(REMEMBER_KEY);
    sessionStorage.removeItem(TOKEN_KEY);
    sessionStorage.removeItem(REFRESH_KEY);
  },
};
