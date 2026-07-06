import type { NotebookItem } from '@/entities/notebook-item'

export interface TreeNode {
  item: NotebookItem
  children: TreeNode[]
}

export function buildTree(items: NotebookItem[]): TreeNode[] {
  const sorted = [...items].sort((a, b) => a.sortOrder - b.sortOrder)
  const map = new Map<string, TreeNode>()
  const roots: TreeNode[] = []

  for (const item of sorted) {
    map.set(item.id, { item, children: [] })
  }

  for (const item of sorted) {
    const node = map.get(item.id)!
    if (item.parentId) {
      const parent = map.get(item.parentId)
      if (parent) {
        parent.children.push(node)
      } else {
        roots.push(node)
      }
    } else {
      roots.push(node)
    }
  }

  return roots
}

export function findFirstPage(node: TreeNode): NotebookItem | null {
  if (node.item.type === 'page') return node.item
  for (const child of node.children) {
    const found = findFirstPage(child)
    if (found) return found
  }
  return null
}

export function findPageByPath(nodes: TreeNode[], path: string): NotebookItem | null {
  for (const node of nodes) {
    if (node.item.type === 'page' && node.item.path === path) {
      return node.item
    }
    const found = findPageByPath(node.children, path)
    if (found) return found
  }
  return null
}

export function flattenTree(nodes: TreeNode[]): NotebookItem[] {
  const result: NotebookItem[] = []
  for (const node of nodes) {
    result.push(node.item)
    result.push(...flattenTree(node.children))
  }
  return result
}
