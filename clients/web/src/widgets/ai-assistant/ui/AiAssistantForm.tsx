import type { FormEvent } from 'react'
import { RotateCcw, Send, Square } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import type { AiAssistantMode } from './AiAssistant'

interface AiAssistantFormProps {
  mode: AiAssistantMode
  draft: string
  setDraft: (value: string) => void
  canUseAssistant: boolean
  canUseEdit: boolean
  isRunning: boolean
  canClear: boolean
  onSubmit: (event: FormEvent<HTMLFormElement>) => void
  onClear: () => void
  onStop: () => void
  onModeChange: (mode: AiAssistantMode) => void
}

export function AiAssistantForm({
  mode,
  draft,
  setDraft,
  canUseAssistant,
  canUseEdit,
  isRunning,
  canClear,
  onSubmit,
  onClear,
  onStop,
  onModeChange,
}: AiAssistantFormProps) {
  const { t } = useTranslation()

  const canSubmit = mode === 'edit' ? canUseEdit : canUseAssistant
  const placeholder = mode === 'edit' ? t('ai.edit.inputPlaceholder') : t('ai.inputPlaceholder')

  return (
    <form onSubmit={onSubmit} className="border-t border-border-subtle p-3">
      <div className="mb-2 flex items-center gap-1">
        <ModeButton active={mode === 'chat'} disabled={isRunning} onClick={() => onModeChange('chat')}>
          {t('ai.mode.chat')}
        </ModeButton>
        <ModeButton active={mode === 'edit'} disabled={isRunning || !canUseEdit} onClick={() => onModeChange('edit')}>
          {t('ai.mode.edit')}
        </ModeButton>
      </div>
      <div className="flex items-end gap-2">
        <textarea
          value={draft}
          onChange={(event) => setDraft(event.target.value)}
          disabled={!canSubmit || isRunning}
          placeholder={placeholder}
          rows={2}
          className="min-h-[42px] flex-1 resize-none rounded-md border border-border-default bg-surface px-2.5 py-2 text-xs leading-5 text-text-primary outline-none transition-colors placeholder:text-text-tertiary focus:border-brand-brown disabled:opacity-60"
        />
        <div className="flex shrink-0 items-center gap-1">
          <button
            type="button"
            onClick={onClear}
            disabled={!canClear || isRunning}
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
              disabled={!draft.trim() || !canSubmit}
              className="flex h-9 w-9 items-center justify-center rounded-md bg-brand-brown-dark dark:bg-brand-brown text-text-inverse transition-opacity hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-40"
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

interface ModeButtonProps {
  active: boolean
  disabled: boolean
  onClick: () => void
  children: React.ReactNode
}

function ModeButton({ active, disabled, onClick, children }: ModeButtonProps) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      aria-pressed={active}
      className={`rounded-md px-2 py-1 text-[11px] font-medium transition-colors disabled:cursor-not-allowed disabled:opacity-40 ${
        active
          ? 'bg-brand-brown-dark dark:bg-brand-brown text-text-inverse'
          : 'border border-border-default bg-surface text-text-secondary hover:bg-surface-hover'
      }`}
    >
      {children}
    </button>
  )
}
