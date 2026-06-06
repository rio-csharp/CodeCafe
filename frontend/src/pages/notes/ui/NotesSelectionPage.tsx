import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Search, Plus, ArrowRight, Coffee } from 'lucide-react'
import type { Notebook } from '@/entities/notebook'
import { useLayout } from '@/shared/model/layoutContext'
import { useDebounce } from '@/shared/hooks/useDebounce'
import { usePublicNotes, useMyNotes } from '@/features/search-notebooks'
import NotebookCard, { SkeletonGrid } from '@/widgets/notebook-card'
import SectionHeader from '@/widgets/section-header'
import { useTranslation } from 'react-i18next'


interface NotebookGridProps {
  notebooks: Notebook[] | undefined
  isPending: boolean
  isError: boolean
  errorMessage: string
  emptyMessage: string
  showVisibility?: boolean
}

function NotebookGrid({ notebooks, isPending, isError, errorMessage, emptyMessage, showVisibility }: NotebookGridProps) {
  if (isPending) return <SkeletonGrid />
  if (isError) return <p className="text-sm text-status-error">{errorMessage}</p>
  if (!notebooks?.length) return <p className="text-sm text-text-tertiary">{emptyMessage}</p>
  return (
    <div className="grid gap-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
      {notebooks.map((nb) => (
        <NotebookCard key={nb.id} notebook={nb} showVisibility={showVisibility} />
      ))}
    </div>
  )
}

export default function NotesSelectionPage() {
  const { user } = useLayout()
  const isAuthenticated = !!user
  const [searchInput, setSearchInput] = useState('')
  const debouncedSearch = useDebounce(searchInput, 300)
  const { t } = useTranslation()

  const {
    data: publicNotes,
    isPending: publicPending,
    isError: publicError,
  } = usePublicNotes(debouncedSearch)

  const {
    data: myNotes,
    isPending: myPending,
    isError: myError,
  } = useMyNotes(debouncedSearch)

  return (
    <div className="pt-24 pb-20 lg:pt-32 lg:pb-24">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-10">
          <h1 className="text-3xl font-bold text-text-primary">{t('notes.title')}</h1>
          <div className="flex items-center gap-3">
            <label className="relative block flex-1 sm:flex-none">
              <span className="sr-only">{t('notes.searchPlaceholder')}</span>
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-text-tertiary" />
              <input
                type="text"
                placeholder={t('notes.searchPlaceholder')}
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
                className="pl-9 pr-4 py-2 rounded-lg border border-border-default text-sm outline-none focus:border-border-hover w-full sm:w-64 bg-surface text-text-primary placeholder:text-text-tertiary"
              />
            </label>
          </div>
        </div>

        <section>
          <SectionHeader
            title={t('notes.publicTitle')}
            description={t('notes.publicDesc')}
            action={
              <Link
                to="/notes"
                className="hidden sm:inline-flex items-center gap-1 text-sm text-text-secondary hover:text-text-primary transition-colors"
              >
                {t('notes.viewAll')} <ArrowRight className="h-3.5 w-3.5" />
              </Link>
            }
          />
          <NotebookGrid
            notebooks={publicNotes}
            isPending={publicPending}
            isError={publicError}
            errorMessage={t('notes.loadPublicError')}
            emptyMessage={t('notes.noPublic')}
          />
        </section>

        {!isAuthenticated && (
          <div className="mt-10 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 rounded-xl border border-border-default bg-surface-elevated px-5 sm:px-6 py-5">
            <div className="flex items-center gap-4">
              <Coffee className="h-8 w-8 text-brand-brown shrink-0" />
              <div>
                <p className="text-sm font-semibold text-text-primary">{t('notes.signInBanner')}</p>
                <p className="text-sm text-text-secondary">{t('notes.signInBannerDesc')}</p>
              </div>
            </div>
            <Link
              to="/login"
              className="inline-flex items-center gap-1 rounded-lg bg-brand-brown px-5 py-2 text-sm font-medium text-text-inverse hover:opacity-90 transition-opacity shrink-0"
            >
              {t('notes.signIn')} <ArrowRight className="h-4 w-4" />
            </Link>
          </div>
        )}

        {isAuthenticated && (
          <section className="mt-14">
            <SectionHeader
              title={t('notes.myTitle')}
              description={t('notes.myDesc')}
              action={
                <Link
                  to="/notes/new"
                  data-testid="new-notebook-button"
                  className="inline-flex items-center gap-1 rounded-lg bg-brand-brown px-4 py-2 text-sm font-medium text-text-inverse hover:opacity-90 transition-opacity"
                >
                  <Plus className="h-4 w-4" />
                  {t('notes.newNotebook')}
                </Link>
              }
            />
            <NotebookGrid
              notebooks={myNotes}
              isPending={myPending}
              isError={myError}
              errorMessage={t('notes.loadMyError')}
              emptyMessage={t('notes.noMine')}
              showVisibility
            />

            <div className="mt-8 flex items-center justify-center gap-2 text-xs text-text-tertiary">
              <span className="inline-block h-4 w-4 rounded-full border border-border-hover text-center leading-4">i</span>
              {t('notes.visibilityTip')}
            </div>
          </section>
        )}
      </div>
    </div>
  )
}
