import { useEffect, useLayoutEffect, useRef, useState } from 'react'
import type { ClipboardEvent, KeyboardEvent, MouseEvent } from 'react'
import type { MarkDto, SpanDto } from '@/entities/block'
import {
  getCaretOffset,
  getSelectionOffsets,
  parseEditableDom,
  setSelectionOffsets,
} from '../lib/editableDom'
import { applyLink, linkHrefInRange, normalizeHref, rangeMarks, toggleMark } from '../lib/marks'
import type { SimpleMarkKind } from '../lib/marks'
import { insertTextAt, spansEqual, spansPlainText } from '../lib/spans'
import { EditableSpans } from './EditableSpans'
import { MarkToolbar } from './MarkToolbar'

/** The editing behaviours every text block shares; per-type wrappers supply styling. */
export interface TextBlockEngineProps {
  spans: readonly SpanDto[]
  /** One-shot caret request from a split/merge/navigation; consumed via onFocusHandled. */
  focusOffset?: number | null
  onChange: (spans: SpanDto[]) => void
  onSplit: (offset: number) => void
  onMergeBackward: () => void
  onFocusPrevious: () => void
  onFocusNext: () => void
  onFocusHandled: () => void
}

export interface TextBlockEditorProps extends TextBlockEngineProps {
  className?: string
  placeholder?: string
  ariaLabel: string
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
    onChange(parsed)
  }

  /**
   * A structural local change (mark toggle, link): the DOM cannot express it,
   * so the content subtree remounts and the selection is restored afterwards.
   */
  const commitStructural = (next: SpanDto[], range: { start: number; end: number }) => {
    pendingSelectionRef.current = range
    setDomSpans(next)
    setRendered((current) => ({ spans: next, epoch: current.epoch + 1 }))
    onChange(next)
  }

  const captureSelection = () => {
    const element = ref.current
    if (element === null) {
      return
    }
    const offsets = getSelectionOffsets(element)
    const domSelection = window.getSelection()
    if (offsets === null || offsets.start === offsets.end || domSelection === null) {
      setSelection(null)
      return
    }
    // jsdom has no layout: fall back to a fixed spot above the block.
    const range = domSelection.getRangeAt(0)
    const rangeRect =
      typeof range.getBoundingClientRect === 'function' ? range.getBoundingClientRect() : null
    const boxRect = element.getBoundingClientRect()
    setSelection({
      ...offsets,
      top: rangeRect !== null ? rangeRect.top - boxRect.top - 4 : -8,
      left: rangeRect !== null ? rangeRect.left - boxRect.left + rangeRect.width / 2 : 24,
    })
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
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault()
      onSplit(getCaretOffset(element))
    } else if (event.key === 'Enter' && event.shiftKey) {
      event.preventDefault()
      const offset = getCaretOffset(element)
      pendingSelectionRef.current = { start: offset + 1, end: offset + 1 }
      onChange(insertTextAt(spans, offset, '\n'))
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
    } else if (event.key === 'Tab') {
      // Indent/outdent arrive with the block-moving slice.
      event.preventDefault()
    }
  }

  const handlePaste = (event: ClipboardEvent<HTMLDivElement>) => {
    event.preventDefault()
    const element = ref.current
    if (element === null) {
      return
    }
    const text = event.clipboardData.getData('text/plain')
    if (text.length === 0) {
      return
    }
    const offset = getCaretOffset(element)
    pendingSelectionRef.current = { start: offset + text.length, end: offset + text.length }
    onChange(insertTextAt(spans, offset, text))
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
      const offsets = getSelectionOffsets(element)
      if (offsets === null || offsets.start === offsets.end) {
        setSelection(null)
        setLinkOpen(false)
      }
    }
    document.addEventListener('selectionchange', retract)
    return () => {
      document.removeEventListener('selectionchange', retract)
    }
  }, [])

  return (
    <div ref={wrapperRef} className="relative">
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
        onMouseUp={captureSelection}
        onKeyUp={(event) => {
          if (event.shiftKey || event.key === 'Shift') {
            captureSelection()
          }
        }}
      >
        <EditableSpans spans={rendered.spans} />
      </div>
    </div>
  )
}
