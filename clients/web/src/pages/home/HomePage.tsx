import { useInfiniteQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router'
import {
  listMyNotebooks,
  listNotebooks,
  notebookKeys,
} from '@/entities/notebook'
import type {
  MyNotebookListFilters,
  NotebookDetails,
  NotebookSort,
  NotebookVisibility,
} from '@/entities/notebook'
import { useSessionStore } from '@/entities/session'
import { CreateNotebookDialog } from '@/features/create-notebook'
import { SearchInput } from '@/features/search-notebooks'
import { LanguageToggle } from '@/features/switch-language'
import { ThemeToggle } from '@/features/switch-theme'
import { Button, buttonClass, Container } from '@/shared/ui'
import { NotebookRows } from '@/widgets/notebook-list'
import { UserMenu } from '@/widgets/site-header'

const SORT_OPTIONS = ['UpdatedDesc', 'CreatedDesc', 'TitleAsc'] as const satisfies readonly NotebookSort[]

const SORT_LABEL: Record<NotebookSort, string> = {
  UpdatedDesc: 'home.sortRecent',
  CreatedDesc: 'home.sortCreated',
  TitleAsc: 'home.sortTitle',
}

const VISIBILITY_OPTIONS = [
  'Private',
  'Unlisted',
  'Public',
] as const satisfies readonly NotebookVisibility[]

/**
 * The shelf: notebooks front and center, no site chrome. Controls float in the
 * top-right corner; anonymous visitors get a compact masthead, signed-in
 * readers go straight to their own shelf above the public one.
 */
export function HomePage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const status = useSessionStore((state) => state.status)
  const user = useSessionStore((state) => state.user)
  const [createOpen, setCreateOpen] = useState(false)

  const onCreated = (notebook: NotebookDetails) => {
    void queryClient.invalidateQueries({ queryKey: notebookKeys.mine() })
    setCreateOpen(false)
    void navigate(`/notebooks/${encodeURIComponent(notebook.slug)}`)
  }

  return (
    <div className="relative min-h-dvh bg-canvas">
      {/* Chrome-free homepage: the controls float over the content corner. */}
      <div className="absolute top-3 right-3 z-10 flex items-center gap-1 sm:top-4 sm:right-4">
        <ThemeToggle />
        <LanguageToggle />
        {status === 'anonymous' ? (
          <Link to="/login" state={{ from: '/' }} className={buttonClass('ghost')}>
            {t('header.login')}
          </Link>
        ) : null}
        {status === 'authenticated' && user !== null ? (
          <UserMenu displayName={user.displayName} />
        ) : null}
      </div>

      <Container width="narrow" className="py-16 sm:py-24">
        {status === 'anonymous' ? (
          <header className="pb-12">
            <h1 className="font-display text-5xl tracking-tight text-ink sm:text-6xl">
              {t('brand.name')}
            </h1>
            <p className="mt-4 text-lg text-muted">{t('home.pitch')}</p>
            <div className="mt-8 flex gap-3">
              <Link to="/register" className={buttonClass('primary')}>
                {t('home.createAccount')}
              </Link>
              <Link to="/login" className={buttonClass('ghost')}>
                {t('header.login')}
              </Link>
            </div>
          </header>
        ) : null}

        {status === 'authenticated' ? (
          <MyShelf
            onCreate={() => {
              setCreateOpen(true)
            }}
          />
        ) : null}

        <PublicShelf />
      </Container>

      <CreateNotebookDialog
        open={createOpen}
        onClose={() => {
          setCreateOpen(false)
        }}
        onCreated={onCreated}
      />
    </div>
  )
}

