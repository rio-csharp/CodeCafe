import type { TFunction } from 'i18next'
import { describe, expect, it } from 'vitest'
import { ApiError } from '@/shared/api/ApiError'
import { getDisplayErrorMessage, getErrorMessage } from './errorUtils'

const FALLBACK = 'Something went wrong'

/** Minimal t() double: looks up the dict, else honors defaultValue like i18next. */
function makeT(dict: Record<string, string>): TFunction {
  return ((key: string, options?: { defaultValue?: string }) =>
    dict[key] ?? options?.defaultValue ?? '') as TFunction
}

describe('getDisplayErrorMessage', () => {
  it('maps a known backend error code to the localized message', () => {
    const t = makeT({ 'errors.slug_taken': 'That slug is already taken' })
    const err = new ApiError(409, 'slug taken', 'slug_taken')

    expect(getDisplayErrorMessage(err, t, FALLBACK)).toBe('That slug is already taken')
  })

  it('shows other 4xx messages as-is when the code has no localization', () => {
    const t = makeT({})
    const err = new ApiError(422, 'Title is required', 'validation_failed')

    expect(getDisplayErrorMessage(err, t, FALLBACK)).toBe('Title is required')
  })

  it('shows 4xx messages as-is when there is no code at all', () => {
    const t = makeT({})
    const err = new ApiError(400, 'Bad request body')

    expect(getDisplayErrorMessage(err, t, FALLBACK)).toBe('Bad request body')
  })

  it('masks 5xx ProblemDetails detail behind the fallback', () => {
    const t = makeT({})
    const serverDetail = 'NullReferenceException at /app/Services/NotesService.cs:line 42'
    const err = new ApiError(500, serverDetail, 'internal_error')

    const message = getDisplayErrorMessage(err, t, FALLBACK)

    expect(message).toBe(FALLBACK)
    expect(message).not.toContain('NotesService')
    expect(message).not.toContain(serverDetail)
  })

  it('masks 5xx even when the code has a localization entry', () => {
    // A localized entry wins for any status — but 5xx detail must still
    // never leak through the fallback path.
    const t = makeT({})
    const err = new ApiError(503, 'Service unavailable: db connection refused')

    expect(getDisplayErrorMessage(err, t, FALLBACK)).toBe(FALLBACK)
  })

  it('returns the message of a plain Error', () => {
    const t = makeT({})

    expect(getDisplayErrorMessage(new Error('network down'), t, FALLBACK)).toBe('network down')
  })

  it('returns the fallback for non-Error values', () => {
    const t = makeT({})

    expect(getDisplayErrorMessage('boom', t, FALLBACK)).toBe(FALLBACK)
    expect(getDisplayErrorMessage(undefined, t, FALLBACK)).toBe(FALLBACK)
  })
})

describe('getErrorMessage', () => {
  it('returns the Error message and falls back otherwise', () => {
    expect(getErrorMessage(new Error('oops'), FALLBACK)).toBe('oops')
    expect(getErrorMessage(42, FALLBACK)).toBe(FALLBACK)
  })
})
