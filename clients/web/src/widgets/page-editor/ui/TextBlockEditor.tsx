import { useLayoutEffect, useRef, useState } from 'react'
import type { ClipboardEvent, KeyboardEvent } from 'react'
import type { SpanDto } from '@/entities/block'
import { getCaretOffset, parseEditableDom, setCaretOffset } from '../lib/editableDom'
import { insertTextAt, spansEqual, spansPlainText } from '../lib/spans'
import { EditableSpans } from './EditableSpans'

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
  const composingRef = useRef(false)
  const pendingCaretRef = useRef<number | null>(null)

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
    pendingCaretRef.current = getCaretOffset(element)
    const parsed = parseEditableDom(element)
    setDomSpans(parsed)
    onChange(parsed)
  }

  // A keystroke re-renders from the new spans; put the caret back afterwards.
  useLayoutEffect(() => {
    const element = ref.current
    if (
      element !== null &&
      document.activeElement === element &&
      pendingCaretRef.current !== null
    ) {
      setCaretOffset(element, pendingCaretRef.current)
      pendingCaretRef.current = null
    }
  })

  useLayoutEffect(() => {
    const element = ref.current
    if (element !== null && focusOffset !== null) {
      element.focus()
      setCaretOffset(element, focusOffset)
      onFocusHandled()
    }
  }, [focusOffset, onFocusHandled])

  const handleKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    const element = ref.current
    if (element === null || composingRef.current) {
      return
    }
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault()
      onSplit(getCaretOffset(element))
    } else if (event.key === 'Enter' && event.shiftKey) {
      event.preventDefault()
      const offset = getCaretOffset(element)
      pendingCaretRef.current = offset + 1
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
    pendingCaretRef.current = offset + text.length
    onChange(insertTextAt(spans, offset, text))
  }

  return (
    <div
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
    >
      <EditableSpans key={rendered.epoch} spans={rendered.spans} />
    </div>
  )
}
