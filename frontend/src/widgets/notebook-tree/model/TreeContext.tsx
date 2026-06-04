import { createContext, useContext } from 'react'

export interface TreeDragState {
  draggingId: string | null
  onDragStart: (id: string) => void
  onDragEnd: () => void
  onDropOnFolder: (folderId: string) => void
  onDropOnRoot: () => void
  onDropReorder: (targetId: string, position: 'before' | 'after' | 'inside') => void
}

export interface TreeContextValue {
  notebookSlug: string
  activePath: string | null
  canEdit: boolean
  dragState?: TreeDragState
  onMoveUp?: (itemId: string) => void
  onMoveDown?: (itemId: string) => void
  onCreateItem: (parentId: string | null, type: 'folder' | 'page') => Promise<void>
  onRenameItem: (itemId: string, title: string, sortOrder: number) => Promise<void>
  onArchiveItem: (itemId: string) => Promise<void>
  onRestoreItem: (itemId: string) => Promise<void>
  onDeleteItem: (itemId: string) => Promise<void>
}

export const TreeContext = createContext<TreeContextValue | undefined>(undefined)

export function useTreeContext() {
  const ctx = useContext(TreeContext)
  if (!ctx) {
    throw new Error('useTreeContext must be used within a TreeContext.Provider')
  }
  return ctx
}
