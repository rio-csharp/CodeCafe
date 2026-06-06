import { Pencil, Archive, ArchiveRestore, Trash2 } from 'lucide-react'
import { useTranslation } from 'react-i18next'

interface TreeNodeActionsProps {
  canEdit: boolean
  isEditing: boolean
  isArchived?: boolean
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
  const { t } = useTranslation()

  if (!canEdit || isEditing) return null

  return (
    <div className="hidden group-hover:flex items-center gap-0.5 shrink-0 ml-1">
      {!isArchived && (
        <button
          type="button"
          onClick={onRename}
          className="p-0.5 text-text-tertiary hover:text-brand-brown rounded transition-colors"
          title={t('notebook.rename')}
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
            title={t('notebook.restore')}
          >
            <ArchiveRestore className="h-3 w-3" />
          </button>
          <button
            type="button"
            onClick={onDelete}
            className="p-0.5 text-text-tertiary hover:text-status-error rounded transition-colors"
            title={t('notebook.deletePermanently')}
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
            title={t('notebook.archive')}
          >
            <Archive className="h-3 w-3" />
          </button>
          <button
            type="button"
            onClick={onDelete}
            className="p-0.5 text-text-tertiary hover:text-status-error rounded transition-colors"
            title={t('notebook.deleteAfterArchiveTitle')}
          >
            <Trash2 className="h-3 w-3" />
          </button>
        </>
      )}
    </div>
  )
}
