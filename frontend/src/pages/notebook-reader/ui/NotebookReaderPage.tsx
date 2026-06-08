import { useState, useMemo, useRef, useEffect, lazy, Suspense } from 'react'
import { useEditorStore } from '@/widgets/notebook-page-editor/store'
import { useParams, Navigate } from 'react-router-dom'
import { useNotebook, useNotebookItems, buildTree, findFirstPage, findPageByPath, extractOutline } from '@/entities/notebook'
import { useSaveNotebookPage } from '@/features/edit-notebook-page'
import { getErrorMessage } from '@/shared/lib/errorUtils'
import NotebookLayout from '@/widgets/notebook-layout'
import NotebookTree from '@/widgets/notebook-tree'
import NotebookPageContent from '@/widgets/notebook-page-content'
import NotebookOutline from '@/widgets/notebook-outline'
import NotebookReaderChrome from '@/widgets/notebook-reader-chrome'
import NotebookPageEmpty from '@/widgets/notebook-page-empty'
import { FloatingAiAssistant } from '@/widgets/ai-assistant'
import RouteGuardSpinner from '@/shared/ui/RouteGuardSpinner'
import { useTranslation } from 'react-i18next'

const NotebookPageEditor = lazy(() => import('@/widgets/notebook-page-editor'))

export default function NotebookReaderPage() {
  const { notebookSlug, '*': splat } = useParams<{ notebookSlug: string; '*': string }>()
  const pagePath = splat ?? ''
  const { editClickedForPath, setEditClickedForPath } = useEditorStore()
  const [showArchived, setShowArchived] = useState(false)
  const [isFullWidth, setIsFullWidth] = useState(true)
  const mainRef = useRef<HTMLElement>(null)
  const isEditingPage = editClickedForPath === pagePath
  const { t } = useTranslation()

  const {
    data: notebook,
    isPending: notebookPending,
    isError: notebookIsError,
    error: notebookError,
    refetch,
    isFetching,
  } = useNotebook(notebookSlug!)

  const { data: notebookItems, isPending: notebookItemsPending } = useNotebookItems(
    notebook?.id ?? '',
    undefined,
    showArchived,
    !!notebook,
  )

  const visibleItems = useMemo(() => {
    const items = notebookItems ?? notebook?.items ?? []
    return showArchived ? items : items.filter((item) => !item.isArchived)
  }, [notebookItems, notebook?.items, showArchived])

  const tree = useMemo(() => buildTree(visibleItems), [visibleItems])

  const activePage = (() => {
    if (!notebook) return null
    let page = pagePath ? findPageByPath(tree, pagePath) : null
    if (!page) {
      for (const node of tree) {
        page = findFirstPage(node)
        if (page) break
      }
    }
    return page
  })()

  const { handleSave: handleSavePage, isPending: isSavingPage } = useSaveNotebookPage(
    notebook?.id ?? '',
    activePage,
    { onSuccess: () => setEditClickedForPath(null) },
  )

  // Keep the editor store pinned to the active page across path-rewriting
  // actions. When a rename (or rename of an ancestor folder) navigates the
  // URL, `editClickedForPath` still holds the pre-rewrite path and would
  // cause `isEditingPage` to flip to false, silently closing the editor
  // mid-edit. Detect the "same item, new path" case and re-pin the store.
  const lastActivePageIdRef = useRef<string | null>(null)
  useEffect(() => {
    const currentId = activePage?.id ?? null
    const previousId = lastActivePageIdRef.current
    lastActivePageIdRef.current = currentId
    if (
      editClickedForPath !== null &&
      activePage !== null &&
      currentId !== null &&
      currentId === previousId &&
      activePage.path !== editClickedForPath
    ) {
      setEditClickedForPath(activePage.path)
    }
  }, [activePage, editClickedForPath, setEditClickedForPath])

  const handleToggleFullWidth = () => setIsFullWidth((prev) => !prev)
  const handleRefresh = () => refetch()
  const handleEdit = () => setEditClickedForPath(pagePath)

  if (notebookPending) {
    return <div className="h-screen flex items-center justify-center bg-surface"><RouteGuardSpinner /></div>
  }

  if (notebookIsError || !notebook) {
    const errMsg = getErrorMessage(notebookError, t('errors.generic'))
    return <div className="h-screen flex items-center justify-center bg-surface"><p className="text-sm text-status-error">{errMsg}</p></div>
  }

  if (notebookItemsPending && notebookItems === undefined) {
    return <div className="h-screen flex items-center justify-center bg-surface"><RouteGuardSpinner /></div>
  }

  // Redirect to first page when no path is specified
  if (!pagePath && activePage) {
    return <Navigate to={`/notes/${notebookSlug}/${activePage.path}`} replace />
  }

  const outline = activePage ? extractOutline(activePage.contentJson) : []
  const contentWrapperClass = isEditingPage
    ? 'px-4 sm:px-6 pb-4 lg:px-12 w-full'
    : `px-4 sm:px-6 pb-4 lg:px-12 ${isFullWidth ? 'w-full' : 'max-w-3xl mx-auto'}`

  return (
    <NotebookLayout
      tree={<NotebookTree notebook={notebook} notebookSlug={notebook.slug} tree={tree} activePage={activePage} showArchived={showArchived} onShowArchivedChange={setShowArchived} onRefreshNotebook={refetch} />}
      contentRef={mainRef}
      content={
        <>
          {activePage ? (
            <div className={contentWrapperClass}>
              <NotebookReaderChrome activePage={activePage} isEditingPage={isEditingPage} isFullWidth={isFullWidth} isFetching={isFetching} canEdit={notebook.canEdit ?? false} onToggleFullWidth={handleToggleFullWidth} onRefresh={handleRefresh} onEdit={handleEdit} />
              {isEditingPage ? (
                <Suspense fallback={<div className="flex items-center justify-center h-32"><div className="h-8 w-8 animate-spin rounded-full border-2 border-border-hover border-t-text-primary" /></div>}>
                  <NotebookPageEditor page={activePage} onSave={handleSavePage} onCancel={() => setEditClickedForPath(null)} isSaving={isSavingPage} />
                </Suspense>
              ) : (
                <NotebookPageContent page={activePage} />
              )}
            </div>
          ) : (
            <NotebookPageEmpty canEdit={notebook.canEdit ?? false} />
          )}
          <FloatingAiAssistant notebook={notebook} activePage={activePage} />
        </>
      }
      rightPanel={
        <div className="flex-1 min-h-0 overflow-y-auto">
          <NotebookOutline headings={outline} scrollContainerRef={mainRef} />
        </div>
      }
    />
  )
}
