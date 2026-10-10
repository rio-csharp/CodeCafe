import { useEffect, useMemo, useRef, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { UseQueryResult } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Navigate, useLocation, useNavigate, useParams } from 'react-router'
import { applyBlockOps, BlockList, assembleBlockTree, extractOutline } from '@/entities/block'
import type { BlockNode, BlockOpWire } from '@/entities/block'
import { flattenPages, getNotebookDetails, getNotebookTree, notebookKeys } from '@/entities/notebook'
import type { PageTreeNode } from '@/entities/notebook'
import { createPage, deletePage, exportPage, getPageByPath, importPage, movePage, pageKeys, updatePage } from '@/entities/page'
import type { MovePageData, PageDetails } from '@/entities/page'
import { ApiError, clearAccessCode, setAccessCode } from '@/shared/api'
import { MARKDOWN_FILE_ACCEPT, readMarkdownFile } from '@/shared/lib'
import { PageEditor } from '@/widgets/page-editor'
import { AiChatPanel } from '@/features/ai-chat'
import { SharePageDialog } from '@/features/share-page'
import { FavoritePageButton } from '@/features/toggle-page-favorite'
import {
  ContentSkeleton,
  EmptyNotebookState,
  NotebookErrorState,
  NotebookMissingState,
  NotebookReaderLayout,
  NotebookUnlockState,
  PageErrorState,
  PageHistoryPanel,
  PageMissingState,
  PageOutline,
  ReaderSkeletonLayout,
  findFirstPagePath,
  normalizePagePath,
  pageNeighbours,
  toPageHref,
} from '@/widgets/notebook-reader'

/**
 * Route glue for the notebook, page tree, reader and snapshot-based edit session.
 */
