import type { FormEvent, RefObject } from 'react'
import { BookOpen } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import {
  getMessageText,
  type AiAssistantErrorCode,
  type AiAssistantVisibleMessage,
  type AiToolActivity,
} from '@/features/ai-assistant'
import { errorKey } from '../lib/labels'
import type { AiAssistantMode, EditMessage } from './AiAssistant'
import { AiAssistantForm } from './AiAssistantForm'
import { AssistantThinking } from './AssistantThinking'
import { MessageBubble } from './MessageBubble'
import { ToolActivityRow } from './ToolActivityRow'

interface AiAssistantContentProps {
  mode: AiAssistantMode
  onModeChange: (mode: AiAssistantMode) => void
  chatMessages: AiAssistantVisibleMessage[]
  editMessages: EditMessage[]
  toolActivities: AiToolActivity[]
  isRunning: boolean
  canUseAssistant: boolean
  canUseEdit: boolean
  error: { code: AiAssistantErrorCode; message: string } | null
  draft: string
  setDraft: (value: string) => void
  onSubmit: (event: FormEvent<HTMLFormElement>) => void
  onClear: () => void
  onStop: () => void
  onReopenProposal?: () => void
  messagesEndRef: RefObject<HTMLDivElement | null>
}

export function AiAssistantContent({
  mode,
  onModeChange,
  chatMessages,
  editMessages,
  toolActivities,
  isRunning,
  canUseAssistant,
  canUseEdit,
  error,
  draft,
  setDraft,
  onSubmit,
  onClear,
  onStop,
  onReopenProposal,
  messagesEndRef,
}: AiAssistantContentProps) {
  const { t } = useTranslation()
  const messages = mode === 'chat' ? chatMessages : editMessages
  const isEmpty = messages.length === 0 && toolActivities.length === 0

  return (
    <>
      <div className="min-h-0 flex-1 overflow-y-auto px-4 py-3">
        <div className="space-y-3">
          {isEmpty ? (
            <div className="rounded-md border border-border-subtle bg-surface-elevated px-3 py-2.5">
              <div className="flex items-start gap-2">
                <BookOpen className="mt-0.5 h-4 w-4 shrink-0 text-brand-brown" />
                <p className="text-sm text-text-secondary">{t('ai.inputPlaceholder')}</p>
              </div>
            </div>
          ) : (
            <div className="space-y-3">
              {mode === 'chat'
                ? chatMessages.map((message) => (
                    <MessageBubble key={message.id} role={message.role} text={getMessageText(message)} />
                  ))
                : editMessages.map((message) => (
                    <EditMessageRow key={message.id} message={message} onReopenProposal={onReopenProposal} />
                  ))}
              {isRunning && <AssistantThinking />}
            </div>
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
        mode={mode}
        draft={draft}
        setDraft={setDraft}
        canUseAssistant={canUseAssistant}
        canUseEdit={canUseEdit}
        isRunning={isRunning}
        canClear={messages.length > 0 || toolActivities.length > 0}
        onSubmit={onSubmit}
        onClear={onClear}
        onStop={onStop}
        onModeChange={onModeChange}
      />
    </>
  )
}

function EditMessageRow({ message, onReopenProposal }: { message: EditMessage; onReopenProposal?: () => void }) {
  const { t } = useTranslation()

  if (message.role === 'proposal' && message.proposal) {
    const proposal = message.proposal
    return (
      <div className="rounded-md border border-border-subtle bg-surface-elevated p-3">
        <p className="text-xs font-medium text-text-primary">
          {proposal.operation === 'create_page'
            ? t('ai.edit.newPageTitle', { title: proposal.title })
            : proposal.title}
        </p>
        {proposal.summary && <p className="mt-1 text-xs text-text-secondary">{proposal.summary}</p>}
        {onReopenProposal ? (
          <button
            type="button"
            onClick={onReopenProposal}
            className="mt-2 text-left text-xs font-medium text-brand-brown hover:underline"
          >
            {t('ai.edit.review')}
          </button>
        ) : (
          <p className="mt-2 text-[11px] text-text-tertiary">{t('ai.edit.previewShown')}</p>
        )}
      </div>
    )
  }

  return <MessageBubble role={message.role as 'user' | 'assistant'} text={message.content ?? ''} />
}
