import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { createNotebook } from '@/entities/notebook'
import type { NotebookDetails, NotebookVisibility } from '@/entities/notebook'
import { ApiError } from '@/shared/api'
import { Button, Input } from '@/shared/ui'

const VISIBILITIES = ['Private', 'Unlisted', 'Public'] as const satisfies readonly NotebookVisibility[]

export interface CreateNotebookDialogProps {
  open: boolean
  onClose: () => void
  onCreated: (notebook: NotebookDetails) => void
}

/**
 * Modal with the minimum a notebook needs: a title, an optional description,
 * and who can see it. The server derives the slug from the title.
 */
export function CreateNotebookDialog({ open, onClose, onCreated }: CreateNotebookDialogProps) {
  if (!open) {
    return null
  }
  // Remounted on every open, so the form always starts blank without a reset.
  return <DialogForm onClose={onClose} onCreated={onCreated} />
}

function DialogForm({ onClose, onCreated }: Omit<CreateNotebookDialogProps, 'open'>) {
  const { t } = useTranslation()
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [visibility, setVisibility] = useState<NotebookVisibility>('Private')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const titleRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    titleRef.current?.focus()

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

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    if (title.trim().length === 0 || submitting) {
      return
    }
    setSubmitting(true)
    setError(null)
    try {
      const notebook = await createNotebook({
        title: title.trim(),
        description,
        visibility,
      })
      onCreated(notebook)
    } catch (cause) {
      setError(cause instanceof ApiError ? cause.code : 'unknown')
      setSubmitting(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 grid place-items-center p-4">
      <div
        aria-hidden="true"
        className="absolute inset-0 bg-canvas/70 backdrop-blur-sm"
        onClick={onClose}
      />

      <div
        role="dialog"
        aria-modal="true"
        aria-label={t('createNotebook.title')}
        className="relative w-full max-w-md rounded-lg border border-line bg-card p-6 shadow-xl"
      >
        <h2 className="font-display text-xl text-ink">{t('createNotebook.title')}</h2>

        <form className="mt-5 flex flex-col gap-4" onSubmit={submit}>
          <label className="flex flex-col gap-1.5 text-sm text-ink">
            {t('createNotebook.name')}
            <Input
              ref={titleRef}
              value={title}
              onChange={(event) => {
                setTitle(event.target.value)
              }}
              maxLength={120}
              required
              placeholder={t('createNotebook.namePlaceholder')}
            />
          </label>

          <label className="flex flex-col gap-1.5 text-sm text-ink">
            {t('createNotebook.description')}
            <Input
              value={description}
              onChange={(event) => {
                setDescription(event.target.value)
              }}
              maxLength={2000}
              placeholder={t('createNotebook.descriptionPlaceholder')}
            />
          </label>

          <label className="flex flex-col gap-1.5 text-sm text-ink">
            {t('createNotebook.visibility')}
            <select
              value={visibility}
              onChange={(event) => {
                setVisibility(event.target.value as NotebookVisibility)
              }}
              className="rounded-md border border-line bg-canvas px-3 py-2 text-sm text-ink focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent"
            >
              {VISIBILITIES.map((option) => (
                <option key={option} value={option}>
                  {t(`visibility.${option}`)}
                </option>
              ))}
            </select>
          </label>

          {error !== null ? (
            <p role="alert" className="text-sm text-danger">
              {t('createNotebook.error')}
            </p>
          ) : null}

          <div className="mt-1 flex justify-end gap-2">
            <Button type="button" variant="ghost" onClick={onClose}>
              {t('createNotebook.cancel')}
            </Button>
            <Button type="submit" disabled={title.trim().length === 0 || submitting}>
              {submitting ? t('createNotebook.submitting') : t('createNotebook.submit')}
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}
