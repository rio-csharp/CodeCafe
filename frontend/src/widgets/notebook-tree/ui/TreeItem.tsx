import { useState, memo } from 'react'
import type { TreeNode } from '@/entities/notebook'
import { useTreeContext } from '../model/TreeContext'
import TreeFolderNode from './TreeFolderNode'
import TreePageNode from './TreePageNode'

type DropIntent = 'before' | 'after' | 'inside'

interface TreeItemProps {
  node: TreeNode
  level: number
  siblingCount: number
  index: number
}

/**
 * Wraps each tree row in a single drop target. The intent is derived from
 * the cursor's Y position relative to the row's bounding box:
 *   - top half    -> 'before'  (insert above this item)
 *   - bottom half -> 'after'   (insert below this item)
 *   - empty folder -> 'inside' (the dragged item becomes the folder's only child)
 *
 * The visual is a single thin line at the top or bottom edge of the row
 * (or a soft background highlight for the empty-folder "inside" case), so
 * the user always sees one indicator, never two competing ones.
 */
function TreeItem({ node, level, siblingCount, index }: TreeItemProps) {
  const { notebookSlug, activePath, dragState } = useTreeContext()
  const [dropIntent, setDropIntent] = useState<DropIntent | null>(null)
  const isFolder = node.item.type === 'folder'
  const isFolderEmpty = isFolder && node.children.length === 0

  const handleDragOver = (e: React.DragEvent) => {
    const draggingId = dragState?.draggingId
    if (!dragState || !draggingId || draggingId === node.item.id) return
    // Don't accept a drop onto a node that's already inside the dragged subtree
    // (the reorder handler also checks this server-side; the client just
    // suppresses the indicator to keep the UX honest).
    e.preventDefault()
    e.stopPropagation()

    if (isFolderEmpty) {
      setDropIntent('inside')
      return
    }

    const rect = e.currentTarget.getBoundingClientRect()
    const midpoint = rect.top + rect.height / 2
    setDropIntent(e.clientY < midpoint ? 'before' : 'after')
  }

  // dragLeave fires when the cursor leaves the row OR moves to a child
  // element (because React bubbles the event). Only clear the intent when
  // the cursor actually leaves the subtree of this row.
  const handleDragLeave = (e: React.DragEvent) => {
    const related = e.relatedTarget as Node | null
    if (related && e.currentTarget.contains(related)) return
    setDropIntent(null)
  }

  const handleDrop = (e: React.DragEvent) => {
    if (!dragState) return
    e.preventDefault()
    e.stopPropagation()
    if (dropIntent) {
      dragState.onDropReorder(node.item.id, dropIntent)
    }
    setDropIntent(null)
  }

  return (
    <div className="relative" onDragOver={handleDragOver} onDragLeave={handleDragLeave} onDrop={handleDrop}>
      {dropIntent === 'before' && (
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 top-0 z-10 h-0.5 bg-brand-brown"
        />
      )}
      {dropIntent === 'after' && (
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 bottom-0 z-10 h-0.5 bg-brand-brown"
        />
      )}
      {dropIntent === 'inside' && (
        <div
          aria-hidden
          className="pointer-events-none absolute inset-0 z-10 rounded-md bg-status-favorite-bg/60"
        />
      )}

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
