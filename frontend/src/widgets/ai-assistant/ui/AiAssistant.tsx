import { useEffect, useRef, useState, type FormEvent, type HTMLAttributes } from 'react'
import { useNavigate } from 'react-router-dom'
import { Loader2, Sparkles } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import type { Notebook } from '@/entities/notebook'
import type { NotebookItem } from '@/entities/notebook-item'
import { useUser } from '@/entities/user'
import {
  useApplyAiNoteDraft,
  useAiAssistantSession,
  useGenerateAiNoteDraft,
  useAiStatus,
  type AiDraftApplyMode,
  type AiDraftIntent,
  type AiNoteDraftResponse,
} from '@/features/ai-assistant'
import { getErrorMessage } from '@/shared/lib/errorUtils'
import { useToast } from '@/shared/ui/Toast'
import { useDraftActions } from '../hooks/useDraftActions'
import { useQuickActions } from '../hooks/useQuickActions'
import { AiAssistantContent } from './AiAssistantContent'
import { AiAssistantGate } from './AiAssistantGate'
import { AiAssistantHeader } from './AiAssistantHeader'

interface AiAssistantProps {
  notebook: Notebook
  activePage: NotebookItem | null
  dragHandleProps?: HTMLAttributes<HTMLDivElement>
  onCollapse?: () => void
  variant?: 'docked' | 'floating'
}

export default function AiAssistant({
  notebook,
  activePage,
  dragHandleProps,
  onCollapse,
  variant = 'docked',
}: AiAssistantProps) {
  const [collapsed, setCollapsed] = useState(false)
  const [draft, setDraft] = useState('')
  const [draftInstruction, setDraftInstruction] = useState('')
  const [noteDraft, setNoteDraft] = useState<AiNoteDraftResponse | null>(null)
  const messagesEndRef = useRef<HTMLDivElement>(null)
  const { t, i18n } = useTranslation()
  const navigate = useNavigate()
  const { showToast } = useToast()
  const aiStatus = useAiStatus()
  const user = useUser()

  const aiEnabled = aiStatus.data?.enabled ?? false
  const endpointPath = aiStatus.data?.endpointPath ?? null
  const draftEndpointPath = aiStatus.data?.draftEndpointPath ?? null
  const isSignedIn = Boolean(user.data?.user)
  const canUseAssistant = aiEnabled && isSignedIn
  const canUseDrafts = canUseAssistant && Boolean(notebook.canEdit) && Boolean(draftEndpointPath)

  const {
    clear,
    error,
    isRunning,
    messages,
    sendMessage,
    stop,
    toolActivities,
  } = useAiAssistantSession({
    enabled: canUseAssistant,
    endpointPath,
    notebook,
    activePage,
  })

  const generateDraft = useGenerateAiNoteDraft({
    draftEndpointPath,
    notebook,
    activePage,
    locale: i18n.resolvedLanguage ?? i18n.language,
  })

  const applyDraft = useApplyAiNoteDraft({
    notebook,
    activePage,
  })

  const quickActions = useQuickActions(activePage, notebook)
  const draftActions = useDraftActions(activePage, notebook)

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView?.({ block: 'end' })
  }, [messages, toolActivities, isRunning])

  const isFloating = variant === 'floating'
  const isCollapsed = !isFloating && collapsed
  const handleCollapse = onCollapse ?? (() => setCollapsed(true))
  const { className: dragHandleClassName, ...dragHandleAttributes } = dragHandleProps ?? {}

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const nextDraft = draft.trim()
    if (!nextDraft || !canUseAssistant || isRunning) {
      return
    }

    setDraft('')
    await sendMessage(nextDraft)
  }

  const handleQuickAction = async (prompt: string) => {
    if (!canUseAssistant || isRunning) {
      return
    }

    await sendMessage(prompt)
  }

  const handleGenerateDraft = async (intent: AiDraftIntent, prompt: string) => {
    if (!canUseDrafts || generateDraft.isPending) {
      return
    }

    try {
      const generatedDraft = await generateDraft.mutateAsync({ intent, prompt })
      setNoteDraft(generatedDraft)
      setDraftInstruction('')
    } catch (err) {
      showToast(getErrorMessage(err, t('ai.drafts.errors.generateFailed')), 'error')
    }
  }

  const handleCustomDraft = async () => {
    const prompt = draftInstruction.trim()
    if (!prompt) {
      return
    }

    await handleGenerateDraft('custom', prompt)
  }

  const handleApplyDraft = async (mode: AiDraftApplyMode) => {
    if (!noteDraft || applyDraft.isPending) {
      return
    }

    try {
      const result = await applyDraft.mutateAsync({
        mode,
        markdown: noteDraft.markdown,
        title: noteDraft.title,
      })
      showToast(t('ai.drafts.applied'))
      setNoteDraft(null)
      navigate(`/notes/${notebook.slug}/${result.path}`)
    } catch (err) {
      showToast(getErrorMessage(err, t('ai.drafts.errors.applyFailed')), 'error')
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
    : 'flex h-[44vh] min-h-[300px] max-h-[540px] shrink-0 flex-col border-t border-border-subtle bg-surface'

  const isGateBlocking =
    aiStatus.isPending || user.isPending || aiStatus.isError || !aiEnabled || !isSignedIn

  return (
    <div className={rootClassName}>
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
          notebook={notebook}
          activePage={activePage}
          messages={messages}
          toolActivities={toolActivities}
          isRunning={isRunning}
          canUseAssistant={canUseAssistant}
          canUseDrafts={canUseDrafts}
          quickActions={quickActions}
          draftActions={draftActions}
          noteDraft={noteDraft}
          draftInstruction={draftInstruction}
          error={error}
          applyPending={applyDraft.isPending}
          generatePending={generateDraft.isPending}
          draft={draft}
          setDraft={setDraft}
          onQuickAction={handleQuickAction}
          onGenerateDraft={handleGenerateDraft}
          onInstructionChange={setDraftInstruction}
          onCustomDraft={handleCustomDraft}
          onApplyDraft={handleApplyDraft}
          onDiscardDraft={() => setNoteDraft(null)}
          onSubmit={handleSubmit}
          onClear={clear}
          onStop={stop}
          messagesEndRef={messagesEndRef}
        />
      )}
    </div>
  )
}
