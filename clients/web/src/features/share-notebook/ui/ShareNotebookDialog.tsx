import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import {
  getNotebookDetails,
  notebookKeys,
  revokeNotebookShare,
  setNotebookAccessCode,
  shareNotebook,
} from '@/entities/notebook'
import type { CollaboratorRole, NotebookDetails } from '@/entities/notebook'
import { ApiError } from '@/shared/api'
import { Button, DialogField, DialogShell, Input } from '@/shared/ui'
import { SettingsError } from '@/features/manage-notebook'

export interface ShareNotebookDialogProps {
  /** null closes the dialog. */
  slug: string | null
  onClose: () => void
}

/** Who can read or edit this notebook, and whether a code gates the door. */
export function ShareNotebookDialog({ slug, onClose }: ShareNotebookDialogProps) {
  const { t } = useTranslation()
  if (slug === null) {
    return null
  }
  return (
    <DialogShell title={t('shareDialog.title')} onClose={onClose} wide>
      <ShareBody key={slug} slug={slug} />
    </DialogShell>
  )
}

function ShareBody({ slug }: { slug: string }) {
  const { t } = useTranslation()
  const details = useQuery({
    queryKey: notebookKeys.details(slug),
    queryFn: ({ signal }) => getNotebookDetails({ slug, signal }),
  })

  if (details.isPending) {
    return <p className="py-10 text-center text-sm text-muted">{t('list.loading')}</p>
  }
  if (details.isError) {
    return <p className="py-10 text-center text-sm text-danger">{t('list.loadError')}</p>
  }

  return (
    <div className="flex flex-col gap-6">
      <SharesSection slug={slug} notebook={details.data} />
      <div aria-hidden="true" className="h-px bg-line" />
      <AccessCodeSection slug={slug} notebook={details.data} />
    </div>
  )
}

function useInvalidator(slug: string) {
  const queryClient = useQueryClient()
  return () => {
    void queryClient.invalidateQueries({ queryKey: notebookKeys.all })
    void queryClient.invalidateQueries({ queryKey: notebookKeys.details(slug) })
  }
}

function SharesSection({ slug, notebook }: { slug: string; notebook: NotebookDetails }) {
  const { t } = useTranslation()
  const [email, setEmail] = useState('')
  const [role, setRole] = useState<CollaboratorRole>('Viewer')
  const [error, setError] = useState<string | null>(null)
  const emailRef = useRef<HTMLInputElement>(null)
  const invalidate = useInvalidator(slug)

  const add = useMutation({
    mutationFn: () => shareNotebook(slug, email.trim(), role),
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
    mutationFn: (userId: string) => revokeNotebookShare(slug, userId),
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
        {notebook.shares.length > 0 ? (
          <ul className="divide-y divide-line rounded-xl border border-line">
            {notebook.shares.map((share) => (
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
            setRole(event.target.value as CollaboratorRole)
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

function AccessCodeSection({ slug, notebook }: { slug: string; notebook: NotebookDetails }) {
  const { t } = useTranslation()
  const [code, setCode] = useState('')
  const [error, setError] = useState<string | null>(null)
  const invalidate = useInvalidator(slug)

  const apply = useMutation({
    mutationFn: (value: string | null) => setNotebookAccessCode(slug, value),
    onSuccess: () => {
      setCode('')
      setError(null)
      invalidate()
    },
    onError: (cause) => {
      setError(cause instanceof ApiError ? cause.code : 'unknown')
    },
  })

  return (
    <section aria-label={t('settings.accessCode')}>
      <DialogField label={t('settings.accessCode')}>
        <p className="text-xs text-muted">
          {notebook.hasAccessCode ? t('settings.accessCodeSet') : t('settings.accessCodeUnset')}
        </p>
        <div className="mt-2 flex gap-2">
          <div className="min-w-0 flex-1">
            <Input
              value={code}
              onChange={(event) => {
                setCode(event.target.value)
              }}
              placeholder={t('settings.accessCodePlaceholder')}
              aria-label={t('settings.accessCodePlaceholder')}
            />
          </div>
          <Button
            disabled={code.trim().length === 0 || apply.isPending}
            onClick={() => {
              apply.mutate(code.trim())
            }}
          >
            {t('settings.accessCodeApply')}
          </Button>
          {notebook.hasAccessCode ? (
            <Button
              variant="ghost"
              disabled={apply.isPending}
              onClick={() => {
                apply.mutate(null)
              }}
            >
              {t('settings.accessCodeClear')}
            </Button>
          ) : null}
        </div>
      </DialogField>
      {error !== null ? (
        <div className="mt-2">
          <SettingsError code={error} />
        </div>
      ) : null}
    </section>
  )
}
