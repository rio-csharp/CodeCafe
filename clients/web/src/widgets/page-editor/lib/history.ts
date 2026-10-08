import type { EditorBlock } from './draft'

/** One undo step: the draft as it was BEFORE a change. */
export interface HistoryEntry {
  draft: EditorBlock[]
  at: number
  /**
   * Coalescing key. Consecutive changes with the same key inside COALESCE_MS
   * collapse into one undo step (a typing run in one block); structural
   * changes pass null and always start a step of their own.
   */
  run: string | null
}

export interface DraftHistory {
  /** Pre-change snapshots, newest last. */
  past: HistoryEntry[]
  /** Undone drafts, newest last; any new change clears the stack. */
  future: EditorBlock[][]
}

export const HISTORY_LIMIT = 100
export const COALESCE_MS = 800

export function emptyHistory(): DraftHistory {
  return { past: [], future: [] }
}

// Block payloads are allowed to mutate in place, so a snapshot that shared
// references would let later edits rewrite history; clone on the way in AND
// on the way out (a restored draft can land in the opposite stack again).
const clone = (draft: EditorBlock[]): EditorBlock[] => structuredClone(draft)

/** Records `current` as the pre-change snapshot of the change about to happen. */
export function recordChange(
  history: DraftHistory,
  current: EditorBlock[],
  run: string | null,
  now: number = Date.now(),
): DraftHistory {
  const last = history.past[history.past.length - 1]
  if (last !== undefined && run !== null && last.run === run && now - last.at < COALESCE_MS) {
    // Still the same run: the older snapshot already holds the pre-run state.
    return {
      past: [...history.past.slice(0, -1), { ...last, at: now }],
      future: [],
    }
  }
  const past = [...history.past, { draft: clone(current), at: now, run }]
  return {
    past: past.length > HISTORY_LIMIT ? past.slice(past.length - HISTORY_LIMIT) : past,
    future: [],
  }
}

/** Pops the latest step; the caller swaps its draft for the returned one. */
export function applyUndo(
  history: DraftHistory,
  current: EditorBlock[],
): { history: DraftHistory; draft: EditorBlock[] } | null {
  const entry = history.past[history.past.length - 1]
  if (entry === undefined) {
    return null
  }
  return {
    history: {
      past: history.past.slice(0, -1),
      future: [...history.future, clone(current)],
    },
    draft: clone(entry.draft),
  }
}

/** Pops the latest undone draft; null when there is nothing to redo. */
export function applyRedo(
  history: DraftHistory,
  current: EditorBlock[],
): { history: DraftHistory; draft: EditorBlock[] } | null {
  const draft = history.future[history.future.length - 1]
  if (draft === undefined) {
    return null
  }
  return {
    history: {
      // The pre-redo state becomes an undo step. The epoch timestamp keeps a
      // following typing run from coalescing into it.
      past: [...history.past, { draft: clone(current), at: Number.NEGATIVE_INFINITY, run: null }],
      future: history.future.slice(0, -1),
    },
    draft: clone(draft),
  }
}
