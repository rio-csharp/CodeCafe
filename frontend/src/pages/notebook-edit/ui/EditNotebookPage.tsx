import { useNavigate, useParams } from 'react-router-dom'
import { ArrowLeft } from 'lucide-react'
import { useNotebook } from '@/entities/notebook'
import NotebookSettingsForm from '@/widgets/notebook-settings'
import RouteGuardSpinner from '@/shared/ui/RouteGuardSpinner'
import { useTranslation } from 'react-i18next'

export default function EditNotebookPage() {
  const { notebookSlug } = useParams<{ notebookSlug: string }>()
  const navigate = useNavigate()
  const { t } = useTranslation()

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
          <p className="text-sm text-status-error">{t('notebook.loadFailed')}</p>
        </div>
      </div>
    )
  }

  if (!notebook.canEdit) {
    return (
      <div className="pt-24 pb-20 lg:pt-32 lg:pb-24">
        <div className="mx-auto max-w-xl px-6 lg:px-8">
          <p className="text-sm text-status-error">{t('notebook.editPermissionDenied')}</p>
        </div>
      </div>
    )
  }

  return (
    <div className="pt-24 pb-20 lg:pt-32 lg:pb-24">
      <div className="mx-auto max-w-xl px-6 lg:px-8">
        <button
          onClick={() => navigate(`/notes/${notebookSlug}`)}
          className="inline-flex items-center gap-1 text-sm text-text-secondary hover:text-text-primary transition-colors mb-6"
        >
          <ArrowLeft className="h-4 w-4" />
          {t('notebook.backToNotebook')}
        </button>

        <h1 className="text-2xl font-bold text-text-primary">{t('notebook.settingsTitle')}</h1>
        <p className="mt-2 text-sm text-text-secondary">
          {t('notebook.settingsDescription')}
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
