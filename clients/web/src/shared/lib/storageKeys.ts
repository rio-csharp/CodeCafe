/**
 * LocalStorage key prefixes for feature-owned persisted data.
 * Kept in shared so cross-layer flows (e.g. logout cleanup) can reference
 * them without importing another feature (FSD layering rule).
 */
export const AI_THREAD_STORAGE_PREFIX = 'codecafe:ai-thread:'
export const AI_EDIT_THREAD_STORAGE_PREFIX = 'codecafe:ai-edit-thread:'

/** Removes every localStorage entry whose key starts with the given prefix. */
export function clearLocalStorageByPrefix(prefix: string): void {
  if (typeof localStorage === 'undefined') return

  try {
    const keys: string[] = []
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i)
      if (key?.startsWith(prefix)) keys.push(key)
    }
    keys.forEach((key) => localStorage.removeItem(key))
  } catch {
    // Ignore storage errors (private mode, etc.)
  }
}
