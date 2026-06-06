import { useState, useMemo, useCallback, useRef, useEffect, lazy, Suspense } from 'react'
import { useEditorStore } from '@/widgets/notebook-page-editor/store'
import { useParams, Navigate } from 'react-router-dom'
import { Edit3, Link as LinkIcon, Maximize2, Minimize2, RefreshCw } from 'lucide-react'
import { useNotebook, useNotebookItems, buildTree, findFirstPage, findPageByPath, extractOutline } from '@/entities/notebook'
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

  const {
    data: notebookItems,
  } = useNotebookItems(notebook?.id ?? '', undefined, showArchived, !!notebook)

  const visibleItems = useMemo(() => {
    const items = notebookItems ?? notebook?.items ?? []
    return showArchived ? items : items.filter((item) => !item.isArchived)
  }, [notebookItems, notebook?.items, showArchived])

  const updateItem = useUpdateNotebookItem(notebook?.id ?? '')
  const { showToast } = useToast()

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

  const contentWrapperClass = isEditingPage
    ? 'px-4 sm:px-6 pb-4 lg:px-12 w-full'
    : `px-4 sm:px-6 pb-4 lg:px-12 ${isFullWidth ? 'w-full' : 'max-w-3xl mx-auto'}`

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
          onRefreshNotebook={refetch}
        />
      }
      contentRef={mainRef}
      content={
        activePage ? (
          <div className={contentWrapperClass}>
            {/* Sticky header: always full width */}
            <div className="sticky top-0 z-10 -mx-4 sm:-mx-6 lg:-mx-12 px-4 sm:px-6 lg:px-12 flex items-start justify-between gap-4 py-1.5 bg-surface/95 backdrop-blur-sm">
              {!isEditingPage && (
                <h1
                  className="text-xl font-semibold text-text-primary truncate min-w-0 cursor-pointer"
                  title={activePage.title}
                  onClick={() => {
                    navigator.clipboard.writeText(activePage.title)
                      .then(() => showToast(t('notebook.titleCopied')))
                      .catch(() => showToast(t('notebook.copyFailed'), 'error'))
                  }}
                >
                  {activePage.title}
                </h1>
              )}
              <div className={`flex items-center gap-2 shrink-0 ${isEditingPage ? '' : 'mt-0.5'}`}>
                {!isEditingPage && (
                  <>
                    <button
                      type="button"
                      onClick={() => setIsFullWidth(!isFullWidth)}
                      className="inline-flex items-center gap-1 rounded-md border border-border-subtle px-2 py-0.5 text-[11px] text-text-secondary hover:bg-surface-hover transition-colors"
                      title={isFullWidth ? 'Collapse width' : 'Expand width'}
                    >
                      {isFullWidth ? <Minimize2 className="h-3 w-3" /> : <Maximize2 className="h-3 w-3" />}
                      {isFullWidth ? 'Collapse' : 'Full width'}
                    </button>
                    <button
                      type="button"
                      onClick={() => refetch()}
                      disabled={isFetching}
                      className="inline-flex items-center gap-1 rounded-lg border border-border-default px-3 py-1.5 text-xs font-medium text-text-secondary hover:bg-surface-hover transition-colors disabled:opacity-50"
                      title="Refresh content"
                    >
                      <RefreshCw className={`h-3 w-3 ${isFetching ? 'animate-spin' : ''}`} />
                      Refresh
                    </button>
                    <button
                      type="button"
                      onClick={() => {
                        navigator.clipboard.writeText(window.location.href)
                          .then(() => showToast(t('notebook.linkCopied')))
                          .catch(() => showToast(t('notebook.copyFailed'), 'error'))
                      }}
                      className="inline-flex items-center gap-1 rounded-lg border border-border-default px-3 py-1.5 text-xs font-medium text-text-secondary hover:bg-surface-hover transition-colors"
                      title={t('notebook.copyLink')}
                    >
                      <LinkIcon className="h-3 w-3" />
                      {t('notebook.copyLink')}
                    </button>
                  </>
                )}
                {!isEditingPage && notebook.canEdit && !activePage?.isArchived && (
                  <button
                    type="button"
                    onClick={() => setEditClickedForPath(pagePath)}
                    className="inline-flex items-center gap-1 rounded-lg border border-border-default px-3 py-1.5 text-xs font-medium text-text-secondary hover:bg-surface-hover transition-colors"
                  >
                    <Edit3 className="h-3 w-3" />
                    {t('notebook.editPage')}
                  </button>
                )}
              </div>
            </div>

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
