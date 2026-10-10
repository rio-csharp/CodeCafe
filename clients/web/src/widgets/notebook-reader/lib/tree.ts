import type { PageTreeNode } from '@/entities/notebook'
import { normalizePagePath } from '@/entities/page'

// The href helpers live with the page entity (the homepage needs them too);
// re-exported here so the reader's public API is unchanged.
export { normalizePagePath, toPageHref } from '@/entities/page'

/**
 * The tree as the reader shows it. Archived pages (and everything under them)
 * are hidden unless `includeArchived` — writers get them back, dimmed and
 * badged, so they can still be un-archived or reorganized.
 */
export function visibleTree(
  roots: readonly PageTreeNode[],
  options?: { includeArchived?: boolean },
): PageTreeNode[] {
  const includeArchived = options?.includeArchived ?? false
  return roots
    .filter((node) => includeArchived || !node.isArchived)
    .map((node) => ({ ...node, children: visibleTree(node.children, options) }))
}

/**
 * The target of the notebook-root redirect: pre-order depth-first, so the first
 * visible root wins over any of its children.
 */
export function findFirstPagePath(roots: readonly PageTreeNode[]): string | null {
  return roots.find((node) => !node.isArchived)?.path ?? null
}

/** Paths whose sections must be open for the active page to be reachable. */
export function ancestorPathsOf(
  roots: readonly PageTreeNode[],
  activePath: string | null,
): Set<string> {
  const ancestors = new Set<string>()
  if (activePath === null) {
    return ancestors
  }

  const target = normalizePagePath(activePath)

  const walk = (nodes: readonly PageTreeNode[], trail: readonly string[]): boolean => {
    for (const node of nodes) {
      const self = normalizePagePath(node.path)
      if (self === target) {
        for (const ancestor of trail) {
          ancestors.add(ancestor)
        }
        return true
      }

      if (walk(node.children, [...trail, self])) {
        return true
      }
    }

    return false
  }

  walk(roots, [])
  return ancestors
}

export interface PageNeighbour {
  path: string
  title: string
}

/**
 * Linear reading-order neighbours of the open page. Tree paths carry a
 * leading slash while the route splat does not — normalize before comparing.
 */
export function pageNeighbours(
  flatPages: readonly PageNeighbour[],
  activePath: string | null,
): { prev: PageNeighbour | null; next: PageNeighbour | null } {
  if (activePath === null) {
    return { prev: null, next: null }
  }

  const target = normalizePagePath(activePath)
  const index = flatPages.findIndex((entry) => normalizePagePath(entry.path) === target)
  if (index === -1) {
    return { prev: null, next: null }
  }

  return {
    prev: index > 0 ? flatPages[index - 1] : null,
    next: index < flatPages.length - 1 ? flatPages[index + 1] : null,
  }
}

export interface PageSearchMatch {
  node: PageTreeNode
  /** Titles of the ancestors, outermost first. */
  trail: readonly string[]
}

/** Pre-order title matches over the visible tree, case-insensitive. */
export function searchVisibleTree(
  roots: readonly PageTreeNode[],
  query: string,
): PageSearchMatch[] {
  const lowered = query.trim().toLocaleLowerCase()
  if (lowered.length === 0) {
    return []
  }

  const matches: PageSearchMatch[] = []
  const walk = (nodes: readonly PageTreeNode[], trail: readonly string[]): void => {
    for (const node of nodes) {
      if (node.title.toLocaleLowerCase().includes(lowered)) {
        matches.push({ node, trail })
      }
      walk(node.children, [...trail, node.title])
    }
  }
  walk(roots, [])
  return matches
}
