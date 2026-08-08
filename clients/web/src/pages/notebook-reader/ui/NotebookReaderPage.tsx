import { useState, useMemo, useRef, useEffect, lazy, Suspense } from 'react'
import { useEditorStore } from '@/widgets/notebook-page-editor/model'
import { useParams, Navigate } from 'react-router-dom'
import {
  useNotebook,
  useNotebookItems,
  useNotebookItem,
  buildTree,
  findFirstPage,
  findPageByPath,
  extractOutline,
  findAdjacentPage,
} from '@/entities/notebook'
import { useSaveNotebookPage } from '@/features/edit-notebook-page'
import { getDisplayErrorMessage } from '@/shared/lib'
import QueryError from '@/shared/ui/QueryError'
import { NotebookChangePreview } from '@/widgets/notebook-change-preview'
import { useAiEditProposalActions } from '@/features/ai-assistant'
import NotebookLayout from '@/widgets/notebook-layout'
import NotebookTree from '@/widgets/notebook-tree'
import NotebookPageContent from '@/widgets/notebook-page-content'
import NotebookOutline from '@/widgets/notebook-outline'
import NotebookReaderChrome from '@/widgets/notebook-reader-chrome'
import NotebookPageEmpty from '@/widgets/notebook-page-empty'
import NotebookPageNavigation from '@/widgets/notebook-page-navigation'
import { FloatingAiAssistant } from '@/widgets/ai-assistant'
import RouteGuardSpinner from '@/shared/ui/RouteGuardSpinner'
import { useTranslation } from 'react-i18next'

const NotebookPageEditor = lazy(() => import('@/widgets/notebook-page-editor'))

function TreeSkeleton() {
  return (
    <div className="p-4 space-y-3">
      {Array.from({ length: 12 }).map((_, i) => (
        <div
          key={i}
          className="h-4 bg-surface-hover rounded animate-pulse"
          style={{ marginLeft: `${(i % 3) * 16}px`, width: `${60 + (i % 4) * 10}%` }}
        />
      ))}
    </div>
  )
}

