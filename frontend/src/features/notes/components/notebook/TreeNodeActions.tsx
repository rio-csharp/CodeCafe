import { ArrowUp, ArrowDown, Pencil, Trash2 } from 'lucide-react'

interface TreeNodeActionsProps {
  canEdit: boolean
  isEditing: boolean
  siblingCount: number
  index: number
  onMoveUp?: (e: React.MouseEvent) => void
  onMoveDown?: (e: React.MouseEvent) => void
  onRename: (e: React.MouseEvent) => void
  onDelete: (e: React.MouseEvent) => void
}

export default function TreeNodeActions({
  canEdit,
  isEditing,
  siblingCount,
  index,
  onMoveUp,
  onMoveDown,
  onRename,
  onDelete,
}: TreeNodeActionsProps) {
  if (!canEdit || isEditing) return null

  return (
    <div className="hidden group-hover:flex items-center gap-0.5 shrink-0 ml-1">
      {siblingCount > 1 && (
        <>
          <button
            type="button"
            onClick={onMoveUp}
            disabled={index === 0}
            className="p-0.5 text-gray-400 hover:text-brand-brown rounded transition-colors disabled:opacity-30"
            title="Move up"
          >
            <ArrowUp className="h-3 w-3" />
          </button>
          <button
            type="button"
            onClick={onMoveDown}
            disabled={index === siblingCount - 1}
            className="p-0.5 text-gray-400 hover:text-brand-brown rounded transition-colors disabled:opacity-30"
            title="Move down"
          >
            <ArrowDown className="h-3 w-3" />
          </button>
        </>
      )}
      <button
        type="button"
        onClick={onRename}
        className="p-0.5 text-gray-400 hover:text-brand-brown rounded transition-colors"
        title="Rename"
      >
        <Pencil className="h-3 w-3" />
      </button>
      <button
        type="button"
        onClick={onDelete}
        className="p-0.5 text-gray-400 hover:text-red-600 rounded transition-colors"
        title="Delete"
      >
        <Trash2 className="h-3 w-3" />
      </button>
    </div>
  )
}
