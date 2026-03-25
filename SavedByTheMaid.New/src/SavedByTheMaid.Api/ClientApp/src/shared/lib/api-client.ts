// Clean API client using centralized auth-storage and error handling.
// This is the future replacement for lib/api.ts — existing file is untouched.

import axios, { type AxiosError } from 'axios';
import { authStorage } from './auth-storage';

const apiClient = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
});

// Request: attach JWT token
apiClient.interceptors.request.use((config) => {
  const token = authStorage.getToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Response: handle errors consistently
apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    const status = error.response?.status;

    if (status === 401) {
      authStorage.clear();
      window.location.href = '/login';
    }

    return Promise.reject(error);
  },
);

export { apiClient };
export default apiClient;
