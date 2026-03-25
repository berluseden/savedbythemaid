import { describe, it, expect, beforeEach } from 'vitest';
import { authStorage } from '../auth-storage';

describe('authStorage', () => {
  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
  });

  describe('setToken', () => {
    it('stores token in localStorage when rememberMe is true', () => {
      authStorage.setToken('abc123', true);
      expect(localStorage.getItem('token')).toBe('abc123');
      expect(sessionStorage.getItem('token')).toBeNull();
    });

    it('stores token in sessionStorage when rememberMe is false', () => {
      authStorage.setToken('abc123', false);
      expect(sessionStorage.getItem('token')).toBe('abc123');
      expect(localStorage.getItem('token')).toBeNull();
    });

    it('stores rememberMe preference in localStorage', () => {
      authStorage.setToken('abc123', true);
      expect(localStorage.getItem('rememberMe')).toBe('true');
    });

    it('stores rememberMe=false in localStorage', () => {
      authStorage.setToken('abc123', false);
      expect(localStorage.getItem('rememberMe')).toBe('false');
    });
  });

  describe('getToken', () => {
    it('returns token from localStorage first', () => {
      localStorage.setItem('token', 'local-token');
      sessionStorage.setItem('token', 'session-token');
      expect(authStorage.getToken()).toBe('local-token');
    });

    it('falls back to sessionStorage', () => {
      sessionStorage.setItem('token', 'session-token');
      expect(authStorage.getToken()).toBe('session-token');
    });

    it('returns null when no token exists', () => {
      expect(authStorage.getToken()).toBeNull();
    });
  });

  describe('clear', () => {
    it('clears all auth-related keys from both storages', () => {
      localStorage.setItem('token', 'a');
      localStorage.setItem('accessToken', 'b');
      localStorage.setItem('refreshToken', 'c');
      localStorage.setItem('rememberMe', 'true');
      sessionStorage.setItem('token', 'd');
      sessionStorage.setItem('accessToken', 'e');

      authStorage.clear();

      expect(localStorage.getItem('token')).toBeNull();
      expect(localStorage.getItem('accessToken')).toBeNull();
      expect(localStorage.getItem('refreshToken')).toBeNull();
      expect(localStorage.getItem('rememberMe')).toBeNull();
      expect(sessionStorage.getItem('token')).toBeNull();
      expect(sessionStorage.getItem('accessToken')).toBeNull();
    });

    it('does not throw when storages are already empty', () => {
      expect(() => authStorage.clear()).not.toThrow();
    });
  });

  describe('isRemembered', () => {
    it('returns true when rememberMe is "true"', () => {
      localStorage.setItem('rememberMe', 'true');
      expect(authStorage.isRemembered()).toBe(true);
    });

    it('returns false when rememberMe is "false"', () => {
      localStorage.setItem('rememberMe', 'false');
      expect(authStorage.isRemembered()).toBe(false);
    });

    it('returns false when rememberMe is not set', () => {
      expect(authStorage.isRemembered()).toBe(false);
    });
  });
});
