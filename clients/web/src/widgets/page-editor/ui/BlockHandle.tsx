import { useEffect, useRef, useState } from 'react'
import type { DragEvent } from 'react'
import { useTranslation } from 'react-i18next'
import type { SlashTarget } from '../lib/blockTypes'
import { SLASH_ITEMS } from '../lib/blockTypes'

export interface BlockHandleProps {
  /** Only text blocks can convert — the others would lose their payload. */
  canTurnInto: boolean
  onTurnInto: (target: SlashTarget) => void
  onDelete: () => void
  onSelect: () => void
  /** Opens the block's revision log; only persisted blocks have one. */
  onShowHistory?: () => void
  onDragStart: (event: DragEvent) => void
  onDragEnd: () => void
}

/**
 * The six-dot grip at a block's left edge: click selects the block and opens
 * its menu (turn into / delete); dragging it reorders the block.
 */
export function BlockHandle({
  canTurnInto,
  onTurnInto,
  onDelete,
  onSelect,
  onShowHistory,
  onDragStart,
  onDragEnd,
}: BlockHandleProps) {
  const { t } = useTranslation()
  const [open, setOpen] = useState(false)
  const rootRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!open) {
      return
    }
    const onPointerDown = (event: MouseEvent) => {
      if (rootRef.current !== null && !rootRef.current.contains(event.target as Node)) {
        setOpen(false)
      }
    }
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        event.stopPropagation()
        setOpen(false)
      }
    }
    document.addEventListener('mousedown', onPointerDown)
    document.addEventListener('keydown', onKeyDown, true)
    return () => {
      document.removeEventListener('mousedown', onPointerDown)
      document.removeEventListener('keydown', onKeyDown, true)
    }
  }, [open])

  return (
    // -left-8 parks the grip in the gutter; the parent group reveals it on hover.
    <div ref={rootRef} className="absolute top-0.5 -left-8 z-10">
      <button
        type="button"
        draggable
        aria-label={t('editor.blockMenu')}
        aria-haspopup="menu"
        aria-expanded={open}
        onClick={() => {
          onSelect()
          setOpen((value) => !value)
        }}
        onDragStart={onDragStart}
        onDragEnd={onDragEnd}
        className={`rounded p-1 text-muted transition-colors hover:bg-muted-soft hover:text-ink focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent ${
          open ? 'opacity-100' : 'opacity-0 group-hover:opacity-100 focus-visible:opacity-100'
        }`}
      >
        <svg viewBox="0 0 10 16" className="size-3.5" fill="currentColor" aria-hidden>
          <circle cx="2.5" cy="2.5" r="1.4" />
          <circle cx="7.5" cy="2.5" r="1.4" />
          <circle cx="2.5" cy="8" r="1.4" />
          <circle cx="7.5" cy="8" r="1.4" />
          <circle cx="2.5" cy="13.5" r="1.4" />
          <circle cx="7.5" cy="13.5" r="1.4" />
        </svg>
      </button>

      {open ? (
        <div
          role="menu"
          aria-label={t('editor.blockMenu')}
          className="absolute left-0 mt-1 w-44 rounded-lg border border-line bg-card p-1 shadow-lg"
        >
          {canTurnInto ? (
            <>
              <div className="px-2 py-1 text-[0.7rem] font-semibold tracking-wide text-muted uppercase">
                {t('editor.turnInto')}
              </div>
              {SLASH_ITEMS.map((item) => (
                <button
                  key={item.id}
                  type="button"
                  role="menuitem"
                  className="block w-full rounded px-2 py-1 text-left text-sm text-ink hover:bg-muted-soft"
                  onClick={() => {
                    setOpen(false)
                    onTurnInto(item.target)
                  }}
                >
                  {t(`editor.slash.${item.id}`)}
                </button>
              ))}
              <div className="my-1 border-t border-line" />
            </>
          ) : null}
          {onShowHistory !== undefined ? (
            <button
              type="button"
              role="menuitem"
              className="block w-full rounded px-2 py-1 text-left text-sm text-ink hover:bg-muted-soft"
              onClick={() => {
                setOpen(false)
                onShowHistory()
              }}
            >
              {t('editor.blockHistory')}
            </button>
          ) : null}
          <button
            type="button"
            role="menuitem"
            className="block w-full rounded px-2 py-1 text-left text-sm text-danger hover:bg-danger-soft"
            onClick={() => {
              setOpen(false)
              onDelete()
            }}
          >
            {t('editor.deleteBlock')}
          </button>
        </div>
      ) : null}
    </div>
  )
}
