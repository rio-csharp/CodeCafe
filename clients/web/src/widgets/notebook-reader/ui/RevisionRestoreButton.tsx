import { useTranslation } from 'react-i18next'

export interface RevisionRestoreButtonProps {
  pending: boolean
  onRestore: () => void
}

/**
 * One click restores — no confirm step. The history is append-only, so a
 * restore is itself undoable by restoring again; that is the safety net.
 */
export function RevisionRestoreButton({ pending, onRestore }: RevisionRestoreButtonProps) {
  const { t } = useTranslation()

  return (
    <button
      type="button"
      disabled={pending}
      onClick={onRestore}
      className="self-start rounded-md bg-accent px-3 py-1.5 text-xs font-medium text-white transition-colors hover:bg-accent-strong focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent disabled:opacity-50"
    >
      {pending ? t('history.restoring') : t('history.restore')}
    </button>
  )
}
