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
import { Button, buttonClass, Container } from '@/shared/ui'
import { EmptyShelf, NotebookGrid } from '@/widgets/notebook-list'
import { SiteHeader } from '@/widgets/site-header'
import { SiteFooter } from '@/widgets/site-footer'

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
 * Public discovery and a signed-in bookshelf share one search/sort surface.
 * Filters always apply to the selected shelf; anonymous readers browse public notebooks.
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
    <div className="flex min-h-dvh flex-col bg-canvas">
      <a href="#notebook-shelf" className="skip-link">{t('home.skipToShelf')}</a>
      <SiteHeader />
      <main className="flex-1">
      <Container width="standard" className="pb-16 pt-8 sm:pt-12">
        <header className={`shelf-intro ${signedIn ? 'shelf-intro-compact' : ''}`}>
          <div className="relative z-10 max-w-xl">
            <p className="mb-4 flex items-center gap-2 text-xs font-semibold tracking-[0.18em] text-accent-strong uppercase">
              <span aria-hidden="true" className="h-px w-8 bg-accent" />
              {t(signedIn ? 'home.workspace' : 'home.overline')}
            </p>
            <h1 className="font-display text-4xl leading-tight tracking-tight text-ink sm:text-6xl">
              {signedIn && user !== null ? greeting(t, user.displayName) : t('brand.name')}
            </h1>
            <p className="mt-5 max-w-md text-base leading-relaxed text-muted sm:text-lg">
              {t(signedIn ? 'home.workspaceHint' : 'home.pitch')}
            </p>
            {status === 'anonymous' ? (
              <div className="mt-7 flex flex-wrap items-center gap-4">
                <Link to="/register" className={buttonClass('primary')}>
                  {t('home.createAccount')} <svg aria-hidden="true" viewBox="0 0 16 16" className="size-4" fill="none" stroke="currentColor" strokeWidth="1.5"><path d="M4 12 12 4M4 4h8v8" /></svg>
                </Link>
                <a href="#notebook-shelf" className="text-sm font-medium text-accent-strong underline-offset-4 hover:underline">
                  {t('home.explore')} <span aria-hidden="true">↓</span>
                </a>
              </div>
            ) : null}
          </div>
          <div aria-hidden="true" className="shelf-illustration">
            <div className="notebook-cover">
              <span className="text-[10px] font-semibold tracking-[0.22em] uppercase">CodeCafe / Notes</span>
              <svg viewBox="0 0 80 64" className="my-7 h-16 w-20" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M15 25h39v17a15 15 0 0 1-15 15h-9a15 15 0 0 1-15-15V25ZM54 28h7a9 9 0 0 1 0 18h-7M10 61h51M27 16c-8-8 8-8 0-16M40 16c-8-8 8-8 0-16" />
              </svg>
              <span className="font-display text-3xl leading-tight">{t('home.coverTitle')}</span>
              <span className="mt-6 block border-t border-current/30 pt-3 text-[10px] tracking-widest uppercase">{t('home.coverCaption')}</span>
            </div>
            <span className="notebook-bookmark" />
          </div>
        </header>

        <section id="notebook-shelf" aria-label={t('home.shelfLabel')} className="scroll-mt-24 border-t border-line pt-8" tabIndex={-1}>
        <div className="mb-6 flex flex-col gap-5 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <p className="text-xs font-medium tracking-widest text-muted uppercase">{t('home.collection')}</p>
            <h2 className="mt-1 font-display text-2xl text-ink sm:text-3xl">{t(signedIn ? 'home.yourShelf' : 'home.publicNotebooks')}</h2>
          </div>
          <div className="w-full sm:max-w-sm">
            <SearchInput value={search} onChange={setSearch} />
          </div>
        </div>

        {signedIn ? (
          <div className="flex flex-wrap items-center justify-between gap-4">
            <div
              role="tablist"
              aria-label={t('home.shelfLabel')}
              className="inline-flex gap-1 rounded-full border border-line bg-card p-1"
            >
              {(['favorites', 'mine', 'public'] as const).map((option) => (
                <button
                  key={option}
                  type="button"
                  role="tab"
                  id={`shelf-tab-${option}`}
                  aria-controls="shelf-panel"
                  aria-selected={activeTab === option}
                  tabIndex={activeTab === option ? 0 : -1}
                  onKeyDown={(event) => {
                    const tabs = ['favorites', 'mine', 'public'] as const
                    const current = tabs.indexOf(option)
                    const next = event.key === 'ArrowRight' ? (current + 1) % tabs.length
                      : event.key === 'ArrowLeft' ? (current + tabs.length - 1) % tabs.length
                        : event.key === 'Home' ? 0 : event.key === 'End' ? tabs.length - 1 : -1
                    if (next < 0) return
                    event.preventDefault()
                    setTab(tabs[next])
                    document.getElementById(`shelf-tab-${tabs[next]}`)?.focus()
                  }}
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

            <div className="hidden items-center gap-2 sm:flex">
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

        <div className="my-5 flex flex-wrap items-center justify-start gap-2">
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

        <div id="shelf-panel" role={signedIn ? 'tabpanel' : undefined} aria-labelledby={signedIn ? `shelf-tab-${activeTab}` : undefined}>
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

        </section>

        {/* On phones the corner cluster hides; create becomes a floating button
            and trash moves into the account menu. */}
        {signedIn ? (
          <button
            type="button"
            aria-label={t('home.newNotebook')}
            title={t('home.newNotebook')}
            onClick={() => {
              setCreateOpen(true)
            }}
            className="fixed right-5 bottom-6 z-40 grid size-12 place-items-center rounded-full bg-accent text-on-accent shadow-lg transition-transform hover:scale-105 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent-strong sm:hidden"
          >
            <svg viewBox="0 0 16 16" className="size-5" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" aria-hidden="true">
              <path d="M8 3v10M3 8h10" />
            </svg>
          </button>
        ) : null}
      </Container>
      </main>
      <SiteFooter />

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

/** The favorites filter also includes starred public notebooks. */
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
      showOwnership={!favoritesOnly}
      onOpenSettings={onOpenSettings}
      onShareNotebook={onShareNotebook}
    />
  )
}

/** Every public notebook, anonymous-readable. */
function PublicShelf({ search, sort }: { search: string; sort: NotebookSort }) {
  const { t } = useTranslation()
  const userId = useSessionStore((state) => state.user?.id ?? null)

  const query = useInfiniteQuery({
    queryKey: [...notebookKeys.publicList({ search, sort }), userId],
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
    <div role="group" aria-label={t('home.sortLabel')} className="flex gap-1 rounded-full border border-line bg-card p-1">
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
