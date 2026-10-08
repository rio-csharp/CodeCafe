import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { assembleBlockTree, BlockList } from '@/entities/block'
import type { BlockDto } from '@/entities/block'
import { formatRelativeTime } from '@/entities/notebook'
import { getPageAtRevision, pageKeys } from '@/entities/page'
import { DialogShell } from '@/shared/ui'
import { diffPageAtRevision } from '../lib/revisionDiff'
import type { RevisionDiffEntry } from '../lib/revisionDiff'
import { useRestoreRevision } from '../lib/useRestoreRevision'
import { RevisionRestoreButton } from './RevisionRestoreButton'

export interface RevisionPreviewDialogProps {
  pageId: string
  /** The batch whose END state is being previewed. */
  atUtc: string
  /** The page's live blocks; the diff compares the snapshot against these. */
  currentBlocks: readonly BlockDto[]
  canWrite: boolean
  onRestored: () => void
  onClose: () => void
}

/**
 * "Look before you leap" for restores: the historical content rendered
 * read-only, plus a block-level diff against the current version. Restoring
 * from here closes the dialog; the reader refetches via onRestored.
 */
export function RevisionPreviewDialog({
  pageId,
  atUtc,
  currentBlocks,
  canWrite,
  onRestored,
  onClose,
}: RevisionPreviewDialogProps) {
  const { t, i18n } = useTranslation()

  const snapshot = useQuery({
    queryKey: [...pageKeys.revisions(pageId), 'at', atUtc],
    queryFn: ({ signal }) => getPageAtRevision(pageId, atUtc, signal),
  })

  const restore = useRestoreRevision(pageId, () => {
    onRestored()
    onClose()
  })

  const nodes = useMemo(
    () => (snapshot.data === undefined ? [] : assembleBlockTree(snapshot.data.blocks)),
    [snapshot.data],
  )
  const diff = useMemo(
    () =>
      snapshot.data === undefined ? null : diffPageAtRevision(currentBlocks, snapshot.data.blocks),
    [currentBlocks, snapshot.data],
  )

  return (
    <DialogShell
      title={t('history.previewTitle', { time: formatRelativeTime(atUtc, i18n.language) })}
      onClose={onClose}
      wide
    >
      <div className="flex max-h-[70vh] flex-col gap-4 overflow-y-auto">
        <section>
          <h3 className="mb-2 text-xs font-semibold tracking-wide text-muted uppercase">
            {t('history.diffTitle')}
          </h3>
          {snapshot.isPending ? (
            <p className="text-xs text-muted">{t('history.loading')}</p>
          ) : snapshot.isError ? (
            <p className="text-xs text-danger">{t('history.loadFailed')}</p>
          ) : diff !== null && diff.length === 0 ? (
            <p className="text-xs text-muted">{t('history.diffSame')}</p>
          ) : (
            <ul className="flex flex-col gap-1">
              {diff?.map((entry) => (
                <DiffRow key={entry.blockId} entry={entry} />
              ))}
            </ul>
          )}
        </section>

        {snapshot.isSuccess ? (
          <section>
            <h3 className="mb-2 text-xs font-semibold tracking-wide text-muted uppercase">
              {t('history.previewContent')}
            </h3>
            <div className="rounded-lg border border-line px-3 py-2 text-sm leading-relaxed">
              {nodes.length === 0 ? (
                <p className="text-xs text-muted">{t('history.emptyContent')}</p>
              ) : (
                <BlockList nodes={nodes} />
              )}
            </div>
          </section>
        ) : null}

        {restore.isError ? (
          <p role="alert" className="text-xs text-danger">
            {t('history.restoreFailed')}
          </p>
        ) : null}

        {canWrite ? (
          <RevisionRestoreButton
            pending={restore.isPending}
            onRestore={() => {
              restore.mutate(atUtc)
            }}
          />
        ) : null}
      </div>
    </DialogShell>
  )
}

function DiffRow({ entry }: { entry: RevisionDiffEntry }) {
  const { t } = useTranslation()
  const badge =
    entry.kind === 'added'
      ? { label: t('history.diffAdded'), className: 'bg-success-soft text-success' }
      : entry.kind === 'removed'
        ? { label: t('history.diffRemoved'), className: 'bg-danger-soft text-danger' }
        : { label: t('history.diffChanged'), className: 'bg-warning-soft text-warning' }

  return (
    <li className="flex items-start gap-2 text-xs">
      <span
        className={`mt-0.5 shrink-0 rounded-full px-2 py-0.5 font-medium ${badge.className}`}
      >
        {badge.label}
      </span>
      <span className="min-w-0 flex-1">
        {entry.kind === 'changed' && entry.contentChanged ? (
          <>
            <span className="block truncate text-muted line-through" title={entry.beforeText ?? ''}>
              {entry.beforeText ?? entry.type}
            </span>
            <span className="block truncate text-ink" title={entry.afterText ?? ''}>
              {entry.afterText ?? entry.type}
            </span>
          </>
        ) : (
          <span
            className={`block truncate ${entry.kind === 'removed' ? 'text-muted line-through' : 'text-ink'}`}
            title={(entry.afterText ?? entry.beforeText) ?? ''}
          >
            {(entry.afterText ?? entry.beforeText) ?? entry.type}
            {entry.kind === 'changed' ? ` · ${t('history.diffMoved')}` : ''}
          </span>
        )}
      </span>
    </li>
  )
}
