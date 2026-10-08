import { useEffect, useLayoutEffect, useRef, useState } from 'react'
import type { ClipboardEvent, KeyboardEvent, MouseEvent } from 'react'
import type { MarkDto, SpanDto } from '@/entities/block'
import {
  detectSlashQuery,
  filterSlashItems,
} from '../lib/blockTypes'
import type { SlashItem, SlashTarget } from '../lib/blockTypes'
import {
  getCaretOffset,
  getClampedSelectionOffsets,
  getSelectionOffsets,
  parseEditableDom,
  rangeForOffsets,
  setSelectionOffsets,
} from '../lib/editableDom'
import { applyLink, linkHrefInRange, normalizeHref, rangeMarks, toggleMark } from '../lib/marks'
import type { SimpleMarkKind } from '../lib/marks'
import { insertTextAt, spansEqual, spansPlainText, splitSpansAt } from '../lib/spans'
import { parsePastedBlocks, parsePastedLine } from '../lib/paste'
import type { PastedBlock } from '../lib/paste'
import { matchBacktickRule, matchDashRule, matchSpaceRule } from '../lib/inputRules'
import { EditableSpans } from './EditableSpans'
import { MarkToolbar } from './MarkToolbar'
import { SlashMenu } from './SlashMenu'

/** The editing behaviours every text block shares; per-type wrappers supply styling. */
export interface TextBlockEngineProps {
  spans: readonly SpanDto[]
  /** One-shot caret request from a split/merge/navigation; consumed via onFocusHandled. */
  focusOffset?: number | null
  /** `structural` marks non-typing edits (marks, links): they never coalesce in undo history. */
  onChange: (spans: SpanDto[], structural?: boolean) => void
  onSplit: (offset: number) => void
  onMergeBackward: () => void
  onFocusPrevious: () => void
  onFocusNext: () => void
  onFocusHandled: () => void
  /** Shift+Enter inserts a soft line break unless the block type is single-line (headings). */
  allowSoftBreak?: boolean
  /** Tab / Shift+Tab: change depth; receives the caret offset for refocusing. */
  onIndent?: (offset: number) => void
  onOutdent?: (offset: number) => void
  /** Ctrl/Cmd+Shift+ArrowUp/Down: reorder within the sibling group. */
  onMoveBlock?: (direction: -1 | 1, offset: number) => void
  /** Present when slash-conversion is enabled; receives the target and the text after the `/query` prefix. */
  onTransform?: (target: SlashTarget, spans: SpanDto[]) => void
  /** Multi-line paste becomes whole blocks; without it, text lands in this block as-is. */
  onPasteBlocks?: (offset: number, blocks: PastedBlock[]) => void
}

export interface TextBlockEditorProps extends TextBlockEngineProps {
  className?: string
  placeholder?: string
  ariaLabel: string
}

/** The contenteditable a selection endpoint sits in, if any. */
function editableRootOf(node: Node | null): HTMLElement | null {
  const element =
    node === null
      ? null
      : node.nodeType === Node.ELEMENT_NODE
        ? (node as HTMLElement)
        : node.parentElement
  const root = element?.closest('[contenteditable]') ?? null
  return root instanceof HTMLElement ? root : null
}

/**
 * One contenteditable per block. The DOM is the source of truth while the
 * block has focus; every local edit records the caret as a plain-text offset
 * and a layout effect puts it back after React re-renders from the new model
 * state. IME composition defers parsing until the composition commits.
 */
