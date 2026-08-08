import { useNavigate, useParams, useLocation } from 'react-router-dom'
import { ArrowLeft } from 'lucide-react'
import { useNotebook } from '@/entities/notebook'
import NotebookSettingsForm from '@/widgets/notebook-settings'
import RouteGuardSpinner from '@/shared/ui/RouteGuardSpinner'
import QueryError from '@/shared/ui/QueryError'
import { getDisplayErrorMessage } from '@/shared/lib'
import { useTranslation } from 'react-i18next'

export default function EditNotebookPage() {
  const { notebookSlug } = useParams<{ notebookSlug: string }>()
  const navigate = useNavigate()
  const location = useLocation()
  const { t } = useTranslation()
  const fromPagePath = (location.state as { fromPagePath?: string } | null)?.fromPagePath

  const { data: notebook, isPending, isError, error, refetch } = useNotebook(notebookSlug!)

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
          <QueryError
            message={getDisplayErrorMessage(error, t, t('notebook.loadFailed'))}
            onRetry={() => refetch()}
          />
        </div>
      </div>
    )
  }

  if (!notebook.canEdit) {
    return (
      <div className="pt-24 pb-20 lg:pt-32 lg:pb-24">
        <div className="mx-auto max-w-xl px-6 lg:px-8">
          <QueryError message={t('notebook.editPermissionDenied')} />
        </div>
      </div>
    )
  }

  const goBack = (slug: string) => {
    if (fromPagePath) {
      navigate(`/notes/${slug}/${fromPagePath}`, { replace: true })
    } else {
      navigate(`/notes/${slug}`, { replace: true })
    }
  }

  return (
    <div className="pt-24 pb-20 lg:pt-32 lg:pb-24">
      <div className="mx-auto max-w-xl px-6 lg:px-8">
        <button
          onClick={() => goBack(notebookSlug!)}
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
          onSlugChange={(newSlug) => goBack(newSlug)}
          onDeleteSuccess={() => navigate('/notes')}
          onCancel={() => goBack(notebookSlug!)}
        />
      </div>
    </div>
  )
}
