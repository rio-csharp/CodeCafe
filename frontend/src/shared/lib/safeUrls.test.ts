import { describe, expect, it } from 'vitest'
import {
  isSafeYoutubeEmbedUrl,
  normalizeEditorImageUrl,
  normalizeEditorLinkUrl,
  normalizeEditorYoutubeUrl,
} from './safeUrls'

describe('safeUrls', () => {
  it('rejects script links', () => {
    expect(normalizeEditorLinkUrl('javascript:alert(1)')).toBeNull()
  })

  it('normalizes host-like links to https', () => {
    expect(normalizeEditorLinkUrl('example.com/docs')).toBe('https://example.com/docs')
  })

  it('allows same-origin relative links and images', () => {
    expect(normalizeEditorLinkUrl('/notes/page')).toBe('/notes/page')
    expect(normalizeEditorImageUrl('/images/photo.png')).toBe('/images/photo.png')
  })

  it('rejects backslash-prefixed external URL smuggling', () => {
    expect(normalizeEditorLinkUrl('/\\\\evil.example')).toBeNull()
    expect(normalizeEditorLinkUrl('/%5C%5Cevil.example')).toBeNull()
    expect(normalizeEditorImageUrl('/\\\\evil.example/image.png')).toBeNull()
    expect(normalizeEditorImageUrl('/%5C%5Cevil.example/image.png')).toBeNull()
  })

  it('rejects data images from editor input', () => {
    expect(normalizeEditorImageUrl('data:image/svg+xml,<svg onload=alert(1)>')).toBeNull()
  })

  it('allows only https YouTube editor URLs with a video id', () => {
    expect(normalizeEditorYoutubeUrl('https://youtu.be/dQw4w9WgXcQ')).toBe('https://youtu.be/dQw4w9WgXcQ')
    expect(normalizeEditorYoutubeUrl('http://youtu.be/dQw4w9WgXcQ')).toBeNull()
    expect(normalizeEditorYoutubeUrl('https://evil.example/watch?v=dQw4w9WgXcQ')).toBeNull()
  })

  it('allows only YouTube embed iframe URLs', () => {
    expect(isSafeYoutubeEmbedUrl('https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ')).toBe(true)
    expect(isSafeYoutubeEmbedUrl('https://evil.example/embed/dQw4w9WgXcQ')).toBe(false)
  })
})
