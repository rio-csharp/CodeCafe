import { useEffect, type RefObject } from 'react'

/** Keeps the message list pinned to the latest entry as the conversation state changes. */
export function useMessagesAutoScroll(
  endRef: RefObject<HTMLDivElement | null>,
  watch: readonly unknown[],
) {
  useEffect(() => {
    endRef.current?.scrollIntoView?.({ block: 'end' })
    // eslint-disable-next-line react-hooks/exhaustive-deps -- `watch` is forwarded from the caller
  }, watch)
}
