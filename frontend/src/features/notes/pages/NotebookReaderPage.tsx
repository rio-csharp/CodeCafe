import { useState, useMemo, useCallback } from 'react'
import { useParams, Navigate } from 'react-router-dom'
import { Edit3 } from 'lucide-react'
import { useNotebook, useNotebookItems, useUpdateNotebookItem } from '../hooks/useNotesQueries'
import { useToast } from '../../../components/ui/useToast'
import { buildTree, findFirstPage, findPageByPath } from '../utils/buildTree'
import { extractOutline } from '../utils/extractOutline'
import NotebookLayout from '../components/notebook/NotebookLayout'
import NotebookTopBar from '../components/notebook/NotebookTopBar'
import NotebookTree from '../components/notebook/NotebookTree'
import NotebookPageContent from '../components/notebook/NotebookPageContent'
import NotebookPageEditor from '../components/notebook/NotebookPageEditor'
import NotebookOutline from '../components/notebook/NotebookOutline'
import AiAssistant from '../components/notebook/AiAssistant'
import RouteGuardSpinner from '../../../components/RouteGuardSpinner'

export default function NotebookReaderPage() {
  const { notebookSlug, '*': splat } = useParams<{ notebookSlug: string; '*': string }>()
  const pagePath = splat ?? ''
  const [editClickedForPath, setEditClickedForPath] = useState<string | null>(null)
  const isEditingPage = editClickedForPath === pagePath

  const {
    data: notebook,
    isPending: notebookPending,
    isError: notebookIsError,
    error: notebookError,
  } = useNotebook(notebookSlug!)

  const {
    data: items,
    isPending: itemsPending,
    isError: itemsIsError,
    error: itemsError,
  } = useNotebookItems(notebook?.id ?? '')

  const updateItem = useUpdateNotebookItem(notebook?.id ?? '')
  const { showToast } = useToast()

  const tree = useMemo(() => buildTree(items ?? []), [items])

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
            showToast('Page saved')
            setEditClickedForPath(null)
          },
          onError: (err: unknown) => {
            const msg = err instanceof Error ? err.message : 'Failed to save page'
            showToast(msg, 'error')
          },
        },
      )
    },
    [activePage, updateItem, showToast],
  )

  if (notebookPending) {
    return (
      <div className="h-screen flex items-center justify-center bg-white">
        <RouteGuardSpinner />
      </div>
    )
  }

  if (notebookIsError || !notebook) {
    const errMsg = notebookError instanceof Error ? notebookError.message : 'Failed to load notebook.'
    return (
      <div className="h-screen flex items-center justify-center bg-white">
        <p className="text-sm text-red-600">{errMsg}</p>
      </div>
    )
  }

  if (itemsPending) {
    return (
      <div className="h-screen flex items-center justify-center bg-white">
        <RouteGuardSpinner />
      </div>
    )
  }

  if (itemsIsError) {
    const errMsg = itemsError instanceof Error ? itemsError.message : 'Failed to load notebook items.'
    return (
      <div className="h-screen flex items-center justify-center bg-white">
        <p className="text-sm text-red-600">{errMsg}</p>
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
        />
      }
      content={
        activePage ? (
          <div className="px-6 py-8 lg:px-12 lg:py-10 max-w-3xl mx-auto">
            <div className="flex items-center justify-between mb-3">
              <div className="flex items-center gap-2">
                <svg className="h-3 w-3 text-gray-400" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M12 2L22 12L12 22L2 12Z" />
                </svg>
                <span className="text-[11px] font-medium text-gray-400 uppercase tracking-wider">Page</span>
              </div>
              {notebook.canEdit && !isEditingPage && (
                <button
                  onClick={() => setEditClickedForPath(pagePath)}
                  className="inline-flex items-center gap-1 rounded-lg border border-gray-200 px-3 py-1.5 text-xs font-medium text-gray-700 hover:bg-gray-50 transition-colors"
                >
                  <Edit3 className="h-3 w-3" />
                  Edit page
                </button>
              )}
            </div>
            <h1 className="text-3xl font-bold text-black mb-6">{activePage.title}</h1>
            {isEditingPage ? (
              <NotebookPageEditor
                page={activePage}
                onSave={handleSavePage}
                onCancel={() => setEditClickedForPath(null)}
                isSaving={updateItem.isPending}
              />
            ) : (
              <NotebookPageContent page={activePage} />
            )}
          </div>
        ) : (
          <div className="flex items-center justify-center h-64">
            <div className="text-center">
              <p className="text-sm text-gray-400">This notebook has no pages yet.</p>
              {notebook.canEdit && (
                <p className="text-xs text-gray-400 mt-2">Use the sidebar to add a new page.</p>
              )}
            </div>
          </div>
        )
      }
      rightPanel={
        <>
          <div className="flex-1 min-h-0 overflow-y-auto">
            <NotebookOutline headings={outline} />
          </div>
          <AiAssistant />
        </>
      }
    />
  )
}
