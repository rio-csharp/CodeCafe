import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
  type MouseEvent as ReactMouseEvent,
  type PointerEvent as ReactPointerEvent,
} from 'react'
import { Sparkles } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import type { Notebook } from '@/entities/notebook'
import type { NotebookItem } from '@/entities/notebook-item'
import AiAssistant from './AiAssistant'

interface FloatingAiAssistantProps {
  notebook: Notebook
  activePage: NotebookItem | null
}

interface Point {
  x: number
  y: number
}

interface PanelSize {
  width: number
  height: number
}

interface ViewportSize {
  width: number
  height: number
}

interface DragState {
  pointerId: number
  startX: number
  startY: number
  originX: number
  originY: number
  hasMoved: boolean
}

const EDGE_OFFSET = 16
const DEFAULT_PANEL_WIDTH = 380
const DEFAULT_PANEL_HEIGHT = 560
const MIN_PANEL_WIDTH = 320
const MIN_PANEL_HEIGHT = 360
const MINIMIZED_SIZE = 44
const DRAG_CLICK_THRESHOLD = 4
const DRAG_CLICK_SUPPRESSION_MS = 150

export default function FloatingAiAssistant({ notebook, activePage }: FloatingAiAssistantProps) {
  const { t } = useTranslation()
  const [viewport, setViewport] = useState(getViewportSize)
  const [minimized, setMinimized] = useState(() => isCompactViewport(getViewportSize()))
  const [position, setPosition] = useState(() => {
    const currentViewport = getViewportSize()
    const initiallyMinimized = isCompactViewport(currentViewport)
    return getDefaultPosition(getPanelSize(initiallyMinimized, currentViewport), currentViewport)
  })
  const [isDragging, setIsDragging] = useState(false)
  const dragRef = useRef<DragState | null>(null)
  const suppressNextClickRef = useRef(false)
  const clickSuppressionTimerRef = useRef<number | null>(null)

  const panelSize = useMemo(() => getPanelSize(minimized, viewport), [minimized, viewport])
  const clampedPosition = useMemo(
    () => clampPosition(position, panelSize, viewport),
    [panelSize, position, viewport],
  )

  useEffect(() => {
    function handleResize() {
      setViewport(getViewportSize())
    }

    window.addEventListener('resize', handleResize)
    return () => window.removeEventListener('resize', handleResize)
  }, [])

  useEffect(() => {
    if (!isDragging) return

    function handlePointerMove(event: PointerEvent) {
      const drag = dragRef.current
      if (!drag || drag.pointerId !== event.pointerId) return

      const deltaX = event.clientX - drag.startX
      const deltaY = event.clientY - drag.startY
      if (Math.hypot(deltaX, deltaY) > DRAG_CLICK_THRESHOLD) {
        drag.hasMoved = true
      }

      const nextPosition = {
        x: drag.originX + deltaX,
        y: drag.originY + deltaY,
      }
      setPosition(clampPosition(nextPosition, panelSize, viewport))
    }

    function handlePointerEnd(event: PointerEvent) {
      const drag = dragRef.current
      if (!drag || drag.pointerId !== event.pointerId) return

      if (drag.hasMoved) {
        suppressNextClickRef.current = true
        if (clickSuppressionTimerRef.current !== null) {
          window.clearTimeout(clickSuppressionTimerRef.current)
        }
        clickSuppressionTimerRef.current = window.setTimeout(() => {
          suppressNextClickRef.current = false
          clickSuppressionTimerRef.current = null
        }, DRAG_CLICK_SUPPRESSION_MS)
      }

      dragRef.current = null
      setIsDragging(false)
    }

    window.addEventListener('pointermove', handlePointerMove)
    window.addEventListener('pointerup', handlePointerEnd)
    window.addEventListener('pointercancel', handlePointerEnd)
    return () => {
      window.removeEventListener('pointermove', handlePointerMove)
      window.removeEventListener('pointerup', handlePointerEnd)
      window.removeEventListener('pointercancel', handlePointerEnd)
    }
  }, [isDragging, panelSize, viewport])

  useEffect(() => () => {
    if (clickSuppressionTimerRef.current !== null) {
      window.clearTimeout(clickSuppressionTimerRef.current)
    }
  }, [])

  const handleDragStart = useCallback((event: ReactPointerEvent<HTMLElement>) => {
    if (event.button !== 0) return

    dragRef.current = {
      pointerId: event.pointerId,
      startX: event.clientX,
      startY: event.clientY,
      originX: clampedPosition.x,
      originY: clampedPosition.y,
      hasMoved: false,
    }
    setIsDragging(true)
  }, [clampedPosition])

  const handleMinimizedClick = useCallback((event: ReactMouseEvent<HTMLButtonElement>) => {
    if (suppressNextClickRef.current) {
      suppressNextClickRef.current = false
      event.preventDefault()
      event.stopPropagation()
      return
    }

    setMinimized(false)
  }, [])

  const floatingStyle = {
    height: `${panelSize.height}px`,
    left: `${clampedPosition.x}px`,
    top: `${clampedPosition.y}px`,
    width: `${panelSize.width}px`,
  }

  if (minimized) {
    return (
      <div className="fixed z-50" style={floatingStyle}>
        <button
          type="button"
          onClick={handleMinimizedClick}
          onPointerDown={handleDragStart}
          className={`flex h-full w-full touch-none items-center justify-center rounded-lg border border-border-default bg-surface text-text-primary shadow-xl transition-colors hover:bg-surface-hover ${isDragging ? 'cursor-grabbing' : 'cursor-grab'}`}
          aria-label={t('ai.open')}
          title={t('ai.open')}
        >
          <Sparkles className="h-4 w-4 text-brand-brown" />
        </button>
      </div>
    )
  }

  return (
    <div className="fixed z-50" style={floatingStyle}>
      <AiAssistant
        key={`${notebook.slug}:${activePage?.path ?? 'notebook'}`}
        notebook={notebook}
        activePage={activePage}
        variant="floating"
        onCollapse={() => setMinimized(true)}
        dragHandleProps={{
          className: `touch-none ${isDragging ? 'cursor-grabbing' : 'cursor-grab'}`,
          onPointerDown: handleDragStart,
          title: t('ai.dragHandle'),
        }}
      />
    </div>
  )
}

