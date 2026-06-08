import { useTranslation } from 'react-i18next'

interface NotebookPageEmptyProps {
  canEdit: boolean
}

export default function NotebookPageEmpty({ canEdit }: NotebookPageEmptyProps) {
  const { t } = useTranslation()

  return (
    <div className="flex items-center justify-center h-64">
      <div className="text-center px-4">
        <p className="text-sm text-text-tertiary">{t('notebook.noPages')}</p>
        {canEdit && (
          <p className="text-xs text-text-tertiary mt-2">{t('notebook.addPageHint')}</p>
        )}
      </div>
    </div>
  )
}
