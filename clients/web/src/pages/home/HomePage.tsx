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
  NotebookDetails,
  NotebookSummary,
  NotebookSort,
  NotebookVisibility,
} from '@/entities/notebook'
import { useSessionStore } from '@/entities/session'
import { CreateNotebookDialog } from '@/features/create-notebook'
import { NotebookSettingsDialog } from '@/features/manage-notebook'
import { ShareNotebookDialog } from '@/features/share-notebook'
import { SearchInput } from '@/features/search-notebooks'
import { LanguageToggle } from '@/features/switch-language'
import { ThemeToggle } from '@/features/switch-theme'
import { Button, buttonClass, Container } from '@/shared/ui'
import { EmptyShelf, NotebookGrid } from '@/widgets/notebook-list'
import { UserMenu } from '@/widgets/site-header'

type ShelfTab = 'favorites' | 'mine' | 'public'

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
 * The shelf: one centered search box, three tabs under it, cards below. The
 * search and sort always act on the tab being looked at. Anonymous visitors
 * get the public tab without the strip.
 */
export function HomePage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const status = useSessionStore((state) => state.status)
  const user = useSessionStore((state) => state.user)
  const [tab, setTab] = useState<ShelfTab>('mine')
  const [search, setSearch] = useState('')
  const [sort, setSort] = useState<NotebookSort>('UpdatedDesc')
  const [visibility, setVisibility] = useState<NotebookVisibility | null>(null)
  const [createOpen, setCreateOpen] = useState(false)
  const [settingsSlug, setSettingsSlug] = useState<string | null>(null)
  const [shareSlug, setShareSlug] = useState<string | null>(null)

  const signedIn = status === 'authenticated'
  const activeTab: ShelfTab = signedIn ? tab : 'public'

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
        {signedIn && user !== null ? <UserMenu displayName={user.displayName} /> : null}
      </div>

      <Container width="standard" className="relative py-16 sm:py-20">
        {status === 'anonymous' ? (
          <header className="pb-12 text-center">
            <p className="text-xs font-semibold tracking-[0.2em] text-accent-strong uppercase">
              {t('home.overline')}
            </p>
            <h1 className="mt-3 text-5xl font-semibold tracking-tight text-ink sm:text-6xl">
              {t('brand.name')}
            </h1>
            <p className="mx-auto mt-4 max-w-md text-lg leading-relaxed text-muted">
              {t('home.pitch')}
            </p>
          </header>
        ) : null}

        {signedIn && user !== null ? (
          <p className="pb-6 text-center text-sm text-muted">
            {greeting(t, user.displayName)}
          </p>
        ) : null}

        {/* The one search box; it filters whichever tab is showing. */}
        <div className="mx-auto max-w-2xl">
          <SearchInput value={search} onChange={setSearch} />
        </div>

        {signedIn ? (
          <div className="relative mt-6 flex items-center justify-center">
            <div
              role="tablist"
              className="inline-flex gap-1 rounded-full border border-line bg-card p-1"
            >
              {(['favorites', 'mine', 'public'] as const).map((option) => (
                <button
                  key={option}
                  type="button"
                  role="tab"
                  aria-selected={activeTab === option}
                  onClick={() => {
                    setTab(option)
                  }}
                  className={pillClass(activeTab === option)}
                >
                  {t(
                    option === 'favorites'
                      ? 'home.favorites'
                      : option === 'mine'
                        ? 'home.mine'
                        : 'home.publicNotebooks',
                  )}
                </button>
              ))}
            </div>

            <div className="absolute right-0 hidden items-center gap-1 sm:flex">
              <Link
                to="/trash"
                aria-label={t('trash.entry')}
                title={t('trash.entry')}
                className="grid size-9 place-items-center rounded-full text-muted transition-colors hover:bg-muted-soft hover:text-ink focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent"
              >
                <svg
                  viewBox="0 0 16 16"
                  className="size-4"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="1.5"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  aria-hidden="true"
                >
                  <path d="M2.5 4h11M6.5 4V2.5h3V4M3.5 4l.7 9a1 1 0 0 0 1 .9h5.6a1 1 0 0 0 1-.9l.7-9M6.5 7v4M9.5 7v4" />
                </svg>
              </Link>
              <Button
                variant="primary"
                size="sm"
                onClick={() => {
                  setCreateOpen(true)
                }}
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
          </div>
        ) : null}

        <div className="mt-5 flex flex-wrap items-center justify-center gap-2 sm:justify-end">
          <SortGroup sort={sort} onChange={setSort} />
          {activeTab === 'mine' ? (
            <select
              aria-label={t('createNotebook.visibility')}
              value={visibility ?? ''}
              onChange={(event) => {
                const value = event.target.value
                setVisibility(value === '' ? null : (value as NotebookVisibility))
              }}
              className="h-9 rounded-full border border-line bg-card px-3 text-xs text-ink focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent"
            >
              <option value="">{t('home.visibilityAll')}</option>
              {VISIBILITY_OPTIONS.map((option) => (
                <option key={option} value={option}>
                  {t(`visibility.${option}`)}
                </option>
              ))}
            </select>
          ) : null}
        </div>

        <div className="mt-5">
          {activeTab === 'public' ? (
            <PublicShelf search={search} sort={sort} />
          ) : (
            <MineShelf
              search={search}
              sort={sort}
              favoritesOnly={activeTab === 'favorites'}
              visibility={activeTab === 'mine' ? visibility : null}
              onOpenSettings={(notebook) => {
                setSettingsSlug(notebook.slug)
              }}
              onShareNotebook={(notebook) => {
                setShareSlug(notebook.slug)
              }}
              onCreateClick={() => {
                setCreateOpen(true)
              }}
            />
          )}
        </div>

        {/* On phones the corner cluster hides; this row takes over. */}
        {signedIn ? (
          <div className="mt-8 flex items-center justify-center gap-3 sm:hidden">
            <Button
              variant="primary"
              onClick={() => {
                setCreateOpen(true)
              }}
            >
              {t('home.newNotebook')}
            </Button>
            <Link to="/trash" className={buttonClass('ghost')}>
              {t('trash.entry')}
            </Link>
          </div>
        ) : null}
      </Container>

      <NotebookSettingsDialog
        slug={settingsSlug}
        onClose={() => {
          setSettingsSlug(null)
        }}
      />

      <ShareNotebookDialog
        slug={shareSlug}
        onClose={() => {
          setShareSlug(null)
        }}
      />

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

