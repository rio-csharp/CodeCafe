/**
 * `/setup/rust-notes` → `setup/rust-notes`. Lets a decoded route splat be
 * compared against a node path without caring who wrote the leading slash.
 */
export function normalizePagePath(path: string): string {
  return path
    .split('/')
    .filter((segment) => segment.length > 0)
    .join('/')
}

/** The reader's link for a page: each path segment encoded, CJK included. */
export function toPageHref(slug: string, path: string): string {
  const prefix = `/notebooks/${encodeURIComponent(slug)}`
  const segments = normalizePagePath(path)

  if (segments.length === 0) {
    return prefix
  }

  const encoded = segments.split('/').map(encodeURIComponent).join('/')
  return `${prefix}/${encoded}`
}
