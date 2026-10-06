import { apiFetch } from '@/shared/api'
import type { PageDetails } from '../model/types'

export interface GetPageByPathParams {
  slug: string
  /** Verbatim from the tree node, leading slash included: `/setup/rust-notes`. */
  path: string
  signal?: AbortSignal
}

/**
 * Slug chains are built by hand, not with `URLSearchParams`: the segments may
 * hold CJK and get percent-encoded, but the separators must stay literal
 * slashes — the server does not decode `%2F` in a query value, so an encoded
 * separator turns a valid path into a 404.
 */
function pathQuery(path: string): string {
  return path
    .split('/')
    .filter((segment) => segment.length > 0)
    .map(encodeURIComponent)
    .map((segment, index) => (index === 0 ? `/${segment}` : segment))
    .join('/')
}

/** Anonymous-readable; blocks come embedded in the page. */
export function getPageByPath({
  slug,
  path,
  signal,
}: GetPageByPathParams): Promise<PageDetails> {
  const query = `path=${pathQuery(path)}`

  return apiFetch<PageDetails>(
    `/api/notebooks/${encodeURIComponent(slug)}/pages/by-path?${query}`,
    { signal },
  )
}
