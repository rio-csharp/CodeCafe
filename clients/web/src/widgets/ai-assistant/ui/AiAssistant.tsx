import { useCallback, useState, type FormEvent, type HTMLAttributes } from 'react'
import { Loader2, Sparkles } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import type { Notebook } from '@/entities/notebook'
import type { NotebookItem } from '@/entities/notebook-item'
import { useUser } from '@/entities/user'
import {
  useAiAssistantSession,
  useAiStatus,
  useCreateAiEditProposal,
  useAiEditStore,
} from '@/features/ai-assistant'
import { getErrorMessage } from '@/shared/lib/errorUtils'
import { useToast } from '@/shared/ui/Toast'
import { AiAssistantContent } from './AiAssistantContent'
import { AiAssistantGate } from './AiAssistantGate'
import { AiAssistantHeader } from './AiAssistantHeader'
import { useDockedResize } from './useDockedResize'
import { useAssistantViewState } from './useAssistantViewState'
import { useEditMessages } from './useEditMessages'

export type AiAssistantMode = 'chat' | 'edit'

interface AiAssistantProps {
  notebook: Notebook
  activePage: NotebookItem | null
  dragHandleProps?: HTMLAttributes<HTMLDivElement>
  onCollapse?: () => void
  variant?: 'docked' | 'floating'
}

function createMessageId(): string {
  return `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`
}

export default function AiAssistant({
  notebook,
  activePage,
  dragHandleProps,
  onCollapse,
  variant = 'docked',
}: AiAssistantProps) {
  const [collapsed, setCollapsed] = useState(false)
  const [mode, setMode] = useState<AiAssistantMode>('chat')
  const { editMessages, setEditMessages, clearEditMessages } = useEditMessages({ notebook, activePage })
  const [draft, setDraft] = useState('')
  const { t } = useTranslation()
  const { showToast } = useToast()
  const aiStatus = useAiStatus()
  const user = useUser()
  const setProposal = useAiEditStore((s) => s.setProposal)
  const clearProposal = useAiEditStore((s) => s.clearProposal)
  const openPreview = useAiEditStore((s) => s.openPreview)

  const aiEnabled = aiStatus.data?.enabled ?? false
  const endpointPath = aiStatus.data?.endpointPath ?? null
  const editEndpointPath = aiStatus.data?.editEndpointPath ?? null
  const isSignedIn = Boolean(user.data?.user)
  const canUseAssistant = aiEnabled && isSignedIn
  const canUseEdit = canUseAssistant && Boolean(notebook.canEdit) && Boolean(editEndpointPath)

  const {
    clear,
    error: chatError,
    isRunning: isChatRunning,
    messages: chatMessages,
    sendMessage,
    stop,
    toolActivities,
  } = useAiAssistantSession({
    enabled: canUseAssistant && mode === 'chat',
    endpointPath,
    notebook,
    activePage,
  })

  const createEdit = useCreateAiEditProposal({
    editEndpointPath,
    notebook,
    activePage,
  })

  const isFloating = variant === 'floating'
  const isCollapsed = !isFloating && collapsed
  const handleCollapse = onCollapse ?? (() => setCollapsed(true))
  const { className: dragHandleClassName, ...dragHandleAttributes } = dragHandleProps ?? {}

  const isRunning = mode === 'chat' ? isChatRunning : createEdit.isPending
  const error = mode === 'chat' ? chatError : null

  const { rootRef, dockedHeight, handleResizeStart, handleResizeKeyDown } = useDockedResize(isFloating)
  const { messagesEndRef, isGateBlocking } = useAssistantViewState({
    aiStatusPending: aiStatus.isPending,
    aiStatusError: aiStatus.isError,
    userPending: user.isPending,
    aiEnabled,
    isSignedIn,
    watch: [chatMessages, editMessages, toolActivities, isChatRunning, createEdit.isPending],
  })

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const prompt = draft.trim()
    if (!prompt || !canUseAssistant || isRunning) {
      return
    }

    if (mode === 'chat') {
      setDraft('')
      await sendMessage(prompt)
      return
    }

    if (!canUseEdit) {
      showToast(t('ai.edit.errors.notConfigured'), 'error')
      return
    }

    setDraft('')
    setEditMessages((current) => [
      ...current,
      { id: createMessageId(), role: 'user', content: prompt },
    ])

    try {
      const response = await createEdit.mutateAsync({ prompt })
      setProposal(response)
      setEditMessages((current) => [
        ...current,
        { id: createMessageId(), role: 'proposal', proposal: response },
      ])
    } catch (err) {
      setEditMessages((current) => [
        ...current,
        { id: createMessageId(), role: 'assistant', content: getErrorMessage(err, t('ai.edit.errors.createFailed')) },
      ])
    }
  }

  const handleReopenProposal = useCallback(() => {
    openPreview()
  }, [openPreview])

  const handleClear = () => {
    if (mode === 'chat') {
      clear()
    } else {
      clearEditMessages()
      clearProposal()
    }
  }

  if (isCollapsed) {
    return (
      <div className="border-t border-border-subtle px-4 py-3">
        <button
          type="button"
          onClick={() => setCollapsed(false)}
          className="flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left transition-colors hover:bg-surface-hover"
        >
          <Sparkles className="h-4 w-4 text-brand-brown" />
          <span className="text-sm font-medium text-text-primary">{t('ai.title')}</span>
          {isRunning && <Loader2 className="ml-auto h-3.5 w-3.5 animate-spin text-brand-brown" />}
        </button>
      </div>
    )
  }

  const rootClassName = isFloating
    ? 'flex h-full min-h-0 flex-col overflow-hidden rounded-lg border border-border-default bg-surface shadow-2xl'
    : 'flex min-h-[300px] max-h-[540px] shrink-0 flex-col border-t border-border-subtle bg-surface'

  const rootStyle = !isFloating && dockedHeight !== null
    ? { height: `${dockedHeight}px` }
    : undefined

  return (
    <div ref={rootRef} className={rootClassName} style={rootStyle}>
      {!isFloating && (
        <div
          role="separator"
          aria-orientation="horizontal"
          aria-label={t('ai.resizeHandle')}
          title={t('ai.resizeHandle')}
          tabIndex={0}
          onPointerDown={handleResizeStart}
          onKeyDown={handleResizeKeyDown}
          className="h-1.5 w-full shrink-0 cursor-ns-resize bg-transparent hover:bg-border-subtle focus-visible:bg-border-subtle focus-visible:outline-none"
        />
      )}
      <AiAssistantHeader
        variant={variant}
        notebook={notebook}
        activePage={activePage}
        dragHandleClassName={dragHandleClassName}
        dragHandleAttributes={dragHandleAttributes}
        onCollapse={handleCollapse}
      />

      {isGateBlocking ? (
        <AiAssistantGate
          aiStatusPending={aiStatus.isPending}
          aiStatusError={aiStatus.isError}
          aiEnabled={aiEnabled}
          userPending={user.isPending}
          isSignedIn={isSignedIn}
        />
      ) : (
        <AiAssistantContent
          mode={mode}
          onModeChange={setMode}
          canUseEdit={canUseEdit}
          chatMessages={chatMessages}
          editMessages={editMessages}
          toolActivities={mode === 'chat' ? toolActivities : []}
          isRunning={isRunning}
          canUseAssistant={canUseAssistant}
          error={error}
          draft={draft}
          setDraft={setDraft}
          onSubmit={handleSubmit}
          onClear={handleClear}
          onStop={stop}
          onReopenProposal={handleReopenProposal}
          messagesEndRef={messagesEndRef}
        />
      )}
    </div>
  )
}
