import { useNavigate, useParams } from 'react-router-dom'
import { ArrowLeft } from 'lucide-react'
import { useNotebook } from '../hooks/useNotesQueries'
import NotebookSettingsForm from '../components/notebook/NotebookSettingsForm'
import RouteGuardSpinner from '../../../components/RouteGuardSpinner'

export default function EditNotebookPage() {
  const { notebookSlug } = useParams<{ notebookSlug: string }>()
  const navigate = useNavigate()

  const { data: notebook, isPending, isError } = useNotebook(notebookSlug!)

  if (isPending) {
    return (
      <div className="pt-24 pb-20 lg:pt-32 lg:pb-24">
        <div className="mx-auto max-w-xl px-6 lg:px-8">
          <RouteGuardSpinner />
        </div>
      </div>
    )
  }

  if (isError || !notebook) {
    return (
      <div className="pt-24 pb-20 lg:pt-32 lg:pb-24">
        <div className="mx-auto max-w-xl px-6 lg:px-8">
          <p className="text-sm text-red-600">Failed to load notebook.</p>
        </div>
      </div>
    )
  }

  if (!notebook.canEdit) {
    return (
      <div className="pt-24 pb-20 lg:pt-32 lg:pb-24">
        <div className="mx-auto max-w-xl px-6 lg:px-8">
          <p className="text-sm text-red-600">You do not have permission to edit this notebook.</p>
        </div>
      </div>
    )
  }

  return (
    <div className="pt-24 pb-20 lg:pt-32 lg:pb-24">
      <div className="mx-auto max-w-xl px-6 lg:px-8">
        <button
          onClick={() => navigate(`/notes/${notebookSlug}`)}
          className="inline-flex items-center gap-1 text-sm text-gray-500 hover:text-black transition-colors mb-6"
        >
          <ArrowLeft className="h-4 w-4" />
          Back to Notebook
        </button>

        <h1 className="text-2xl font-bold text-black">Notebook Settings</h1>
        <p className="mt-2 text-sm text-gray-500">
          Update your notebook title, description, and visibility.
        </p>

        <NotebookSettingsForm
          notebook={notebook}
          onSlugChange={(newSlug) => navigate(`/notes/${newSlug}`, { replace: true })}
          onDeleteSuccess={() => navigate('/notes')}
          onCancel={() => navigate(`/notes/${notebookSlug}`)}
        />
      </div>
    </div>
  )
}
