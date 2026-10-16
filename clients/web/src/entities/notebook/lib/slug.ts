/**
 * Mirrors the server's `Slug.IsValid`: letters (CJK too), digits, hyphens,
 * no edge or double dashes. The server stays authoritative; this only saves
 * a round trip on clearly malformed input.
 */
export const NOTEBOOK_SLUG_PATTERN = /^[\p{L}\p{N}](?:[\p{L}\p{N}]|-(?!-))*[\p{L}\p{N}]$|^[\p{L}\p{N}]$/u

/** Same normalization the server applies before comparing slugs. */
export function normalizeNotebookSlug(value: string): string {
  return value.trim().toLowerCase()
}
