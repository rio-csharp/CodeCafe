import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import {
  getNotebookDetails,
  notebookKeys,
  revokeNotebookShare,
  setNotebookAccessCode,
  setNotebookTags,
  shareNotebook,
  updateNotebook,
} from '@/entities/notebook'
import type { CollaboratorRole, NotebookDetails, NotebookVisibility } from '@/entities/notebook'
import { ApiError } from '@/shared/api'
import { Button, Input } from '@/shared/ui'

export interface NotebookSettingsDialogProps {
  /** null closes the dialog. */
  slug: string | null
  onClose: () => void
}

/** Everything about one notebook that isn't its pages: basics, tags, shares, access code. */
export function NotebookSettingsDialog({ slug, onClose }: NotebookSettingsDialogProps) {
  if (slug === null) {
    return null
  }
  // Remounted per notebook, so every section starts from server truth.
  return <SettingsBody key={slug} slug={slug} onClose={onClose} />
}

function SettingsBody({ slug, onClose }: { slug: string; onClose: () => void }) {
  const { t } = useTranslation()

  const details = useQuery({
    queryKey: notebookKeys.details(slug),
    queryFn: ({ signal }) => getNotebookDetails({ slug, signal }),
  })

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        onClose()
      }
    }
    document.addEventListener('keydown', onKeyDown)
    return () => {
      document.removeEventListener('keydown', onKeyDown)
    }
  }, [onClose])

  return (
    <div className="fixed inset-0 z-50 grid place-items-center overflow-y-auto p-4">
      <div
        aria-hidden="true"
        className="absolute inset-0 bg-canvas/70 backdrop-blur-sm"
        onClick={onClose}
      />

      <div
        role="dialog"
        aria-modal="true"
        aria-label={t('settings.title')}
        className="relative my-8 w-full max-w-lg rounded-lg border border-line bg-card p-6 shadow-xl"
      >
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold text-ink">{t('settings.title')}</h2>
          <button
            type="button"
            aria-label={t('settings.close')}
            onClick={onClose}
            className="grid size-8 place-items-center rounded-full text-muted transition-colors hover:bg-muted-soft hover:text-ink"
          >
            <svg viewBox="0 0 16 16" className="size-4" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" aria-hidden="true">
              <path d="m4 4 8 8M12 4l-8 8" />
            </svg>
          </button>
        </div>

        {details.isPending ? (
          <p className="py-10 text-center text-sm text-muted">{t('list.loading')}</p>
        ) : details.isError ? (
          <p className="py-10 text-center text-sm text-danger">{t('list.loadError')}</p>
        ) : (
          <div className="mt-5 flex flex-col gap-7">
            <BasicsSection slug={slug} notebook={details.data} />
            <TagsSection slug={slug} notebook={details.data} />
            <SharesSection slug={slug} notebook={details.data} />
            <AccessCodeSection slug={slug} notebook={details.data} />
          </div>
        )}
      </div>
    </div>
  )
}

function SectionHeading({ children }: { children: string }) {
  return (
    <h3 className="mb-2 text-xs font-semibold tracking-wide text-muted uppercase">{children}</h3>
  )
}

function SectionError({ code }: { code: string }) {
  const { t } = useTranslation()
  const key =
    code === 'share_target_not_found'
      ? 'settings.error.shareTargetNotFound'
      : code === 'cannot_share_with_owner'
        ? 'settings.error.cannotShareWithOwner'
        : code === 'slug_already_taken'
          ? 'settings.error.slugTaken'
          : 'settings.error.generic'
  return (
    <p role="alert" className="mt-2 text-xs text-danger">
      {t(key)}
    </p>
  )
}

function useInvalidator(slug: string) {
  const queryClient = useQueryClient()
  return () => {
    void queryClient.invalidateQueries({ queryKey: notebookKeys.all })
    void queryClient.invalidateQueries({ queryKey: notebookKeys.details(slug) })
  }
}

