import type { TreeNode } from '../../utils/buildTree'

export function findSiblings(
  tree: TreeNode[],
  itemId: string,
): { siblings: TreeNode[]; index: number; parent: TreeNode | null } {
  for (let i = 0; i < tree.length; i++) {
    if (tree[i].item.id === itemId) {
      return { siblings: tree, index: i, parent: null }
    }
    const result = findSiblings(tree[i].children, itemId)
    if (result.index !== -1) {
      if (result.parent === null) {
        return { siblings: result.siblings, index: result.index, parent: tree[i] }
      }
      return result
    }
  }
  return { siblings: [], index: -1, parent: null }
}
