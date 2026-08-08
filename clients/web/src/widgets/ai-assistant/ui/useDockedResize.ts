import { useCallback, useEffect, useRef, useState } from 'react'

const DOCKED_MIN_HEIGHT = 300
const DOCKED_MAX_HEIGHT = 540
const KEYBOARD_RESIZE_STEP = 24

function clampHeight(height: number): number {
  return Math.min(DOCKED_MAX_HEIGHT, Math.max(DOCKED_MIN_HEIGHT, height))
}

/**
 * Pointer + keyboard resizing for the docked (bottom-panel) assistant.
 * The resize handle is a focusable `role="separator"`: ArrowUp grows the
 * panel, ArrowDown shrinks it (the panel is anchored to the bottom, so
 * "up" means taller, matching the pointer drag direction).
 */
export function useDockedResize(isFloating: boolean) {
  const [dockedHeight, setDockedHeight] = useState<number | null>(null)
  const [resizing, setResizing] = useState(false)
  const rootRef = useRef<HTMLDivElement>(null)
  const resizeStartRef = useRef<{ pointerId: number; startY: number; startHeight: number } | null>(null)

  const handleResizeStart = useCallback((event: React.PointerEvent<HTMLDivElement>) => {
    if (event.button !== 0 || isFloating) return
    event.preventDefault()
    resizeStartRef.current = {
      pointerId: event.pointerId,
      startY: event.clientY,
      startHeight: dockedHeight ?? rootRef.current?.getBoundingClientRect().height ?? DOCKED_MIN_HEIGHT,
    }
    ;(event.currentTarget as HTMLElement).setPointerCapture(event.pointerId)
    setResizing(true)
  }, [dockedHeight, isFloating])

  const handleResizeKeyDown = useCallback((event: React.KeyboardEvent<HTMLDivElement>) => {
    if (isFloating || (event.key !== 'ArrowUp' && event.key !== 'ArrowDown')) return
    event.preventDefault()
    const current = dockedHeight ?? rootRef.current?.getBoundingClientRect().height ?? DOCKED_MIN_HEIGHT
    const delta = event.key === 'ArrowUp' ? KEYBOARD_RESIZE_STEP : -KEYBOARD_RESIZE_STEP
    setDockedHeight(clampHeight(current + delta))
  }, [dockedHeight, isFloating])

  // Gated on `resizing` state (not a ref) so the listeners are actually
  // registered when a drag starts and removed when it ends.
  useEffect(() => {
    if (!resizing) return

    function handlePointerMove(event: PointerEvent) {
      const start = resizeStartRef.current
      if (!start || event.pointerId !== start.pointerId) return
      const deltaY = start.startY - event.clientY
      setDockedHeight(clampHeight(start.startHeight + deltaY))
    }

    function handlePointerEnd(event: PointerEvent) {
      const start = resizeStartRef.current
      if (!start || event.pointerId !== start.pointerId) return
      resizeStartRef.current = null
      setResizing(false)
    }

    window.addEventListener('pointermove', handlePointerMove)
    window.addEventListener('pointerup', handlePointerEnd)
    window.addEventListener('pointercancel', handlePointerEnd)
    return () => {
      window.removeEventListener('pointermove', handlePointerMove)
      window.removeEventListener('pointerup', handlePointerEnd)
      window.removeEventListener('pointercancel', handlePointerEnd)
    }
  }, [resizing])

  return { rootRef, dockedHeight, handleResizeStart, handleResizeKeyDown }
}
