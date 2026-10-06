import type { PageTreeNode } from '@/entities/notebook'

/**
 * `/setup/rust-notes` → `setup/rust-notes`. Lets a decoded route splat be
 * compared against a node path without caring who wrote the leading slash.
 */
export function normalizePagePath(path: string): string {
  return path
    .split('/')
    .filter((segment) => segment.length > 0)
    .join('/')
}

/** The reader's link for a page: each path segment encoded, CJK included. */
export function toPageHref(slug: string, path: string): string {
  const prefix = `/notebooks/${encodeURIComponent(slug)}`
  const segments = normalizePagePath(path)

  if (segments.length === 0) {
    return prefix
  }

  const encoded = segments.split('/').map(encodeURIComponent).join('/')
  return `${prefix}/${encoded}`
}

/** Archived pages are hidden from the reader; so is everything under them. */
export function visibleTree(roots: readonly PageTreeNode[]): PageTreeNode[] {
  return roots
    .filter((node) => !node.isArchived)
    .map((node) => ({ ...node, children: visibleTree(node.children) }))
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
