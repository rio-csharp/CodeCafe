import { useState, useRef, useEffect, memo } from 'react'
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
 *
 * The intent is mirrored into a ref because `setDropIntent` only schedules
 * a re-render; the subsequent `drop` event fires before React commits, so
 * the closure would otherwise see a stale `null` and silently skip the
 * reorder. The state drives the visual, the ref drives the drop dispatch.
 *
 * `dragend` listener below clears any leftover intent when any drag in the
 * document ends. The native `dragend` fires on the source element and
 * bubbles to the document for every drag termination path (drop on a
 * target, release outside the tree, Escape cancel, drop on a non-tree
 * element) — the cases where no `drop` event lands on a TreeItem and the
 * indicator line would otherwise stick on the last-hovered item.
 */
function TreeItem({ node, level, siblingCount, index }: TreeItemProps) {
  const { notebookSlug, activePath, dragState } = useTreeContext()
  const [dropIntent, setDropIntent] = useState<DropIntent | null>(null)
  const dropIntentRef = useRef<DropIntent | null>(null)
  const isFolder = node.item.type === 'folder'
  const isFolderEmpty = isFolder && node.children.length === 0

  const updateIntent = (next: DropIntent | null) => {
    dropIntentRef.current = next
    setDropIntent(next)
  }

  const handleDragOver = (e: React.DragEvent) => {
    const draggingId = dragState?.draggingId
    if (!dragState || !draggingId || draggingId === node.item.id) return
    e.preventDefault()
    e.stopPropagation()

    let next: DropIntent
    if (isFolderEmpty) {
      next = 'inside'
    } else {
      const rect = e.currentTarget.getBoundingClientRect()
      const midpoint = rect.top + rect.height / 2
      next = e.clientY < midpoint ? 'before' : 'after'
    }
    if (dropIntentRef.current !== next) {
      updateIntent(next)
    }
  }

  // dragLeave always clears. We deliberately do NOT keep the intent when
  // the cursor moves to a descendant, because the descendants that have
  // their own drop targets (child TreeItems) call e.stopPropagation() in
  // their own handleDragOver, so this wrapper's dragOver never re-fires
  // to re-set the intent — the indicator would stick on the parent even
  // though the child is now the real target. Non-drop-target descendants
  // (buttons, icons inside the row) re-fire dragOver via bubbling and
  // the intent comes back the same tick, so the visible state is the
  // same as the previous contains() short-circuit at no real cost.
  const handleDragLeave = () => {
    updateIntent(null)
  }

  const handleDrop = (e: React.DragEvent) => {
    if (!dragState) return
    e.preventDefault()
    e.stopPropagation()
    const intent = dropIntentRef.current
    updateIntent(null)
    if (intent) {
      dragState.onDropReorder(node.item.id, intent)
    }
  }

  useEffect(() => {
    const handleDragEnd = () => {
      updateIntent(null)
    }
    document.addEventListener('dragend', handleDragEnd)
    return () => document.removeEventListener('dragend', handleDragEnd)
  }, [])

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
