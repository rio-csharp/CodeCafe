import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { pageKeys, revokePageShare, sharePage } from '@/entities/page'
import type { PageDetails, PageShareRole } from '@/entities/page'
import { ApiError } from '@/shared/api'
import { Button, DialogField, DialogShell, Input } from '@/shared/ui'
import { SettingsError } from '@/features/manage-notebook'

export interface SharePageDialogProps {
  /** The open page's live details; null closes the dialog. */
  page: PageDetails | null
  onClose: () => void
}

/**
 * Who can read or edit this one page. Unlike the notebook dialog the shares
 * ride in on the already-loaded page details, so there is no fetch here —
 * mutations just invalidate the page namespace and the prop refreshes.
 */
export function SharePageDialog({ page, onClose }: SharePageDialogProps) {
  const { t } = useTranslation()
  if (page === null) {
    return null
  }
  return (
    <DialogShell title={t('pageShare.title')} onClose={onClose}>
      <ShareBody key={page.id} page={page} />
    </DialogShell>
  )
}

function ShareBody({ page }: { page: PageDetails }) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [email, setEmail] = useState('')
  const [role, setRole] = useState<PageShareRole>('Viewer')
  const [error, setError] = useState<string | null>(null)
  const emailRef = useRef<HTMLInputElement>(null)

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: pageKeys.all })
  }

  const add = useMutation({
    mutationFn: () => sharePage(page.id, email.trim(), role),
    onSuccess: () => {
      setEmail('')
      setError(null)
      invalidate()
      emailRef.current?.focus()
    },
    onError: (cause) => {
      setError(cause instanceof ApiError ? cause.code : 'unknown')
    },
  })

  const remove = useMutation({
    mutationFn: (userId: string) => revokePageShare(page.id, userId),
    onSuccess: invalidate,
  })

  const submit = (event: FormEvent) => {
    event.preventDefault()
    if (email.trim().length === 0 || add.isPending) {
      return
    }
    add.mutate()
  }

  return (
    <section aria-label={t('settings.shares')}>
      <DialogField label={t('settings.shares')}>
        {page.shares.length > 0 ? (
          <ul className="divide-y divide-line rounded-xl border border-line">
            {page.shares.map((share) => (
              <li key={share.userId} className="flex items-center gap-3 px-3 py-2.5">
                <span
                  aria-hidden="true"
                  className="grid size-6 shrink-0 place-items-center rounded-full bg-accent-soft text-[10px] font-medium text-accent-strong"
                >
                  {share.userName.trim().charAt(0).toUpperCase() || '·'}
                </span>
                <span className="min-w-0 flex-1 truncate text-sm text-ink">{share.userName}</span>
                <span className="shrink-0 rounded-full border border-line px-2 py-px text-[11px] text-muted">
                  {t(`settings.role.${share.role}`)}
                </span>
                <button
                  type="button"
                  aria-label={t('settings.removeShare', { name: share.userName })}
                  disabled={remove.isPending}
                  onClick={() => {
                    remove.mutate(share.userId)
                  }}
                  className="grid size-6 shrink-0 place-items-center rounded-full text-muted transition-colors hover:bg-danger-soft hover:text-danger"
                >
                  <svg viewBox="0 0 16 16" className="size-3" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" aria-hidden="true">
                    <path d="m4 4 8 8M12 4l-8 8" />
                  </svg>
                </button>
              </li>
            ))}
          </ul>
        ) : (
          <p className="rounded-xl border border-dashed border-line px-3 py-4 text-center text-xs text-muted">
            {t('settings.noShares')}
          </p>
        )}
      </DialogField>

      <form className="mt-3 flex gap-2" onSubmit={submit}>
        <div className="min-w-0 flex-1">
          <Input
            ref={emailRef}
            type="email"
            value={email}
            onChange={(event) => {
              setEmail(event.target.value)
            }}
            placeholder={t('settings.shareEmailPlaceholder')}
            aria-label={t('settings.shareEmailPlaceholder')}
          />
        </div>
        <select
          value={role}
          onChange={(event) => {
            setRole(event.target.value as PageShareRole)
          }}
          aria-label={t('settings.shareRole')}
          className="h-11 shrink-0 rounded-xl border border-line bg-canvas px-2 text-sm text-ink"
        >
          <option value="Viewer">{t('settings.role.Viewer')}</option>
          <option value="Editor">{t('settings.role.Editor')}</option>
        </select>
        <Button type="submit" disabled={email.trim().length === 0 || add.isPending}>
          {t('settings.shareAdd')}
        </Button>
      </form>
      {error !== null ? (
        <div className="mt-2">
          <SettingsError code={error} />
        </div>
      ) : null}
    </section>
  )
}
