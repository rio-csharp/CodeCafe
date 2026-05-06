import { useMemo, useRef, useState } from 'react'
import {
  clampPanelPosition,
  getAnchoredPanelPosition,
  isMobileViewport,
} from './notesAiLayout'
import { loadFabPosition, saveFabPosition } from './notesAiStorage'
import type { DragState, PanelPosition } from './notesAiTypes'

export function useNotesAiPanelPosition() {
  const [fabPosition, setFabPosition] = useState(() => loadFabPosition())
  const [panelPosition, setPanelPosition] = useState<PanelPosition | null>(null)
  const dragStateRef = useRef<DragState | null>(null)
  const panelDragStateRef = useRef<{
    initialLeft: number
    initialTop: number
    pointerId: number
    startX: number
    startY: number
  } | null>(null)

  const isMobile = isMobileViewport()
  const viewportWidth = typeof window === 'undefined' ? 1440 : window.innerWidth
  const viewportHeight = typeof window === 'undefined' ? 900 : window.innerHeight
  const clampedFabPosition = useMemo(() => ({
    x: Math.min(Math.max(12, fabPosition.x), Math.max(12, viewportWidth - 60)),
    y: Math.min(Math.max(12, fabPosition.y), Math.max(12, viewportHeight - 60)),
  }), [fabPosition.x, fabPosition.y, viewportHeight, viewportWidth])
  const effectivePanelPosition = useMemo(
    () => panelPosition ?? getAnchoredPanelPosition(clampedFabPosition, isMobile),
    [clampedFabPosition, isMobile, panelPosition],
  )

  function handleFabPointerDown(event: React.PointerEvent<HTMLButtonElement>) {
    dragStateRef.current = {
      didMove: false,
      initialX: fabPosition.x,
      initialY: fabPosition.y,
      pointerId: event.pointerId,
      startX: event.clientX,
      startY: event.clientY,
    }

    event.currentTarget.setPointerCapture?.(event.pointerId)
  }

  function handleFabPointerMove(event: React.PointerEvent<HTMLButtonElement>) {
    const dragState = dragStateRef.current

    if (!dragState || dragState.pointerId !== event.pointerId) {
      return
    }

    const deltaX = event.clientX - dragState.startX
    const deltaY = event.clientY - dragState.startY

    if (Math.abs(deltaX) > 3 || Math.abs(deltaY) > 3) {
      dragState.didMove = true
    }

    const nextX = Math.max(12, dragState.initialX - deltaX)
    const nextY = Math.max(12, dragState.initialY - deltaY)
    setFabPosition({ x: nextX, y: nextY })
  }

  function handleFabPointerUp(
    event: React.PointerEvent<HTMLButtonElement>,
    onOpen: () => void,
  ) {
    const dragState = dragStateRef.current

    if (!dragState || dragState.pointerId !== event.pointerId) {
      return
    }

    saveFabPosition(clampedFabPosition)

    if (!dragState.didMove) {
      onOpen()
    }

    dragStateRef.current = null
    event.currentTarget.releasePointerCapture?.(event.pointerId)
  }

  function handlePanelPointerDown(event: React.PointerEvent<HTMLElement>) {
    if (isMobile) {
      return
    }

    const target = event.target

    if (
      target instanceof HTMLElement &&
      target.closest('button, select, input, textarea, a')
    ) {
      return
    }

    panelDragStateRef.current = {
      initialLeft: effectivePanelPosition.left,
      initialTop: effectivePanelPosition.top,
      pointerId: event.pointerId,
      startX: event.clientX,
      startY: event.clientY,
    }

    event.currentTarget.setPointerCapture?.(event.pointerId)
  }

  function handlePanelPointerMove(event: React.PointerEvent<HTMLElement>) {
    const dragState = panelDragStateRef.current

    if (!dragState || dragState.pointerId !== event.pointerId || isMobile) {
      return
    }

    const nextLeft = dragState.initialLeft + (event.clientX - dragState.startX)
    const nextTop = dragState.initialTop + (event.clientY - dragState.startY)
    setPanelPosition(clampPanelPosition({
      left: nextLeft,
      top: nextTop,
    }, false))
  }

  function handlePanelPointerUp(event: React.PointerEvent<HTMLElement>) {
    if (panelDragStateRef.current?.pointerId === event.pointerId) {
      panelDragStateRef.current = null
      event.currentTarget.releasePointerCapture?.(event.pointerId)
    }
  }

  return {
    effectivePanelPosition,
    fabPosition: clampedFabPosition,
    handleFabPointerDown,
    handleFabPointerMove,
    handleFabPointerUp,
    handlePanelPointerDown,
    handlePanelPointerMove,
    handlePanelPointerUp,
    isMobile,
  }
}
