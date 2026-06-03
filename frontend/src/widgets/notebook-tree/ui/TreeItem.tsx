import type { TreeNode } from '@/entities/notebook'
import { useTreeContext } from '../model/TreeContext'
import TreeFolderNode from './TreeFolderNode'
import TreePageNode from './TreePageNode'

interface TreeItemProps {
  node: TreeNode
  level: number
  siblingCount: number
  index: number
}

export default function TreeItem({ node, level, siblingCount, index }: TreeItemProps) {
  const { notebookSlug, activePath } = useTreeContext()
  const isFolder = node.item.type === 'folder'

  if (isFolder) {
    return (
      <TreeFolderNode
        node={node}
        level={level}
        siblingCount={siblingCount}
        index={index}
      >
        {node.children.map((child, childIndex) => (
          <TreeItem
            key={child.item.id}
            node={child}
            level={level + 1}
            siblingCount={node.children.length}
            index={childIndex}
          />
        ))}
      </TreeFolderNode>
    )
  }

  return (
    <TreePageNode
      node={node}
      notebookSlug={notebookSlug}
      activePath={activePath}
      level={level}
      siblingCount={siblingCount}
      index={index}
    />
  )
}
