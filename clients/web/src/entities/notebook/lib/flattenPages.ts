import type { PageTreeNode } from '../model/types'

/**
 * Depth-first over the visible tree, in reading order — the sequence the
 * prev/next pills walk. Archived pages don't belong in a reading flow.
 */
export function flattenPages(roots: readonly PageTreeNode[]): { path: string; title: string }[] {
  const pages: { path: string; title: string }[] = []
  const walk = (nodes: readonly PageTreeNode[]) => {
    for (const node of nodes) {
      if (!node.isArchived) {
        pages.push({ path: node.path, title: node.title })
      }
      walk(node.children)
    }
  }
  walk(roots)
  return pages
}
