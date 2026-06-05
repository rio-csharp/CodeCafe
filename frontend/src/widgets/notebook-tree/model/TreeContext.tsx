import { createContext, useContext } from 'react'

export interface TreeDragState {
  draggingId: string | null
  onDragStart: (id: string) => void
  onDragEnd: () => void
  onDropOnRoot: () => void
  /**
   * The single drop semantic. Position is computed from the cursor's Y
   * position in the item (top half = 'before', bottom half = 'after').
   * 'inside' is only used for the empty-folder case (the dropped item
   * becomes the folder's only child).
   */
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
