import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { deleteNotebook, notebookKeys } from '@/entities/notebook'

export interface NotebookCardMenuProps {
  notebookId: string
  title: string
  /** Opens the settings dialog for this notebook. */
  onSettings: () => void
}

/**
 * The three-dot menu on a card's corner. Deleting is a soft delete — the
 * notebook lands in the trash and can come back — so there is deliberately
 * no confirm step here.
 */
export function NotebookCardMenu({ notebookId, title, onSettings }: NotebookCardMenuProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [open, setOpen] = useState(false)
  const rootRef = useRef<HTMLDivElement>(null)

  const remove = useMutation({
    mutationFn: () => deleteNotebook(notebookId),
    onSuccess: () => {
      // Several shelves can hold this card at once; drop them all.
      void queryClient.invalidateQueries({ queryKey: notebookKeys.all })
    },
  })

  useEffect(() => {
    if (!open) {
      return
    }
    const onPointerDown = (event: PointerEvent) => {
      if (rootRef.current !== null && !rootRef.current.contains(event.target as Node)) {
        setOpen(false)
      }
    }
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setOpen(false)
      }
    }
    document.addEventListener('pointerdown', onPointerDown)
    document.addEventListener('keydown', onKeyDown)
    return () => {
      document.removeEventListener('pointerdown', onPointerDown)
      document.removeEventListener('keydown', onKeyDown)
    }
  }, [open])

  return (
    <div ref={rootRef} className="relative">
      <button
        type="button"
        aria-haspopup="menu"
        aria-expanded={open}
        aria-label={t('menu.label', { title })}
        title={t('menu.label', { title })}
        onClick={(event) => {
          // The menu floats over the card's link — never let clicks navigate.
          event.preventDefault()
          event.stopPropagation()
          setOpen((value) => !value)
        }}
        className="rounded-md p-1.5 text-muted transition-all hover:bg-muted-soft hover:text-ink focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent sm:opacity-0 sm:group-hover:opacity-100 sm:group-focus-within:opacity-100"
      >
        <svg
          viewBox="0 0 16 16"
          className="size-4"
          fill="currentColor"
          aria-hidden="true"
        >
          <circle cx="3" cy="8" r="1.3" />
          <circle cx="8" cy="8" r="1.3" />
          <circle cx="13" cy="8" r="1.3" />
        </svg>
      </button>

      {open ? (
        <div
          role="menu"
          aria-label={t('menu.label', { title })}
          className="absolute right-0 z-20 mt-1 min-w-32 rounded-xl border border-line bg-card p-1 shadow-lg"
        >
          <button
            type="button"
            role="menuitem"
            onClick={(event) => {
              event.preventDefault()
              event.stopPropagation()
              setOpen(false)
              onSettings()
            }}
            className="w-full rounded-lg px-3 py-2 text-left text-sm text-ink transition-colors hover:bg-canvas focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent"
          >
            {t('menu.settings')}
          </button>
          <button
            type="button"
            role="menuitem"
            disabled={remove.isPending}
            onClick={(event) => {
              event.preventDefault()
              event.stopPropagation()
              setOpen(false)
              remove.mutate()
            }}
            className="w-full rounded-lg px-3 py-2 text-left text-sm text-danger transition-colors hover:bg-danger-soft focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent disabled:opacity-50"
          >
            {t('menu.delete')}
          </button>
        </div>
      ) : null}
    </div>
  )
}
