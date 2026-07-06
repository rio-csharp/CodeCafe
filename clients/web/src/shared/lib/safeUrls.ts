const HOST_LIKE_URL = /^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?(?:\.[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)+(?:[/:?#].*)?$/i
const YOUTUBE_HOSTS = new Set([
  'youtube.com',
  'www.youtube.com',
  'm.youtube.com',
  'youtu.be',
  'youtube-nocookie.com',
  'www.youtube-nocookie.com',
])
const YOUTUBE_EMBED_HOSTS = new Set([
  'www.youtube.com',
  'www.youtube-nocookie.com',
])

function cleanUrlInput(input: string): string | null {
  const trimmed = input.trim()
  if (!trimmed || hasWhitespaceOrControl(trimmed) || hasBackslash(trimmed)) return null
  return trimmed
}

function hasWhitespaceOrControl(input: string): boolean {
  return [...input].some((character) => {
    const code = character.charCodeAt(0)
    return code <= 0x20 || code === 0x7f
  })
}

function hasBackslash(input: string): boolean {
  return input.includes('\\') || /%5c/i.test(input)
}

function toUrl(input: string): URL | null {
  try {
    return new URL(input, window.location.origin)
  } catch {
    return null
  }
}

function hasExplicitScheme(input: string): boolean {
  return /^[a-z][a-z0-9+.-]*:/i.test(input)
}

function normalizeAbsoluteOrHostLike(input: string): string {
  if (hasExplicitScheme(input)) return input
  if (HOST_LIKE_URL.test(input)) return `https://${input}`
  return input
}

export function normalizeEditorLinkUrl(input: string): string | null {
  const cleaned = cleanUrlInput(input)
  if (!cleaned) return null

  if (cleaned.startsWith('#')) return cleaned
  if (cleaned.startsWith('/') && !cleaned.startsWith('//')) return cleaned

  const candidate = normalizeAbsoluteOrHostLike(cleaned)
  const url = toUrl(candidate)
  if (!url) return null

  if (url.protocol === 'http:' || url.protocol === 'https:' || url.protocol === 'mailto:') {
    return hasExplicitScheme(candidate) ? url.toString() : candidate
  }

  return null
}

export function normalizeEditorImageUrl(input: string): string | null {
  const cleaned = cleanUrlInput(input)
  if (!cleaned) return null

  if (cleaned.startsWith('/') && !cleaned.startsWith('//')) return cleaned

  const candidate = normalizeAbsoluteOrHostLike(cleaned)
  const url = toUrl(candidate)
  if (!url) return null

  if (url.protocol === 'http:' || url.protocol === 'https:') {
    return hasExplicitScheme(candidate) ? url.toString() : candidate
  }

  return null
}

function getYoutubeVideoId(url: URL): string | null {
  if (url.hostname === 'youtu.be') {
    return url.pathname.split('/').filter(Boolean)[0] ?? null
  }

  const segments = url.pathname.split('/').filter(Boolean)
  if (segments[0] === 'embed' || segments[0] === 'shorts') {
    return segments[1] ?? null
  }

  return url.searchParams.get('v')
}

export function normalizeEditorYoutubeUrl(input: string): string | null {
  const cleaned = cleanUrlInput(input)
  if (!cleaned) return null

  const candidate = normalizeAbsoluteOrHostLike(cleaned)
  const url = toUrl(candidate)
  if (!url || url.protocol !== 'https:' || !YOUTUBE_HOSTS.has(url.hostname)) return null

  const videoId = getYoutubeVideoId(url)
  if (!videoId || !/^[a-zA-Z0-9_-]{6,}$/.test(videoId)) return null

  return url.toString()
}

export function isSafeHtmlLinkUrl(input: string): boolean {
  return normalizeEditorLinkUrl(input) !== null
}

export function isSafeHtmlImageUrl(input: string): boolean {
  return normalizeEditorImageUrl(input) !== null
}

export function isSafeYoutubeEmbedUrl(input: string): boolean {
  const cleaned = cleanUrlInput(input)
  if (!cleaned) return false

  const url = toUrl(cleaned)
  if (!url || url.protocol !== 'https:' || !YOUTUBE_EMBED_HOSTS.has(url.hostname)) {
    return false
  }

  const segments = url.pathname.split('/').filter(Boolean)
  const videoId = segments[0] === 'embed' ? segments[1] : null
  return !!videoId && /^[a-zA-Z0-9_-]{6,}$/.test(videoId)
}
