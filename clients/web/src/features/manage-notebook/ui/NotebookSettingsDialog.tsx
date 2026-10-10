import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import {
  changeNotebookSlug,
  exportNotebook,
  getNotebookDetails,
  getNotebookSlugAvailability,
  notebookKeys,
  normalizeNotebookSlug,
  NOTEBOOK_SLUG_PATTERN,
  setNotebookTags,
  updateNotebook,
} from '@/entities/notebook'
import type { NotebookDetails, NotebookVisibility } from '@/entities/notebook'
import { ApiError } from '@/shared/api'
import { Button, DialogField, DialogShell, Input } from '@/shared/ui'

export interface NotebookSettingsDialogProps {
  /** null closes the dialog. */
  slug: string | null
  onClose: () => void
}

/** Identity of one notebook: its name, blurb, visibility, and tags. */
export function NotebookSettingsDialog({ slug, onClose }: NotebookSettingsDialogProps) {
  const { t } = useTranslation()
  if (slug === null) {
    return null
  }
  return (
    <DialogShell title={t('settings.title')} onClose={onClose} wide>
      {/* Remounted per notebook, so every field starts from server truth. */}
      <SettingsBody key={slug} slug={slug} />
    </DialogShell>
  )
}

function SettingsBody({ slug }: { slug: string }) {
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
    <div className="flex flex-col gap-4">
      <SettingsForm slug={slug} notebook={details.data} />
      <NotebookExportSection slug={slug} />
    </div>
  )
}

/** The whole notebook as one markdown file, straight from the save dialog. */
function NotebookExportSection({ slug }: { slug: string }) {
  const { t } = useTranslation()
  const [failed, setFailed] = useState(false)

  const exportMutation = useMutation({
    mutationFn: () => exportNotebook(slug),
    onSuccess: () => {
      setFailed(false)
    },
    onError: () => {
      setFailed(true)
    },
  })

  return (
    <section aria-label={t('settings.export')} className="border-t border-line pt-4">
      <div className="flex items-center justify-between gap-3">
        <h3 className="text-sm font-semibold text-ink">{t('settings.export')}</h3>
        <Button
          variant="ghost"
          size="sm"
          disabled={exportMutation.isPending}
          onClick={() => {
            exportMutation.mutate()
          }}
        >
          {t('settings.exportMarkdown')}
        </Button>
      </div>
      {failed ? (
        <p role="alert" className="mt-2 text-xs text-danger">
          {t('settings.exportFailed')}
        </p>
      ) : null}
    </section>
  )
}

function useInvalidator(slug: string) {
  const queryClient = useQueryClient()
  return () => {
    void queryClient.invalidateQueries({ queryKey: notebookKeys.all })
    void queryClient.invalidateQueries({ queryKey: notebookKeys.details(slug) })
  }
}

export function SettingsError({ code }: { code: string }) {  const { t } = useTranslation()
  const key =
    code === 'share_target_not_found'
      ? 'settings.error.shareTargetNotFound'
      : code === 'cannot_share_with_owner'
        ? 'settings.error.cannotShareWithOwner'
        : code === 'slug_already_taken'
          ? 'settings.error.slugTaken'
          : 'settings.error.generic'
  return (
    <p role="alert" className="text-xs text-danger">
      {t(key)}
    </p>
  )
}

const SLUG_CHECK_DEBOUNCE_MS = 300

type SlugState =
  | { status: 'idle' }
  | { status: 'checking' }
  | { status: 'available' }
  | { status: 'taken'; suggestions: string[] }