export function TextBlockEditor({
  spans,
  className,
  placeholder,
  ariaLabel,
  focusOffset = null,
  onChange,
  onSplit,
  onMergeBackward,
  onFocusPrevious,
  onFocusNext,
  onFocusHandled,
  allowSoftBreak = true,
  onIndent,
  onOutdent,
  onMoveBlock,
  onTransform,
  onPasteBlocks,
}: TextBlockEditorProps) {
  const ref = useRef<HTMLDivElement>(null)
  const wrapperRef = useRef<HTMLDivElement>(null)
  const composingRef = useRef(false)
  const pendingSelectionRef = useRef<{ start: number; end: number } | null>(null)
  const [selection, setSelection] = useState<{
    start: number
    end: number
    top: number
    left: number
  } | null>(null)
  const [linkOpen, setLinkOpen] = useState(false)
  const [slashQuery, setSlashQuery] = useState<string | null>(null)
  const [slashIndex, setSlashIndex] = useState(0)

  //
  // The contenteditable crash guard. The browser mutates this DOM directly as
  // the user types, so React must NEVER reconcile the content subtree against
  // it: a stale virtual tree plus a browser-mutated DOM is the classic
  // removeChild crash. Local keystrokes update `domSpans` but leave
  // `rendered` alone — old and new vdom are identical, so React performs zero
  // DOM operations. Only an external change (split/merge, decided by the
  // parent) swaps `rendered`, and that remounts the subtree wholesale.
  //
  const [domSpans, setDomSpans] = useState<readonly SpanDto[]>(spans)
  const [rendered, setRendered] = useState<{ spans: readonly SpanDto[]; epoch: number }>({
    spans,
    epoch: 0,
  })
  if (!spansEqual(spans, domSpans)) {
    setDomSpans(spans)
    setRendered({ spans, epoch: rendered.epoch + 1 })
  }

  const commitFromDom = () => {
    const element = ref.current
    if (element === null) {
      return
    }
    const caret = getCaretOffset(element)
    pendingSelectionRef.current = { start: caret, end: caret }
    const parsed = parseEditableDom(element)
    setDomSpans(parsed)
    if (onTransform !== undefined) {
      syncSlash(spansPlainText(parsed))
    }
    onChange(parsed)
  }

  const syncSlash = (plainText: string) => {
    const query = detectSlashQuery(plainText)
    setSlashQuery((current) => {
      if (query !== current) {
        setSlashIndex(0)
      }
      return query
    })
  }

  const slashItems = slashQuery === null ? [] : filterSlashItems(slashQuery)

  const pickSlashItem = (item: SlashItem) => {
    if (slashQuery === null || onTransform === undefined) {
      return
    }
    // Strip the "/query" prefix; what remains becomes the new block's text.
    const [, rest] = splitSpansAt(spans, 1 + slashQuery.length)
    setSlashQuery(null)
    onTransform(item.target, rest)
  }

  /**
   * A structural local change (mark toggle, link): the DOM cannot express it,
   * so the content subtree remounts and the selection is restored afterwards.
   */
  const commitStructural = (next: SpanDto[], range: { start: number; end: number }) => {
    pendingSelectionRef.current = range
    setDomSpans(next)
    setRendered((current) => ({ spans: next, epoch: current.epoch + 1 }))
    onChange(next, true)
  }

  const captureSelection = () => {
    const element = ref.current
    if (element === null) {
      return
    }
    const offsets = ownedSelectionOffsets(element)
    if (offsets === null) {
      setSelection(null)
      return
    }
    // Measure the in-block portion, so a selection spilling past the block
    // boundary does not drag the toolbar's position along with it. jsdom has
    // no layout: fall back to a fixed spot above the block.
    const range = rangeForOffsets(element, offsets.start, offsets.end)
    const rangeRect =
      range !== null && typeof range.getBoundingClientRect === 'function'
        ? range.getBoundingClientRect()
        : null
    const boxRect = element.getBoundingClientRect()
    setSelection({
      ...offsets,
      top: rangeRect !== null ? rangeRect.top - boxRect.top - 4 : -8,
      left: rangeRect !== null ? rangeRect.left - boxRect.left + rangeRect.width / 2 : 24,
    })
  }

  /**
   * This block's share of the document selection, clamped to its content —
   * or null when the selection misses the block, or another block owns it.
   * The anchor's block wins; when the drag started outside every block, the
   * block under the pointer takes over. Either way, ONE toolbar shows.
   */
  const ownedSelectionOffsets = (
    element: HTMLElement,
  ): { start: number; end: number } | null => {
    const domSelection = window.getSelection()
    if (domSelection === null) {
      return null
    }
    const owner =
      editableRootOf(domSelection.anchorNode) ?? editableRootOf(domSelection.focusNode)
    if (owner !== element) {
      return null
    }
    return getClampedSelectionOffsets(element)
  }

  const handleToggleMark = (kind: SimpleMarkKind) => {
    if (selection === null) {
      return
    }
    commitStructural(toggleMark(spans, selection.start, selection.end, kind), {
      start: selection.start,
      end: selection.end,
    })
  }

  const handleLinkClick = () => {
    if (selection !== null) {
      setLinkOpen(true)
    }
  }

  // A keystroke re-renders from the new spans; put the caret back afterwards.
  // Structural changes remount the editable div, which drops focus — take it
  // back before restoring the selection.
  useLayoutEffect(() => {
    const element = ref.current
    if (element !== null && pendingSelectionRef.current !== null) {
      if (document.activeElement !== element) {
        element.focus()
      }
      setSelectionOffsets(element, pendingSelectionRef.current.start, pendingSelectionRef.current.end)
      pendingSelectionRef.current = null
    }
  })

  useLayoutEffect(() => {
    const element = ref.current
    if (element !== null && focusOffset !== null) {
      element.focus()
      setSelectionOffsets(element, focusOffset, focusOffset)
      onFocusHandled()
    }
  }, [focusOffset, onFocusHandled])

  const handleKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    const element = ref.current
    if (element === null || composingRef.current) {
      return
    }
    // While the slash menu is open it owns navigation, confirmation and Escape.
    if (slashQuery !== null) {
      if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
        event.preventDefault()
        const count = slashItems.length
        if (count > 0) {
          const delta = event.key === 'ArrowDown' ? 1 : -1
          setSlashIndex((current) => (current + delta + count) % count)
        }
        return
      }
      if (event.key === 'Enter') {
        event.preventDefault()
        const item = slashItems[slashIndex]
        if (item !== undefined) {
          pickSlashItem(item)
        }
        return
      }
      if (event.key === 'Escape') {
        // Close the menu, not the editor — keep the event from bubbling up.
        event.preventDefault()
        event.stopPropagation()
        setSlashQuery(null)
        return
      }
    }
    // Escape bubbles up to the page: selection-mode clearing or cancel handles it.
    // Markdown input rules: "- " + Space converts the block, Notion-style;
    // the third backtick of ``` opens a code block, the third dash a divider.
    // The marker is the whole pre-caret text, so rules never fire mid-sentence.
    if (onTransform !== undefined && !event.metaKey && !event.ctrlKey && !event.altKey) {
      const offset = getCaretOffset(element)
      const beforeCaret = spansPlainText(spans).slice(0, offset)
      const target =
        event.key === ' '
          ? matchSpaceRule(beforeCaret)
          : event.key === '`'
            ? matchBacktickRule(beforeCaret)
            : event.key === '-'
              ? matchDashRule(beforeCaret)
              : null
      if (target !== null) {
        event.preventDefault()
        const [, rest] = splitSpansAt(spans, offset)
        setSlashQuery(null)
        onTransform(target, rest)
        return
      }
    }
    // Ctrl/Cmd+B/I/U apply marks without opening the toolbar.
    if ((event.metaKey || event.ctrlKey) && !event.shiftKey && !event.altKey) {
      const shortcut: Record<string, SimpleMarkKind> = { b: 'bold', i: 'italic', u: 'underline' }
      const kind = shortcut[event.key.toLowerCase()]
      const offsets = getSelectionOffsets(element)
      if (kind !== undefined && offsets !== null && offsets.start < offsets.end) {
        event.preventDefault()
        commitStructural(toggleMark(spans, offsets.start, offsets.end, kind), offsets)
        captureSelection()
        return
      }
    }
    if (event.key === 'Tab') {
      event.preventDefault()
      const offset = getCaretOffset(element)
      if (event.shiftKey) {
        onOutdent?.(offset)
      } else {
        onIndent?.(offset)
      }
      return
    }
    // Ctrl/Cmd+Shift+Arrow reorders the block within its sibling group.
    if (
      (event.metaKey || event.ctrlKey) &&
      event.shiftKey &&
      (event.key === 'ArrowUp' || event.key === 'ArrowDown')
    ) {
      event.preventDefault()
      onMoveBlock?.(event.key === 'ArrowUp' ? -1 : 1, getCaretOffset(element))
      return
    }
    // Enter splits the block; Shift+Enter inserts a soft line break where the
    // block type supports one (headings are single-line). Ctrl/Cmd+Enter is
    // the save shortcut — untouched.
    if (event.key === 'Enter' && !event.ctrlKey && !event.metaKey) {
      event.preventDefault()
      if (event.shiftKey && allowSoftBreak) {
        const offset = getCaretOffset(element)
        pendingSelectionRef.current = { start: offset + 1, end: offset + 1 }
        onChange(insertTextAt(spans, offset, '\n'))
      } else {
        onSplit(getCaretOffset(element))
      }
    } else if (event.key === 'Backspace' && getCaretOffset(element) === 0) {
      event.preventDefault()
      onMergeBackward()
    } else if (event.key === 'ArrowUp' && getCaretOffset(element) === 0) {
      event.preventDefault()
      onFocusPrevious()
    } else if (
      event.key === 'ArrowDown' &&
      getCaretOffset(element) === spansPlainText(spans).length
    ) {
      event.preventDefault()
      onFocusNext()
    }
  }

  const handlePaste = (event: ClipboardEvent<HTMLDivElement>) => {
    event.preventDefault()
    const element = ref.current
    if (element === null) {
      return
    }
    // Normalize line endings so Windows clipboards do not leak CR.
    const text = event.clipboardData.getData('text/plain').replace(/\r\n?/g, '\n')
    if (text.length === 0) {
      return
    }
    const offset = getCaretOffset(element)
    const parsed = parsePastedBlocks(text)
    if (parsed !== null && onPasteBlocks !== undefined) {
      // A multi-line paste supersedes any open slash menu along with the text.
      setSlashQuery(null)
      onPasteBlocks(offset, parsed)
      return
    }
    // A single markdown line pasted into an EMPTY block converts the block.
    if (onPasteBlocks !== undefined && spansPlainText(spans).length === 0) {
      const single = parsePastedLine(text)
      if (single !== null) {
        setSlashQuery(null)
        onPasteBlocks(offset, [single])
        return
      }
    }
    // Otherwise: a plain in-block insert.
    pendingSelectionRef.current = { start: offset + text.length, end: offset + text.length }
    const next = insertTextAt(spans, offset, text)
    if (onTransform !== undefined) {
      syncSlash(spansPlainText(next))
    }
    onChange(next)
  }

  // Editing must never navigate: links inside the block are inert here.
  const handleClick = (event: MouseEvent<HTMLDivElement>) => {
    if ((event.target as HTMLElement).closest('a') !== null) {
      event.preventDefault()
    }
  }

  const activeMarks: ReadonlySet<MarkDto['kind']> =
    selection === null
      ? new Set<MarkDto['kind']>()
      : rangeMarks(spans, selection.start, selection.end)

  // One toolbar at a time, and it must not linger: whenever the document's
  // selection leaves this block (or collapses), retract ours.
  useEffect(() => {
    const retract = () => {
      const element = ref.current
      const wrapper = wrapperRef.current
      if (element === null || wrapper === null) {
        return
      }
      // Focus inside the wrapper but outside the editable area means the link
      // input is being used — keep the toolbar open for it.
      const active = document.activeElement
      if (active !== null && active !== element && wrapper.contains(active)) {
        return
      }
      if (ownedSelectionOffsets(element) === null) {
        setSelection(null)
        setLinkOpen(false)
      }
    }
    document.addEventListener('selectionchange', retract)
    // A drag that starts inside the block may end anywhere on the page; the
    // div's own onMouseUp never fires then, so capture at the document level.
    document.addEventListener('mouseup', captureSelection)
    return () => {
      document.removeEventListener('selectionchange', retract)
      document.removeEventListener('mouseup', captureSelection)
    }
  }, [])

  return (
    <div ref={wrapperRef} className="relative">
      {slashQuery !== null && onTransform !== undefined ? (
        <SlashMenu
          items={slashItems}
          activeIndex={slashIndex}
          onPick={pickSlashItem}
          onHover={setSlashIndex}
        />
      ) : null}
      {selection !== null ? (
        <MarkToolbar
          top={selection.top}
          left={selection.left}
          active={activeMarks}
          linkOpen={linkOpen}
          initialHref={linkHrefInRange(spans, selection.start, selection.end)}
          onToggle={handleToggleMark}
          onLinkClick={handleLinkClick}
          onLinkSubmit={(href) => {
            setLinkOpen(false)
            if (href.length > 0) {
              commitStructural(applyLink(spans, selection.start, selection.end, normalizeHref(href)), {
                start: selection.start,
                end: selection.end,
              })
            }
          }}
          onLinkRemove={() => {
            setLinkOpen(false)
            commitStructural(applyLink(spans, selection.start, selection.end, null), {
              start: selection.start,
              end: selection.end,
            })
          }}
          onLinkCancel={() => {
            setLinkOpen(false)
          }}
        />
      ) : null}
      {/**
       * The epoch key is on the EDITABLE DIV, not the span subtree: React can
       * only remove nodes it created itself, and the browser may have added
       * its own (typing into an empty block). Remounting the div wholesale
       * takes every foreign node down with it — no duplicated text.
       */}
      <div
        key={rendered.epoch}
        ref={ref}
        contentEditable
        suppressContentEditableWarning
        role="textbox"
        aria-label={ariaLabel}
        aria-multiline="true"
        spellCheck
        data-placeholder={placeholder}
        className={`rounded px-2 py-1 outline-none focus:bg-muted-soft/40 empty:before:text-muted empty:before:content-[attr(data-placeholder)] ${className ?? ''}`}
        onInput={() => {
          if (!composingRef.current) {
            commitFromDom()
          }
        }}
        onCompositionStart={() => {
          composingRef.current = true
        }}
        onCompositionEnd={() => {
          composingRef.current = false
          commitFromDom()
        }}
        onKeyDown={handleKeyDown}
        onPaste={handlePaste}
        onClick={handleClick}
        onBlur={() => {
          setSlashQuery(null)
        }}
        onMouseUp={captureSelection}
        onKeyUp={(event) => {
          const selectAll = (event.metaKey || event.ctrlKey) && event.key.toLowerCase() === 'a'
          if (event.shiftKey || event.key === 'Shift' || selectAll) {
            captureSelection()
          }
        }}
      >
        <EditableSpans spans={rendered.spans} />
      </div>
    </div>
  )
}
