import { useCallback, useEffect, useMemo, useRef, useState, type PointerEvent as ReactPointerEvent } from 'react'
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
}

const EDGE_OFFSET = 16
const DEFAULT_PANEL_WIDTH = 380
const DEFAULT_PANEL_HEIGHT = 560
const MIN_PANEL_WIDTH = 320
const MIN_PANEL_HEIGHT = 360
const MINIMIZED_WIDTH = 228
const MINIMIZED_HEIGHT = 48

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

      const nextPosition = {
        x: drag.originX + event.clientX - drag.startX,
        y: drag.originY + event.clientY - drag.startY,
      }
      setPosition(clampPosition(nextPosition, panelSize, viewport))
    }

    function handlePointerEnd(event: PointerEvent) {
      const drag = dragRef.current
      if (!drag || drag.pointerId !== event.pointerId) return

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

  const handleDragStart = useCallback((event: ReactPointerEvent<HTMLDivElement>) => {
    if (event.button !== 0) return

    dragRef.current = {
      pointerId: event.pointerId,
      startX: event.clientX,
      startY: event.clientY,
      originX: clampedPosition.x,
      originY: clampedPosition.y,
    }
    setIsDragging(true)
    event.preventDefault()
  }, [clampedPosition])

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
          onClick={() => setMinimized(false)}
          className="flex h-full w-full items-center gap-2 rounded-lg border border-border-default bg-surface px-3 text-left text-sm font-medium text-text-primary shadow-xl transition-colors hover:bg-surface-hover"
          aria-label={t('ai.open')}
        >
          <Sparkles className="h-4 w-4 shrink-0 text-brand-brown" />
          <span className="min-w-0 truncate">{t('ai.title')}</span>
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
          className: isDragging ? 'cursor-grabbing' : 'cursor-grab',
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
      width: Math.min(MINIMIZED_WIDTH, availableWidth),
      height: Math.min(MINIMIZED_HEIGHT, availableHeight),
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
