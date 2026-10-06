import { useEffect, useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import type { UseQueryResult } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Navigate, useParams } from 'react-router'
import { BlockList, assembleBlockTree, extractOutline } from '@/entities/block'
import type { BlockNode } from '@/entities/block'
import { getNotebookDetails, getNotebookTree, notebookKeys } from '@/entities/notebook'
import { getPageByPath, pageKeys } from '@/entities/page'
import type { PageDetails } from '@/entities/page'
import { ApiError } from '@/shared/api'
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

  // The notebook root is not a page: jump to the first one, or admit there is none.
  if (pagePath === null) {
    const first = findFirstPagePath(roots)

    if (first !== null) {
      return <Navigate to={toPageHref(notebook.slug, first)} replace />
    }

    return (
      <NotebookReaderLayout notebook={notebook} roots={roots}>
        <EmptyNotebookState />
      </NotebookReaderLayout>
    )
  }

  return (
    <NotebookReaderLayout
      notebook={notebook}
      roots={roots}
      activePath={pagePath}
      outline={<PageOutline headings={outline} />}
    >
      <PageBody page={page} nodes={nodes} />
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

  return <PageArticle page={page.data} nodes={nodes} />
}

function PageArticle({ page, nodes }: { page: PageDetails; nodes: BlockNode[] }) {
  return (
    <article>
      <h2 className="font-display text-3xl leading-tight text-ink">{page.title}</h2>
      <div className="mt-6">
        <BlockList nodes={nodes} />
      </div>
    </article>
  )
}

function isNotFound(error: unknown): boolean {
  return error instanceof ApiError && error.status === 404
}