function getViewportSize(): ViewportSize {
  if (typeof window === 'undefined') {
    return { width: 1280, height: 720 }
  }

  return {
    width: window.innerWidth,
    height: window.innerHeight,
  }
}

function isCompactViewport(viewport: ViewportSize): boolean {
  return viewport.width < 768
}

function getPanelSize(minimized: boolean, viewport: ViewportSize): PanelSize {
  const availableWidth = Math.max(0, viewport.width - EDGE_OFFSET * 2)
  const availableHeight = Math.max(0, viewport.height - EDGE_OFFSET * 2)

  if (minimized) {
    return {
      width: Math.min(MINIMIZED_SIZE, availableWidth),
      height: Math.min(MINIMIZED_SIZE, availableHeight),
    }
  }

  return {
    width: availableWidth < MIN_PANEL_WIDTH ? availableWidth : Math.min(DEFAULT_PANEL_WIDTH, availableWidth),
    height: availableHeight < MIN_PANEL_HEIGHT ? availableHeight : Math.min(DEFAULT_PANEL_HEIGHT, availableHeight),
  }
}

function getDefaultPosition(panelSize: PanelSize, viewport: ViewportSize): Point {
  return clampPosition({
    x: viewport.width - panelSize.width - 24,
    y: viewport.height - panelSize.height - 24,
  }, panelSize, viewport)
}

function clampPosition(position: Point, panelSize: PanelSize, viewport: ViewportSize): Point {
  return {
    x: clamp(position.x, EDGE_OFFSET, Math.max(EDGE_OFFSET, viewport.width - panelSize.width - EDGE_OFFSET)),
    y: clamp(position.y, EDGE_OFFSET, Math.max(EDGE_OFFSET, viewport.height - panelSize.height - EDGE_OFFSET)),
  }
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(Math.max(value, min), max)
}
