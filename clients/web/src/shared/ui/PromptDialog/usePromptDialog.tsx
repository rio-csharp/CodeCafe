import { useCallback, useRef, useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Modal } from '@/shared/ui/Modal'

interface PromptOptions {
  title: string
  label?: string
  defaultValue?: string
  placeholder?: string
  /** Return an error message to reject the value, or null to accept. */
  validate?: (value: string) => string | null
}

interface PromptState extends PromptOptions {
  value: string
  error: string | null
}

/**
 * Promise-based replacement for window.prompt(), with inline validation.
 * const { requestPrompt, promptDialog } = usePromptDialog()
 * const url = await requestPrompt({ title, validate }) // string | null
 * Render {promptDialog} once in the component tree.
 */
export function usePromptDialog() {
  const { t } = useTranslation()
  const [state, setState] = useState<PromptState | null>(null)
  const resolverRef = useRef<((value: string | null) => void) | null>(null)

  const requestPrompt = useCallback((options: PromptOptions) => {
    setState({ ...options, value: options.defaultValue ?? '', error: null })
    return new Promise<string | null>((resolve) => {
      resolverRef.current = resolve
    })
  }, [])

  const settle = useCallback((value: string | null) => {
    setState(null)
    resolverRef.current?.(value)
    resolverRef.current = null
  }, [])

  const submit = useCallback(() => {
    setState((current) => {
      if (!current) return current
      const error = current.validate?.(current.value) ?? null
      if (error) return { ...current, error }
      // Use a microtask so resolver runs after state settles.
      queueMicrotask(() => settle(current.value))
      return current
    })
  }, [settle])

  const promptDialog: ReactNode = state ? (
    <Modal isOpen onClose={() => settle(null)} title={state.title}>
      <form
        onSubmit={(e) => {
          e.preventDefault()
          submit()
        }}
      >
        {state.label && (
          <label htmlFor="shared-prompt-input" className="block text-sm font-medium text-text-primary mb-1.5">
            {state.label}
          </label>
        )}
        <input
          id="shared-prompt-input"
          type="text"
          autoFocus
          value={state.value}
          onChange={(e) => setState((current) => (current ? { ...current, value: e.target.value, error: null } : current))}
          placeholder={state.placeholder}
          className={`w-full rounded-lg border bg-surface px-3 py-2.5 text-sm text-text-primary outline-none transition-colors focus:border-border-focus ${
            state.error ? 'border-status-error-border' : 'border-border-default'
          }`}
        />
        {state.error && <p className="mt-1.5 text-xs text-status-error">{state.error}</p>}
        <div className="mt-6 flex justify-end gap-3">
          <button
            type="button"
            onClick={() => settle(null)}
            className="rounded-lg border border-border-default px-4 py-2 text-sm font-medium text-text-secondary hover:bg-surface-hover transition-colors"
          >
            {t('common.cancel')}
          </button>
          <button
            type="submit"
            className="rounded-lg bg-text-primary px-4 py-2 text-sm font-medium text-text-inverse hover:bg-surface-inverse-hover transition-colors"
          >
            {t('common.confirm')}
          </button>
        </div>
      </form>
    </Modal>
  ) : null

  return { requestPrompt, promptDialog }
}
