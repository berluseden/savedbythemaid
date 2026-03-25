import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api from '@/lib/api';

interface UseCRUDOptions<T> {
  endpoint: string;
  queryKey: string[];
  enabled?: boolean;
  transform?: (data: T[]) => T[];
}

export function useCRUD<T extends { id: number }>({
  endpoint,
  queryKey,
  enabled = true,
  transform,
}: UseCRUDOptions<T>) {
  const queryClient = useQueryClient();
  const invalidate = () => queryClient.invalidateQueries({ queryKey });

  const list = useQuery({
    queryKey,
    queryFn: async () => {
      const { data } = await api.get<T[]>(endpoint);
      return transform ? transform(data) : data;
    },
    enabled,
  });

  const create = useMutation({
    mutationFn: (item: Partial<T>) => api.post(endpoint, item),
    onSuccess: invalidate,
  });

  const update = useMutation({
    mutationFn: ({ id, ...data }: T) => api.put(`${endpoint}/${id}`, data),
    onSuccess: invalidate,
  });

  const remove = useMutation({
    mutationFn: (id: number) => api.delete(`${endpoint}/${id}`),
    onSuccess: invalidate,
  });

  return {
    data: list.data ?? [],
    isLoading: list.isLoading,
    isError: list.isError,
    error: list.error,
    refetch: list.refetch,
    create,
    update,
    remove,
  };
}
