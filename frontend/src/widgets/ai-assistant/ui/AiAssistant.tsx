import { useCallback, useEffect, useRef, useState, type FormEvent, type HTMLAttributes } from 'react'
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
import { useEditMessages } from './useEditMessages'

export type AiAssistantMode = 'chat' | 'edit'

interface AiAssistantProps {
  notebook: Notebook
  activePage: NotebookItem | null
  dragHandleProps?: HTMLAttributes<HTMLDivElement>
  onCollapse?: () => void
  variant?: 'docked' | 'floating'
}

const DOCKED_MIN_HEIGHT = 300
const DOCKED_MAX_HEIGHT = 540

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
  const [dockedHeight, setDockedHeight] = useState<number | null>(null)
  const messagesEndRef = useRef<HTMLDivElement>(null)
  const rootRef = useRef<HTMLDivElement>(null)
  const resizeStartRef = useRef<{ pointerId: number; startY: number; startHeight: number } | null>(null)
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

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView?.({ block: 'end' })
  }, [chatMessages, editMessages, toolActivities, isChatRunning, createEdit.isPending])

  const isFloating = variant === 'floating'
  const isCollapsed = !isFloating && collapsed
  const handleCollapse = onCollapse ?? (() => setCollapsed(true))
  const { className: dragHandleClassName, ...dragHandleAttributes } = dragHandleProps ?? {}

  const isRunning = mode === 'chat' ? isChatRunning : createEdit.isPending
  const error = mode === 'chat' ? chatError : null

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

  const handleResizeStart = useCallback((event: React.PointerEvent<HTMLDivElement>) => {
    if (event.button !== 0 || isFloating) return
    event.preventDefault()
    resizeStartRef.current = {
      pointerId: event.pointerId,
      startY: event.clientY,
      startHeight: dockedHeight ?? rootRef.current?.getBoundingClientRect().height ?? DOCKED_MIN_HEIGHT,
    }
    ;(event.currentTarget as HTMLElement).setPointerCapture(event.pointerId)
  }, [dockedHeight, isFloating])

  useEffect(() => {
    if (!resizeStartRef.current) return

    function handlePointerMove(event: PointerEvent) {
      const start = resizeStartRef.current
      if (!start || event.pointerId !== start.pointerId) return
      const deltaY = start.startY - event.clientY
      const nextHeight = Math.min(DOCKED_MAX_HEIGHT, Math.max(DOCKED_MIN_HEIGHT, start.startHeight + deltaY))
      setDockedHeight(nextHeight)
    }

    function handlePointerEnd(event: PointerEvent) {
      const start = resizeStartRef.current
      if (!start || event.pointerId !== start.pointerId) return
      resizeStartRef.current = null
    }

    window.addEventListener('pointermove', handlePointerMove)
    window.addEventListener('pointerup', handlePointerEnd)
    window.addEventListener('pointercancel', handlePointerEnd)
    return () => {
      window.removeEventListener('pointermove', handlePointerMove)
      window.removeEventListener('pointerup', handlePointerEnd)
      window.removeEventListener('pointercancel', handlePointerEnd)
    }
  }, [])

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

  const isGateBlocking =
    aiStatus.isPending || user.isPending || aiStatus.isError || !aiEnabled || !isSignedIn

  return (
    <div ref={rootRef} className={rootClassName} style={rootStyle}>
      {!isFloating && (
        <div
          role="separator"
          aria-orientation="horizontal"
          aria-label={t('ai.resizeHandle')}
          title={t('ai.resizeHandle')}
          onPointerDown={handleResizeStart}
          className="h-1.5 w-full shrink-0 cursor-ns-resize bg-transparent hover:bg-border-subtle"
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
