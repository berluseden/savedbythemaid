import { describe, it, expect, vi } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useFormModal } from '../use-form-modal';

describe('useFormModal', () => {
  it('starts closed with no editing item', () => {
    const { result } = renderHook(() => useFormModal());
    expect(result.current.isOpen).toBe(false);
    expect(result.current.isEditing).toBe(false);
    expect(result.current.editingItem).toBeNull();
  });

  it('opens for create (no item)', () => {
    const { result } = renderHook(() => useFormModal());
    act(() => result.current.open());
    expect(result.current.isOpen).toBe(true);
    expect(result.current.isEditing).toBe(false);
    expect(result.current.editingItem).toBeNull();
  });

  it('opens for edit (with item)', () => {
    const { result } = renderHook(() => useFormModal<{ id: number; name: string }>());
    act(() => result.current.open({ id: 1, name: 'Test' }));
    expect(result.current.isOpen).toBe(true);
    expect(result.current.isEditing).toBe(true);
    expect(result.current.editingItem).toEqual({ id: 1, name: 'Test' });
  });

  it('closes the modal', () => {
    vi.useFakeTimers();
    const { result } = renderHook(() => useFormModal<{ id: number; name: string }>());
    act(() => result.current.open({ id: 1, name: 'Test' }));
    act(() => result.current.close());
    expect(result.current.isOpen).toBe(false);
    // editingItem is cleared after a setTimeout(150ms)
    act(() => { vi.advanceTimersByTime(200); });
    expect(result.current.editingItem).toBeNull();
    vi.useRealTimers();
  });

  it('can reopen after closing', () => {
    vi.useFakeTimers();
    const { result } = renderHook(() => useFormModal<{ id: number; name: string }>());
    act(() => result.current.open({ id: 1, name: 'First' }));
    act(() => result.current.close());
    act(() => { vi.advanceTimersByTime(200); });
    act(() => result.current.open({ id: 2, name: 'Second' }));
    expect(result.current.isOpen).toBe(true);
    expect(result.current.editingItem).toEqual({ id: 2, name: 'Second' });
    vi.useRealTimers();
  });

  it('opens for create after editing', () => {
    vi.useFakeTimers();
    const { result } = renderHook(() => useFormModal<{ id: number; name: string }>());
    act(() => result.current.open({ id: 1, name: 'Edit' }));
    act(() => result.current.close());
    act(() => { vi.advanceTimersByTime(200); });
    act(() => result.current.open());
    expect(result.current.isOpen).toBe(true);
    expect(result.current.isEditing).toBe(false);
    expect(result.current.editingItem).toBeNull();
    vi.useRealTimers();
  });
});
