import { useState } from 'react'
import { useTranslation } from 'react-i18next'

export interface RevisionRestoreButtonProps {
  pending: boolean
  onRestore: () => void
}

/**
 * Two-click restore: the first click arms it (danger styling, confirm copy),
 * the second fires. The arming state is per button, so a list of them never
 * confirms the wrong row.
 */
export function RevisionRestoreButton({ pending, onRestore }: RevisionRestoreButtonProps) {
  const { t } = useTranslation()
  const [confirming, setConfirming] = useState(false)

  return (
    <button
      type="button"
      disabled={pending}
      onClick={() => {
        if (confirming) {
          onRestore()
        } else {
          setConfirming(true)
        }
      }}
      className={`self-start rounded-md px-2 py-1 text-xs font-medium transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent disabled:opacity-50 ${
        confirming
          ? 'bg-danger-soft text-danger'
          : 'text-accent-strong hover:bg-muted-soft'
      }`}
    >
      {pending && confirming
        ? t('history.restoring')
        : confirming
          ? t('history.restoreConfirm')
          : t('history.restore')}
    </button>
  )
}
