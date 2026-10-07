import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useEffect, useId } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { notebookKeys } from '@/entities/notebook'
import { useSessionStore } from '@/entities/session'
import { setNotebookFavorite } from '../api/setNotebookFavorite'

export interface FavoriteNotebookButtonProps {
  notebookId: string
  isFavorite: boolean
}

// How long the error bubble stays before dismissing itself.
const ERROR_DISMISS_MS = 4000

/**
 * The star on a notebook card's corner. Guests who click it are sent to sign
 * in, like 3.4 did. On success every notebook list refetches — favorites live
 * on their own shelf, so several keys change at once.
 */
export function FavoriteNotebookButton({ notebookId, isFavorite }: FavoriteNotebookButtonProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const status = useSessionStore((state) => state.status)
  const errorId = useId()

  const mutation = useMutation({
    mutationFn: (next: boolean) => setNotebookFavorite(notebookId, next),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: notebookKeys.all }),
  })

  // Without this a failed toggle would pin the error bubble to the card forever.
  const { reset } = mutation
  useEffect(() => {
    if (!mutation.isError) {
      return
    }
    const timeout = setTimeout(reset, ERROR_DISMISS_MS)
    return () => clearTimeout(timeout)
  }, [mutation.isError, reset])

  const label = isFavorite ? t('favorite.remove') : t('favorite.add')

  return (
    <span className="relative inline-flex">
      <button
        type="button"
        aria-label={label}
        aria-pressed={isFavorite}
        aria-describedby={mutation.isError ? errorId : undefined}
        aria-busy={mutation.isPending}
        title={label}
        disabled={mutation.isPending}
        onClick={(event) => {
          // The star floats over the card's link — never let the click navigate.
          event.preventDefault()
          event.stopPropagation()
          if (status !== 'authenticated') {
            void navigate('/login')
            return
          }
          mutation.mutate(!isFavorite)
        }}
        className={[
          'rounded-md p-1.5 transition-all focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent',
          isFavorite
            ? 'text-accent opacity-100'
            : 'text-muted hover:bg-accent-soft hover:text-accent sm:opacity-0 sm:group-hover:opacity-100 sm:group-focus-within:opacity-100',
        ].join(' ')}
      >
        <svg
          viewBox="0 0 16 16"
          className={`size-4 ${isFavorite ? 'fill-accent' : ''}`}
          fill="none"
          stroke="currentColor"
          strokeWidth="1.5"
          strokeLinecap="round"
          strokeLinejoin="round"
          aria-hidden="true"
        >
          <path d="M8 1.6 9.9 5.5l4.3.6-3.1 3 .7 4.3L8 11.5l-3.8 2 .7-4.3-3.1-3 4.3-.6Z" />
        </svg>
      </button>
      {mutation.isError ? (
        <button
          type="button"
          id={errorId}
          role="alert"
          onClick={(event) => {
            event.preventDefault()
            event.stopPropagation()
            reset()
          }}
          className="absolute top-full right-0 z-10 mt-1 w-52 cursor-pointer rounded-md border border-line bg-card p-2 text-left text-xs text-danger shadow-md"
        >
          {t('favorite.error')}
        </button>
      ) : null}
    </span>
  )
}
