import type { PanelPosition } from './notesAiTypes'

const desktopPanelWidth = 520
const desktopPanelHeight = 680
const mobilePanelWidth = 460
const mobilePanelHeight = 620

export function getFabStyle(position: { x: number; y: number }) {
  return {
    bottom: `${position.y}px`,
    right: `${position.x}px`,
  }
}

export function getPanelStyle(position: PanelPosition, isMobile: boolean) {
  return {
    left: `${position.left}px`,
    top: `${position.top}px`,
    ...(isMobile
      ? {
          height: `min(70vh, ${mobilePanelHeight}px)`,
          width: `min(calc(100vw - 24px), ${mobilePanelWidth}px)`,
        }
      : {
          height: `min(calc(100vh - 126px), ${desktopPanelHeight}px)`,
          width: `min(calc(100vw - 36px), ${desktopPanelWidth}px)`,
        }),
  }
}

export function getAnchoredPanelPosition(fabPosition: { x: number; y: number }, isMobile: boolean) {
  const viewportWidth = typeof window === 'undefined' ? 1440 : window.innerWidth
  const viewportHeight = typeof window === 'undefined' ? 900 : window.innerHeight
  const panelWidth = Math.min(viewportWidth - (isMobile ? 24 : 36), isMobile ? mobilePanelWidth : desktopPanelWidth)
  const panelHeight = Math.min(viewportHeight - (isMobile ? 96 : 126), isMobile ? mobilePanelHeight : desktopPanelHeight)
  const left = viewportWidth - fabPosition.x - panelWidth
  const top = viewportHeight - fabPosition.y - panelHeight - 64

  return clampPanelPosition({
    left,
    top,
  }, isMobile)
}

export function clampPanelPosition(position: PanelPosition, isMobile: boolean) {
  const viewportWidth = typeof window === 'undefined' ? 1440 : window.innerWidth
  const viewportHeight = typeof window === 'undefined' ? 900 : window.innerHeight
  const panelWidth = Math.min(viewportWidth - (isMobile ? 24 : 36), isMobile ? mobilePanelWidth : desktopPanelWidth)
  const panelHeight = Math.min(viewportHeight - (isMobile ? 96 : 126), isMobile ? mobilePanelHeight : desktopPanelHeight)
  const margin = isMobile ? 12 : 18

  return {
    left: Math.min(Math.max(margin, position.left), viewportWidth - panelWidth - margin),
    top: Math.min(Math.max(margin, position.top), viewportHeight - panelHeight - margin),
  }
}

export function isMobileViewport() {
  return globalThis.matchMedia?.('(max-width: 820px)').matches ?? false
}
