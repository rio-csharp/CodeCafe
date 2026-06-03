import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Search, Plus, ArrowRight, Coffee } from 'lucide-react'
import { useLayout } from '@/shared/model/layoutContext'
import { useDebounce } from '@/shared/hooks/useDebounce'
import { usePublicNotes, useMyNotes } from '@/features/search-notebooks'
import NotebookCard, { SkeletonGrid } from '@/widgets/notebook-card'
import SectionHeader from '@/widgets/section-header'


export default function NotesSelectionPage() {
  const { user } = useLayout()
  const isAuthenticated = !!user
  const [searchInput, setSearchInput] = useState('')
  const debouncedSearch = useDebounce(searchInput, 300)

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
      <div className="mx-auto max-w-7xl px-6 lg:px-8">
        <div className="flex items-center justify-between mb-10">
          <h1 className="text-3xl font-bold text-text-primary">Notebooks</h1>
          <div className="hidden sm:flex items-center gap-3">
            <label className="relative block">
              <span className="sr-only">Search notebooks</span>
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-text-tertiary" />
              <input
                type="text"
                placeholder="Search notebooks..."
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
                className="pl-9 pr-4 py-2 rounded-lg border border-border-default text-sm outline-none focus:border-border-hover w-64"
              />
            </label>
          </div>
        </div>

        <section>
          <SectionHeader
            title="Public notebooks"
            description="Explore notebooks the community has chosen to publish."
            action={
              <Link
                to="/notes"
                className="hidden sm:inline-flex items-center gap-1 text-sm text-text-secondary hover:text-text-primary transition-colors"
              >
                View all <ArrowRight className="h-3.5 w-3.5" />
              </Link>
            }
          />
          {publicPending ? (
            <SkeletonGrid />
          ) : publicError ? (
            <p className="text-sm text-status-error">Failed to load public notebooks.</p>
          ) : !publicNotes?.length ? (
            <p className="text-sm text-text-tertiary">No public notebooks yet.</p>
          ) : (
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
              {publicNotes.map((nb) => (
                <NotebookCard key={nb.id} notebook={nb} />
              ))}
            </div>
          )}
        </section>

        {!isAuthenticated && (
          <div className="mt-10 flex items-center justify-between rounded-xl border border-border-default bg-surface-elevated px-6 py-5">
            <div className="flex items-center gap-4">
              <Coffee className="h-8 w-8 text-brand-brown shrink-0" />
              <div>
                <p className="text-sm font-semibold text-text-primary">Sign in to build your own notebook library</p>
                <p className="text-sm text-text-secondary">Create private notes, share by link, or publish the notebooks you want others to discover.</p>
              </div>
            </div>
            <Link
              to="/login"
              className="inline-flex items-center gap-1 rounded-lg bg-brand-brown px-5 py-2 text-sm font-medium text-text-inverse hover:opacity-90 transition-opacity shrink-0"
            >
              Sign in <ArrowRight className="h-4 w-4" />
            </Link>
          </div>
        )}

        {isAuthenticated && (
          <section className="mt-14">
            <SectionHeader
              title="My notebooks"
              description="Your private, unlisted, and published notebooks."
              action={
                <Link
                  to="/notes/new"
                  data-testid="new-notebook-button"
                  className="inline-flex items-center gap-1 rounded-lg bg-brand-brown px-4 py-2 text-sm font-medium text-text-inverse hover:opacity-90 transition-opacity"
                >
                  <Plus className="h-4 w-4" />
                  New Notebook
                </Link>
              }
            />
            {myPending ? (
              <SkeletonGrid />
            ) : myError ? (
              <p className="text-sm text-status-error">Failed to load your notebooks.</p>
            ) : !myNotes?.length ? (
              <p className="text-sm text-text-tertiary">You haven't created any notebooks yet.</p>
            ) : (
              <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
                {myNotes.map((nb) => (
                  <NotebookCard key={nb.id} notebook={nb} showVisibility />
                ))}
              </div>
            )}

            <div className="mt-8 flex items-center justify-center gap-2 text-xs text-text-tertiary">
              <span className="inline-block h-4 w-4 rounded-full border border-border-hover text-center leading-4">i</span>
              Tip: Notebook visibility lives in settings, so you can switch between private, unlisted, and public later.
            </div>
          </section>
        )}
      </div>
    </div>
  )
}