/** Own + shared notebooks, with the filters only the authenticated list has. */
function MyShelf({ onCreate }: { onCreate: () => void }) {
  const { t } = useTranslation()
  const [sort, setSort] = useState<NotebookSort>('UpdatedDesc')
  const [favoritesOnly, setFavoritesOnly] = useState(false)
  const [visibility, setVisibility] = useState<NotebookVisibility | null>(null)

  const filters: MyNotebookListFilters = {
    search: '',
    sort,
    favoritesOnly,
    visibility,
  }

  const query = useInfiniteQuery({
    queryKey: notebookKeys.myList(filters),
    queryFn: ({ pageParam, signal }) =>
      listMyNotebooks({ search: '', sort, favoritesOnly, visibility, page: pageParam, signal }),
    initialPageParam: 1,
    getNextPageParam: (lastPage) => (lastPage.hasNextPage ? lastPage.page + 1 : undefined),
  })

  return (
    <section aria-label={t('home.mine')} className="pb-12">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-line pb-3">
        <h2 className="font-display text-2xl text-ink">{t('home.mine')}</h2>
        <Button
          variant="primary"
          onClick={onCreate}
          className="inline-flex items-center gap-1.5"
        >
          <svg
            viewBox="0 0 16 16"
            className="size-3.5"
            fill="none"
            stroke="currentColor"
            strokeWidth="1.8"
            strokeLinecap="round"
            aria-hidden="true"
          >
            <path d="M8 3v10M3 8h10" />
          </svg>
          {t('home.newNotebook')}
        </Button>
      </div>

      <div className="flex flex-wrap items-center gap-2 pt-3 pb-1">
        <div role="group" className="flex gap-1 rounded-full border border-line bg-card p-1">
          {([false, true] as const).map((value) => (
            <button
              key={String(value)}
              type="button"
              aria-pressed={favoritesOnly === value}
              onClick={() => {
                setFavoritesOnly(value)
              }}
              className={pillClass(favoritesOnly === value)}
            >
              {value ? t('home.filterFavorites') : t('home.filterAll')}
            </button>
          ))}
        </div>

        <select
          aria-label={t('createNotebook.visibility')}
          value={visibility ?? ''}
          onChange={(event) => {
            const value = event.target.value
            setVisibility(value === '' ? null : (value as NotebookVisibility))
          }}
          className="rounded-full border border-line bg-card px-3 py-1.5 text-sm text-ink focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent"
        >
          <option value="">{t('home.visibilityAll')}</option>
          {VISIBILITY_OPTIONS.map((option) => (
            <option key={option} value={option}>
              {t(`visibility.${option}`)}
            </option>
          ))}
        </select>

        <SortGroup sort={sort} onChange={setSort} />
      </div>

      <NotebookRows
        items={query.data?.pages.flatMap((page) => page.items) ?? []}
        isPending={query.isPending}
        isError={query.isError}
        onRetry={() => {
          void query.refetch()
        }}
        emptyText={t('home.emptyMine')}
        hasNextPage={query.hasNextPage}
        isFetchingNextPage={query.isFetchingNextPage}
        onLoadMore={() => {
          void query.fetchNextPage()
        }}
        showOwnership
      />
    </section>
  )
}

/** Every public notebook, anonymous-readable. */
function PublicShelf() {
  const { t } = useTranslation()
  const [search, setSearch] = useState('')
  const [sort, setSort] = useState<NotebookSort>('UpdatedDesc')

  const filters = { search, sort }

  const query = useInfiniteQuery({
    queryKey: notebookKeys.publicList(filters),
    queryFn: ({ pageParam, signal }) =>
      listNotebooks({ search, sort, page: pageParam, signal }),
    initialPageParam: 1,
    getNextPageParam: (lastPage) => (lastPage.hasNextPage ? lastPage.page + 1 : undefined),
  })

  return (
    <section aria-label={t('home.publicNotebooks')}>
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-line pb-3">
        <h2 className="font-display text-2xl text-ink">{t('home.publicNotebooks')}</h2>
        <div className="w-full sm:w-64">
          <SearchInput value={search} onChange={setSearch} />
        </div>
      </div>

      <div className="pt-3 pb-1">
        <SortGroup sort={sort} onChange={setSort} />
      </div>

      <NotebookRows
        items={query.data?.pages.flatMap((page) => page.items) ?? []}
        isPending={query.isPending}
        isError={query.isError}
        onRetry={() => {
          void query.refetch()
        }}
        emptyText={t('home.emptyPublic')}
        hasNextPage={query.hasNextPage}
        isFetchingNextPage={query.isFetchingNextPage}
        onLoadMore={() => {
          void query.fetchNextPage()
        }}
      />
    </section>
  )
}

function SortGroup({ sort, onChange }: { sort: NotebookSort; onChange: (sort: NotebookSort) => void }) {
  const { t } = useTranslation()
  return (
    <div role="group" className="flex gap-1 rounded-full border border-line bg-card p-1">
      {SORT_OPTIONS.map((option) => (
        <button
          key={option}
          type="button"
          aria-pressed={option === sort}
          onClick={() => {
            onChange(option)
          }}
          className={pillClass(option === sort)}
        >
          {t(SORT_LABEL[option])}
        </button>
      ))}
    </div>
  )
}

function pillClass(active: boolean): string {
  return [
    'rounded-full px-3 py-1 text-sm transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent',
    active ? 'bg-accent-soft text-accent-strong' : 'text-muted hover:text-ink',
  ].join(' ')
}