function ContentSkeleton() {
  return (
    <div className="space-y-4 py-4">
      <div className="h-8 bg-surface-hover rounded animate-pulse w-2/3" />
      {Array.from({ length: 8 }).map((_, i) => (
        <div key={i} className="h-4 bg-surface-hover rounded animate-pulse" style={{ width: `${70 + (i % 5) * 6}%` }} />
      ))}
    </div>
  )
}

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

  const notebookId = notebook?.id ?? ''

  const {
    proposal,
    isAiEditPreviewActive,
    isProcessing: isAiEditProcessing,
    handleApplyAiEdit,
    handleCloseAiEditPreview,
    handleDiscardAiEdit,
    handleContinueAiEdit,
  } = useAiEditProposalActions({
    notebookSlug,
    notebookId,
    pagePath,
    onEnterEditMode: setEditClickedForPath,
  })

  const aiEditInitialContent = useMemo(() => {
    if (!isEditingPage || !proposal) return undefined
    if (proposal.pagePath && editClickedForPath === proposal.pagePath) return proposal.afterContentJson
    return undefined
  }, [isEditingPage, proposal, editClickedForPath])

  const {
    data: notebookItems,
    isPending: notebookItemsPending,
    isFetching: notebookItemsFetching,
    refetch: refetchItems,
  } = useNotebookItems(notebookId, undefined, showArchived, !!notebook, false)

  const visibleItems = useMemo(() => {
    const items = notebookItems ?? []
    return showArchived ? items : items.filter((item) => !item.isArchived)
  }, [notebookItems, showArchived])

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
  }, [notebook, pagePath, tree])

  const {
    data: activePageContent,
    isPending: contentPending,
    isError: contentIsError,
    error: contentError,
    refetch: refetchContent,
  } = useNotebookItem(notebookId, activePage?.id ?? null, !!activePage)

  const activePageWithContent = useMemo<typeof activePage>(() => {
    if (!activePage) return null
    if (!activePageContent) return activePage
    return {
      ...activePage,
      contentJson: activePageContent.contentJson,
      plainTextContent: activePageContent.plainTextContent,
    }
  }, [activePage, activePageContent])

  const { handleSave: handleSavePage, isPending: isSavingPage } = useSaveNotebookPage(
    notebookId,
    activePageWithContent,
    {
      onSuccess: () => {
        setEditClickedForPath(null)
      },
    },
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
  const handleRefresh = () => {
    refetch()
    refetchItems()
    if (activePage) {
      refetchContent()
    }
  }
  const handleEdit = () => setEditClickedForPath(pagePath)

  const { prev, next } = useMemo(() => {
    if (!activePage) return { prev: null, next: null }
    return findAdjacentPage(tree, activePage.id)
  }, [tree, activePage])

  const outline = useMemo(
    () => (activePageWithContent?.contentJson ? extractOutline(activePageWithContent.contentJson) : []),
    [activePageWithContent],
  )

  const contentWrapperClass = isEditingPage
    ? 'px-4 sm:px-6 pb-4 lg:px-12 w-full'
    : `px-4 sm:px-6 pb-4 lg:px-12 ${isFullWidth ? 'w-full' : 'max-w-3xl mx-auto'}`

  // Show a full-screen spinner only while we don't even know which notebook this is.
  // Once the notebook metadata is loading, render the layout shell so the user feels
  // they have already entered the notebook.
  if (notebookPending) {
    return (
      <NotebookLayout
        tree={<TreeSkeleton />}
        contentRef={mainRef}
        notebookSlug={notebookSlug}
        prevPage={null}
        nextPage={null}
        content={
          <div className="h-full flex items-center justify-center">
            <RouteGuardSpinner />
          </div>
        }
        rightPanel={<div className="flex-1 min-h-0 overflow-y-auto" />}
      />
    )
  }

  if (notebookIsError || !notebook) {
    const errMsg = getDisplayErrorMessage(notebookError, t, t('errors.generic'))
    return (
      <NotebookLayout
        tree={<TreeSkeleton />}
        contentRef={mainRef}
        notebookSlug={notebookSlug}
        prevPage={null}
        nextPage={null}
        content={
          <div className="h-full flex items-center justify-center px-6">
            <QueryError message={errMsg} onRetry={() => refetch()} className="w-full max-w-md" />
          </div>
        }
        rightPanel={<div className="flex-1 min-h-0 overflow-y-auto" />}
      />
    )
  }

  // Point the URL at the page that is actually shown: either no path was
  // given, or the path is stale (renamed/deleted elsewhere) and we fell
  // back to the first page. Skip while items are (re)fetching so a rename's
  // own URL rewrite isn't fought by a stale cache mid-refetch.
  if (activePage && !notebookItemsPending && !notebookItemsFetching && activePage.path !== pagePath) {
    return <Navigate to={`/notes/${notebookSlug}/${activePage.path}`} replace />
  }

  const treePanel = notebookItemsPending ? (
    <TreeSkeleton />
  ) : (
    <NotebookTree
      notebook={notebook}
      notebookSlug={notebook.slug}
      tree={tree}
      activePage={activePage}
      showArchived={showArchived}
      onShowArchivedChange={setShowArchived}
      onRefreshNotebook={handleRefresh}
    />
  )

  const contentPanel = (
    <>
      {activePage ? (
        <div className={contentWrapperClass}>
          <NotebookReaderChrome
            activePage={activePage}
            isEditingPage={isEditingPage}
            isFullWidth={isFullWidth}
            isFetching={isFetching || contentPending}
            canEdit={notebook.canEdit ?? false}
            onToggleFullWidth={handleToggleFullWidth}
            onRefresh={handleRefresh}
            onEdit={handleEdit}
          />
          {contentPending ? (
            <ContentSkeleton />
          ) : contentIsError ? (
            <QueryError
              message={getDisplayErrorMessage(contentError, t, t('errors.generic'))}
              onRetry={() => refetchContent()}
            />
          ) : isEditingPage ? (
            <Suspense
              fallback={
                <div className="flex items-center justify-center h-32">
                  <div className="h-8 w-8 animate-spin rounded-full border-2 border-border-hover border-t-text-primary" />
                </div>
              }
            >
              <NotebookPageEditor
                page={activePageWithContent ?? activePage}
                onSave={handleSavePage}
                onCancel={() => {
                  setEditClickedForPath(null)
                }}
                isSaving={isSavingPage}
                initialContentJson={aiEditInitialContent}
              />
            </Suspense>
          ) : (
            <>
              <NotebookPageContent page={activePageWithContent ?? activePage} />
              <NotebookPageNavigation notebookSlug={notebookSlug!} prev={prev} next={next} />
            </>
          )}
        </div>
      ) : (
        <NotebookPageEmpty canEdit={notebook.canEdit ?? false} />
      )}
      <FloatingAiAssistant notebook={notebook} activePage={activePageWithContent ?? activePage} />
    </>
  )

  const rightPanel = (
    <div className="flex-1 min-h-0 overflow-y-auto">
      {contentPending ? (
        <div className="p-4 space-y-3">
          {Array.from({ length: 6 }).map((_, i) => (
            <div
              key={i}
              className="h-4 bg-surface-hover rounded animate-pulse"
              style={{ marginLeft: `${(i % 3) * 12}px`, width: `${60 + (i % 4) * 10}%` }}
            />
          ))}
        </div>
      ) : (
        <NotebookOutline headings={outline} scrollContainerRef={mainRef} />
      )}
    </div>
  )

  return (
    <>
      {isAiEditPreviewActive && proposal && (
        <NotebookChangePreview
          title={
            proposal.operation === 'create_page'
              ? t('ai.edit.newPageTitle', { title: proposal.title })
              : proposal.title
          }
          beforeContentJson={proposal.beforeContentJson}
          afterContentJson={proposal.afterContentJson}
          beforeText={proposal.beforePlainTextContent}
          afterText={proposal.afterPlainTextContent}
          summary={proposal.summary}
          operation={proposal.operation}
          canSave
          onSave={handleApplyAiEdit}
          onCancel={handleCloseAiEditPreview}
          onDiscard={handleDiscardAiEdit}
          onEdit={handleContinueAiEdit}
          disableEdit={!proposal.pagePath && proposal.operation !== 'create_page'}
          isSaving={isAiEditProcessing}
        />
      )}
      <NotebookLayout
        tree={treePanel}
        contentRef={mainRef}
        notebookSlug={notebookSlug}
        prevPage={prev}
        nextPage={next}
        content={contentPanel}
        rightPanel={rightPanel}
      />
    </>
  )
}