/** One form, one save button — the two endpoints behind it are plumbing. */
function SettingsForm({ slug, notebook }: { slug: string; notebook: NotebookDetails }) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [title, setTitle] = useState(notebook.title)
  const [description, setDescription] = useState(notebook.description ?? '')
  const [visibility, setVisibility] = useState(notebook.visibility)
  const [tags, setTags] = useState<string[]>(notebook.tags)
  const [tagDraft, setTagDraft] = useState('')
  const [slugDraft, setSlugDraft] = useState(notebook.slug)
  // Tagged with the slug it describes, so a stale verdict never leaks onto a
  // fresh draft after the user keeps typing.
  const [slugProbe, setSlugProbe] = useState<{ slug: string; state: SlugState } | null>(null)
  const [error, setError] = useState<string | null>(null)
  const invalidate = useInvalidator(slug)

  const normalizedSlug = normalizeNotebookSlug(slugDraft)
  const slugChanged = normalizedSlug !== notebook.slug
  const slugWellFormed = normalizedSlug.length > 0 && NOTEBOOK_SLUG_PATTERN.test(normalizedSlug)
  const slugDirty = slugChanged && slugWellFormed
  const slugState: SlugState =
    slugDirty && slugProbe?.slug === normalizedSlug ? slugProbe.state : { status: 'idle' }

  // Live availability probe for a changed slug. The current slug is the
  // notebook's own, so probing it would always report "taken".
  useEffect(() => {
    if (!slugDirty) {
      return
    }
    const controller = new AbortController()
    const timer = setTimeout(() => {
      setSlugProbe({ slug: normalizedSlug, state: { status: 'checking' } })
      getNotebookSlugAvailability(normalizedSlug, controller.signal)
        .then((result) => {
          setSlugProbe({
            slug: normalizedSlug,
            state: result.isAvailable
              ? { status: 'available' }
              : { status: 'taken', suggestions: result.suggestions },
          })
        })
        .catch(() => {
          // A failed probe must not block saving; the server decides anyway.
          setSlugProbe({ slug: normalizedSlug, state: { status: 'idle' } })
        })
    }, SLUG_CHECK_DEBOUNCE_MS)
    return () => {
      clearTimeout(timer)
      controller.abort()
    }
  }, [slugDirty, normalizedSlug])

  const basicsDirty =
    title !== notebook.title ||
    description !== (notebook.description ?? '') ||
    visibility !== notebook.visibility
  const tagsDirty =
    tags.length !== notebook.tags.length || tags.some((tag, index) => tag !== notebook.tags[index])
  const dirty = basicsDirty || tagsDirty || slugDirty
  const slugBlocked =
    slugChanged &&
    (!slugWellFormed || slugState.status === 'checking' || slugState.status === 'taken')

  const save = useMutation({
    mutationFn: async () => {
      let activeSlug = slug
      if (slugDirty) {
        // The rename moves the address; every other patch must follow it,
        // or they would 404 on the old slug.
        const renamed = await changeNotebookSlug(slug, normalizedSlug)
        activeSlug = renamed.slug
      }
      // Only the touched halves ride out; unchanged halves stay put.
      await Promise.all([
        basicsDirty
          ? updateNotebook(activeSlug, {
              title: title.trim(),
              description: description.trim() || null,
              visibility,
            })
          : Promise.resolve(null),
        tagsDirty ? setNotebookTags(activeSlug, tags) : Promise.resolve(null),
      ])
      return activeSlug
    },
    onSuccess: (activeSlug) => {
      setError(null)
      invalidate()
      if (activeSlug !== slug) {
        // Every link to this notebook just moved; land the reader on the new one.
        navigate(`/notebooks/${encodeURIComponent(activeSlug)}`, { replace: true })
      }
    },
    onError: (cause) => {
      setError(cause instanceof ApiError ? cause.code : 'unknown')
    },
  })

  const addTag = () => {
    const tag = tagDraft.trim().toLowerCase()
    if (tag.length === 0 || tags.includes(tag) || tags.length >= 20) {
      return
    }
    setTags([...tags, tag])
    setTagDraft('')
  }

  const submit = (event: FormEvent) => {
    event.preventDefault()
    if (!dirty || title.trim().length === 0 || save.isPending || slugBlocked) {
      return
    }
    save.mutate()
  }

  return (
    <form className="flex flex-col gap-4" onSubmit={submit} aria-label={t('settings.basics')}>
      <DialogField label={t('createNotebook.name')}>
        <Input
          value={title}
          onChange={(event) => {
            setTitle(event.target.value)
          }}
          maxLength={120}
          required
        />
      </DialogField>

      <DialogField label={t('createNotebook.description')}>
        <Input
          value={description}
          onChange={(event) => {
            setDescription(event.target.value)
          }}
          maxLength={2000}
          placeholder={t('createNotebook.descriptionPlaceholder')}
        />
      </DialogField>

      {notebook.isOwner ? (
        <>
          <DialogField label={t('settings.slug')}>
            <Input
              value={slugDraft}
              onChange={(event) => {
                setSlugDraft(event.target.value)
              }}
              maxLength={80}
              className="font-mono"
            />
          </DialogField>
          <SlugHint changed={slugChanged} wellFormed={slugWellFormed} state={slugState} />
        </>
      ) : null}

      <DialogField label={t('createNotebook.visibility')}>
        <select
          value={visibility}
          onChange={(event) => {
            setVisibility(event.target.value as NotebookVisibility)
          }}
          className="h-11 rounded-xl border border-line bg-canvas px-3 text-sm text-ink focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent"
        >
          {(['Private', 'Unlisted', 'Public'] as const).map((option) => (
            <option key={option} value={option}>
              {t(`visibility.${option}`)}
            </option>
          ))}
        </select>
      </DialogField>

      <DialogField label={t('settings.tags')}>
        <div className="flex flex-wrap items-center gap-1.5 rounded-xl border border-line bg-canvas p-2">
          {tags.map((tag) => (
            <span
              key={tag}
              className="inline-flex items-center gap-1 rounded-full bg-accent-soft px-2.5 py-1 text-xs text-accent-strong"
            >
              #{tag}
              <button
                type="button"
                aria-label={t('settings.removeTag', { tag })}
                onClick={() => {
                  setTags(tags.filter((item) => item !== tag))
                }}
                className="opacity-70 transition-opacity hover:opacity-100"
              >
                ×
              </button>
            </span>
          ))}
          <input
            value={tagDraft}
            onChange={(event) => {
              setTagDraft(event.target.value)
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
            className="min-w-24 flex-1 bg-transparent px-1.5 py-1 text-xs text-ink placeholder:text-muted focus:outline-none"
          />
        </div>
      </DialogField>

      {error !== null ? <SettingsError code={error} /> : null}

      <div className="flex justify-end">
        <Button
          type="submit"
          disabled={!dirty || title.trim().length === 0 || save.isPending || slugBlocked}
        >
          {save.isPending ? t('settings.saving') : t('settings.save')}
        </Button>
      </div>
    </form>
  )
}

/** The one line under the slug field: nothing while unchanged, then format or availability. */
function SlugHint({
  changed,
  wellFormed,
  state,
}: {
  changed: boolean
  wellFormed: boolean
  state: SlugState
}) {
  const { t } = useTranslation()

  if (!changed) {
    return null
  }

  if (!wellFormed) {
    return (
      <p role="alert" className="-mt-2 text-xs text-danger">
        {t('createNotebook.slugHint')}
      </p>
    )
  }

  if (state.status === 'taken') {
    return (
      <p role="alert" className="-mt-2 text-xs text-danger">
        {t('createNotebook.slugTaken')}
      </p>
    )
  }

  return (
    <p
      aria-live="polite"
      className={`-mt-2 text-xs ${state.status === 'available' ? 'text-success' : 'text-muted'}`}
    >
      {t(state.status === 'available' ? 'createNotebook.slugAvailable' : 'createNotebook.slugChecking')}
    </p>
  )
}
