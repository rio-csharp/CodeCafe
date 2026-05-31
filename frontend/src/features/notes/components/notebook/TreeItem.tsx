import type { TreeNode } from '../../utils/buildTree'
import TreeFolderNode from './TreeFolderNode'
import TreePageNode from './TreePageNode'

interface TreeItemProps {
  node: TreeNode
  notebookSlug: string
  activePath: string | null
  level: number
  canEdit: boolean
  onMoveUp?: (itemId: string) => void
  onMoveDown?: (itemId: string) => void
  siblingCount: number
  index: number
  dragState?: {
    draggingId: string | null
    onDragStart: (id: string) => void
    onDragEnd: () => void
    onDropOnFolder: (folderId: string) => void
    onDropOnRoot: () => void
  }
  onCreateItem: (parentId: string | null, type: 'folder' | 'page') => Promise<void>
  onRenameItem: (itemId: string, title: string, sortOrder: number) => Promise<void>
  onArchiveItem: (itemId: string) => Promise<void>
  onRestoreItem: (itemId: string) => Promise<void>
  onDeleteItem: (itemId: string) => Promise<void>
}

export default function TreeItem({
  node,
  notebookSlug,
  activePath,
  level,
  canEdit,
  onMoveUp,
  onMoveDown,
  siblingCount,
  index,
  dragState,
  onCreateItem,
  onRenameItem,
  onArchiveItem,
  onRestoreItem,
  onDeleteItem,
}: TreeItemProps) {
  const isFolder = node.item.type === 'folder'

  if (isFolder) {
    return (
      <TreeFolderNode
        node={node}
        level={level}
        canEdit={canEdit}
        onMoveUp={onMoveUp}
        onMoveDown={onMoveDown}
        siblingCount={siblingCount}
        index={index}
        dragState={dragState}
        onCreateItem={onCreateItem}
        onRenameItem={onRenameItem}
        onArchiveItem={onArchiveItem}
        onRestoreItem={onRestoreItem}
        onDeleteItem={onDeleteItem}
      >
        {node.children.map((child, childIndex) => (
          <TreeItem
            key={child.item.id}
            node={child}
            notebookSlug={notebookSlug}
            activePath={activePath}
            level={level + 1}
            canEdit={canEdit}
            onMoveUp={onMoveUp}
            onMoveDown={onMoveDown}
            siblingCount={node.children.length}
            index={childIndex}
            dragState={dragState}
            onCreateItem={onCreateItem}
            onRenameItem={onRenameItem}
            onArchiveItem={onArchiveItem}
            onRestoreItem={onRestoreItem}
            onDeleteItem={onDeleteItem}
          />
        ))}
      </TreeFolderNode>
    )
  }

  return (
    <TreePageNode
      node={node}
      notebookSlug={notebookSlug}
      activePath={activePath}
      level={level}
      canEdit={canEdit}
      onMoveUp={onMoveUp}
      onMoveDown={onMoveDown}
      siblingCount={siblingCount}
      index={index}
      dragState={dragState}
      onRenameItem={onRenameItem}
      onArchiveItem={onArchiveItem}
      onRestoreItem={onRestoreItem}
      onDeleteItem={onDeleteItem}
    />
  )
}
