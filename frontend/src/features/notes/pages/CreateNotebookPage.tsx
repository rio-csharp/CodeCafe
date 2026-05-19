import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ArrowLeft, Loader2 } from 'lucide-react'
import { useCreateNotebook } from '../hooks/useNotesQueries'
import { useToast } from '../../../components/ui/useToast'
import type { NotebookVisibility } from '../types'

const visibilityHelp: Record<NotebookVisibility, string> = {
  private: 'Only you can access this notebook.',
  unlisted: 'Only people with the link can access this notebook.',
  public: 'This notebook will be published and visible to everyone.',
}

export default function CreateNotebookPage() {
  const navigate = useNavigate()
  const create = useCreateNotebook()
  const { showToast } = useToast()
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [visibility, setVisibility] = useState<NotebookVisibility>('private')
  const [error, setError] = useState('')

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    if (!title.trim()) {
      setError('Title is required')
      return
    }
    create.mutate(
      { title: title.trim(), description: description.trim(), visibility },
      {
        onSuccess: (data) => {
          showToast('Notebook created')
          navigate(`/notes/${data.slug}`)
        },
        onError: (err: unknown) => {
          const msg = err instanceof Error ? err.message : 'Failed to create notebook'
          setError(msg)
          showToast(msg, 'error')
        },
      },
    )
  }

  return (
    <div className="pt-24 pb-20 lg:pt-32 lg:pb-24">
      <div className="mx-auto max-w-xl px-6 lg:px-8">
        <button
          onClick={() => navigate('/notes')}
          className="inline-flex items-center gap-1 text-sm text-gray-500 hover:text-black transition-colors mb-6"
        >
          <ArrowLeft className="h-4 w-4" />
          Back to Notes
        </button>

        <h1 className="text-2xl font-bold text-black">Create Notebook</h1>
        <p className="mt-2 text-sm text-gray-500">
          Start a new knowledge base. You can add folders and pages inside.
        </p>

        <form onSubmit={handleSubmit} className="mt-8 space-y-5">
          <div>
            <label className="block text-sm font-medium text-black mb-1">Title</label>
            <input
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="e.g., System Design Notes"
              className="w-full rounded-lg border border-gray-200 px-4 py-2.5 text-sm outline-none focus:border-gray-300"
              autoFocus
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-black mb-1">Description</label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="What is this notebook about?"
              rows={3}
              className="w-full rounded-lg border border-gray-200 px-4 py-2.5 text-sm outline-none focus:border-gray-300 resize-none"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-black mb-1">Visibility</label>
            <div className="flex items-center gap-3">
              {(
                [
                  { value: 'private', label: 'Private' },
                  { value: 'unlisted', label: 'Unlisted' },
                  { value: 'public', label: 'Public' },
                ] as { value: NotebookVisibility; label: string }[]
              ).map((opt) => (
                <label
                  key={opt.value}
                  className={`flex items-center gap-2 rounded-lg border px-4 py-2 text-sm cursor-pointer transition-colors ${
                    visibility === opt.value
                      ? 'border-brand-brown bg-stone-50 text-black'
                      : 'border-gray-200 text-gray-600 hover:bg-gray-50'
                  }`}
                >
                  <input
                    type="radio"
                    name="visibility"
                    value={opt.value}
                    checked={visibility === opt.value}
                    onChange={() => setVisibility(opt.value)}
                    className="accent-brand-brown"
                  />
                  {opt.label}
                </label>
              ))}
            </div>
            <p className="mt-2 text-xs text-gray-400">{visibilityHelp[visibility]}</p>
          </div>

          {error && <p className="text-sm text-red-600">{error}</p>}

          <div className="flex items-center gap-3 pt-2">
            <button
              type="submit"
              disabled={create.isPending}
              className="inline-flex items-center gap-2 rounded-lg bg-brand-brown px-6 py-2.5 text-sm font-medium text-white hover:opacity-90 transition-opacity disabled:opacity-50"
            >
              {create.isPending && <Loader2 className="h-4 w-4 animate-spin" />}
              Create Notebook
            </button>
            <button
              type="button"
              onClick={() => navigate('/notes')}
              className="rounded-lg border border-gray-200 px-6 py-2.5 text-sm font-medium text-gray-700 hover:bg-gray-50 transition-colors"
            >
              Cancel
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
