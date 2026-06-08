import type { FormEvent, RefObject } from 'react'
import { BookOpen } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import type { Notebook } from '@/entities/notebook'
import type { NotebookItem } from '@/entities/notebook-item'
import {
  getMessageText,
  type AiAssistantErrorCode,
  type AiAssistantVisibleMessage,
  type AiDraftApplyMode,
  type AiDraftIntent,
  type AiNoteDraftResponse,
  type AiToolActivity,
} from '@/features/ai-assistant'
import { errorKey } from '../lib/labels'
import type { DraftQuickAction, QuickAction } from '../lib/types'
import { AiAssistantForm } from './AiAssistantForm'
import { AssistantThinking } from './AssistantThinking'
import { DraftWorkspace } from './DraftWorkspace'
import { MessageBubble } from './MessageBubble'
import { QuickActionButton } from './QuickActionButton'
import { ToolActivityRow } from './ToolActivityRow'

interface AiAssistantContentProps {
  notebook: Notebook
  activePage: NotebookItem | null
  messages: AiAssistantVisibleMessage[]
  toolActivities: AiToolActivity[]
  isRunning: boolean
  canUseAssistant: boolean
  canUseDrafts: boolean
  quickActions: QuickAction[]
  draftActions: DraftQuickAction[]
  noteDraft: AiNoteDraftResponse | null
  draftInstruction: string
  error: { code: AiAssistantErrorCode; message: string } | null
  applyPending: boolean
  generatePending: boolean
  draft: string
  setDraft: (value: string) => void
  onQuickAction: (prompt: string) => void
  onGenerateDraft: (intent: AiDraftIntent, prompt: string) => void
  onInstructionChange: (value: string) => void
  onCustomDraft: () => void
  onApplyDraft: (mode: AiDraftApplyMode) => void
  onDiscardDraft: () => void
  onSubmit: (event: FormEvent<HTMLFormElement>) => void
  onClear: () => void
  onStop: () => void
  messagesEndRef: RefObject<HTMLDivElement | null>
}

export function AiAssistantContent({
  notebook,
  activePage,
  messages,
  toolActivities,
  isRunning,
  canUseAssistant,
  canUseDrafts,
  quickActions,
  draftActions,
  noteDraft,
  draftInstruction,
  error,
  applyPending,
  generatePending,
  draft,
  setDraft,
  onQuickAction,
  onGenerateDraft,
  onInstructionChange,
  onCustomDraft,
  onApplyDraft,
  onDiscardDraft,
  onSubmit,
  onClear,
  onStop,
  messagesEndRef,
}: AiAssistantContentProps) {
  const { t } = useTranslation()

  return (
    <>
      <div className="min-h-0 flex-1 overflow-y-auto px-4 py-3">
        <div className="space-y-3">
          {messages.length === 0 ? (
            <div>
              <div className="mb-3 flex items-start gap-2 rounded-md border border-border-subtle bg-surface-elevated px-3 py-2.5">
                <BookOpen className="mt-0.5 h-4 w-4 shrink-0 text-brand-brown" />
                <div className="min-w-0">
                  <p className="truncate text-xs font-medium text-text-primary">
                    {activePage?.title ?? notebook.title}
                  </p>
                  <p className="mt-0.5 text-[11px] text-text-tertiary">
                    {activePage ? activePage.path : notebook.slug}
                  </p>
                </div>
              </div>
              <div className="grid gap-2">
                {quickActions.map((action) => (
                  <QuickActionButton
                    key={action.id}
                    action={action}
                    disabled={!canUseAssistant || isRunning}
                    onClick={onQuickAction}
                  />
                ))}
              </div>
            </div>
          ) : (
            <div className="space-y-3">
              {messages.map((message) => (
                <MessageBubble key={message.id} role={message.role} text={getMessageText(message)} />
              ))}
              {isRunning && <AssistantThinking />}
            </div>
          )}

          {canUseDrafts && (
            <DraftWorkspace
              activePage={activePage}
              applyPending={applyPending}
              draft={noteDraft}
              draftActions={draftActions}
              generatePending={generatePending}
              hasConversation={messages.length > 0}
              instruction={draftInstruction}
              onApply={onApplyDraft}
              onGenerate={onGenerateDraft}
              onInstructionChange={onInstructionChange}
              onCustomGenerate={onCustomDraft}
              onDiscardDraft={onDiscardDraft}
            />
          )}
        </div>

        {toolActivities.length > 0 && (
          <div className="mt-3 space-y-1.5">
            {toolActivities.map((activity) => (
              <ToolActivityRow key={activity.id} activity={activity} />
            ))}
          </div>
        )}
        <div ref={messagesEndRef} />
      </div>

      {error && (
        <div className="mx-4 mb-2 rounded-md border border-status-error-border bg-status-error-bg px-3 py-2 text-[11px] text-status-error">
          {t(errorKey(error.code), { defaultValue: error.message })}
        </div>
      )}

      <AiAssistantForm
        draft={draft}
        setDraft={setDraft}
        canUseAssistant={canUseAssistant}
        isRunning={isRunning}
        canClear={messages.length > 0 || toolActivities.length > 0}
        onSubmit={onSubmit}
        onClear={onClear}
        onStop={onStop}
      />
    </>
  )
}
