import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { FileText, ListPlus, PenLine, Plus, Wand2 } from 'lucide-react'
import type { Notebook } from '@/entities/notebook'
import type { NotebookItem } from '@/entities/notebook-item'
import type { DraftQuickAction } from '../lib/types'

export function useDraftActions(activePage: NotebookItem | null, notebook: Notebook) {
  const { t } = useTranslation()

  return useMemo<DraftQuickAction[]>(() => {
    const pageTitle = activePage?.title ?? notebook.title
    const pagePath = activePage?.path ?? ''

    return [
      {
        id: 'summarize',
        icon: FileText,
        label: t('ai.drafts.actions.summary'),
        prompt: activePage
          ? t('ai.drafts.prompts.summaryPage', { pageTitle, pagePath, notebookSlug: notebook.slug })
          : t('ai.drafts.prompts.summaryNotebook', { notebookTitle: notebook.title, notebookSlug: notebook.slug }),
      },
      {
        id: 'outline',
        icon: ListPlus,
        label: t('ai.drafts.actions.outline'),
        prompt: activePage
          ? t('ai.drafts.prompts.outlinePage', { pageTitle, pagePath, notebookSlug: notebook.slug })
          : t('ai.drafts.prompts.outlineNotebook', { notebookTitle: notebook.title, notebookSlug: notebook.slug }),
      },
      {
        id: 'rewrite',
        icon: PenLine,
        label: t('ai.drafts.actions.rewrite'),
        prompt: activePage
          ? t('ai.drafts.prompts.rewritePage', { pageTitle, pagePath, notebookSlug: notebook.slug })
          : t('ai.drafts.prompts.rewriteNotebook', { notebookTitle: notebook.title, notebookSlug: notebook.slug }),
      },
      {
        id: 'expand',
        icon: Plus,
        label: t('ai.drafts.actions.expand'),
        prompt: activePage
          ? t('ai.drafts.prompts.expandPage', { pageTitle, pagePath, notebookSlug: notebook.slug })
          : t('ai.drafts.prompts.expandNotebook', { notebookTitle: notebook.title, notebookSlug: notebook.slug }),
      },
      {
        id: 'continue',
        icon: Wand2,
        label: t('ai.drafts.actions.continue'),
        prompt: activePage
          ? t('ai.drafts.prompts.continuePage', { pageTitle, pagePath, notebookSlug: notebook.slug })
          : t('ai.drafts.prompts.continueNotebook', { notebookTitle: notebook.title, notebookSlug: notebook.slug }),
      },
    ]
  }, [activePage, notebook.slug, notebook.title, t])
}
