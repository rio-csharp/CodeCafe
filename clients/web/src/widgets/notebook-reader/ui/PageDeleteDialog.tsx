import { useTranslation } from 'react-i18next'
import type { PageTreeNode } from '@/entities/notebook'
import { Button, DialogShell } from '@/shared/ui'

export interface PageDeleteDialogProps {
  node: PageTreeNode
  busy?: boolean
  onCancel: () => void
  onConfirm: () => void
}

/**
 * Page deletion is a soft delete, but it still asks first: the page takes its
 * whole subtree to the trash with it.
 */
export function PageDeleteDialog({ node, busy = false, onCancel, onConfirm }: PageDeleteDialogProps) {
  const { t } = useTranslation()

  return (
    <DialogShell title={t('reader.deletePageTitle')} onClose={onCancel}>
      <p className="text-sm text-ink">{t('reader.deletePageBody', { title: node.title })}</p>
      <div className="mt-5 flex justify-end gap-2">
        <Button variant="ghost" size="sm" onClick={onCancel}>
          {t('editor.cancel')}
        </Button>
        <button
          type="button"
          disabled={busy}
          onClick={onConfirm}
          className="inline-flex items-center justify-center rounded-full bg-danger px-3.5 py-1.5 text-xs font-medium text-card transition-colors hover:opacity-90 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent disabled:cursor-not-allowed disabled:opacity-60"
        >
          {t('reader.deletePageConfirm')}
        </button>
      </div>
    </DialogShell>
  )
}
