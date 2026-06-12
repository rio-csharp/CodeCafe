import { Check, Pencil, Trash2, X } from 'lucide-react'
import { useEffect, useId, useMemo, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import type { AiEditOperation } from '@/features/ai-assistant'
import { diffTextByLine, type TextDiffSegment } from '@/shared/lib/textDiff'
import TipTapViewer from '@/shared/ui/TipTapViewer'

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
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [isSaving, onCancel])

  const isDelete = operation === 'delete_page'
  const hasChanges = diff.summary.added > 0 || diff.summary.removed > 0
  const saveEnabled = canSave ?? isDelete ?? hasChanges
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
        <header className="border-b border-border-subtle bg-surface px-4 py-3 sm:px-6 lg:px-8">
          <div className="mx-auto flex max-w-5xl flex-wrap items-center justify-between gap-3">
            <div className="min-w-0">
              <p className="text-xs font-medium uppercase tracking-wide text-text-tertiary">
                {t('notebook.changePreview.label')}
              </p>
              <h2 id={titleId} className="truncate text-lg font-semibold text-text-primary">{title}</h2>
              {summary && <p className="mt-0.5 text-xs text-text-secondary">{summary}</p>}
            </div>
            <div className="flex shrink-0 items-center gap-2">
              {!isDelete && (
                <button
                  type="button"
                  onClick={onEdit}
                  disabled={isSaving || disableEdit}
                  className="inline-flex items-center gap-1.5 rounded-lg border border-border-default px-3 py-1.5 text-xs font-medium text-text-secondary transition-colors hover:bg-surface-hover disabled:cursor-not-allowed disabled:opacity-50"
                >
                  <Pencil className="h-3.5 w-3.5" />
                  {t('notebook.changePreview.edit')}
                </button>
              )}
              {onDiscard && !isDelete && (
                <button
                  type="button"
                  onClick={onDiscard}
                  disabled={isSaving}
                  className="inline-flex items-center gap-1.5 rounded-lg border border-status-error-border px-3 py-1.5 text-xs font-medium text-status-error transition-colors hover:bg-status-error-bg disabled:cursor-not-allowed disabled:opacity-50"
                >
                  <Trash2 className="h-3.5 w-3.5" />
                  {t('notebook.changePreview.discard')}
                </button>
              )}
              <button
                type="button"
                onClick={onCancel}
                disabled={isSaving}
                className="inline-flex items-center gap-1.5 rounded-lg border border-border-default px-3 py-1.5 text-xs font-medium text-text-secondary transition-colors hover:bg-surface-hover disabled:cursor-not-allowed disabled:opacity-50"
              >
                <X className="h-3.5 w-3.5" />
                {t('notebook.cancel')}
              </button>
              <button
                type="button"
                onClick={onSave}
                disabled={isSaving || !saveEnabled}
                className="inline-flex items-center gap-1.5 rounded-lg bg-brand-brown px-3 py-1.5 text-xs font-medium text-text-inverse transition-opacity hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
              >
                <Check className="h-3.5 w-3.5" />
                {isSaving
                  ? t('notebook.saving')
                  : isDelete
                    ? t('notebook.changePreview.delete')
                    : t('notebook.changePreview.save')}
              </button>
            </div>
          </div>
        </header>

        <main className="min-h-0 flex-1 overflow-y-auto px-4 py-4 sm:px-6 lg:px-8">
          <div className="mx-auto max-w-5xl">
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

function DiffSegment({ segment }: { segment: TextDiffSegment }) {
  const className = segment.type === 'added'
    ? 'bg-status-success-bg text-text-primary'
    : segment.type === 'removed'
      ? 'bg-status-error-bg text-text-primary'
      : 'bg-surface text-text-secondary'
  const prefix = segment.type === 'added' ? '+' : segment.type === 'removed' ? '-' : ' '

  return (
    <div className={className}>
      {segment.lines.map((line, index) => (
        <div key={index} className="grid grid-cols-[2rem_1fr] gap-2 px-3 py-0.5">
          <span className="select-none text-right text-text-tertiary">{prefix}</span>
          <span className="whitespace-pre-wrap break-words">{line || ' '}</span>
        </div>
      ))}
    </div>
  )
}
