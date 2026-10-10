import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { CreatedPersonalAccessToken } from '@/entities/session'
import { Button, DialogShell } from '@/shared/ui'

export interface TokenRevealDialogProps {
  /** The freshly created token; its raw value is shown exactly once. */
  token: CreatedPersonalAccessToken
  onClose: () => void
}

/**
 * The one-time reveal after creation: the server only stores the hash, so
 * closing this dialog means the token can never be shown again.
 */
export function TokenRevealDialog({ token, onClose }: TokenRevealDialogProps) {
  const { t } = useTranslation()
  const [copied, setCopied] = useState(false)
  const [copyFailed, setCopyFailed] = useState(false)
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  useEffect(() => {
    return () => {
      if (timerRef.current !== null) {
        clearTimeout(timerRef.current)
      }
    }
  }, [])

  const copy = async () => {
    setCopyFailed(false)
    try {
      if (navigator.clipboard === undefined) {
        throw new Error('Clipboard API unavailable')
      }
      await navigator.clipboard.writeText(token.token)
      setCopied(true)
    } catch {
      setCopied(false)
      setCopyFailed(true)
    }
    if (timerRef.current !== null) {
      clearTimeout(timerRef.current)
    }
    timerRef.current = setTimeout(() => {
      setCopied(false)
      setCopyFailed(false)
    }, 1500)
  }

  return (
    <DialogShell title={t('account.revealTitle')} onClose={onClose}>
      <p className="text-sm text-muted">{t('account.revealBody')}</p>

      <code className="mt-3 block overflow-x-auto rounded-xl border border-line bg-canvas px-3 py-2.5 font-mono text-xs break-all text-ink select-all">
        {token.token}
      </code>

      <div className="mt-4 flex items-center gap-2">
        <Button
          onClick={() => {
            void copy()
          }}
          className={
            copied
              ? 'border-success/40 text-success'
              : copyFailed
                ? 'border-danger/40 text-danger'
                : undefined
          }
        >
          {copied
            ? t('account.revealCopied')
            : copyFailed
              ? t('account.revealCopyFailed')
              : t('account.revealCopy')}
        </Button>
        <Button variant="ghost" onClick={onClose}>
          {t('account.revealDone')}
        </Button>
      </div>
    </DialogShell>
  )
}
