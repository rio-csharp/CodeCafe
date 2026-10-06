import { useInfiniteQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { NotebookCard, listNotebooks, notebookKeys } from '@/entities/notebook'
import type { NotebookSortOption } from '@/entities/notebook'
import { Button, Container, Spinner } from '@/shared/ui'

const SORT_OPTIONS = ['UpdatedDesc', 'TitleAsc'] as const satisfies readonly NotebookSortOption[]

const SORT_LABEL: Record<NotebookSortOption, string> = {
  UpdatedDesc: 'catalog.sort.recent',
  TitleAsc: 'catalog.sort.title',
}

const SKELETON_KEYS = ['a', 'b', 'c', 'd', 'e', 'f'] as const

export interface NotebookCatalogProps {
  search: string
}

export function NotebookCatalog({ search }: NotebookCatalogProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [sort, setSort] = useState<NotebookSortOption>('UpdatedDesc')

  const filters = { search, sort }

  const query = useInfiniteQuery({
    queryKey: notebookKeys.publicList(filters),
    queryFn: ({ pageParam, signal }) =>
      listNotebooks({ search, sort, page: pageParam, signal }),
    initialPageParam: 1,
    getNextPageParam: (lastPage) => (lastPage.hasNextPage ? lastPage.page + 1 : undefined),
  })

  const items = query.data?.pages.flatMap((page) => page.items) ?? []

  return (
    <section className="pb-16">
      <Container>
        <div className="flex flex-wrap items-center justify-between gap-3">
          <h2 className="font-display text-2xl text-ink">{t('catalog.title')}</h2>

          <div
            role="group"
            aria-label={t('catalog.sort.label')}
            className="flex gap-1 rounded-full border border-line bg-card p-1"
          >
            {SORT_OPTIONS.map((option) => {
              const active = option === sort
              return (
                <button
                  key={option}
                  type="button"
                  aria-pressed={active}
                  onClick={() => {
                    setSort(option)
                  }}
                  className={[
                    'rounded-full px-4 py-1.5 text-sm transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent',
                    active ? 'bg-accent text-card' : 'text-ink hover:text-accent-strong',
                  ].join(' ')}
                >
                  {t(SORT_LABEL[option])}
                </button>
              )
            })}
          </div>
        </div>

        <div className="mt-6">
          {query.isPending ? (
            <div
              aria-busy="true"
              aria-live="polite"
              className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3"
            >
              <span className="sr-only">{t('catalog.loading')}</span>
              {SKELETON_KEYS.map((key) => (
                <div
                  key={key}
                  className="animate-pulse rounded-2xl border border-line bg-card p-5"
                >
                  <div className="h-5 w-2/3 rounded bg-line" />
                  <div className="mt-4 h-3 w-full rounded bg-line" />
                  <div className="mt-2 h-3 w-4/5 rounded bg-line" />
                  <div className="mt-6 h-3 w-1/2 rounded bg-line" />
                </div>
              ))}
            </div>
          ) : null}

          {query.isError ? (
            <div className="rounded-2xl border border-line bg-card p-8 text-center">
              <h3 className="font-display text-xl text-ink">{t('catalog.error.title')}</h3>
              <div className="mt-4">
                <Button
                  onClick={() => {
                    void queryClient.resetQueries({ queryKey: notebookKeys.publicList(filters) })
                  }}
                >
                  {t('catalog.error.retry')}
                </Button>
              </div>
            </div>
          ) : null}

          {!query.isPending && !query.isError && items.length === 0 ? (
            <div className="rounded-2xl border border-line bg-card p-8 text-center">
              <h3 className="font-display text-xl text-ink">{t('catalog.empty.title')}</h3>
              <p className="mt-2 text-sm text-muted">{t('catalog.empty.body')}</p>
            </div>
          ) : null}

          {!query.isPending && !query.isError && items.length > 0 ? (
            <>
              <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
                {items.map((notebook) => (
                  <NotebookCard key={notebook.id} notebook={notebook} />
                ))}
              </div>

              {query.hasNextPage ? (
                <div className="mt-8 flex justify-center">
                  <Button
                    variant="ghost"
                    disabled={query.isFetchingNextPage}
                    onClick={() => {
                      void query.fetchNextPage()
                    }}
                  >
                    {query.isFetchingNextPage ? <Spinner /> : null}
                    {t('catalog.loadMore')}
                  </Button>
                </div>
              ) : null}
            </>
          ) : null}
        </div>
      </Container>
    </section>
  )
}
