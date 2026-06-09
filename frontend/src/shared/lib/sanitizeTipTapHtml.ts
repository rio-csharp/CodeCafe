import DOMPurify, { type Config } from 'dompurify'
import {
  isSafeHtmlImageUrl,
  isSafeHtmlLinkUrl,
  isSafeYoutubeEmbedUrl,
} from './safeUrls'

const SANITIZE_CONFIG: Config = {
  ADD_TAGS: ['iframe'],
  ADD_ATTR: [
    'allow',
    'allowfullscreen',
    'frameborder',
    'referrerpolicy',
    'scrolling',
    'target',
    'type',
    'checked',
    'disabled',
  ],
  ALLOW_UNKNOWN_PROTOCOLS: false,
  ALLOWED_URI_REGEXP: /^(?:(?:https?|mailto):|[#/])/i,
}

export function sanitizeTipTapHtml(html: string): string {
  const sanitized = DOMPurify.sanitize(html, SANITIZE_CONFIG)
  const container = document.createElement('div')
  container.innerHTML = sanitized

  container.querySelectorAll<HTMLAnchorElement>('a[href]').forEach((anchor) => {
    const href = anchor.getAttribute('href')
    if (!href || !isSafeHtmlLinkUrl(href)) {
      anchor.removeAttribute('href')
      return
    }
    if (anchor.target === '_blank') {
      anchor.rel = 'noopener noreferrer'
    }
  })

  container.querySelectorAll<HTMLImageElement>('img[src]').forEach((image) => {
    const src = image.getAttribute('src')
    if (!src || !isSafeHtmlImageUrl(src)) {
      image.remove()
    }
  })

  container.querySelectorAll<HTMLIFrameElement>('iframe[src]').forEach((iframe) => {
    const src = iframe.getAttribute('src')
    if (!src || !isSafeYoutubeEmbedUrl(src)) {
      iframe.remove()
      return
    }
    iframe.setAttribute('sandbox', 'allow-scripts allow-same-origin allow-presentation')
    iframe.setAttribute('referrerpolicy', 'strict-origin-when-cross-origin')
  })

  return container.innerHTML
}
