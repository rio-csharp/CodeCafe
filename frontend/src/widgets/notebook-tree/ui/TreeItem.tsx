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

  const updateIntent = (next: DropIntent | null) => {
    dropIntentRef.current = next
    setDropIntent(next)
  }

  const handleDragOver = (e: React.DragEvent) => {
    const draggingId = dragState?.draggingId
    if (!dragState || !draggingId) return
    // The dragged item itself can't drop on itself. (This is also caught
    // downstream by handleDropReorder, but rejecting here keeps the
    // indicator off in the first place.)
    if (draggingId === node.item.id) return
    // If the cursor is on the dragged item or any of its descendants,
    // don't show an indicator — those drops are either no-ops or
    // rejected by the server-side descendant check, so the line would
    // be a lie. The draggable row carries `data-tree-item-id`; we walk
    // up via closest() so the check still works when the cursor is on
    // a button/icon inside the row.
    const targetEl = e.target as HTMLElement | null
    const targetItemId = targetEl?.closest('[data-tree-item-id]')?.getAttribute('data-tree-item-id')
    if (targetItemId && dragState.draggedSubtreeIds.has(targetItemId)) return

    e.preventDefault()
    e.stopPropagation()

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
    const clearIntent = () => {
      updateIntent(null)
    }
    // dragend is the canonical "drag finished" signal — fires on the
    // source element and bubbles to the document. Covers the normal
    // drop-on-a-TreeItem path as well as release-outside / Esc / etc.
    document.addEventListener('dragend', clearIntent)
    // dragstart safety net: if a previous drag ended without firing
    // dragend (some browser bugs, drag cancelled by the OS), the
    // leftover intent would otherwise stay armed under the cursor
    // until the next drop. Clearing on the next dragstart makes that
    // impossible regardless of what the previous drag did.
    document.addEventListener('dragstart', clearIntent)
    // mouseup on window is a defensive fallback: there are edge cases
    // (e.g. dropping on a non-DOM scrollbar, very fast release, certain
    // browser bugs) where dragend doesn't reach the document. The handler
    // is a no-op when the intent is already null, so it doesn't
    // interfere with the normal flow — the worst case is the indicator
    // clears one tick earlier on a non-drag mouseup, which is invisible
    // because the indicator was already null.
    window.addEventListener('mouseup', clearIntent)
    // If the tab loses visibility mid-drag (alt-tab, browser losing
    // focus), the drag is effectively over from the user's POV; the
    // dragend often doesn't fire in that path either.
    document.addEventListener('visibilitychange', clearIntent)
    return () => {
      document.removeEventListener('dragend', clearIntent)
      document.removeEventListener('dragstart', clearIntent)
      window.removeEventListener('mouseup', clearIntent)
      document.removeEventListener('visibilitychange', clearIntent)
    }
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
