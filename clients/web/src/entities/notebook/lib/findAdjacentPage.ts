import type { TreeNode } from './buildTree'
import { flattenTree } from './buildTree'
import type { NotebookItem } from '@/entities/notebook-item'

export function findAdjacentPage(
  tree: TreeNode[],
  currentPageId: string,
): { prev: NotebookItem | null; next: NotebookItem | null } {
  const pages = flattenTree(tree).filter((item) => item.type === 'page')
  const idx = pages.findIndex((p) => p.id === currentPageId)
  if (idx === -1) return { prev: null, next: null }
  return {
    prev: idx > 0 ? pages[idx - 1] : null,
    next: idx < pages.length - 1 ? pages[idx + 1] : null,
  }
}
