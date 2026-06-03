import { useState, useMemo, useCallback, useRef, lazy, Suspense } from 'react'
import { useEditorStore } from '@/widgets/notebook-page-editor/store'
import { useParams, Navigate } from 'react-router-dom'
import { Edit3 } from 'lucide-react'
import { useNotebook, buildTree, findFirstPage, findPageByPath, extractOutline } from '@/entities/notebook'
import { useUpdateNotebookItem } from '@/features/manage-notebook-items'
import { useToast } from '@/shared/ui/Toast'
import { getErrorMessage } from '@/shared/lib/errorUtils'
import NotebookLayout from '@/widgets/notebook-layout'
import NotebookTopBar from '@/widgets/notebook-top-bar'
import NotebookTree from '@/widgets/notebook-tree'
import NotebookPageContent from '@/widgets/notebook-page-content'
import NotebookOutline from '@/widgets/notebook-outline'
import AiAssistant from '@/widgets/ai-assistant'
import RouteGuardSpinner from '@/shared/ui/RouteGuardSpinner'
import { useTranslation } from 'react-i18next'

const NotebookPageEditor = lazy(() => import('@/widgets/notebook-page-editor'))

export default function NotebookReaderPage() {
  const { notebookSlug, '*': splat } = useParams<{ notebookSlug: string; '*': string }>()
  const pagePath = splat ?? ''
  const { editClickedForPath, setEditClickedForPath } = useEditorStore()
  const [showArchived, setShowArchived] = useState(false)
  const mainRef = useRef<HTMLElement>(null)
  const isEditingPage = editClickedForPath === pagePath
  const { t } = useTranslation()

  const {
    data: notebook,
    isPending: notebookPending,
    isError: notebookIsError,
    error: notebookError,
  } = useNotebook(notebookSlug!)

  const visibleItems = useMemo(() => {
    const items = notebook?.items ?? []
    return showArchived ? items : items.filter((item) => !item.isArchived)
  }, [notebook?.items, showArchived])

  const updateItem = useUpdateNotebookItem(notebook?.id ?? '')
  const { showToast } = useToast()

  const tree = useMemo(() => buildTree(visibleItems), [visibleItems])

  const activePage = useMemo(() => {
    if (!notebook) return null
    let page = pagePath ? findPageByPath(tree, pagePath) : null
    if (!page) {
      for (const node of tree) {
        page = findFirstPage(node)
        if (page) break
      }
    }
    return page
  }, [notebook, tree, pagePath])

  const handleSavePage = useCallback(
    (contentJson: Record<string, unknown>) => {
      if (!activePage) return
      updateItem.mutate(
        {
          itemId: activePage.id,
          data: {
            title: activePage.title,
            sortOrder: activePage.sortOrder,
            contentJson,
          },
        },
        {
          onSuccess: () => {
            showToast(t('notebook.saved'))
            setEditClickedForPath(null)
          },
          onError: (err: unknown) => {
            showToast(getErrorMessage(err, t('notebook.saveFailed')), 'error')
          },
        },
      )
    },
    [activePage, updateItem, showToast, setEditClickedForPath, t],
  )

  if (notebookPending) {
    return (
      <div className="h-screen flex items-center justify-center bg-surface">
        <RouteGuardSpinner />
      </div>
    )
  }

  if (notebookIsError || !notebook) {
    const errMsg = getErrorMessage(notebookError, t('errors.generic'))
    return (
      <div className="h-screen flex items-center justify-center bg-surface">
        <p className="text-sm text-status-error">{errMsg}</p>
      </div>
    )
  }

  // Redirect to first page when no path is specified
  if (!pagePath && activePage) {
    return <Navigate to={`/notes/${notebookSlug}/${activePage.path}`} replace />
  }

  const outline = activePage ? extractOutline(activePage.contentJson) : []

  return (
    <NotebookLayout
      topBar={<NotebookTopBar notebook={notebook} />}
      tree={
        <NotebookTree
          notebook={notebook}
          notebookSlug={notebook.slug}
          tree={tree}
          activePage={activePage}
          showArchived={showArchived}
          onShowArchivedChange={setShowArchived}
        />
      }
      contentRef={mainRef}
      content={
        activePage ? (
          <div className="px-4 sm:px-6 py-8 lg:px-12 lg:py-10 max-w-3xl mx-auto">
            <div className="flex items-center justify-between mb-3">
              <div className="flex items-center gap-2">
                <svg className="h-3 w-3 text-text-tertiary" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M12 2L22 12L12 22L2 12Z" />
                </svg>
                <span className="text-[11px] font-medium text-text-tertiary uppercase tracking-wider">{t('notebook.page')}</span>
              </div>
              {notebook.canEdit && !isEditingPage && !activePage?.isArchived && (
                <button
                  onClick={() => setEditClickedForPath(pagePath)}
                  className="inline-flex items-center gap-1 rounded-lg border border-border-default px-3 py-1.5 text-xs font-medium text-text-secondary hover:bg-surface-hover transition-colors"
                >
                  <Edit3 className="h-3 w-3" />
                  {t('notebook.editPage')}
                </button>
              )}
            </div>
            <h1 className="text-2xl sm:text-3xl font-bold text-text-primary mb-6">{activePage.title}</h1>
            {isEditingPage ? (
              <Suspense fallback={<div className="flex items-center justify-center h-32"><div className="h-8 w-8 animate-spin rounded-full border-2 border-border-hover border-t-text-primary" /></div>}>
                <NotebookPageEditor
                  page={activePage}
                  onSave={handleSavePage}
                  onCancel={() => setEditClickedForPath(null)}
                  isSaving={updateItem.isPending}
                />
              </Suspense>
            ) : (
              <NotebookPageContent page={activePage} />
            )}
          </div>
        ) : (
          <div className="flex items-center justify-center h-64">
            <div className="text-center px-4">
              <p className="text-sm text-text-tertiary">{t('notebook.noPages')}</p>
              {notebook.canEdit && (
                <p className="text-xs text-text-tertiary mt-2">{t('notebook.addPageHint')}</p>
              )}
            </div>
          </div>
        )
      }
      rightPanel={
        <>
          <div className="flex-1 min-h-0 overflow-y-auto">
            <NotebookOutline headings={outline} scrollContainerRef={mainRef} />
          </div>
          <AiAssistant />
        </>
      }
    />
  )
}
