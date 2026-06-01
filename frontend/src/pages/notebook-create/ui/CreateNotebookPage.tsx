import { useNavigate } from 'react-router-dom'
import { ArrowLeft, Loader2 } from 'lucide-react'
import { useCreateNotebookForm } from '@/features/create-notebook'
import { useToast } from '@/shared/ui/Toast'
import { getErrorMessage } from '@/shared/lib/errorUtils'
import { VisibilityField } from '@/widgets/notebook-settings'

export default function CreateNotebookPage() {
  const navigate = useNavigate()
  const { showToast } = useToast()

  const handleSuccess = (slug: string) => {
    showToast('Notebook created')
    navigate(`/notes/${slug}`)
  }

  const handleError = (message: string) => {
    showToast(getErrorMessage(message, 'Failed to create notebook'), 'error')
  }

  const {
    register,
    handleSubmit,
    control,
    errors,
    isPending,
  } = useCreateNotebookForm(handleSuccess, handleError)

  return (
    <div className="pt-24 pb-20 lg:pt-32 lg:pb-24">
      <div className="mx-auto max-w-xl px-6 lg:px-8">
        <button
          type="button"
          onClick={() => navigate('/notes')}
          className="inline-flex items-center gap-1 text-sm text-text-secondary hover:text-text-primary transition-colors mb-6"
        >
          <ArrowLeft className="h-4 w-4" />
          Back to Notes
        </button>

        <h1 className="text-2xl font-bold text-text-primary">Create Notebook</h1>
        <p className="mt-2 text-sm text-text-secondary">
          Start a new knowledge base. You can add folders and pages inside.
        </p>

        <form onSubmit={handleSubmit} className="mt-8 space-y-5">
          <div>
            <label htmlFor="notebook-title" className="block text-sm font-medium text-text-primary mb-1">
              Title
            </label>
            <input
              id="notebook-title"
              type="text"
              data-testid="create-notebook-title"
              {...register('title')}
              placeholder="e.g., System Design Notes"
              className="w-full rounded-lg border border-border-default px-4 py-2.5 text-sm outline-none focus:border-border-hover"
              autoFocus
            />
            {errors.title && <p className="text-sm text-status-error mt-1">{errors.title.message}</p>}
          </div>

          <div>
            <label htmlFor="notebook-description" className="block text-sm font-medium text-text-primary mb-1">
              Description
            </label>
            <textarea
              id="notebook-description"
              data-testid="create-notebook-description"
              {...register('description')}
              placeholder="What is this notebook about?"
              rows={3}
              className="w-full rounded-lg border border-border-default px-4 py-2.5 text-sm outline-none focus:border-border-hover resize-none"
            />
          </div>

          <VisibilityField control={control} />

          <div className="flex items-center gap-3 pt-2">
            <button
              type="submit"
              data-testid="create-notebook-submit"
              disabled={isPending}
              className="inline-flex items-center gap-2 rounded-lg bg-brand-brown px-6 py-2.5 text-sm font-medium text-text-inverse hover:opacity-90 transition-opacity disabled:opacity-50"
            >
              {isPending && <Loader2 className="h-4 w-4 animate-spin" />}
              Create Notebook
            </button>
            <button
              type="button"
              onClick={() => navigate('/notes')}
              className="rounded-lg border border-border-default px-6 py-2.5 text-sm font-medium text-text-secondary hover:bg-surface-hover transition-colors"
            >
              Cancel
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
