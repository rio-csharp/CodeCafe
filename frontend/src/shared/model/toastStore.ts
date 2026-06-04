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
const timeouts = new Map<string, number>()

export const useToastStore = create<ToastStore>((set, get) => ({
  toasts: [],
  showToast: (message, type = 'success') => {
    const id = `${Date.now()}-${++nextId}`
    set((state) => ({ toasts: [...state.toasts, { id, message, type }] }))
    const handle = window.setTimeout(() => {
      timeouts.delete(id)
      get().removeToast(id)
    }, 3000)
    timeouts.set(id, handle)
  },
  removeToast: (id) => {
    const handle = timeouts.get(id)
    if (handle) {
      window.clearTimeout(handle)
      timeouts.delete(id)
    }
    set((state) => ({ toasts: state.toasts.filter((t) => t.id !== id) }))
  },
}))

export function useToast() {
  const showToast = useToastStore((state) => state.showToast)
  return { showToast }
}
