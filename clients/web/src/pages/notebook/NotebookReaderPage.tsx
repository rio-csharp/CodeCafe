import { useEffect, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { UseQueryResult } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Navigate, useNavigate, useParams } from 'react-router'
import { applyBlockOps, BlockList, assembleBlockTree, extractOutline } from '@/entities/block'
import type { BlockNode, BlockOpWire } from '@/entities/block'
import { flattenPages, getNotebookDetails, getNotebookTree, notebookKeys } from '@/entities/notebook'
import { createPage, getPageByPath, pageKeys, updatePage } from '@/entities/page'
import type { PageDetails } from '@/entities/page'
import { ApiError } from '@/shared/api'
import { PageEditor } from '@/widgets/page-editor'
import {
  ContentSkeleton,
  EmptyNotebookState,
  NotebookErrorState,
  NotebookMissingState,
  NotebookReaderLayout,
  PageErrorState,
  PageMissingState,
  PageOutline,
  ReaderSkeletonLayout,
  findFirstPagePath,
  normalizePagePath,
  pageNeighbours,
  toPageHref,
} from '@/widgets/notebook-reader'

/**
 * Route glue for `/notebooks/:slug` and `/notebooks/:slug/*`. Everything it
 * does is read-only: the notebook, its tree, and the requested page.
 */
export function NotebookReaderPage() {
  const { t } = useTranslation()
  const params = useParams()
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

  const notebookTitle = details.data?.title
  const pageTitle = page.data?.title
  const brand = t('brand.name')

  const queryClient = useQueryClient()
  // The path of the page being edited; any other pagePath means "not editing".
  // Deriving it this way keeps navigation from needing a reset effect.
  const [editingPath, setEditingPath] = useState<string | null>(null)
  const editing = editingPath !== null && editingPath === pagePath

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
      const writes: Promise<unknown>[] = []
      if (title !== previousTitle) {
        writes.push(updatePage(pageId, { title }))
      }
      if (ops.length > 0) {
        writes.push(applyBlockOps(pageId, ops))
      }
      await Promise.all(writes)
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: pageKeys.all })
      void queryClient.invalidateQueries({ queryKey: notebookKeys.tree(slug) })
      setEditingPath(null)
    },
  })

  const navigate = useNavigate()
  // New pages open straight in edit mode so the title can be named right away.
  const addPage = useMutation({
    mutationFn: (parentPath: string | null) =>
      createPage({ slug, title: t('editor.untitled'), parentPath }),
    onSuccess: (created) => {
      void queryClient.invalidateQueries({ queryKey: notebookKeys.tree(slug) })
      setEditingPath(normalizePagePath(created.path))
      void navigate(toPageHref(slug, created.path))
    },
  })

  const handleAddPage = (parentPath?: string) => {
    if (!addPage.isPending) {
      addPage.mutate(parentPath ?? null)
    }
  }

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
      >
        <EmptyNotebookState />
      </NotebookReaderLayout>
    )
  }

  return (
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
      ]}
      pageTitle={editing ? null : (page.data?.title ?? null)}
      canEdit={notebook.canWrite}
      onEdit={() => {
        setEditingPath(pagePath)
      }}
      onAddPage={notebook.canWrite ? handleAddPage : undefined}
      prevPage={prevPage}
      nextPage={nextPage}
      refreshing={page.isRefetching}
      onRefresh={() => {
        void page.refetch()
      }}
    >
      {editing && page.data !== undefined ? (
        <PageEditor
          page={page.data}
          saving={savePage.isPending}
          error={savePage.isError ? t('editor.saveFailed') : null}
          onSave={(title, ops) => {
            savePage.mutate({ pageId: page.data.id, title, previousTitle: page.data.title, ops })
          }}
          onCancel={() => {
            savePage.reset()
            setEditingPath(null)
          }}
        />
      ) : (
        <PageBody page={page} nodes={nodes} />
      )}
    </NotebookReaderLayout>
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
