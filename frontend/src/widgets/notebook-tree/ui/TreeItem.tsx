import { useState, useRef, useEffect, memo } from 'react'
import type { TreeNode } from '@/entities/notebook'
import { useTreeContext } from '../model/TreeContext'
import TreeFolderNode from './TreeFolderNode'
import TreePageNode from './TreePageNode'

type DropIntent = 'before' | 'after' | 'inside'

interface TreeItemProps {
  node: TreeNode
  level: number
}

/**
 * Wraps each tree row in a single drop target. The intent is derived from
 * the cursor's Y position relative to the row's bounding box:
 *   - top half    -> 'before'  (insert above this item)
 *   - bottom half -> 'after'   (insert below this item)
 *
 * The visual is a single thin line at the top or bottom edge of the row,
 * so the user always sees one indicator, never two competing ones. The
 * empty-folder case (drop = "as only child") is handled downstream in
 * handleDropReorder — it rewrites the newParentId when the target is an
 * empty folder, so the visual stays the same thin line regardless of
 * whether the target is a page, a folder, or an empty folder.
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
function TreeItem({ node, level }: TreeItemProps) {
  const { notebookSlug, activePath, dragState } = useTreeContext()
  const [dropIntent, setDropIntent] = useState<DropIntent | null>(null)
  const dropIntentRef = useRef<DropIntent | null>(null)
  const isFolder = node.item.type === 'folder'

  const updateIntent = (next: DropIntent | null) => {
    dropIntentRef.current = next
    setDropIntent(next)
  }

  const handleDragOver = (e: React.DragEvent) => {
    const draggingId = dragState?.draggingId
    if (!dragState || !draggingId) return
    // Always stop propagation when a drag is in progress, regardless of
    // whether THIS item is a valid drop target. The dragged item itself
    // (cursor sitting on it without moving) and any of its descendants
    // (cursor on a child while dragging a folder) must not let the
    // dragover bubble to the root container in TreeContent — otherwise
    // its `onDragOver` paints the entire tree sidebar with the
    // `rootDragOver` background. The early-return below handles "is
    // this a valid target"; the bubble control is independent.
    e.stopPropagation()
    // The dragged item's own subtree (itself included, since
    // draggedSubtreeIds is built by walking from the dragged node
    // downward) is not a valid drop target — those drops are either
    // no-ops (self-drop) or rejected by the server-side descendant
    // check. The draggable row carries `data-tree-item-id`; we walk
    // up via closest() so the check still works when the cursor is
    // on a button/icon inside the row.
    const targetEl = e.target as HTMLElement | null
    const targetItemId = targetEl?.closest('[data-tree-item-id]')?.getAttribute('data-tree-item-id')
    if (targetItemId && dragState.draggedSubtreeIds.has(targetItemId)) return

    e.preventDefault()

    // The drop intent is the cursor's Y position relative to the row's
    // bounding box. The empty-folder case (drop = "as only child") is
    // handled downstream in handleDropReorder — it just rewrites the
    // newParentId when the target is an empty folder, so the visual can
    // stay the same thin line at the top/bottom edge regardless of
    // whether the target is a folder, a page, or an empty folder.
    const rect = e.currentTarget.getBoundingClientRect()
    const midpoint = rect.top + rect.height / 2
    const next: DropIntent = e.clientY < midpoint ? 'before' : 'after'
    if (dropIntentRef.current !== next) {
      updateIntent(next)
    }
  }

  // dragLeave always clears. Children with their own drop targets
  // stopPropagation in their handleDragOver, so this wrapper's dragOver
  // never re-fires to re-set the intent when the cursor moves to a
  // child — the indicator would stick on the parent even though the
  // child is now the real target. Clearing here is the simpler
  // invariant: "cursor not on this row → no line".
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
      {dropIntent && (
        <div
          aria-hidden
          className={`pointer-events-none absolute inset-x-0 z-10 h-0.5 bg-brand-brown ${dropIntent === 'before' ? 'top-0' : 'bottom-0'}`}
        />
      )}

      {isFolder ? (
        <TreeFolderNode
          node={node}
          level={level}
        >
          {node.children.map((child) => (
            <TreeItem
              key={child.item.id}
              node={child}
              level={level + 1}
            />
          ))}
        </TreeFolderNode>
      ) : (
        <TreePageNode
          node={node}
          notebookSlug={notebookSlug}
          activePath={activePath}
          level={level}
        />
      )}
    </div>
  )
}

export default memo(TreeItem)
