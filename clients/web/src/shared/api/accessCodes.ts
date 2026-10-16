/**
 * Per-notebook access codes, remembered for the browser tab only
 * (sessionStorage). A locked notebook answers every read with
 * `access_code_required`; once the reader types the code, every later request
 * to that notebook carries it automatically.
 */
export const ACCESS_CODE_HEADER = 'X-CodeCafe-Access-Code'

const KEY_PREFIX = 'codecafe.access.'
const NOTEBOOKS_PREFIX = '/api/notebooks/'

export function getAccessCode(slug: string): string | null {
  return storage()?.getItem(KEY_PREFIX + slug) ?? null
}

export function setAccessCode(slug: string, code: string): void {
  storage()?.setItem(KEY_PREFIX + slug, code)
}

export function clearAccessCode(slug: string): void {
  storage()?.removeItem(KEY_PREFIX + slug)
}

/**
 * The stored code for the notebook a request path targets, if any. The slug
 * is the first segment after `/api/notebooks/`; anything else (the catalog,
 * page endpoints) has no notebook to unlock.
 */
export function accessCodeForPath(path: string): { slug: string; code: string } | null {
  if (!path.startsWith(NOTEBOOKS_PREFIX)) {
    return null
  }
  const segment = path.slice(NOTEBOOKS_PREFIX.length).split(/[/?#]/, 1)[0]
  if (segment === undefined || segment.length === 0) {
    return null
  }
  let slug: string
  try {
    // Store keys are the decoded slug; request paths arrive encoded.
    slug = decodeURIComponent(segment)
  } catch {
    return null
  }
  const code = getAccessCode(slug)
  return code === null ? null : { slug, code }
}

/** sessionStorage can throw in hardened browsers; a missing store just means no codes. */
function storage(): Storage | null {
  try {
    return window.sessionStorage
  } catch {
    return null
  }
}
