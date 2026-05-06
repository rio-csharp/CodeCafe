import type { MutableRefObject } from 'react'

const notesWorkspaceStorageKey = 'codecafe-notes-workspace'

export type NotesWorkspaceState = {
  activePath: string | null
  expandedDirectories: string[]
  scrollTopByPath: Record<string, number>
}

export function loadNotesWorkspaceState(): NotesWorkspaceState {
  if (typeof window === 'undefined') {
    return createDefaultNotesWorkspaceState()
  }

  try {
    const rawState = window.localStorage.getItem(notesWorkspaceStorageKey)

    if (!rawState) {
      return createDefaultNotesWorkspaceState()
    }

    const parsedState = JSON.parse(rawState) as Partial<NotesWorkspaceState>

    return {
      activePath: typeof parsedState.activePath === 'string' ? parsedState.activePath : null,
      expandedDirectories: Array.isArray(parsedState.expandedDirectories)
        ? parsedState.expandedDirectories.filter((entry): entry is string => typeof entry === 'string')
        : [],
      scrollTopByPath: isScrollTopMap(parsedState.scrollTopByPath) ? parsedState.scrollTopByPath : {},
    }
  } catch {
    return createDefaultNotesWorkspaceState()
  }
}

export function saveNotesWorkspaceState(state: NotesWorkspaceState) {
  if (typeof window === 'undefined') {
    return
  }

  window.localStorage.setItem(notesWorkspaceStorageKey, JSON.stringify(state))
}

export function rememberScrollPosition(
  path: string,
  scrollContainer: HTMLElement | null,
  scrollTopByPathRef: MutableRefObject<Record<string, number>>,
  workspaceState: NotesWorkspaceState,
) {
  if (!path || !scrollContainer) {
    return
  }

  scrollTopByPathRef.current = {
    ...scrollTopByPathRef.current,
    [path]: scrollContainer.scrollTop,
  }
  saveNotesWorkspaceState({
    ...workspaceState,
    scrollTopByPath: scrollTopByPathRef.current,
  })
}

export function getAncestorPaths(path: string) {
  const segments = path.split('/').filter(Boolean)

  return segments.slice(0, -1).map((_, index) => segments.slice(0, index + 1).join('/'))
}

export function mergeExpandedDirectories(currentDirectories: string[], nextDirectories: string[]) {
  return Array.from(new Set([...currentDirectories, ...nextDirectories]))
}

export function persistWorkspaceBeforeNavigation({
  activePath,
  currentPath,
  expandedDirectories,
  scrollContainer,
  scrollTopByPathRef,
  workspaceState,
}: {
  activePath: string
  currentPath: string
  expandedDirectories: string[]
  scrollContainer: HTMLElement | null
  scrollTopByPathRef: MutableRefObject<Record<string, number>>
  workspaceState: NotesWorkspaceState
}) {
  const nextScrollTopByPath =
    currentPath && scrollContainer
      ? {
          ...scrollTopByPathRef.current,
          [currentPath]: scrollContainer.scrollTop,
        }
      : scrollTopByPathRef.current

  scrollTopByPathRef.current = nextScrollTopByPath
  saveNotesWorkspaceState({
    ...workspaceState,
    activePath,
    expandedDirectories,
    scrollTopByPath: nextScrollTopByPath,
  })
}

function createDefaultNotesWorkspaceState(): NotesWorkspaceState {
  return {
    activePath: null,
    expandedDirectories: [],
    scrollTopByPath: {},
  }
}

function isScrollTopMap(value: unknown): value is Record<string, number> {
  return Boolean(value) && typeof value === 'object' && !Array.isArray(value)
}
