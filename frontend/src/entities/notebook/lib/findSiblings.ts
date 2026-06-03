import type { TreeNode } from './buildTree'

export function findSiblings(
  tree: TreeNode[],
  itemId: string,
): { siblings: TreeNode[]; index: number } {
  function search(nodes: TreeNode[]): { siblings: TreeNode[]; index: number } | null {
    for (let i = 0; i < nodes.length; i++) {
      if (nodes[i].item.id === itemId) {
        return { siblings: nodes, index: i }
      }
      const found = search(nodes[i].children)
      if (found) return found
    }
    return null
  }
  const result = search(tree)
  if (!result) return { siblings: [], index: -1 }
  return result
}
