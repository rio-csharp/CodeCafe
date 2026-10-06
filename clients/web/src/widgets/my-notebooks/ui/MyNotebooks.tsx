import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { NotebookCard, listMyNotebooks, notebookKeys } from '@/entities/notebook'
import { useSessionStore } from '@/entities/session'
import { Button, Container } from '@/shared/ui'

const SKELETON_KEYS = ['a', 'b', 'c'] as const

/**
 * The signed-in shelf: own + shared notebooks, first page only. Search, sort and
 * load-more belong to the dashboard milestone, so the catalog keeps them.
 */
export function MyNotebooks() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const status = useSessionStore((state) => state.status)
  const authenticated = status === 'authenticated'

  const query = useQuery({
    queryKey: notebookKeys.mine(),
    queryFn: ({ signal }) => listMyNotebooks({ page: 1, signal }),
    enabled: authenticated,
  })

  // Anonymous visitors must see exactly the M1/M2 homepage: nothing here, not
  // even an empty section.
  if (!authenticated) {
    return null
  }

  const items = query.data?.items ?? []

  return (
    <section className="pb-12">
      <Container>
        <h2 className="font-display text-2xl text-ink">{t('myNotebooks.title')}</h2>

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
                    void queryClient.resetQueries({ queryKey: notebookKeys.mine() })
                  }}
                >
                  {t('catalog.error.retry')}
                </Button>
              </div>
            </div>
          ) : null}

          {!query.isPending && !query.isError && items.length === 0 ? (
            <div className="rounded-2xl border border-line bg-card p-8 text-center">
              <h3 className="font-display text-xl text-ink">{t('myNotebooks.empty')}</h3>
            </div>
          ) : null}

          {!query.isPending && !query.isError && items.length > 0 ? (
            <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
              {items.map((notebook) => (
                <NotebookCard key={notebook.id} notebook={notebook} />
              ))}
            </div>
          ) : null}
        </div>
      </Container>
    </section>
  )
}
