/**
 * The segment that names a page: `/setup/untitled-4821` → `untitled-4821`.
 * Slugs are unique per notebook, so the last segment always identifies the page.
 */
export function pageSlug(path: string): string {
  const segments = path.split('/').filter((segment) => segment.length > 0)
  return segments.length === 0 ? '' : segments[segments.length - 1]!
}

/**
 * Mirrors the server's slug rule (`Slug.GenerateFromTitle`): Unicode letters and
 * digits survive, every other run collapses into a single hyphen, and the column's
 * length cap trims the tail. It recognises the placeholder slug a nameless page was
 * created with and drafts the URL a title would earn, so drifting from the server
 * degrades to a wrong suggestion the user can still edit — never to a wrong write.
 */
export function slugify(title: string): string {
  const slug = title
    .trim()
    .toLowerCase()
    .replace(/[^\p{L}\p{N}]+/gu, '-')
    .replace(/^-+|-+$/g, '')

  return slug.length > MAX_PAGE_SLUG_LENGTH
    ? slug.slice(0, MAX_PAGE_SLUG_LENGTH).replace(/-+$/g, '')
    : slug
}

/** Mirrors the server's column cap, `Page.MaxSlugLength`. */
export const MAX_PAGE_SLUG_LENGTH = 80

/**
 * Mirrors the server's `Slug.IsValid` for a hand-typed URL segment: letters, digits and
 * single hyphens in between. The server keeps the last word — this only saves a round trip,
 * so a slug that slips through comes back as a 400.
 */
export function isValidPageSlug(slug: string): boolean {
  if (slug.length === 0 || slug.length > MAX_PAGE_SLUG_LENGTH) {
    return false
  }

  if (slug.startsWith('-') || slug.endsWith('-') || slug.includes('--')) {
    return false
  }

  return /^[\p{L}\p{N}-]+$/u.test(slug)
}

/**
 * Whether a page still carries the URL it was given when it was created nameless,
 * i.e. `untitled` / `untitled-4821` for an "Untitled" page. The server adds a random
 * suffix only when the plain placeholder is taken, hence the optional numeric tail.
 */
export function isPlaceholderSlug(slug: string, placeholderTitle: string): boolean {
  const placeholder = slugify(placeholderTitle)
  if (placeholder.length === 0) {
    return false
  }

  const escaped = placeholder.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
  return new RegExp(`^${escaped}(?:-\\d+)?$`).test(slug)
}
