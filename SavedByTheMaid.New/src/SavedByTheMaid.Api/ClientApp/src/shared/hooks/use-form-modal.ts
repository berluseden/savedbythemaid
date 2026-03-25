import { useState, useCallback } from 'react';

export function useFormModal<T extends { id: number | string } | null = null>() {
  const [isOpen, setIsOpen] = useState(false);
  const [editingItem, setEditingItem] = useState<T | null>(null);

  const open = useCallback((item?: T) => {
    setEditingItem(item ?? null);
    setIsOpen(true);
  }, []);

  const close = useCallback(() => {
    setIsOpen(false);
    // Small delay to allow close animation before clearing
    setTimeout(() => setEditingItem(null), 150);
  }, []);

  return {
    isOpen,
    isEditing: editingItem !== null,
    editingItem,
    open,
    close,
  };
}
