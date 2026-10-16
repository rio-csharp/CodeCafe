import { useEffect, useState } from 'react'

/**
 * Fallback reading line when the reader's chrome has not published its height
 * yet — roughly the sticky bar plus its breathing room.
 */
const FALLBACK_LINE_PX = 60

/** Slack for "the page cannot scroll any further". */
const BOTTOM_SLACK_PX = 8

/** The sticky chrome's own offset variable, so both use the same line. */
function readingLine(): number {
  const raw = getComputedStyle(document.documentElement).getPropertyValue('--reader-chrome-offset')
  const value = Number.parseFloat(raw)
  return Number.isFinite(value) && value > 0 ? value : FALLBACK_LINE_PX
}

/** The nearest ancestor that actually scrolls, if any. */
function scrollParentOf(node: HTMLElement): HTMLElement | null {
  let parent = node.parentElement
  while (parent !== null) {
    const overflowY = getComputedStyle(parent).overflowY
    if (overflowY === 'auto' || overflowY === 'scroll') {
      return parent
    }
    parent = parent.parentElement
  }
  return null
}

/**
 * The id of the heading whose section is being read: the last one that has
 * scrolled past the sticky chrome. A long section therefore stays lit until the
 * next heading takes over. Nothing is lit above the first heading, and the last
 * one lights up early once the page has no more scrolling left in it.
 */
export function useActiveHeading(ids: readonly string[]): string | null {
  const [activeId, setActiveId] = useState<string | null>(null)
  // Keyed by content so a fresh array on every render does not re-subscribe.
  const key = ids.join(' ')

  useEffect(() => {
    const headings = key
      .split(' ')
      .map((id) => document.getElementById(id))
      .filter((heading): heading is HTMLElement => heading !== null)

    let frame: number | null = null
    // Stable for the life of the effect, and getComputedStyle is not cheap.
    const scroller = headings.length === 0 ? null : scrollParentOf(headings[0])

    const measure = () => {
      frame = null
      if (headings.length === 0) {
        setActiveId(null)
        return
      }

      const line = readingLine()
      let current: HTMLElement | null = null
      for (const heading of headings) {
        if (heading.getBoundingClientRect().top > line) {
          break
        }
        current = heading
      }

      const atEnd =
        scroller !== null &&
        scroller.scrollHeight > scroller.clientHeight &&
        scroller.scrollHeight - scroller.scrollTop - scroller.clientHeight <= BOTTOM_SLACK_PX

      setActiveId(atEnd ? headings[headings.length - 1].id : (current?.id ?? null))
    }

    const schedule = () => {
      if (frame === null) {
        frame = requestAnimationFrame(measure)
      }
    }

    // The first measurement waits for a frame: setting state straight from the
    // mount commit would only cascade a second render.
    schedule()
    // Scroll events do not bubble, but they do capture: this is how the
    // outline hears about <main>, whose scrolling moves the headings.
    document.addEventListener('scroll', schedule, { capture: true, passive: true })
    window.addEventListener('resize', schedule)

    // Content can move headings without a scroll or viewport resize: for
    // example, a refreshed page can replace text, or an image can be inserted
    // before a later section. Re-measure after DOM changes and image loads.
    const mutationObserver =
      typeof MutationObserver === 'undefined' || scroller === null
        ? null
        : new MutationObserver(schedule)
    if (mutationObserver !== null && scroller !== null) {
      mutationObserver.observe(scroller, { subtree: true, childList: true, characterData: true })
    }
    const resizeObserver =
      typeof ResizeObserver === 'undefined' || scroller === null ? null : new ResizeObserver(schedule)
    resizeObserver?.observe(scroller!)
    scroller?.addEventListener('load', schedule, true)

    return () => {
      if (frame !== null) {
        cancelAnimationFrame(frame)
      }
      document.removeEventListener('scroll', schedule, { capture: true })
      window.removeEventListener('resize', schedule)
      mutationObserver?.disconnect()
      resizeObserver?.disconnect()
      scroller?.removeEventListener('load', schedule, true)
    }
  }, [key])

  return activeId
}
