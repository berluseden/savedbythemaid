import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useDebounce } from '../use-debounce';

describe('useDebounce', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('returns initial value immediately', () => {
    const { result } = renderHook(() => useDebounce('hello'));
    expect(result.current).toBe('hello');
  });

  it('does not update value before delay', () => {
    const { result, rerender } = renderHook(
      ({ value }) => useDebounce(value, 300),
      { initialProps: { value: 'initial' } }
    );
    rerender({ value: 'updated' });
    act(() => { vi.advanceTimersByTime(100); });
    expect(result.current).toBe('initial');
  });

  it('updates value after delay', () => {
    const { result, rerender } = renderHook(
      ({ value }) => useDebounce(value, 300),
      { initialProps: { value: 'initial' } }
    );
    rerender({ value: 'updated' });
    act(() => { vi.advanceTimersByTime(300); });
    expect(result.current).toBe('updated');
  });

  it('uses default 300ms delay', () => {
    const { result, rerender } = renderHook(
      ({ value }) => useDebounce(value),
      { initialProps: { value: 'a' } }
    );
    rerender({ value: 'b' });
    act(() => { vi.advanceTimersByTime(299); });
    expect(result.current).toBe('a');
    act(() => { vi.advanceTimersByTime(1); });
    expect(result.current).toBe('b');
  });

  it('respects custom delay', () => {
    const { result, rerender } = renderHook(
      ({ value }) => useDebounce(value, 500),
      { initialProps: { value: 'a' } }
    );
    rerender({ value: 'b' });
    act(() => { vi.advanceTimersByTime(400); });
    expect(result.current).toBe('a');
    act(() => { vi.advanceTimersByTime(100); });
    expect(result.current).toBe('b');
  });

  it('resets timer on rapid changes (only last value is used)', () => {
    const { result, rerender } = renderHook(
      ({ value }) => useDebounce(value, 300),
      { initialProps: { value: 'a' } }
    );
    rerender({ value: 'b' });
    act(() => { vi.advanceTimersByTime(200); });
    rerender({ value: 'c' });
    act(() => { vi.advanceTimersByTime(200); });
    // Only 200ms since last change, so still 'a'
    expect(result.current).toBe('a');
    act(() => { vi.advanceTimersByTime(100); });
    // Now 300ms since last change ('c'), so it updates
    expect(result.current).toBe('c');
  });

  it('works with numeric values', () => {
    const { result, rerender } = renderHook(
      ({ value }) => useDebounce(value, 300),
      { initialProps: { value: 0 } }
    );
    rerender({ value: 42 });
    act(() => { vi.advanceTimersByTime(300); });
    expect(result.current).toBe(42);
  });

  it('works with object values', () => {
    const obj1 = { key: 'a' };
    const obj2 = { key: 'b' };
    const { result, rerender } = renderHook(
      ({ value }) => useDebounce(value, 300),
      { initialProps: { value: obj1 } }
    );
    rerender({ value: obj2 });
    act(() => { vi.advanceTimersByTime(300); });
    expect(result.current).toEqual({ key: 'b' });
  });
});
