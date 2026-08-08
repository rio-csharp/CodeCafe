import { useEffect, useRef, type RefObject } from 'react'

// Hoisted so the default doesn't create a new array (and re-run the effect)
// on every render of the caller.
const DEFAULT_EVENTS: Array<keyof DocumentEventMap> = ['mousedown', 'touchstart']

export function useClickOutside(
  ref: RefObject<HTMLElement | null>,
  callback: () => void,
  events: Array<keyof DocumentEventMap> = DEFAULT_EVENTS,
) {
  // Mirror the latest callback in a ref so the document listeners are
  // registered once instead of on every render (callers pass inline arrows).
  const callbackRef = useRef(callback)
  useEffect(() => {
    callbackRef.current = callback
  })

  useEffect(() => {
    const handler = (event: Event) => {
      const target = event.target as Node | null
      if (ref.current && target && !ref.current.contains(target)) {
        callbackRef.current()
      }
    }
    events.forEach((event) => document.addEventListener(event, handler, { capture: true }))
    return () => {
      events.forEach((event) => document.removeEventListener(event, handler, { capture: true }))
    }
  }, [ref, events])
}
