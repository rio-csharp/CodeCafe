import { ACCESS_CODE_HEADER, accessCodeForPath } from './accessCodes'
import { ApiError, kindFromStatus } from './errors'
import { getAccessToken } from './session'

export interface DownloadFileOptions {
  /** Extra headers (e.g. the access-code header); the bearer token is added on top. */
  headers?: HeadersInit
  /** Used when the response carries no Content-Disposition filename. */
  fallbackFileName?: string
}

/**
 * File downloads are the envelope's escape hatch: a 2xx body IS the file, so
 * this sits next to `requestJson` instead of inside it. The blob is handed to
 * the browser's own download flow; failures still arrive as the JSON envelope
 * and surface as {@link ApiError}.
 */
export async function downloadFile(path: string, options: DownloadFileOptions = {}): Promise<void> {
  const headers = new Headers(options.headers)
  const token = getAccessToken()
  if (token !== null && !headers.has('Authorization')) {
    headers.set('Authorization', `Bearer ${token}`)
  }
  // A locked notebook's export needs the remembered code, same as the JSON reads.
  const access = accessCodeForPath(path)
  if (access !== null && !headers.has(ACCESS_CODE_HEADER)) {
    headers.set(ACCESS_CODE_HEADER, access.code)
  }

  const response = await fetch(path, { headers })
  if (!response.ok) {
    throw await envelopeError(response)
  }

  const blob = await response.blob()
  const fileName =
    fileNameFromDisposition(response.headers.get('Content-Disposition')) ??
    options.fallbackFileName ??
    'download'

  const url = URL.createObjectURL(blob)
  try {
    const anchor = document.createElement('a')
    anchor.href = url
    anchor.download = fileName
    anchor.rel = 'noopener'
    document.body.append(anchor)
    anchor.click()
    anchor.remove()
  } finally {
    URL.revokeObjectURL(url)
  }
}

/** `filename*=UTF-8''…` (RFC 5987) wins over the plain `filename=` fallback. */
function fileNameFromDisposition(disposition: string | null): string | null {
  if (disposition === null) {
    return null
  }

  const encoded = /filename\*=(?:UTF-8'')?([^;]+)/i.exec(disposition)
  if (encoded !== null) {
    try {
      return decodeURIComponent(encoded[1].trim().replace(/^"|"$/g, ''))
    } catch {
      // Malformed percent-encoding: fall through to the plain filename.
    }
  }

  const plain = /filename="?([^";]+)"?/i.exec(disposition)
  return plain === null ? null : plain[1]
}

async function envelopeError(response: Response): Promise<ApiError> {
  const base = { status: response.status, kind: kindFromStatus(response.status) }
  try {
    const payload: unknown = await response.json()
    const error = (payload as { error?: { code?: unknown; message?: unknown } } | null)?.error
    if (typeof error?.code === 'string' && typeof error.message === 'string') {
      return new ApiError({ ...base, code: error.code, message: error.message })
    }
  } catch {
    // Not the envelope (e.g. a proxy error page) — the generic error covers it.
  }
  return new ApiError({
    ...base,
    code: 'client.request_failed',
    message: `Request failed with status ${response.status}.`,
  })
}
