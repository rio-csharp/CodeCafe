import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { FileText, Search, Wand2 } from 'lucide-react'
import type { Notebook } from '@/entities/notebook'
import type { NotebookItem } from '@/entities/notebook-item'
import type { QuickAction } from '../lib/types'

export function useQuickActions(activePage: NotebookItem | null, notebook: Notebook) {
  const { t } = useTranslation()

  return useMemo<QuickAction[]>(() => {
    const pageTitle = activePage?.title ?? notebook.title
    const pagePath = activePage?.path ?? ''

    return [
      {
        id: 'summarize',
        icon: FileText,
        label: t('ai.actions.summarize'),
        prompt: activePage
          ? t('ai.prompts.summarizePage', { pageTitle, pagePath, notebookSlug: notebook.slug })
          : t('ai.prompts.summarizeNotebook', { notebookTitle: notebook.title, notebookSlug: notebook.slug }),
      },
      {
        id: 'related',
        icon: Search,
        label: t('ai.actions.related'),
        prompt: activePage
          ? t('ai.prompts.relatedPage', { pageTitle, pagePath, notebookSlug: notebook.slug })
          : t('ai.prompts.relatedNotebook', { notebookTitle: notebook.title, notebookSlug: notebook.slug }),
      },
      {
        id: 'outline',
        icon: Wand2,
        label: t('ai.actions.outline'),
        prompt: activePage
          ? t('ai.prompts.outlinePage', { pageTitle, pagePath, notebookSlug: notebook.slug })
          : t('ai.prompts.outlineNotebook', { notebookTitle: notebook.title, notebookSlug: notebook.slug }),
      },
    ]
  }, [activePage, notebook.slug, notebook.title, t])
}
