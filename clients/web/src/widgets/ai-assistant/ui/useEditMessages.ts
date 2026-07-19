import { useCallback, useMemo, useSyncExternalStore } from 'react'
import type { Notebook } from '@/entities/notebook'
import type { NotebookItem } from '@/entities/notebook-item'
import {
  clearEditThread,
  loadEditThread,
  saveEditThread,
  type EditMessage,
} from '@/features/ai-assistant'

interface UseEditMessagesOptions {
  notebook: Notebook
  activePage: NotebookItem | null
}

function getServerSnapshot(): EditMessage[] {
  return []
}

function createThreadStore(threadKey: string) {
  let messages: EditMessage[] = []
  const listeners = new Set<() => void>()

  function load() {
    const persisted = loadEditThread(threadKey)
    messages = persisted?.messages ?? []
    listeners.forEach((notify) => notify())
  }

  function getSnapshot() {
    return messages
  }

  function subscribe(listener: () => void) {
    listeners.add(listener)
    return () => {
      listeners.delete(listener)
    }
  }

  function set(nextMessages: EditMessage[] | ((current: EditMessage[]) => EditMessage[])) {
    messages = typeof nextMessages === 'function' ? nextMessages(messages) : nextMessages
    saveEditThread(threadKey, messages)
    listeners.forEach((notify) => notify())
  }

  function clear() {
    messages = []
    clearEditThread(threadKey)
    listeners.forEach((notify) => notify())
  }

  load()

  return { getSnapshot, subscribe, set, clear, load }
}

const stores = new Map<string, ReturnType<typeof createThreadStore>>()

// Cap resident stores; eviction is safe because every mutation is already
// persisted to localStorage, so a recreated store reloads from disk.
const MAX_RESIDENT_STORES = 20

function getStore(threadKey: string) {
  let store = stores.get(threadKey)
  if (!store) {
    store = createThreadStore(threadKey)
    stores.set(threadKey, store)
    if (stores.size > MAX_RESIDENT_STORES) {
      // Map iteration order is insertion order — evict the oldest entry
      // that isn't the one just accessed.
      for (const key of stores.keys()) {
        if (key !== threadKey) {
          stores.delete(key)
          break
        }
      }
    }
  }
  return store
}

export function useEditMessages({ notebook, activePage }: UseEditMessagesOptions) {
  const threadKey = useMemo(
    () => `codecafe:${notebook.slug}:${activePage?.path ?? 'notebook'}`,
    [activePage?.path, notebook.slug],
  )

  const store = useMemo(() => getStore(threadKey), [threadKey])

  const editMessages = useSyncExternalStore(
    store.subscribe,
    store.getSnapshot,
    getServerSnapshot,
  )

  const setEditMessages = useCallback(
    (next: EditMessage[] | ((current: EditMessage[]) => EditMessage[])) => {
      store.set(next)
    },
    [store],
  )

  const clearEditMessages = useCallback(() => {
    store.clear()
  }, [store])

  return {
    editMessages,
    setEditMessages,
    clearEditMessages,
  }
}
