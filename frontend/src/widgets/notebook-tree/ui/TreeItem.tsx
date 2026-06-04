import { memo } from 'react'
import type { TreeNode } from '@/entities/notebook'
import { useTreeContext } from '../model/TreeContext'
import TreeFolderNode from './TreeFolderNode'
import TreePageNode from './TreePageNode'
import DropZone from './DropZone'

interface TreeItemProps {
  node: TreeNode
  level: number
  siblingCount: number
  index: number
}

function TreeItem({ node, level, siblingCount, index }: TreeItemProps) {
  const { notebookSlug, activePath, dragState } = useTreeContext()
  const isFolder = node.item.type === 'folder'

  return (
    <div>
      <DropZone onDrop={() => dragState?.onDropReorder(node.item.id, 'before')} />
      {isFolder ? (
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
      ) : (
        <TreePageNode
          node={node}
          notebookSlug={notebookSlug}
          activePath={activePath}
          level={level}
          siblingCount={siblingCount}
          index={index}
        />
      )}
    </div>
  )
}

export default memo(TreeItem)
