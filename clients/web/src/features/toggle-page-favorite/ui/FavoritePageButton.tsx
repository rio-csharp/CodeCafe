import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useEffect, useId } from 'react'
import { useTranslation } from 'react-i18next'
import { notebookKeys } from '@/entities/notebook'
import { pageKeys, setPageFavorite } from '@/entities/page'
import { useSessionStore } from '@/entities/session'

export interface FavoritePageButtonProps {
  pageId: string
  /** Notebook slug, needed to refetch the tree (its nodes carry the star). */
  slug: string
  isFavorite: boolean
}

// How long the error bubble stays before dismissing itself.
const ERROR_DISMISS_MS = 4000

/**
 * The page star in the reader chrome. Guests never see it — unlike notebooks,
 * the reader stays anonymous-friendly and the star is the only writer-ish
 * affordance a signed-in reader gets. Styled to sit among the chrome pills.
 */
export function FavoritePageButton({ pageId, slug, isFavorite }: FavoritePageButtonProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const status = useSessionStore((state) => state.status)
  const errorId = useId()

  const mutation = useMutation({
    mutationFn: (next: boolean) => setPageFavorite(pageId, next),
    onSuccess: () => {
      // The star is echoed by the page details, the tree node and every
      // favorites list, so all three namespaces refetch.
      void queryClient.invalidateQueries({ queryKey: pageKeys.all })
      void queryClient.invalidateQueries({ queryKey: notebookKeys.tree(slug) })
    },
  })

  // Without this a failed toggle would pin the error bubble to the chrome forever.
  const { reset } = mutation
  useEffect(() => {
    if (!mutation.isError) {
      return
    }
    const timeout = setTimeout(reset, ERROR_DISMISS_MS)
    return () => clearTimeout(timeout)
  }, [mutation.isError, reset])

  if (status !== 'authenticated') {
    return null
  }

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
        onClick={() => {
          mutation.mutate(!isFavorite)
        }}
        className={[
          'inline-flex items-center gap-1.5 rounded-lg border px-2.5 py-1.5 text-xs font-medium transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent disabled:opacity-50',
          isFavorite
            ? 'border-accent/40 bg-accent-soft text-accent-strong'
            : 'border-line text-muted hover:bg-muted-soft hover:text-ink',
        ].join(' ')}
      >
        <svg
          viewBox="0 0 16 16"
          className="size-3.5 shrink-0"
          fill={isFavorite ? 'currentColor' : 'none'}
          stroke="currentColor"
          strokeWidth="1.6"
          strokeLinecap="round"
          strokeLinejoin="round"
          aria-hidden="true"
        >
          <path d="M8 1.6 9.9 5.5l4.3.6-3.1 3 .7 4.3L8 11.5l-3.8 2 .7-4.3-3.1-3 4.3-.6Z" />
        </svg>
        <span className="hidden sm:inline">{label}</span>
      </button>
      {mutation.isError ? (
        <button
          type="button"
          id={errorId}
          role="alert"
          onClick={() => {
            reset()
          }}
          className="absolute top-full right-0 z-20 mt-1 w-52 cursor-pointer rounded-md border border-line bg-card p-2 text-left text-xs text-danger shadow-md"
        >
          {t('favorite.error')}
        </button>
      ) : null}
    </span>
  )
}
