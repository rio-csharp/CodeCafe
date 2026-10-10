import { useInfiniteQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, Navigate, useLocation } from 'react-router'
import { pageKeys, searchPages, toPageHref } from '@/entities/page'
import type { PageSearchHit } from '@/entities/page'
import { useSessionStore } from '@/entities/session'
import { SearchInput } from '@/features/search-notebooks'
import { Button, Container, Spinner } from '@/shared/ui'

/**
 * Full-text search across every page the signed-in user can read. Anonymous
 * visitors are sent to login (the endpoint is auth-only) with a breadcrumb in
 * `location.state.from` so they land back here.
 */
export function SearchPage() {
  const { t } = useTranslation()
  const location = useLocation()
  const status = useSessionStore((state) => state.status)
  // `query` is the committed (debounced) term; the input owns the raw text.
  const [query, setQuery] = useState('')
  const trimmed = query.trim()

  const results = useInfiniteQuery({
    queryKey: pageKeys.search(trimmed),
    queryFn: ({ pageParam, signal }) =>
      searchPages({ query: trimmed, cursor: pageParam, signal }),
    initialPageParam: null as string | null,
    getNextPageParam: (last) => last.nextCursor,
    enabled: status === 'authenticated' && trimmed.length > 0,
  })

  if (status === 'anonymous') {
    return <Navigate to="/login" state={{ from: location.pathname }} replace />
  }

  const hits = results.data?.pages.flatMap((page) => page.items) ?? []

  return (
    <div className="min-h-dvh bg-canvas">
      <Container width="narrow" className="py-12 sm:py-16">
        <Link
          to="/"
          className="inline-flex items-center gap-1.5 text-sm text-muted transition-colors hover:text-ink"
        >
          <svg
            viewBox="0 0 16 16"
            className="size-3.5"
            fill="none"
            stroke="currentColor"
            strokeWidth="1.6"
            strokeLinecap="round"
            strokeLinejoin="round"
            aria-hidden="true"
          >
            <path d="M9.5 3.5 5 8l4.5 4.5" />
          </svg>
          {t('trash.backHome')}
        </Link>

        <h1 className="mt-6 mb-6 border-b border-line pb-4 text-xl font-semibold text-ink">
          {t('pageSearch.title')}
        </h1>

        <SearchInput
          value={query}
          onChange={setQuery}
          label={t('pageSearch.label')}
          placeholder={t('pageSearch.placeholder')}
        />

        <div className="mt-8">
          {trimmed.length === 0 ? (
            <p className="py-12 text-center text-sm text-muted">{t('pageSearch.idle')}</p>
          ) : results.isPending || status !== 'authenticated' ? (
            <div className="grid place-items-center py-16">
              <Spinner />
              <span className="sr-only">{t('list.loading')}</span>
            </div>
          ) : results.isError ? (
            <div className="py-12 text-center">
              <p className="text-sm text-muted">{t('list.loadError')}</p>
              <Button
                variant="ghost"
                className="mt-3"
                onClick={() => {
                  void results.refetch()
                }}
              >
                {t('list.retry')}
              </Button>
            </div>
          ) : hits.length === 0 ? (
            <p className="py-12 text-center text-sm text-muted">{t('pageSearch.empty')}</p>
          ) : (
            <>
              <ul className="flex flex-col gap-2">
                {hits.map((hit) => (
                  <SearchHitRow key={hit.pageId} hit={hit} />
                ))}
              </ul>
              {results.hasNextPage ? (
                <div className="mt-6 text-center">
                  <Button
                    variant="ghost"
                    disabled={results.isFetchingNextPage}
                    onClick={() => {
                      void results.fetchNextPage()
                    }}
                  >
                    {results.isFetchingNextPage ? t('list.loading') : t('list.loadMore')}
                  </Button>
                </div>
              ) : null}
            </>
          )}
        </div>
      </Container>
    </div>
  )
}

function SearchHitRow({ hit }: { hit: PageSearchHit }) {
  return (
    <li className="rounded-xl border border-line bg-card px-4 py-3 transition-colors hover:border-accent/40">
      <p className="text-xs text-muted">{hit.notebookTitle}</p>
      <Link
        to={toPageHref(hit.notebookSlug, hit.path)}
        className="mt-0.5 block truncate text-sm font-medium text-ink transition-colors hover:text-accent-strong focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent"
      >
        {hit.title}
      </Link>
      {hit.snippet.length > 0 ? (
        <p className="mt-1 line-clamp-2 text-xs text-muted">{hit.snippet}</p>
      ) : null}
    </li>
  )
}
