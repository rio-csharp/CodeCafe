import { create } from 'zustand'

interface Toast {
  id: string
  message: string
  type: 'success' | 'error'
}

interface ToastStore {
  toasts: Toast[]
  /** Ids currently playing the leave animation; removed after it finishes. */
  leavingIds: string[]
  showToast: (message: string, type?: 'success' | 'error') => void
  /** Marks a toast as leaving so it animates out before removal. */
  dismissToast: (id: string) => void
  removeToast: (id: string) => void
}

let nextId = 0
const timeouts = new Map<string, number>()

function clearAutoDismiss(id: string) {
  const handle = timeouts.get(id)
  if (handle) {
    window.clearTimeout(handle)
    timeouts.delete(id)
  }
}

export const useToastStore = create<ToastStore>((set, get) => ({
  toasts: [],
  leavingIds: [],
  showToast: (message, type = 'success') => {
    const id = `${Date.now()}-${++nextId}`
    set((state) => ({ toasts: [...state.toasts, { id, message, type }] }))
    const handle = window.setTimeout(() => {
      timeouts.delete(id)
      get().dismissToast(id)
    }, 3000)
    timeouts.set(id, handle)
  },
  dismissToast: (id) => {
    if (get().leavingIds.includes(id)) return
    clearAutoDismiss(id)
    set((state) => ({ leavingIds: [...state.leavingIds, id] }))
    window.setTimeout(() => {
      set((state) => ({
        toasts: state.toasts.filter((t) => t.id !== id),
        leavingIds: state.leavingIds.filter((leavingId) => leavingId !== id),
      }))
    }, 200)
  },
  removeToast: (id) => {
    clearAutoDismiss(id)
    set((state) => ({
      toasts: state.toasts.filter((t) => t.id !== id),
      leavingIds: state.leavingIds.filter((leavingId) => leavingId !== id),
    }))
  },
}))

export function useToast() {
  const showToast = useToastStore((state) => state.showToast)
  return { showToast }
}
