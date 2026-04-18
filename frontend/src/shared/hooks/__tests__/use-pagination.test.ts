import { describe, it, expect } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { usePagination } from '../use-pagination';

describe('usePagination', () => {
  const items = Array.from({ length: 50 }, (_, i) => i + 1);

  it('returns first page of items with default page size', () => {
    const { result } = renderHook(() => usePagination(items));
    expect(result.current.items).toHaveLength(20);
    expect(result.current.items[0]).toBe(1);
    expect(result.current.items[19]).toBe(20);
  });

  it('calculates total pages correctly', () => {
    const { result } = renderHook(() => usePagination(items));
    expect(result.current.totalPages).toBe(3); // 50 / 20 = 2.5 -> 3
  });

  it('respects custom page size', () => {
    const { result } = renderHook(() => usePagination(items, { pageSize: 10 }));
    expect(result.current.items).toHaveLength(10);
    expect(result.current.totalPages).toBe(5);
    expect(result.current.pageSize).toBe(10);
  });

  it('navigates to next page', () => {
    const { result } = renderHook(() => usePagination(items, { pageSize: 10 }));
    act(() => result.current.nextPage());
    expect(result.current.currentPage).toBe(2);
    expect(result.current.items[0]).toBe(11);
  });

  it('navigates to previous page', () => {
    const { result } = renderHook(() => usePagination(items, { pageSize: 10 }));
    act(() => result.current.goToPage(3));
    act(() => result.current.prevPage());
    expect(result.current.currentPage).toBe(2);
  });

  it('does not go below page 1', () => {
    const { result } = renderHook(() => usePagination(items));
    act(() => result.current.prevPage());
    expect(result.current.currentPage).toBe(1);
  });

  it('does not go above total pages', () => {
    const { result } = renderHook(() => usePagination(items, { pageSize: 10 }));
    act(() => result.current.goToPage(100));
    expect(result.current.currentPage).toBe(5);
  });

  it('reports hasNextPage and hasPrevPage correctly', () => {
    const { result } = renderHook(() => usePagination(items, { pageSize: 10 }));
    expect(result.current.hasPrevPage).toBe(false);
    expect(result.current.hasNextPage).toBe(true);
    act(() => result.current.goToPage(5));
    expect(result.current.hasPrevPage).toBe(true);
    expect(result.current.hasNextPage).toBe(false);
  });

  it('handles last page with fewer items', () => {
    const { result } = renderHook(() => usePagination(items)); // 50 items, 20/page
    act(() => result.current.goToPage(3));
    expect(result.current.items).toHaveLength(10); // 50 - 40 = 10
  });

  it('handles empty array', () => {
    const { result } = renderHook(() => usePagination([]));
    expect(result.current.items).toHaveLength(0);
    expect(result.current.totalPages).toBe(0);
    expect(result.current.totalItems).toBe(0);
  });

  it('resets page to 1', () => {
    const { result } = renderHook(() => usePagination(items, { pageSize: 10 }));
    act(() => result.current.goToPage(3));
    act(() => result.current.resetPage());
    expect(result.current.currentPage).toBe(1);
  });

  it('reports totalItems correctly', () => {
    const { result } = renderHook(() => usePagination(items));
    expect(result.current.totalItems).toBe(50);
  });
});
