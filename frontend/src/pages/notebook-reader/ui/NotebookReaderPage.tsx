import { useState, useMemo, useRef, useEffect, lazy, Suspense } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { useEditorStore } from '@/widgets/notebook-page-editor/store'
import { useParams, Navigate, useNavigate } from 'react-router-dom'
import { useNotebook, useNotebookItems, buildTree, findFirstPage, findPageByPath, extractOutline, findAdjacentPage, notesKeys } from '@/entities/notebook'
import { useSaveNotebookPage } from '@/features/edit-notebook-page'
import { getErrorMessage } from '@/shared/lib/errorUtils'
import { NotebookChangePreview } from '@/widgets/notebook-change-preview'
import { useAiEditStore, useApplyAiEditProposal, useDiscardAiEditProposal } from '@/features/ai-assistant'
import NotebookLayout from '@/widgets/notebook-layout'
import NotebookTree from '@/widgets/notebook-tree'
import NotebookPageContent from '@/widgets/notebook-page-content'
import NotebookOutline from '@/widgets/notebook-outline'
import NotebookReaderChrome from '@/widgets/notebook-reader-chrome'
import NotebookPageEmpty from '@/widgets/notebook-page-empty'
import NotebookPageNavigation from '@/widgets/notebook-page-navigation/ui/NotebookPageNavigation'
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
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const proposal = useAiEditStore((s) => s.proposal)
  const previewOpen = useAiEditStore((s) => s.previewOpen)
  const closePreview = useAiEditStore((s) => s.closePreview)
  const discardProposal = useDiscardAiEditProposal()
  const isAiEditPreviewActive =
    previewOpen &&
    proposal !== null &&
    proposal.notebookSlug === notebookSlug &&
    (proposal.operation === 'create_page' || proposal.pagePath === pagePath)
  const aiEditInitialContent = useMemo(() => {
    if (!isEditingPage || !proposal) return undefined
    if (proposal.pagePath && editClickedForPath === proposal.pagePath) return proposal.afterContentJson
    return undefined
  }, [isEditingPage, proposal, editClickedForPath])

  const {
    data: notebook,
    isPending: notebookPending,
    isError: notebookIsError,
    error: notebookError,
    refetch,
    isFetching,
  } = useNotebook(notebookSlug!)

  const applyProposal = useApplyAiEditProposal(notebook?.id ?? '')

  const {
    data: notebookItems,
    isPending: notebookItemsPending,
    refetch: refetchItems,
  } = useNotebookItems(
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

  const { handleSave: handleSavePage, isPending: isSavingPage } = useSaveNotebookPage(
    notebook?.id ?? '',
    activePage,
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
  }
  const handleEdit = () => setEditClickedForPath(pagePath)

  const handleApplyAiEdit = () => {
    if (!proposal) return
    applyProposal.mutate(
      { applyPath: proposal.applyPath },
      {
        onSuccess: async (result) => {
          await queryClient.refetchQueries({ queryKey: notesKeys.itemsRoot(notebook?.id ?? '') })
          if (result.operation === 'delete_page') {
            navigate(`/notes/${notebookSlug}`)
            return
          }
          if (result.pagePath && result.pagePath !== pagePath) {
            navigate(`/notes/${notebookSlug}/${result.pagePath}`)
          }
        },
      },
    )
  }

  const handleCloseAiEditPreview = () => {
    closePreview()
  }

  const handleDiscardAiEdit = () => {
    if (proposal) discardProposal.mutate({ discardPath: proposal.discardPath })
  }

  const handleContinueAiEdit = () => {
    if (!proposal) return
    if (proposal.operation === 'create_page') {
      applyProposal.mutate(
        { applyPath: proposal.applyPath },
        {
          onSuccess: async (result) => {
            await queryClient.refetchQueries({ queryKey: notesKeys.itemsRoot(notebook?.id ?? '') })
            if (result.pagePath) {
              setEditClickedForPath(result.pagePath)
              navigate(`/notes/${notebookSlug}/${result.pagePath}`)
            }
          },
        },
      )
    } else if (proposal.pagePath) {
      closePreview()
      setEditClickedForPath(proposal.pagePath)
    }
  }

  const { prev, next } = useMemo(() => {
    if (!activePage) return { prev: null, next: null }
    return findAdjacentPage(tree, activePage.id)
  }, [tree, activePage])

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
    <>
      {isAiEditPreviewActive && proposal && (
        <NotebookChangePreview
          title={proposal.operation === 'create_page' ? t('ai.edit.newPageTitle', { title: proposal.title }) : proposal.title}
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
          isSaving={applyProposal.isPending || discardProposal.isPending}
        />
      )}
      <NotebookLayout
        tree={<NotebookTree notebook={notebook} notebookSlug={notebook.slug} tree={tree} activePage={activePage} showArchived={showArchived} onShowArchivedChange={setShowArchived} onRefreshNotebook={handleRefresh} />}
        contentRef={mainRef}
        notebookSlug={notebookSlug}
        prevPage={prev}
        nextPage={next}
        content={
          <>
            {activePage ? (
            <div className={contentWrapperClass}>
              <NotebookReaderChrome activePage={activePage} isEditingPage={isEditingPage} isFullWidth={isFullWidth} isFetching={isFetching} canEdit={notebook.canEdit ?? false} onToggleFullWidth={handleToggleFullWidth} onRefresh={handleRefresh} onEdit={handleEdit} />
              {isEditingPage ? (
                <Suspense fallback={<div className="flex items-center justify-center h-32"><div className="h-8 w-8 animate-spin rounded-full border-2 border-border-hover border-t-text-primary" /></div>}>
                  <NotebookPageEditor page={activePage} onSave={handleSavePage} onCancel={() => { setEditClickedForPath(null) }} isSaving={isSavingPage} initialContentJson={aiEditInitialContent} />
                </Suspense>
              ) : (
                <>
                  <NotebookPageContent page={activePage} />
                  <NotebookPageNavigation notebookSlug={notebookSlug!} prev={prev} next={next} />
                </>
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
    </>
  )
}
