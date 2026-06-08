import type { FormEvent } from 'react'
import { RotateCcw, Send, Square } from 'lucide-react'
import { useTranslation } from 'react-i18next'

interface AiAssistantFormProps {
  draft: string
  setDraft: (value: string) => void
  canUseAssistant: boolean
  isRunning: boolean
  canClear: boolean
  onSubmit: (event: FormEvent<HTMLFormElement>) => void
  onClear: () => void
  onStop: () => void
}

export function AiAssistantForm({
  draft,
  setDraft,
  canUseAssistant,
  isRunning,
  canClear,
  onSubmit,
  onClear,
  onStop,
}: AiAssistantFormProps) {
  const { t } = useTranslation()

  return (
    <form onSubmit={onSubmit} className="border-t border-border-subtle p-3">
      <div className="flex items-end gap-2">
        <textarea
          value={draft}
          onChange={(event) => setDraft(event.target.value)}
          disabled={!canUseAssistant || isRunning}
          placeholder={t('ai.inputPlaceholder')}
          rows={2}
          className="min-h-[42px] flex-1 resize-none rounded-md border border-border-default bg-surface px-2.5 py-2 text-xs leading-5 text-text-primary outline-none transition-colors placeholder:text-text-tertiary focus:border-brand-brown disabled:opacity-60"
        />
        <div className="flex shrink-0 items-center gap-1">
          <button
            type="button"
            onClick={onClear}
            disabled={!canClear}
            className="flex h-9 w-9 items-center justify-center rounded-md border border-border-default text-text-secondary transition-colors hover:bg-surface-hover disabled:cursor-not-allowed disabled:opacity-40"
            aria-label={t('ai.clear')}
            title={t('ai.clear')}
          >
            <RotateCcw className="h-3.5 w-3.5" />
          </button>
          {isRunning ? (
            <button
              type="button"
              onClick={onStop}
              className="flex h-9 w-9 items-center justify-center rounded-md bg-text-primary text-text-inverse transition-colors hover:bg-surface-inverse-hover"
              aria-label={t('ai.stop')}
              title={t('ai.stop')}
            >
              <Square className="h-3.5 w-3.5" />
            </button>
          ) : (
            <button
              type="submit"
              disabled={!draft.trim() || !canUseAssistant}
              className="flex h-9 w-9 items-center justify-center rounded-md bg-brand-brown text-text-inverse transition-opacity hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-40"
              aria-label={t('ai.send')}
              title={t('ai.send')}
            >
              <Send className="h-3.5 w-3.5" />
            </button>
          )}
        </div>
      </div>
    </form>
  )
}
