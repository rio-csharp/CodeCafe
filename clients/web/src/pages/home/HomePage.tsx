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
import { NotebookGrid } from '@/widgets/notebook-list'
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
 * top-right corner; a soft warm glow behind the masthead is the only flourish.
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
      {/* One warm wash, strongest at the top and gone by the first shelf. */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-x-0 top-0 h-[22rem] bg-[radial-gradient(65%_100%_at_35%_0%,var(--color-accent-soft),transparent_75%)]"
      />

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

      <Container width="standard" className="relative py-16 sm:py-24">
        {status === 'anonymous' ? (
          <header className="pb-14">
            <p className="text-xs font-semibold tracking-[0.2em] text-accent-strong uppercase">
              {t('home.overline')}
            </p>
            <h1 className="mt-3 font-display text-5xl tracking-tight text-ink sm:text-6xl">
              {t('brand.name')}
            </h1>
            <p className="mt-4 max-w-md text-lg leading-relaxed text-muted">{t('home.pitch')}</p>
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

        {status === 'authenticated' && user !== null ? (
          <>
            <p className="pb-6 text-sm text-muted">{greeting(t, user.displayName)}</p>
            <MyShelf
              onCreate={() => {
                setCreateOpen(true)
              }}
            />
          </>
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

/** A small time-of-day hello — the one warm touch, no fanfare. */
function greeting(t: (key: string, options?: Record<string, unknown>) => string, name: string): string {
  const hour = new Date().getHours()
  const key = hour < 6 ? 'night' : hour < 12 ? 'morning' : hour < 18 ? 'afternoon' : 'evening'
  return t(`home.greeting.${key}`, { name })
}

/** Section header: quiet label, hairline rule, controls right-aligned. */
function ShelfHeader({ title, children }: { title: string; children?: React.ReactNode }) {
  return (
    <div className="flex items-center gap-3 pb-4">
      <h2 className="shrink-0 text-sm font-semibold text-ink">{title}</h2>
      <div aria-hidden="true" className="h-px min-w-4 flex-1 bg-line" />
      <div className="flex shrink-0 items-center gap-2">{children}</div>
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
      <ShelfHeader title={t('home.mine')}>
        <SortGroup sort={sort} onChange={setSort} />
        <Button variant="primary" onClick={onCreate} className="inline-flex items-center gap-1.5">
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
      </ShelfHeader>

      <div className="flex flex-wrap items-center gap-2 pb-2">
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
      </div>

      <NotebookGrid
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
      <ShelfHeader title={t('home.publicNotebooks')}>
        <SortGroup sort={sort} onChange={setSort} />
      </ShelfHeader>

      <div className="pb-5">
        <SearchInput value={search} onChange={setSearch} />
      </div>

      <NotebookGrid
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
    'rounded-full px-3 py-1 text-xs transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent',
    active ? 'bg-accent-soft text-accent-strong' : 'text-muted hover:text-ink',
  ].join(' ')
}
