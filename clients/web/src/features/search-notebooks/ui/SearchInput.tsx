import { useEffect, useRef, useState } from 'react'
import type { ChangeEvent } from 'react'
import { useTranslation } from 'react-i18next'

export const SEARCH_DEBOUNCE_MS = 300

export interface SearchInputProps {
  /** The committed (debounced) value owned by the parent. */
  value: string
  onChange: (value: string) => void
  /** Overrides for the default notebook-search copy (e.g. the page search). */
  label?: string
  placeholder?: string
}

export function SearchInput({ value, onChange, label, placeholder }: SearchInputProps) {
  const { t } = useTranslation()
  const [text, setText] = useState(value)
  const [syncedValue, setSyncedValue] = useState(value)
  const timerRef = useRef<number | null>(null)

  // A parent-driven change (e.g. a reset from elsewhere) must reach the input,
  // while our own echo must not fight the user's keystrokes. Adjusting state
  // during render keeps this to a single extra pass instead of an effect.
  if (value !== syncedValue) {
    setSyncedValue(value)
    setText(value)
  }

  useEffect(
    () => () => {
      if (timerRef.current !== null) {
        window.clearTimeout(timerRef.current)
      }
    },
    [],
  )

  const schedule = (next: string) => {
    if (timerRef.current !== null) {
      window.clearTimeout(timerRef.current)
    }
    timerRef.current = window.setTimeout(() => {
      timerRef.current = null
      onChange(next)
    }, SEARCH_DEBOUNCE_MS)
  }

  const handleChange = (event: ChangeEvent<HTMLInputElement>) => {
    setText(event.target.value)
    schedule(event.target.value)
  }

  const handleClear = () => {
    if (timerRef.current !== null) {
      window.clearTimeout(timerRef.current)
      timerRef.current = null
    }
    setText('')
    onChange('')
  }

  return (
    <div className="relative flex w-full items-center">
      <svg
        className="pointer-events-none absolute left-4 h-5 w-5 text-muted"
        viewBox="0 0 24 24"
        fill="none"
        aria-hidden="true"
      >
        <circle cx="11" cy="11" r="6.5" stroke="currentColor" strokeWidth="1.8" />
        <path d="m16 16 4.5 4.5" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
      </svg>

      <input
        type="search"
        value={text}
        onChange={handleChange}
        aria-label={label ?? t('search.label')}
        placeholder={placeholder ?? t('search.placeholder')}
        className="h-13 w-full rounded-full border border-line bg-card pl-12 pr-24 text-ink placeholder:text-muted focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent"
      />

      {text.length === 0 ? null : (
        <button
          type="button"
          onClick={handleClear}
          className="absolute right-3 rounded-full px-3 py-1 text-sm text-muted transition-colors hover:text-accent-strong focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
        >
          {t('search.clear')}
        </button>
      )}
    </div>
  )
}
