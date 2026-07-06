import { useTranslation } from 'react-i18next'
import type { NotebookVisibility } from '../model/types'

export function useNotebookVisibilityLabels() {
  const { t } = useTranslation()
  return {
    private: t('notebook.visibilityPrivate'),
    unlisted: t('notebook.visibilityUnlisted'),
    public: t('notebook.visibilityPublic'),
  } satisfies Record<NotebookVisibility, string>
}

export function useNotebookVisibilityContextLabels() {
  const { t } = useTranslation()
  return {
    private: t('notebook.visibilityPrivateContext'),
    unlisted: t('notebook.visibilityUnlistedContext'),
    public: t('notebook.visibilityPublicContext'),
  } satisfies Record<NotebookVisibility, string>
}

export function useNotebookVisibilityCollectionLabels() {
  const { t } = useTranslation()
  return {
    private: t('notes.myTitle'),
    unlisted: t('notes.sharedTitle'),
    public: t('notes.publicTitle'),
  } satisfies Record<NotebookVisibility, string>
}

export function useNotebookVisibilityHelpText() {
  const { t } = useTranslation()
  return {
    private: t('notebook.visibilityPrivateHelp'),
    unlisted: t('notebook.visibilityUnlistedHelp'),
    public: t('notebook.visibilityPublicHelp'),
  } satisfies Record<NotebookVisibility, string>
}