/** Own + shared notebooks; the favorites tab is this same query, starred. */
function MineShelf({
  search,
  sort,
  favoritesOnly,
  visibility,
  onOpenSettings,
  onShareNotebook,
  onCreateClick,
}: {
  search: string
  sort: NotebookSort
  favoritesOnly: boolean
  visibility: NotebookVisibility | null
  onOpenSettings: (notebook: NotebookSummary) => void
  onShareNotebook: (notebook: NotebookSummary) => void
  onCreateClick: () => void
}) {
  const { t } = useTranslation()

  const query = useInfiniteQuery({
    queryKey: notebookKeys.myList({ search, sort, favoritesOnly, visibility }),
    queryFn: ({ pageParam, signal }) =>
      listMyNotebooks({ search, sort, favoritesOnly, visibility, page: pageParam, signal }),
    initialPageParam: 1,
    getNextPageParam: (lastPage) => (lastPage.hasNextPage ? lastPage.page + 1 : undefined),
  })

  return (
    <NotebookGrid
      items={query.data?.pages.flatMap((page) => page.items) ?? []}
      isPending={query.isPending}
      isError={query.isError}
      onRetry={() => {
        void query.refetch()
      }}
      emptyText={favoritesOnly ? t('home.emptyFavorites') : t('home.emptyMine')}
      emptyState={
        search.trim().length > 0 ? (
          <EmptyShelf
            icon={<SearchGlyph />}
            title={t('home.emptySearch', { query: search.trim() })}
          />
        ) : favoritesOnly ? (
          <EmptyShelf
            icon={<StarGlyph />}
            title={t('home.emptyFavorites')}
            hint={t('home.emptyFavoritesHint')}
          />
        ) : (
          <EmptyShelf
            icon={<NotebookGlyph />}
            title={t('home.emptyMine')}
            hint={t('home.emptyMineHint')}
            action={
              <Button size="sm" onClick={onCreateClick}>
                {t('home.newNotebook')}
              </Button>
            }
          />
        )
      }
      hasNextPage={query.hasNextPage}
      isFetchingNextPage={query.isFetchingNextPage}
      onLoadMore={() => {
        void query.fetchNextPage()
      }}
      showOwnership
      onOpenSettings={onOpenSettings}
      onShareNotebook={onShareNotebook}
    />
  )
}

/** Every public notebook, anonymous-readable. */
function PublicShelf({ search, sort }: { search: string; sort: NotebookSort }) {
  const { t } = useTranslation()

  const query = useInfiniteQuery({
    queryKey: notebookKeys.publicList({ search, sort }),
    queryFn: ({ pageParam, signal }) =>
      listNotebooks({ search, sort, page: pageParam, signal }),
    initialPageParam: 1,
    getNextPageParam: (lastPage) => (lastPage.hasNextPage ? lastPage.page + 1 : undefined),
  })

  return (
    <NotebookGrid
      items={query.data?.pages.flatMap((page) => page.items) ?? []}
      isPending={query.isPending}
      isError={query.isError}
      onRetry={() => {
        void query.refetch()
      }}
      emptyText={t('home.emptyPublic')}
      emptyState={
        search.trim().length > 0 ? (
          <EmptyShelf
            icon={<SearchGlyph />}
            title={t('home.emptySearch', { query: search.trim() })}
          />
        ) : undefined
      }
      hasNextPage={query.hasNextPage}
      isFetchingNextPage={query.isFetchingNextPage}
      onLoadMore={() => {
        void query.fetchNextPage()
      }}
    />
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

/* Small line glyphs for the empty shelves. */
function StarGlyph() {
  return (
    <svg viewBox="0 0 20 20" className="size-5" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round" aria-hidden="true">
      <path d="M10 2.5 12.3 7.2l5.2.8-3.75 3.65.9 5.15L10 14.4l-4.65 2.4.9-5.15L2.5 8l5.2-.8Z" />
    </svg>
  )
}

function NotebookGlyph() {
  return (
    <svg viewBox="0 0 20 20" className="size-5" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M5.5 2.5h9a1 1 0 0 1 1 1v13a1 1 0 0 1-1 1h-9a1 1 0 0 1-1-1v-13a1 1 0 0 1 1-1ZM8 2.5v15" />
    </svg>
  )
}

function SearchGlyph() {
  return (
    <svg viewBox="0 0 20 20" className="size-5" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" aria-hidden="true">
      <path d="M13.5 13.5 17 17M9 15A6 6 0 1 0 9 3a6 6 0 0 0 0 12Z" />
    </svg>
  )
}
