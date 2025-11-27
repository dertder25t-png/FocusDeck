import React, { useState, useCallback, ReactNode, useEffect } from 'react';
import { Toast, ToastTitle, ToastDescription } from '../components/Toast';
import { ToastContext, ToastMessage } from './ToastContext';

let toastCount = 0;

export const ToastProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [toasts, setToasts] = useState<ToastMessage[]>([]);

  const addToast = useCallback((toast: Omit<ToastMessage, 'id'>) => {
    const id = (toastCount++).toString();
    setToasts((prevToasts) => [...prevToasts, { ...toast, id, duration: toast.duration || 5000 }]);
  }, []);

  const removeToast = useCallback((id: string) => {
    setToasts((prevToasts) => prevToasts.filter((toast) => toast.id !== id));
  }, []);

  useEffect(() => {
    const timers = toasts.map((toast) => {
      if (toast.duration) {
        return setTimeout(() => {
          removeToast(toast.id);
        }, toast.duration);
      }
      return null;
    });

    return () => {
      timers.forEach((timer) => {
        if (timer) {
          clearTimeout(timer);
        }
      });
    };
  }, [toasts, removeToast]);

  return (
    <ToastContext.Provider value={{ addToast, removeToast }}>
      {children}
      {toasts.map((toast) => (
        <Toast key={toast.id} variant={toast.variant}>
          <div className="flex-1">
            <ToastTitle>{toast.title}</ToastTitle>
            {toast.description && <ToastDescription>{toast.description}</ToastDescription>}
          </div>
          <button onClick={() => removeToast(toast.id)} className="text-white">X</button>
        </Toast>
      ))}
    </ToastContext.Provider>
  );
};
