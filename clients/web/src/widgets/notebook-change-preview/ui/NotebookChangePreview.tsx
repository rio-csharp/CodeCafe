import { Trash2 } from 'lucide-react'
import { useEffect, useId, useMemo, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import type { AiEditOperation } from '@/features/ai-assistant'
import { diffTextByLine } from '@/shared/lib/textDiff'
import TipTapViewer from '@/shared/ui/TipTapViewer'
import { ChangePreviewHeader } from './ChangePreviewHeader'
import { DiffSegment } from './DiffSegment'

interface NotebookChangePreviewProps {
  afterContentJson?: Record<string, unknown> | null
  afterText?: string | null
  beforeContentJson?: Record<string, unknown> | null
  beforeText?: string | null
  canSave?: boolean
  disableEdit?: boolean
  isSaving?: boolean
  operation?: AiEditOperation
  summary?: string | null
  onCancel: () => void
  onDiscard?: () => void
  onEdit: () => void
  onSave: () => void
  title: string
}

const FOCUSABLE_SELECTOR =
  'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'

export function NotebookChangePreview({
  afterContentJson,
  afterText,
  beforeContentJson,
  beforeText,
  canSave,
  disableEdit = false,
  isSaving = false,
  operation,
  summary,
  onCancel,
  onDiscard,
  onEdit,
  onSave,
  title,
}: NotebookChangePreviewProps) {
  const { t } = useTranslation()
  const titleId = useId()
  const containerRef = useRef<HTMLDivElement>(null)
  const diff = useMemo(
    () => diffTextByLine(beforeText ?? '', afterText ?? ''),
    [afterText, beforeText],
  )

  useEffect(() => {
    containerRef.current?.focus()

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && !isSaving) {
        onCancel()
        return
      }

      // aria-modal dialog: keep Tab cycling inside the container so focus
      // cannot escape to the background layer.
      if (event.key !== 'Tab') return
      const container = containerRef.current
      if (!container) return
      const focusable = Array.from(container.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR))
      if (focusable.length === 0) {
        event.preventDefault()
        return
      }
      const first = focusable[0]
      const last = focusable[focusable.length - 1]
      const active = document.activeElement as HTMLElement | null
      if (event.shiftKey && (active === first || !container.contains(active))) {
        event.preventDefault()
        last.focus()
      } else if (!event.shiftKey && (active === last || !container.contains(active))) {
        event.preventDefault()
        first.focus()
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [isSaving, onCancel])

  const isDelete = operation === 'delete_page'
  const hasChanges = diff.summary.added > 0 || diff.summary.removed > 0
  const saveEnabled = canSave ?? (isDelete || hasChanges)
  const hasJson = Boolean(afterContentJson) && !isDelete

  return (
    <div
      ref={containerRef}
      role="dialog"
      aria-modal="true"
      aria-labelledby={titleId}
      tabIndex={-1}
      className="fixed inset-0 z-50 bg-surface outline-none"
    >
      <div className="flex h-full flex-col">
        <ChangePreviewHeader
          titleId={titleId}
          title={title}
          summary={summary}
          isDelete={isDelete}
          isSaving={isSaving}
          disableEdit={disableEdit}
          saveEnabled={saveEnabled}
          onCancel={onCancel}
          onDiscard={onDiscard}
          onEdit={onEdit}
          onSave={onSave}
        />

        <main className="min-h-0 flex-1 overflow-y-auto px-4 py-4 sm:px-6 lg:px-8">
          <div>
            <div className="mb-3 flex flex-wrap items-center gap-2 text-xs">
              <span className="rounded-md bg-status-success-bg px-2 py-1 font-medium text-status-success">
                +{diff.summary.added} {t('notebook.changePreview.added')}
              </span>
              <span className="rounded-md bg-status-error-bg px-2 py-1 font-medium text-status-error">
                -{diff.summary.removed} {t('notebook.changePreview.removed')}
              </span>
              {!hasChanges && (
                <span className="text-text-tertiary">{t('notebook.changePreview.noChanges')}</span>
              )}
            </div>

            {isDelete ? (
              <div className="rounded-lg border border-status-error-border bg-status-error-bg px-4 py-8 text-center">
                <Trash2 className="mx-auto h-8 w-8 text-status-error" />
                <p className="mt-3 text-sm font-medium text-text-primary">
                  {t('notebook.changePreview.deleteWarning', { title })}
                </p>
                {summary && <p className="mt-1 text-xs text-text-secondary">{summary}</p>}
              </div>
            ) : hasJson ? (
              <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
                <div className="overflow-hidden rounded-lg border border-border-default bg-surface-elevated p-4">
                  <p className="mb-2 text-xs font-medium uppercase tracking-wide text-text-tertiary">
                    {t('notebook.changePreview.before')}
                  </p>
                  {beforeContentJson ? (
                    <TipTapViewer content={beforeContentJson} />
                  ) : (
                    <p className="text-sm text-text-tertiary">{t('common.pageEmpty')}</p>
                  )}
                </div>
                <div className="overflow-hidden rounded-lg border border-border-default bg-surface-elevated p-4">
                  <p className="mb-2 text-xs font-medium uppercase tracking-wide text-text-tertiary">
                    {t('notebook.changePreview.after')}
                  </p>
                  {afterContentJson && <TipTapViewer content={afterContentJson} />}
                </div>
              </div>
            ) : (
              <div className="overflow-hidden rounded-lg border border-border-default bg-surface-elevated">
                {diff.segments.length > 0 ? (
                  <div className="divide-y divide-border-subtle font-mono text-xs leading-5">
                    {diff.segments.map((segment, index) => (
                      <DiffSegment key={index} segment={segment} />
                    ))}
                  </div>
                ) : (
                  <p className="px-4 py-8 text-center text-sm text-text-tertiary">
                    {t('notebook.changePreview.empty')}
                  </p>
                )}
              </div>
            )}
          </div>
        </main>
      </div>
    </div>
  )
}
