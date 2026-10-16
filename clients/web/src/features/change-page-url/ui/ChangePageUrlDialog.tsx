import { useMutation } from '@tanstack/react-query'
import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { changePageSlug, isValidPageSlug, normalizePagePath, pageSlug, slugify } from '@/entities/page'
import type { PageDetails } from '@/entities/page'
import { ApiError } from '@/shared/api'
import { Button, DialogField, DialogShell, Input } from '@/shared/ui'

export interface ChangePageUrlDialogProps {
  /** The open page's live details; null keeps the dialog closed. */
  page: PageDetails | null
  /** How many subpages carry this page's slug inside their paths; 0 hides the warning. */
  movesDescendants: number
  onClose: () => void
  /** The page's path once the dialog is done — unchanged when nothing was renamed. */
  onDone: (path: string) => void
}

/**
 * Rewrites the last segment of a page's URL. The ancestors own the prefix, so only
 * the page's own slug is editable here. The server still arbitrates uniqueness, and
 * a clash it reports is the caller's to fix — that is the whole point of a picked
 * slug, unlike one derived from the title.
 */
export function ChangePageUrlDialog({
  page,
  movesDescendants,
  onClose,
  onDone,
}: ChangePageUrlDialogProps) {
  const { t } = useTranslation()

  if (page === null) {
    return null
  }

  return (
    <DialogShell title={t('pageUrl.title')} onClose={onClose}>
      <UrlBody
        key={page.id}
        page={page}
        movesDescendants={movesDescendants}
        onClose={onClose}
        onDone={onDone}
      />
    </DialogShell>
  )
}

const FAILURE_MESSAGES = {
  invalid: 'pageUrl.invalid',
  taken: 'pageUrl.taken',
  failed: 'pageUrl.failed',
} as const

type Failure = keyof typeof FAILURE_MESSAGES

function UrlBody({
  page,
  movesDescendants,
  onClose,
  onDone,
}: {
  page: PageDetails
  movesDescendants: number
  onClose: () => void
  onDone: (path: string) => void
}) {
  const { t } = useTranslation()
  const inputRef = useRef<HTMLInputElement>(null)
  // Opened with a draft URL for the title the page already carries, so naming a page and
  // fixing its address stay one decision. The current slug is the fallback for a title with
  // nothing slug-worthy in it, which keeps the field from ever starting out empty.
  const [value, setValue] = useState(() => slugify(page.title) || pageSlug(page.path))
  const [failure, setFailure] = useState<Failure | null>(null)

  useEffect(() => {
    inputRef.current?.select()
  }, [])

  const rename = useMutation({
    mutationFn: (slug: string) => changePageSlug(page.id, slug),
    onSuccess: (renamed) => {
      onDone(renamed.path)
    },
    onError: (cause) => {
      setFailure(cause instanceof ApiError && cause.code === 'slug_already_taken' ? 'taken' : 'failed')
    },
  })

  const segments = normalizePagePath(page.path).split('/')
  const prefix = segments.length > 1 ? `/${segments.slice(0, -1).join('/')}/` : '/'
  const slug = value.trim().toLowerCase()

  const submit = (event: FormEvent) => {
    event.preventDefault()

    if (!isValidPageSlug(slug)) {
      setFailure('invalid')
      return
    }

    if (slug === pageSlug(page.path)) {
      // Nothing to rename: close without a round trip.
      onDone(page.path)
      return
    }

    setFailure(null)
    rename.mutate(slug)
  }

  return (
    <form onSubmit={submit} className="flex flex-col gap-3">
      <DialogField label={t('pageUrl.label')}>
        <Input
          ref={inputRef}
          value={value}
          invalid={failure === 'invalid'}
          onChange={(event) => {
            setValue(event.target.value)
            setFailure(null)
          }}
          aria-describedby="page-url-preview"
          autoComplete="off"
          spellCheck={false}
        />
      </DialogField>

      <p id="page-url-preview" className="text-xs text-muted">
        {t('pageUrl.preview', { path: `${prefix}${slug}` })}
      </p>

      {failure !== null ? (
        <p role="alert" className="text-xs text-danger">
          {t(FAILURE_MESSAGES[failure])}
        </p>
      ) : null}

      {movesDescendants > 0 ? (
        <p className="text-xs text-accent-strong">
          {t('pageUrl.movesDescendants', { count: movesDescendants })}
        </p>
      ) : null}

      <div className="flex justify-end gap-2 pt-1">
        <Button
          type="button"
          variant="ghost"
          onClick={onClose}
          disabled={rename.isPending}
        >
          {t('editor.cancel')}
        </Button>
        <Button type="submit" disabled={rename.isPending}>
          {rename.isPending ? t('settings.saving') : t('settings.save')}
        </Button>
      </div>
    </form>
  )
}
