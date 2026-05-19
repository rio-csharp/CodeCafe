import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Search, Plus, ArrowRight, Coffee } from 'lucide-react'
import { useLayout } from '../../../app/LayoutContext'
import { useDebounce } from '../../../hooks/useDebounce'
import { usePublicNotes, useMyNotes } from '../hooks/useNotesQueries'
import NotebookCard from '../components/NotebookCard'

function SectionHeader({
  title,
  description,
  action,
}: {
  title: string
  description: string
  action?: React.ReactNode
}) {
  return (
    <div className="flex items-center justify-between mb-6">
      <div>
        <h2 className="text-xl font-bold text-black">{title}</h2>
        <p className="mt-1 text-sm text-gray-500">{description}</p>
      </div>
      {action}
    </div>
  )
}

function SkeletonCard() {
  return (
    <div className="rounded-xl border border-gray-100 bg-white p-5 animate-pulse">
      <div className="flex items-start gap-4">
        <div className="h-10 w-10 rounded-lg bg-gray-100 shrink-0" />
        <div className="flex-1 space-y-2 min-w-0">
          <div className="h-4 w-28 bg-gray-100 rounded" />
          <div className="h-3 w-full bg-gray-100 rounded" />
          <div className="h-3 w-2/3 bg-gray-100 rounded" />
        </div>
      </div>
      <div className="mt-4 flex items-center justify-between">
        <div className="h-3 w-16 bg-gray-100 rounded" />
        <div className="h-3 w-20 bg-gray-100 rounded" />
      </div>
    </div>
  )
}

function SkeletonGrid({ count = 4 }: { count?: number }) {
  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
      {Array.from({ length: count }).map((_, i) => (
        <SkeletonCard key={i} />
      ))}
    </div>
  )
}

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
        {/* 页面标题始终显示 */}
        <div className="flex items-center justify-between mb-10">
          <h1 className="text-3xl font-bold text-black">Notes</h1>
          <div className="hidden sm:flex items-center gap-3">
            <label className="relative block">
              <span className="sr-only">Search notebooks</span>
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
              <input
                type="text"
                placeholder="Search notebooks..."
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
                className="pl-9 pr-4 py-2 rounded-lg border border-gray-200 text-sm outline-none focus:border-gray-300 w-64"
              />
            </label>
          </div>
        </div>

        {/* Public Notes */}
        <section>
          <SectionHeader
            title="Public Notes"
            description="Explore notebooks shared by the community."
            action={
              <Link
                to="/notes"
                className="hidden sm:inline-flex items-center gap-1 text-sm text-gray-500 hover:text-black transition-colors"
              >
                View all <ArrowRight className="h-3.5 w-3.5" />
              </Link>
            }
          />
          {publicPending ? (
            <SkeletonGrid />
          ) : publicError ? (
            <p className="text-sm text-red-600">Failed to load public notes.</p>
          ) : !publicNotes?.length ? (
            <p className="text-sm text-gray-400">No public notebooks yet.</p>
          ) : (
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
              {publicNotes.map((nb) => (
                <NotebookCard key={nb.id} notebook={nb} />
              ))}
            </div>
          )}
        </section>

        {/* Guest CTA — 未登录时始终显示 */}
        {!isAuthenticated && (
          <div className="mt-10 flex items-center justify-between rounded-xl border border-gray-200 bg-stone-50 px-6 py-5">
            <div className="flex items-center gap-4">
              <Coffee className="h-8 w-8 text-brand-brown shrink-0" />
              <div>
                <p className="text-sm font-semibold text-black">Sign in to create your own notebooks</p>
                <p className="text-sm text-gray-500">Save, organize, and share your knowledge with CodeCafe.</p>
              </div>
            </div>
            <Link
              to="/login"
              className="inline-flex items-center gap-1 rounded-lg bg-brand-brown px-5 py-2 text-sm font-medium text-white hover:opacity-90 transition-opacity shrink-0"
            >
              Sign in <ArrowRight className="h-4 w-4" />
            </Link>
          </div>
        )}

        {/* My Notes — 已登录时始终显示区域标题 */}
        {isAuthenticated && (
          <section className="mt-14">
            <SectionHeader
              title="My Notes"
              description="Your personal notebooks."
              action={
                <Link
                  to="/notes/new"
                  className="inline-flex items-center gap-1 rounded-lg bg-brand-brown px-4 py-2 text-sm font-medium text-white hover:opacity-90 transition-opacity"
                >
                  <Plus className="h-4 w-4" />
                  New Notebook
                </Link>
              }
            />
            {myPending ? (
              <SkeletonGrid />
            ) : myError ? (
              <p className="text-sm text-red-600">Failed to load your notes.</p>
            ) : !myNotes?.length ? (
              <p className="text-sm text-gray-400">You haven't created any notebooks yet.</p>
            ) : (
              <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
                {myNotes.map((nb) => (
                  <NotebookCard key={nb.id} notebook={nb} showVisibility />
                ))}
              </div>
            )}

            <div className="mt-8 flex items-center justify-center gap-2 text-xs text-gray-400">
              <span className="inline-block h-4 w-4 rounded-full border border-gray-300 text-center leading-4">i</span>
              Tip: You can change the visibility of your notebooks at any time.
            </div>
          </section>
        )}
      </div>
    </div>
  )
}
