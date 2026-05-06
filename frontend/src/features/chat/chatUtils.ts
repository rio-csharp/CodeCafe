import type { ChatSession } from './chatTypes'

export function summarizeTitle(message: string) {
  const trimmed = message.trim()

  if (!trimmed) {
    return 'New chat'
  }

  return trimmed.length > 36 ? `${trimmed.slice(0, 36)}...` : trimmed
}

export function summarizePreview(message: string) {
  const normalized = message.replace(/\s+/g, ' ').trim()

  if (!normalized) {
    return 'No messages yet.'
  }

  return normalized.length > 56 ? `${normalized.slice(0, 56)}...` : normalized
}

export function upsertSession(sessions: ChatSession[], session: ChatSession) {
  return [
    session,
    ...sessions.filter((item) => item.id !== session.id),
  ]
}

export function formatRelativeTime(value: string) {
  const elapsedMs = Date.now() - new Date(value).getTime()
  const elapsedMinutes = Math.max(1, Math.floor(elapsedMs / 60_000))

  if (elapsedMinutes < 60) {
    return `${elapsedMinutes}m`
  }

  const elapsedHours = Math.floor(elapsedMinutes / 60)

  if (elapsedHours < 24) {
    return `${elapsedHours}h`
  }

  const elapsedDays = Math.floor(elapsedHours / 24)

  return `${elapsedDays}d`
}

export function isMobileViewport() {
  return globalThis.matchMedia?.('(max-width: 820px)').matches ?? false
}