function BasicsSection({ slug, notebook }: { slug: string; notebook: NotebookDetails }) {
  const { t } = useTranslation()
  const [title, setTitle] = useState(notebook.title)
  const [description, setDescription] = useState(notebook.description ?? '')
  const [visibility, setVisibility] = useState(notebook.visibility)
  const [error, setError] = useState<string | null>(null)
  const invalidate = useInvalidator(slug)

  const save = useMutation({
    mutationFn: () =>
      updateNotebook(slug, {
        title: title.trim(),
        description: description.trim() || null,
        visibility,
      }),
    onSuccess: () => {
      setError(null)
      invalidate()
    },
    onError: (cause) => {
      setError(cause instanceof ApiError ? cause.code : 'unknown')
    },
  })

  const dirty =
    title !== notebook.title ||
    description !== (notebook.description ?? '') ||
    visibility !== notebook.visibility

  const submit = (event: FormEvent) => {
    event.preventDefault()
    if (!dirty || title.trim().length === 0 || save.isPending) {
      return
    }
    save.mutate()
  }

  return (
    <section aria-label={t('settings.basics')}>
      <SectionHeading>{t('settings.basics')}</SectionHeading>
      <form className="flex flex-col gap-3" onSubmit={submit}>
        <Input
          value={title}
          onChange={(event) => {
            setTitle(event.target.value)
          }}
          maxLength={120}
          required
          aria-label={t('createNotebook.name')}
        />
        <Input
          value={description}
          onChange={(event) => {
            setDescription(event.target.value)
          }}
          maxLength={2000}
          placeholder={t('createNotebook.descriptionPlaceholder')}
          aria-label={t('createNotebook.description')}
        />
        <select
          value={visibility}
          onChange={(event) => {
            setVisibility(event.target.value as NotebookVisibility)
          }}
          aria-label={t('createNotebook.visibility')}
          className="h-11 rounded-xl border border-line bg-canvas px-3 text-sm text-ink focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent"
        >
          {(['Private', 'Unlisted', 'Public'] as const).map((option) => (
            <option key={option} value={option}>
              {t(`visibility.${option}`)}
            </option>
          ))}
        </select>
        {error !== null ? <SectionError code={error} /> : null}
        <div className="flex justify-end">
          <Button type="submit" disabled={!dirty || title.trim().length === 0 || save.isPending}>
            {save.isPending ? t('settings.saving') : t('settings.save')}
          </Button>
        </div>
      </form>
    </section>
  )
}

function TagsSection({ slug, notebook }: { slug: string; notebook: NotebookDetails }) {
  const { t } = useTranslation()
  const [tags, setTags] = useState<string[]>(notebook.tags)
  const [draft, setDraft] = useState('')
  const [error, setError] = useState<string | null>(null)
  const invalidate = useInvalidator(slug)

  const save = useMutation({
    mutationFn: () => setNotebookTags(slug, tags),
    onSuccess: () => {
      setError(null)
      invalidate()
    },
    onError: (cause) => {
      setError(cause instanceof ApiError ? cause.code : 'unknown')
    },
  })

  const addTag = () => {
    const tag = draft.trim().toLowerCase()
    if (tag.length === 0 || tags.includes(tag) || tags.length >= 20) {
      return
    }
    setTags([...tags, tag])
    setDraft('')
  }

  const dirty =
    tags.length !== notebook.tags.length || tags.some((tag, index) => tag !== notebook.tags[index])

  return (
    <section aria-label={t('settings.tags')}>
      <SectionHeading>{t('settings.tags')}</SectionHeading>
      <div className="flex flex-wrap items-center gap-1.5">
        {tags.map((tag) => (
          <span
            key={tag}
            className="inline-flex items-center gap-1 rounded-full border border-line bg-canvas px-2.5 py-1 text-xs text-ink"
          >
            #{tag}
            <button
              type="button"
              aria-label={t('settings.removeTag', { tag })}
              onClick={() => {
                setTags(tags.filter((item) => item !== tag))
              }}
              className="text-muted hover:text-danger"
            >
              ×
            </button>
          </span>
        ))}
        <input
          value={draft}
          onChange={(event) => {
            setDraft(event.target.value)
          }}
          onKeyDown={(event) => {
            if (event.key === 'Enter') {
              event.preventDefault()
              addTag()
            }
          }}
          maxLength={30}
          placeholder={t('settings.tagPlaceholder')}
          aria-label={t('settings.tagPlaceholder')}
          className="min-w-24 flex-1 rounded-full border border-dashed border-line bg-transparent px-2.5 py-1 text-xs text-ink placeholder:text-muted focus:outline-none focus:border-accent"
        />
      </div>
      {error !== null ? <SectionError code={error} /> : null}
      <div className="mt-3 flex justify-end">
        <Button
          variant="ghost"
          disabled={!dirty || save.isPending}
          onClick={() => {
            save.mutate()
          }}
        >
          {save.isPending ? t('settings.saving') : t('settings.save')}
        </Button>
      </div>
    </section>
  )
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
      <SectionHeading>{t('settings.shares')}</SectionHeading>

      {notebook.shares.length > 0 ? (
        <ul className="mb-3 divide-y divide-line rounded-xl border border-line">
          {notebook.shares.map((share) => (
            <li key={share.userId} className="flex items-center gap-3 px-3 py-2">
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
                className="shrink-0 text-xs text-muted transition-colors hover:text-danger"
              >
                {t('settings.removeShareShort')}
              </button>
            </li>
          ))}
        </ul>
      ) : (
        <p className="mb-3 text-xs text-muted">{t('settings.noShares')}</p>
      )}

      <form className="flex gap-2" onSubmit={submit}>
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
      {error !== null ? <SectionError code={error} /> : null}
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
      <SectionHeading>{t('settings.accessCode')}</SectionHeading>
      <p className="mb-2 text-xs text-muted">
        {notebook.hasAccessCode ? t('settings.accessCodeSet') : t('settings.accessCodeUnset')}
      </p>
      <div className="flex gap-2">
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
      {error !== null ? <SectionError code={error} /> : null}
    </section>
  )
}
