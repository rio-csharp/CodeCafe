import { useTranslation } from 'react-i18next'
import { ListPlus, Loader2, PenLine, Plus, Sparkles } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import type { NotebookItem } from '@/entities/notebook-item'
import type {
  AiDraftApplyMode,
  AiDraftIntent,
  AiNoteDraftResponse,
} from '@/features/ai-assistant'
import type { DraftQuickAction } from '../lib/types'

interface DraftWorkspaceProps {
  activePage: NotebookItem | null
  applyPending: boolean
  draft: AiNoteDraftResponse | null
  draftActions: DraftQuickAction[]
  generatePending: boolean
  instruction: string
  onApply: (mode: AiDraftApplyMode) => void
  onGenerate: (intent: AiDraftIntent, prompt: string) => void
  onInstructionChange: (value: string) => void
  onCustomGenerate: () => void
  onDiscardDraft: () => void
}

export function DraftWorkspace({
  activePage,
  applyPending,
  draft,
  draftActions,
  generatePending,
  instruction,
  onApply,
  onGenerate,
  onInstructionChange,
  onCustomGenerate,
  onDiscardDraft,
}: DraftWorkspaceProps) {
  const { t } = useTranslation()

  return (
    <div className="rounded-md border border-border-subtle bg-surface-elevated p-3">
      <div className="mb-2 flex items-center justify-between gap-2">
        <span className="text-xs font-semibold text-text-primary">{t('ai.drafts.title')}</span>
        {generatePending && <Loader2 className="h-3.5 w-3.5 animate-spin text-brand-brown" />}
      </div>

      {draft ? (
        <div className="space-y-2">
          <div className="rounded-md border border-border-subtle bg-surface px-2.5 py-2">
            <p className="truncate text-xs font-medium text-text-primary">{draft.title}</p>
            <pre className="mt-2 max-h-40 overflow-y-auto whitespace-pre-wrap break-words text-[11px] leading-5 text-text-secondary">
              {draft.markdown}
            </pre>
          </div>
          <div className="grid grid-cols-3 gap-1.5">
            <DraftApplyButton
              disabled={applyPending}
              icon={Plus}
              label={t('ai.drafts.apply.create')}
              onClick={() => onApply('create')}
            />
            <DraftApplyButton
              disabled={applyPending || !activePage}
              icon={ListPlus}
              label={t('ai.drafts.apply.append')}
              onClick={() => onApply('append')}
            />
            <DraftApplyButton
              disabled={applyPending || !activePage}
              icon={PenLine}
              label={t('ai.drafts.apply.replace')}
              onClick={() => onApply('replace')}
            />
          </div>
          <button
            type="button"
            onClick={onDiscardDraft}
            disabled={applyPending}
            className="w-full rounded-md px-2 py-1.5 text-xs text-text-tertiary transition-colors hover:bg-surface-hover hover:text-text-secondary disabled:cursor-not-allowed disabled:opacity-50"
          >
            {t('ai.drafts.discard')}
          </button>
        </div>
      ) : (
        <div className="space-y-2">
          <div className="grid gap-1.5">
            {draftActions.map((action) => (
              <DraftActionButton
                key={action.id}
                action={action}
                disabled={generatePending}
                onClick={onGenerate}
              />
            ))}
          </div>
          <textarea
            value={instruction}
            onChange={(event) => onInstructionChange(event.target.value)}
            disabled={generatePending}
            placeholder={t('ai.drafts.placeholder')}
            rows={2}
            className="min-h-[42px] w-full resize-none rounded-md border border-border-default bg-surface px-2.5 py-2 text-xs leading-5 text-text-primary outline-none transition-colors placeholder:text-text-tertiary focus:border-brand-brown disabled:opacity-60"
          />
          <button
            type="button"
            onClick={onCustomGenerate}
            disabled={!instruction.trim() || generatePending}
            className="inline-flex w-full items-center justify-center gap-1.5 rounded-md bg-brand-brown px-2.5 py-2 text-xs font-medium text-text-inverse transition-opacity hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-40"
          >
            {generatePending ? (
              <Loader2 className="h-3.5 w-3.5 animate-spin" />
            ) : (
              <Sparkles className="h-3.5 w-3.5" />
            )}
            {t('ai.drafts.generate')}
          </button>
        </div>
      )}
    </div>
  )
}

interface DraftActionButtonProps {
  action: DraftQuickAction
  disabled: boolean
  onClick: (intent: AiDraftIntent, prompt: string) => void
}

function DraftActionButton({ action, disabled, onClick }: DraftActionButtonProps) {
  const Icon = action.icon

  return (
    <button
      type="button"
      onClick={() => onClick(action.id, action.prompt)}
      disabled={disabled}
      className="flex min-h-9 items-center gap-2 rounded-md border border-border-subtle bg-surface px-2.5 py-1.5 text-left text-xs font-medium text-text-secondary transition-colors hover:border-border-hover hover:bg-surface-hover disabled:cursor-not-allowed disabled:opacity-50"
    >
      <Icon className="h-3.5 w-3.5 shrink-0 text-brand-brown" />
      <span className="min-w-0 truncate">{action.label}</span>
    </button>
  )
}

interface DraftApplyButtonProps {
  disabled: boolean
  icon: LucideIcon
  label: string
  onClick: () => void
}

function DraftApplyButton({ disabled, icon: Icon, label, onClick }: DraftApplyButtonProps) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      className="flex min-h-9 items-center justify-center gap-1 rounded-md border border-border-default px-2 py-1.5 text-[11px] font-medium text-text-secondary transition-colors hover:bg-surface-hover disabled:cursor-not-allowed disabled:opacity-40"
    >
      <Icon className="h-3 w-3 shrink-0" />
      <span className="truncate">{label}</span>
    </button>
  )
}