export function NotebookReaderPage() {
  const { t } = useTranslation()
  const params = useParams()
  const location = useLocation()
  const slug = params.slug ?? ''

  /** The splat is the page path, already decoded by the router. */
  const pagePath = useMemo(() => {
    const normalized = normalizePagePath(params['*'] ?? '')
    return normalized.length === 0 ? null : normalized
  }, [params])

  const details = useQuery({
    queryKey: notebookKeys.details(slug),
    queryFn: ({ signal }) => getNotebookDetails({ slug, signal }),
    retry: false,
  })

  // Nothing to browse without the notebook, so the tree waits for it.
  const tree = useQuery({
    queryKey: notebookKeys.tree(slug),
    queryFn: ({ signal }) => getNotebookTree({ slug, signal }),
    enabled: details.isSuccess,
    retry: false,
  })

  const page = useQuery({
    queryKey: pageKeys.byPath(slug, pagePath ?? ''),
    // The route carries a bare segment chain; the API speaks `/a/b` like the tree does.
    queryFn: ({ signal }) =>
      getPageByPath({ slug, path: pagePath === null ? '' : `/${pagePath}`, signal }),
    enabled: details.isSuccess && pagePath !== null,
    retry: false,
  })

  // Access-code unlock: a 403 `access_code_required` renders the prompt instead
  // of the vague 404 state, and a successful code simply re-runs the details query.
  // The status is tagged with its slug so navigating between locked notebooks
  // cannot leak one prompt's state into the other.
  const [unlock, setUnlock] = useState<{ slug: string; status: 'pending' | 'wrongCode' } | null>(
    null,
  )
  const unlockStatus = unlock?.slug === slug ? unlock.status : 'idle'

  const handleUnlock = async (code: string) => {
    setUnlock({ slug, status: 'pending' })
    setAccessCode(slug, code)
    const retried = await details.refetch()
    if (retried.isSuccess) {
      return
    }
    if (isAccessCodeRequired(retried.error)) {
      // A refused code must not linger: every later call would carry it.
      clearAccessCode(slug)
      setUnlock({ slug, status: 'wrongCode' })
      return
    }
    // A different failure (network, 500) belongs to the generic error state.
    setUnlock(null)
  }

  const notebookTitle = details.data?.title
  const pageTitle = page.data?.title
  const brand = t('brand.name')

  const queryClient = useQueryClient()
  // Freeze the page snapshot that started the edit. Background refetches (or
  // AI edits) must not silently replace the diff baseline under a local draft.
  const [editingSession, setEditingSession] = useState<{
    slug: string
    page: PageDetails
    locationKey: string | null
  } | null>(null)
  if (
    editingSession !== null &&
    editingSession.locationKey === null &&
    editingSession.slug === slug &&
    normalizePagePath(editingSession.page.path) === pagePath
  ) {
    // A newly created page starts editing before navigation supplies its new
    // location key. Latch that key on the first render at the target route.
    setEditingSession({ ...editingSession, locationKey: location.key })
  }
  const editing =
    editingSession !== null &&
    editingSession.slug === slug &&
    normalizePagePath(editingSession.page.path) === pagePath &&
    (editingSession.locationKey === null || editingSession.locationKey === location.key)

  const savePage = useMutation({
    mutationFn: async ({
      pageId,
      title,
      previousTitle,
      ops,
    }: {
      pageId: string
      title: string
      previousTitle: string
      ops: BlockOpWire[]
    }) => {
      if (title !== previousTitle) {
        await updatePage(pageId, { title })
        // If the block batch fails next, a retry must not repeat the title half
        // that already committed successfully.
        setEditingSession((current) =>
          current?.page.id === pageId
            ? { ...current, page: { ...current.page, title } }
            : current,
        )
      }
      if (ops.length > 0) {
        await applyBlockOps(pageId, ops)
      }
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: pageKeys.all })
      void queryClient.invalidateQueries({ queryKey: notebookKeys.tree(slug) })
      setEditingSession(null)
    },
  })

  const navigate = useNavigate()
  // New pages open straight in edit mode so the title can be named right away.
  const addPage = useMutation({
    mutationFn: (parentPath: string | null) =>
      createPage({ slug, title: t('editor.untitled'), parentPath }),
    onSuccess: (created) => {
      // Page count lives in notebook details and list summaries; the new page
      // also changes the tree, so invalidate the complete notebook namespace.
      void queryClient.invalidateQueries({ queryKey: notebookKeys.all })
      setEditingSession({ slug, page: created, locationKey: null })
      void navigate(toPageHref(slug, created.path))
    },
  })

  const handleAddPage = (parentPath?: string) => {
    if (!addPage.isPending) {
      addPage.mutate(parentPath ?? null)
    }
  }

  // Tree housekeeping for writers: move (drag-and-drop), archive, delete.
  const invalidateTree = () => {
    void queryClient.invalidateQueries({ queryKey: notebookKeys.tree(slug) })
    void queryClient.invalidateQueries({ queryKey: notebookKeys.details(slug) })
    void queryClient.invalidateQueries({ queryKey: pageKeys.all })
  }

  const movePageMutation = useMutation({
    mutationFn: ({ pageId, data }: { pageId: string; data: MovePageData }) =>
      movePage(pageId, data),
    onSuccess: (moved, { pageId }) => {
      // The open page may have been the one dragged: follow it to its new
      // address before refetches can 404 the old one.
      if (page.data?.id === pageId && normalizePagePath(moved.path) !== pagePath) {
        void navigate(toPageHref(slug, moved.path))
      }
      invalidateTree()
    },
  })

  const archivePageMutation = useMutation({
    mutationFn: (node: PageTreeNode) => updatePage(node.id, { isArchived: !node.isArchived }),
    onSuccess: invalidateTree,
  })

  const deletePageMutation = useMutation({
    mutationFn: (node: PageTreeNode) => deletePage(node.id),
    onSuccess: (_value, node) => {
      // The open page just left the menu: fall back to the notebook root,
      // which redirects to the first remaining page (or the empty state).
      if (page.data?.id === node.id) {
        void navigate(`/notebooks/${encodeURIComponent(slug)}`)
      }
      invalidateTree()
    },
  })

  // Import/export plumbing. Chrome actions (export, import) report failures in
  // one floating alert rather than a dialog, so a misclick never traps focus.
  const [actionError, setActionError] = useState<string | null>(null)
  // A neutral counterpart to actionError for good news (block restored).
  const [notice, setNotice] = useState<string | null>(null)
  // Page sharing is a dialog over the already-loaded page details; writers only.
  const [shareOpen, setShareOpen] = useState(false)
  useEffect(() => {
    if (actionError === null) {
      return
    }
    const timeout = setTimeout(() => {
      setActionError(null)
    }, 4000)
    return () => clearTimeout(timeout)
  }, [actionError])

  useEffect(() => {
    if (notice === null) {
      return
    }
    const timeout = setTimeout(() => {
      setNotice(null)
    }, 4000)
    return () => clearTimeout(timeout)
  }, [notice])

  const importInputRef = useRef<HTMLInputElement>(null)
  // Set before the picker opens: the import target survives the file dialog.
  const importParentPathRef = useRef<string | null>(null)

  const importPageMutation = useMutation({
    mutationFn: ({ fileName, markdown, parentPath }: { fileName: string; markdown: string; parentPath: string | null }) =>
      importPage(slug, { fileName, markdown, parentPath }),
    onSuccess: (imported) => {
      invalidateTree()
      void navigate(toPageHref(slug, imported.path))
    },
    onError: () => {
      setActionError(t('import.failed'))
    },
  })

  const handleImportPage = (parentPath?: string) => {
    importParentPathRef.current = parentPath ?? null
    importInputRef.current?.click()
  }

  const handleImportFile = async (file: File | undefined) => {
    if (file === undefined) {
      return
    }
    const read = await readMarkdownFile(file)
    if (!read.ok) {
      setActionError(read.reason === 'tooLarge' ? t('import.tooLarge') : t('import.failed'))
      return
    }
    importPageMutation.mutate({
      fileName: read.fileName,
      markdown: read.markdown,
      parentPath: importParentPathRef.current,
    })
  }

  const handleExportPage = () => {
    if (page.data === undefined) {
      return
    }
    exportPage(page.data.id).catch(() => {
      setActionError(t('reader.exportFailed'))
    })
  }

  // The hidden picker and the floating alert travel with both layout branches.
  const importUi = (
    <>
      <input
        ref={importInputRef}
        type="file"
        accept={MARKDOWN_FILE_ACCEPT}
        className="hidden"
        aria-hidden="true"
        tabIndex={-1}
        onChange={(event) => {
          void handleImportFile(event.target.files?.[0])
          // Re-picking the same file must fire again.
          event.target.value = ''
        }}
      />
      {actionError !== null ? (
        <button
          type="button"
          role="alert"
          onClick={() => {
            setActionError(null)
          }}
          className="fixed bottom-5 left-1/2 z-50 -translate-x-1/2 rounded-md border border-line bg-card px-3 py-2 text-xs text-danger shadow-lg"
        >
          {actionError}
        </button>
      ) : null}
      {notice !== null ? (
        <button
          type="button"
          role="status"
          onClick={() => {
            setNotice(null)
          }}
          className="fixed bottom-5 left-1/2 z-50 -translate-x-1/2 rounded-md border border-line bg-card px-3 py-2 text-xs text-ink shadow-lg"
        >
          {notice}
        </button>
      ) : null}
    </>
  )

  // One assembly feeds both the article and the outline, so they cannot drift.
  const nodes = useMemo(
    () => (page.data === undefined ? [] : assembleBlockTree(page.data.blocks)),
    [page.data],
  )
  const outline = useMemo(() => extractOutline(nodes), [nodes])

  useEffect(() => {
    document.title = [pageTitle, notebookTitle, brand].filter(Boolean).join(' · ')
  }, [pageTitle, notebookTitle, brand])

  if (details.isPending) {
    return <ReaderSkeletonLayout />
  }

  if (details.isError) {
    if (isAccessCodeRequired(details.error)) {
      return (
        <NotebookUnlockState
          pending={unlockStatus === 'pending'}
          wrongCode={unlockStatus === 'wrongCode'}
          onUnlock={(code) => {
            void handleUnlock(code)
          }}
        />
      )
    }
    if (isNotFound(details.error)) {
      return <NotebookMissingState />
    }
    return (
      <NotebookErrorState
        onRetry={() => {
          void details.refetch()
        }}
      />
    )
  }

  const notebook = details.data

  if (tree.isPending) {
    return <ReaderSkeletonLayout />
  }

  if (tree.isError) {
    return isNotFound(tree.error) ? (
      <NotebookMissingState />
    ) : (
      <NotebookErrorState
        onRetry={() => {
          void tree.refetch()
        }}
      />
    )
  }

  const roots = tree.data.roots
  const flatPages = flattenPages(roots)
  const { prev: prevPage, next: nextPage } = pageNeighbours(flatPages, pagePath)


  // The notebook root is not a page: jump to the first one, or admit there is none.
  if (pagePath === null) {
    const first = findFirstPagePath(roots)

    if (first !== null) {
      return <Navigate to={toPageHref(notebook.slug, first)} replace />
    }

    return (
      <NotebookReaderLayout
        notebook={notebook}
        roots={roots}
        canEdit={notebook.canWrite}
        onAddPage={notebook.canWrite ? handleAddPage : undefined}
        onImportPage={notebook.canWrite ? handleImportPage : undefined}
        onMovePage={
          notebook.canWrite
            ? (pageId, data) => {
                movePageMutation.mutate({ pageId, data })
              }
            : undefined
        }
        onToggleArchive={
          notebook.canWrite
            ? (node) => {
                archivePageMutation.mutate(node)
              }
            : undefined
        }
        onDeletePage={
          notebook.canWrite
            ? (node) => {
                deletePageMutation.mutate(node)
              }
            : undefined
        }
        rightTabs={[
          {
            id: 'ai',
            label: t('ai.title'),
            icon: (
              <path d="M8 2.5 9.3 6l3.5 1.3L9.3 8.6 8 12.1 6.7 8.6 3.2 7.3 6.7 6zM12.5 10.5l.7 1.8 1.8.7-1.8.7-.7 1.8-.7-1.8-1.8-.7 1.8-.7z" />
            ),
            content: (
              <AiChatPanel
                slug={notebook.slug}
                onAiChanged={() => {
                  void queryClient.invalidateQueries({ queryKey: notebookKeys.tree(slug) })
                }}
              />
            ),
          },
        ]}
      >
        {importUi}
        <EmptyNotebookState />
      </NotebookReaderLayout>
    )
  }

  return (
    <>
      <NotebookReaderLayout
      notebook={notebook}
      roots={roots}
      activePath={pagePath}
      rightTabs={[
        {
          id: 'outline',
          label: t('reader.outline'),
          icon: <path d="M2.5 4h11M2.5 8h7M2.5 11.5h9" />,
          content: <PageOutline headings={outline} />,
        },
        {
          id: 'ai',
          label: t('ai.title'),
          icon: (
            <path d="M8 2.5 9.3 6l3.5 1.3L9.3 8.6 8 12.1 6.7 8.6 3.2 7.3 6.7 6zM12.5 10.5l.7 1.8 1.8.7-1.8.7-.7 1.8-.7-1.8-1.8-.7 1.8-.7z" />
          ),
          content: (
            <AiChatPanel
              slug={notebook.slug}
              onAiChanged={() => {
                void queryClient.invalidateQueries({ queryKey: pageKeys.all })
                void queryClient.invalidateQueries({ queryKey: notebookKeys.tree(slug) })
              }}
            />
          ),
        },
        // The history is page-scoped and read-only while editing (restoring
        // under an open draft would silently discard it).
        ...(page.data !== undefined && !editing
          ? [
              {
                id: 'history',
                label: t('reader.history'),
                icon: (
                  <path d="M8 4.2v4l2.6 1.6M13.8 8a5.8 5.8 0 1 1-1.7-4.1M13.8 2.8v2.7h-2.7" />
                ),
                content: (
                  <PageHistoryPanel
                    pageId={page.data.id}
                    currentBlocks={page.data.blocks}
                    canWrite={notebook.canWrite}
                    onRestored={() => {
                      void queryClient.invalidateQueries({ queryKey: pageKeys.all })
                    }}
                  />
                ),
              },
            ]
          : []),
      ]}
      pageTitle={editing ? null : (page.data?.title ?? null)}
      canEdit={notebook.canWrite}
      onEdit={() => {
        if (page.data !== undefined) {
          setEditingSession({ slug, page: page.data, locationKey: location.key })
        }
      }}
      favoriteAction={
        page.data !== undefined && !editing ? (
          <FavoritePageButton
            pageId={page.data.id}
            slug={slug}
            isFavorite={page.data.isFavorite}
          />
        ) : undefined
      }
      onExportPage={page.data !== undefined && !editing ? handleExportPage : undefined}
      onSharePage={
        page.data !== undefined && !editing && notebook.canWrite
          ? () => {
              setShareOpen(true)
            }
          : undefined
      }
      onAddPage={notebook.canWrite ? handleAddPage : undefined}
      onImportPage={notebook.canWrite ? handleImportPage : undefined}
      onMovePage={
        notebook.canWrite
          ? (pageId, data) => {
              movePageMutation.mutate({ pageId, data })
            }
          : undefined
      }
      onToggleArchive={
        notebook.canWrite
          ? (node) => {
              archivePageMutation.mutate(node)
            }
          : undefined
      }
      onDeletePage={
        notebook.canWrite
          ? (node) => {
              deletePageMutation.mutate(node)
            }
          : undefined
      }
      prevPage={prevPage}
      nextPage={nextPage}
      refreshing={page.isRefetching}
      onRefresh={() => {
        void page.refetch()
      }}
    >
      {editing && editingSession !== null ? (
        <>
          {importUi}
          <PageEditor
          page={editingSession.page}
          saving={savePage.isPending}
          error={
            savePage.isError
              ? savePage.error instanceof ApiError
                ? t('editor.saveFailedWithReason', { reason: savePage.error.message })
                : t('editor.saveFailed')
              : null
          }
          onSave={(title, ops) => {
            savePage.mutate({
              pageId: editingSession.page.id,
              title,
              previousTitle: editingSession.page.title,
              ops,
            })
          }}
          onCancel={() => {
            savePage.reset()
            setEditingSession(null)
          }}
          onBlockRestored={() => {
            setNotice(t('editor.blockRestored'))
          }}
        />
        </>
      ) : (
        <>
          {importUi}
          <PageBody page={page} nodes={nodes} />
        </>
      )}
      </NotebookReaderLayout>

      {/* Live page details, not a snapshot: a share mutation invalidates the
          page namespace and the dialog's list refreshes in place. */}
      <SharePageDialog
        page={shareOpen && page.data !== undefined ? page.data : null}
        onClose={() => {
          setShareOpen(false)
        }}
      />
    </>
  )
}

/** Everything but the chrome: whichever state the page lookup landed in. */
function PageBody({ page, nodes }: { page: UseQueryResult<PageDetails, Error>; nodes: BlockNode[] }) {
  if (page.isPending) {
    return <ContentSkeleton />
  }

  // A valid notebook with a bad path keeps the chrome, so there is a way out.
  if (page.isError) {
    return isNotFound(page.error) ? (
      <PageMissingState />
    ) : (
      <PageErrorState
        onRetry={() => {
          void page.refetch()
        }}
      />
    )
  }

  return <PageArticle nodes={nodes} />
}

/** The chrome already pins the title; the article starts straight at the blocks. */
function PageArticle({ nodes }: { nodes: BlockNode[] }) {
  return (
    <article className="text-sm leading-relaxed">
      <BlockList nodes={nodes} />
    </article>
  )
}

function isNotFound(error: unknown): boolean {
  return error instanceof ApiError && error.status === 404
}

function isAccessCodeRequired(error: unknown): boolean {
  return error instanceof ApiError && error.code === 'access_code_required'
}
