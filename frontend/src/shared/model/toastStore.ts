import { create } from 'zustand'

interface Toast {
  id: string
  message: string
  type: 'success' | 'error'
}

interface ToastStore {
  toasts: Toast[]
  showToast: (message: string, type?: 'success' | 'error') => void
  removeToast: (id: string) => void
}

let nextId = 0

export const useToastStore = create<ToastStore>((set, get) => ({
  toasts: [],
  showToast: (message, type = 'success') => {
    const id = `${Date.now()}-${++nextId}`
    set((state) => ({ toasts: [...state.toasts, { id, message, type }] }))
    window.setTimeout(() => {
      get().removeToast(id)
    }, 3000)
  },
  removeToast: (id) => {
    set((state) => ({ toasts: state.toasts.filter((t) => t.id !== id) }))
  },
}))

export function useToast() {
  const showToast = useToastStore((state) => state.showToast)
  return { showToast }
}
