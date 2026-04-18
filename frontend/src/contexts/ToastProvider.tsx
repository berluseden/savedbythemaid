import React, { useEffect, useState } from 'react';
import { onToast, type ToastMessage } from '@/lib/toast';

export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [toasts, setToasts] = useState<ToastMessage[]>([]);

  useEffect(() => {
    const unsub = onToast((t) => {
      setToasts((s) => [...s, t]);
      // auto remove after 5s
      setTimeout(() => setToasts((s) => s.filter((x) => x.id !== t.id)), 5000);
    });
    return unsub;
  }, []);

  return (
    <>
      {children}
      <div className="fixed right-4 bottom-4 z-50 flex flex-col gap-3">
        {toasts.map((t) => (
          <div key={t.id} className={`max-w-sm w-full rounded-lg p-3 shadow-md border ${
            t.variant === 'error' ? 'bg-red-50 border-red-200 text-red-800' :
            t.variant === 'success' ? 'bg-green-50 border-green-200 text-green-800' :
            t.variant === 'warning' ? 'bg-yellow-50 border-yellow-200 text-yellow-800' :
            'bg-blue-50 border-blue-200 text-blue-800'
          }`} role="status">
            <div className="text-sm">{t.message}</div>
          </div>
        ))}
      </div>
    </>
  );
}
