import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { PageTreeNode } from '@/entities/notebook'

export interface PageNodeMenuProps {
  node: PageTreeNode
  onToggleArchive: (node: PageTreeNode) => void
  /** Opens the confirm dialog; the actual delete lives one step further. */
  onDelete: (node: PageTreeNode) => void
  /** Markdown import as a subpage of this node; omitted hides the entry. */
  onImportSubpage?: (node: PageTreeNode) => void
}

/**
 * The ⋯ on a tree row. Archiving toggles in place — it is reversible — while
 * deleting hands off to a confirm dialog.
 */
export function PageNodeMenu({ node, onToggleArchive, onDelete, onImportSubpage }: PageNodeMenuProps) {
  const { t } = useTranslation()
  const [open, setOpen] = useState(false)
  const rootRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!open) {
      return
    }
    const onPointerDown = (event: PointerEvent) => {
      if (rootRef.current !== null && !rootRef.current.contains(event.target as Node)) {
        setOpen(false)
      }
    }
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setOpen(false)
      }
    }
    document.addEventListener('pointerdown', onPointerDown)
    document.addEventListener('keydown', onKeyDown)
    return () => {
      document.removeEventListener('pointerdown', onPointerDown)
      document.removeEventListener('keydown', onKeyDown)
    }
  }, [open])

  return (
    <div ref={rootRef} className="relative shrink-0">
      <button
        type="button"
        aria-haspopup="menu"
        aria-expanded={open}
        aria-label={t('reader.pageMenu', { title: node.title })}
        title={t('reader.pageMenu', { title: node.title })}
        onClick={() => {
          setOpen((value) => !value)
        }}
        className="grid size-5 place-items-center rounded text-muted transition-all hover:bg-muted-soft hover:text-ink focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent sm:opacity-0 sm:group-hover:opacity-100 sm:group-focus-within:opacity-100"
      >
        <svg viewBox="0 0 16 16" className="size-3.5" fill="currentColor" aria-hidden="true">
          <circle cx="3" cy="8" r="1.3" />
          <circle cx="8" cy="8" r="1.3" />
          <circle cx="13" cy="8" r="1.3" />
        </svg>
      </button>

      {open ? (
        <div
          role="menu"
          aria-label={t('reader.pageMenu', { title: node.title })}
          className="absolute right-0 z-20 mt-1 min-w-32 rounded-xl border border-line bg-card p-1 shadow-lg"
        >
          {onImportSubpage !== undefined ? (
            <button
              type="button"
              role="menuitem"
              onClick={() => {
                setOpen(false)
                onImportSubpage(node)
              }}
              className="w-full rounded-lg px-3 py-2 text-left text-sm text-ink transition-colors hover:bg-canvas focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent"
            >
              {t('reader.importSubpage', { title: node.title })}
            </button>
          ) : null}
          <button
            type="button"
            role="menuitem"
            onClick={() => {
              setOpen(false)
              onToggleArchive(node)
            }}
            className="w-full rounded-lg px-3 py-2 text-left text-sm text-ink transition-colors hover:bg-canvas focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent"
          >
            {node.isArchived ? t('reader.unarchive') : t('reader.archive')}
          </button>
          <button
            type="button"
            role="menuitem"
            onClick={() => {
              setOpen(false)
              onDelete(node)
            }}
            className="w-full rounded-lg px-3 py-2 text-left text-sm text-danger transition-colors hover:bg-danger-soft focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent"
          >
            {t('menu.delete')}
          </button>
        </div>
      ) : null}
    </div>
  )
}
