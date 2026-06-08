import { useEffect, useCallback, type RefObject } from 'react'

export function useClickOutside(
  ref: RefObject<HTMLElement | null>,
  callback: () => void,
  events: Array<keyof DocumentEventMap> = ['mousedown', 'touchstart'],
) {
  const handler = useCallback(
    (event: Event) => {
      const target = event.target as Node | null
      if (ref.current && target && !ref.current.contains(target)) {
        callback()
      }
    },
    [ref, callback],
  )

  useEffect(() => {
    events.forEach((event) => document.addEventListener(event, handler, { capture: true }))
    return () => {
      events.forEach((event) => document.removeEventListener(event, handler, { capture: true }))
    }
  }, [events, handler])
}
