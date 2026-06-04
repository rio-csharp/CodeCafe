import { Pencil, Archive, ArchiveRestore, Trash2 } from 'lucide-react'

interface TreeNodeActionsProps {
  canEdit: boolean
  isEditing: boolean
  siblingCount: number
  index: number
  isArchived?: boolean
  onMoveUp?: (e: React.MouseEvent) => void
  onMoveDown?: (e: React.MouseEvent) => void
  onRename: (e: React.MouseEvent) => void
  onArchive?: (e: React.MouseEvent) => void
  onRestore?: (e: React.MouseEvent) => void
  onDelete: (e: React.MouseEvent) => void
}

export default function TreeNodeActions({
  canEdit,
  isEditing,
  isArchived = false,
  onRename,
  onArchive,
  onRestore,
  onDelete,
}: TreeNodeActionsProps) {
  if (!canEdit || isEditing) return null

  return (
    <div className="hidden group-hover:flex items-center gap-0.5 shrink-0 ml-1">
      {!isArchived && (
        <button
          type="button"
          onClick={onRename}
          className="p-0.5 text-text-tertiary hover:text-brand-brown rounded transition-colors"
          title="Rename"
        >
          <Pencil className="h-3 w-3" />
        </button>
      )}
      {isArchived ? (
        <>
          <button
            type="button"
            onClick={onRestore}
            className="p-0.5 text-text-tertiary hover:text-brand-brown rounded transition-colors"
            title="Restore"
          >
            <ArchiveRestore className="h-3 w-3" />
          </button>
          <button
            type="button"
            onClick={onDelete}
            className="p-0.5 text-text-tertiary hover:text-status-error rounded transition-colors"
            title="Delete permanently"
          >
            <Trash2 className="h-3 w-3" />
          </button>
        </>
      ) : (
        <>
          <button
            type="button"
            onClick={onArchive}
            className="p-0.5 text-text-tertiary hover:text-status-favorite rounded transition-colors"
            title="Archive"
          >
            <Archive className="h-3 w-3" />
          </button>
          <button
            type="button"
            onClick={onDelete}
            className="p-0.5 text-text-tertiary hover:text-status-error rounded transition-colors"
            title="Delete"
          >
            <Trash2 className="h-3 w-3" />
          </button>
        </>
      )}
    </div>
  )
}
