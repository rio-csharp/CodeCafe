import { useEffect, useMemo, useRef, useState, type FormEvent, type HTMLAttributes } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import {
  AlertCircle,
  BookOpen,
  FileText,
  ListPlus,
  Loader2,
  Lock,
  Minus,
  PenLine,
  Plus,
  RotateCcw,
  Search,
  Send,
  Sparkles,
  Square,
  Wand2,
} from 'lucide-react'
import { useTranslation } from 'react-i18next'
import type { LucideIcon } from 'lucide-react'
import type { Notebook } from '@/entities/notebook'
import type { NotebookItem } from '@/entities/notebook-item'
import { useUser } from '@/entities/user'
import {
  getMessageText,
  useApplyAiNoteDraft,
  useAiAssistantSession,
  useGenerateAiNoteDraft,
  useAiStatus,
  type AiDraftApplyMode,
  type AiDraftIntent,
  type AiAssistantErrorCode,
  type AiNoteDraftResponse,
  type AiToolActivity,
} from '@/features/ai-assistant'
import { getErrorMessage } from '@/shared/lib/errorUtils'
import { useToast } from '@/shared/ui/Toast'

interface AiAssistantProps {
  notebook: Notebook
  activePage: NotebookItem | null
  dragHandleProps?: HTMLAttributes<HTMLDivElement>
  onCollapse?: () => void
  variant?: 'docked' | 'floating'
}

interface QuickAction {
  id: string
  icon: LucideIcon
  label: string
  prompt: string
}

interface DraftQuickAction {
  id: AiDraftIntent
  icon: LucideIcon
  label: string
  prompt: string
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

  const quickActions = useMemo<QuickAction[]>(() => {
    const pageTitle = activePage?.title ?? notebook.title
    const pagePath = activePage?.path ?? ''

    return [
      {
        id: 'summarize',
        icon: FileText,
        label: t('ai.actions.summarize'),
        prompt: activePage
          ? t('ai.prompts.summarizePage', { pageTitle, pagePath, notebookSlug: notebook.slug })
          : t('ai.prompts.summarizeNotebook', { notebookTitle: notebook.title, notebookSlug: notebook.slug }),
      },
      {
        id: 'related',
        icon: Search,
        label: t('ai.actions.related'),
        prompt: activePage
          ? t('ai.prompts.relatedPage', { pageTitle, pagePath, notebookSlug: notebook.slug })
          : t('ai.prompts.relatedNotebook', { notebookTitle: notebook.title, notebookSlug: notebook.slug }),
      },
      {
        id: 'outline',
        icon: Wand2,
        label: t('ai.actions.outline'),
        prompt: activePage
          ? t('ai.prompts.outlinePage', { pageTitle, pagePath, notebookSlug: notebook.slug })
          : t('ai.prompts.outlineNotebook', { notebookTitle: notebook.title, notebookSlug: notebook.slug }),
      },
    ]
  }, [activePage, notebook.slug, notebook.title, t])

  const draftActions = useMemo<DraftQuickAction[]>(() => {
    const pageTitle = activePage?.title ?? notebook.title
    const pagePath = activePage?.path ?? ''

    return [
      {
        id: 'summarize',
        icon: FileText,
        label: t('ai.drafts.actions.summary'),
        prompt: activePage
          ? t('ai.drafts.prompts.summaryPage', { pageTitle, pagePath, notebookSlug: notebook.slug })
          : t('ai.drafts.prompts.summaryNotebook', { notebookTitle: notebook.title, notebookSlug: notebook.slug }),
      },
      {
        id: 'outline',
        icon: ListPlus,
        label: t('ai.drafts.actions.outline'),
        prompt: activePage
          ? t('ai.drafts.prompts.outlinePage', { pageTitle, pagePath, notebookSlug: notebook.slug })
          : t('ai.drafts.prompts.outlineNotebook', { notebookTitle: notebook.title, notebookSlug: notebook.slug }),
      },
      {
        id: 'rewrite',
        icon: PenLine,
        label: t('ai.drafts.actions.rewrite'),
        prompt: activePage
          ? t('ai.drafts.prompts.rewritePage', { pageTitle, pagePath, notebookSlug: notebook.slug })
          : t('ai.drafts.prompts.rewriteNotebook', { notebookTitle: notebook.title, notebookSlug: notebook.slug }),
      },
      {
        id: 'expand',
        icon: Plus,
        label: t('ai.drafts.actions.expand'),
        prompt: activePage
          ? t('ai.drafts.prompts.expandPage', { pageTitle, pagePath, notebookSlug: notebook.slug })
          : t('ai.drafts.prompts.expandNotebook', { notebookTitle: notebook.title, notebookSlug: notebook.slug }),
      },
      {
        id: 'continue',
        icon: Wand2,
        label: t('ai.drafts.actions.continue'),
        prompt: activePage
          ? t('ai.drafts.prompts.continuePage', { pageTitle, pagePath, notebookSlug: notebook.slug })
          : t('ai.drafts.prompts.continueNotebook', { notebookTitle: notebook.title, notebookSlug: notebook.slug }),
      },
    ]
  }, [activePage, notebook.slug, notebook.title, t])

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
  const headerClassName = isFloating
    ? `flex select-none items-start justify-between gap-3 border-b border-border-subtle px-4 py-2.5 ${dragHandleClassName ?? ''}`
    : 'flex items-start justify-between gap-3 px-4 py-2.5'

  return (
    <div className={rootClassName}>
      <div {...dragHandleAttributes} className={headerClassName}>
        <div className="min-w-0">
          <div className="flex items-center gap-2">
            <Sparkles className="h-4 w-4 shrink-0 text-brand-brown" />
            <span className="truncate text-sm font-medium text-text-primary">{t('ai.title')}</span>
          </div>
          <div className="mt-1 flex items-center gap-1.5">
            <span className="max-w-[138px] truncate rounded-sm bg-surface-elevated px-1.5 py-0.5 text-[10px] text-text-tertiary">
              {activePage?.title ?? notebook.title}
            </span>
            <span className="rounded-sm bg-brand-brown/10 px-1.5 py-0.5 text-[10px] font-medium text-brand-brown">
              {t('ai.readOnly')}
            </span>
          </div>
        </div>
        <button
          type="button"
          onClick={handleCollapse}
          onPointerDown={(event) => event.stopPropagation()}
          className="rounded p-1 text-text-tertiary transition-colors hover:bg-surface-hover hover:text-text-primary"
          aria-label={t('ai.collapse')}
          title={t('ai.collapse')}
        >
          <Minus className="h-3.5 w-3.5" />
        </button>
      </div>

      {renderGate({
        aiStatusPending: aiStatus.isPending,
        aiStatusError: aiStatus.isError,
        aiEnabled,
        userPending: user.isPending,
        isSignedIn,
        t,
      }) ?? (
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
                        onClick={handleQuickAction}
                      />
                    ))}
                  </div>
                </div>
              ) : (
                <div className="space-y-3">
                  {messages.map((message) => (
                    <MessageBubble key={message.id} role={message.role} text={getMessageText(message)} />
                  ))}
                  {isRunning && <AssistantThinking label={t('ai.thinking')} />}
                </div>
              )}

              {canUseDrafts && (
                <DraftWorkspace
                  activePage={activePage}
                  applyPending={applyDraft.isPending}
                  draft={noteDraft}
                  draftActions={draftActions}
                  generatePending={generateDraft.isPending}
                  instruction={draftInstruction}
                  onApply={handleApplyDraft}
                  onGenerate={handleGenerateDraft}
                  onInstructionChange={setDraftInstruction}
                  onCustomGenerate={handleCustomDraft}
                  onDiscardDraft={() => setNoteDraft(null)}
                  t={t}
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

          <form onSubmit={handleSubmit} className="border-t border-border-subtle p-3">
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
                  onClick={clear}
                  disabled={messages.length === 0 && toolActivities.length === 0}
                  className="flex h-9 w-9 items-center justify-center rounded-md border border-border-default text-text-secondary transition-colors hover:bg-surface-hover disabled:cursor-not-allowed disabled:opacity-40"
                  aria-label={t('ai.clear')}
                  title={t('ai.clear')}
                >
                  <RotateCcw className="h-3.5 w-3.5" />
                </button>
                {isRunning ? (
                  <button
                    type="button"
                    onClick={stop}
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
        </>
      )}
    </div>
  )
}

function QuickActionButton({
  action,
  disabled,
  onClick,
}: {
  action: QuickAction
  disabled: boolean
  onClick: (prompt: string) => void
}) {
  const Icon = action.icon

  return (
    <button
      type="button"
      onClick={() => onClick(action.prompt)}
      disabled={disabled}
      className="flex min-h-10 items-center gap-2 rounded-md border border-border-subtle px-3 py-2 text-left text-xs font-medium text-text-secondary transition-colors hover:border-border-hover hover:bg-surface-hover disabled:cursor-not-allowed disabled:opacity-50"
    >
      <Icon className="h-3.5 w-3.5 shrink-0 text-brand-brown" />
      <span className="min-w-0 truncate">{action.label}</span>
    </button>
  )
}

function DraftWorkspace({
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
  t,
}: {
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
  t: ReturnType<typeof useTranslation>['t']
}) {
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

function DraftActionButton({
  action,
  disabled,
  onClick,
}: {
  action: DraftQuickAction
  disabled: boolean
  onClick: (intent: AiDraftIntent, prompt: string) => void
}) {
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

function DraftApplyButton({
  disabled,
  icon: Icon,
  label,
  onClick,
}: {
  disabled: boolean
  icon: LucideIcon
  label: string
  onClick: () => void
}) {
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

function MessageBubble({ role, text }: { role: 'assistant' | 'user'; text: string }) {
  const isUser = role === 'user'

  return (
    <div className={`flex ${isUser ? 'justify-end' : 'justify-start'}`}>
      <div
        className={`max-w-[92%] whitespace-pre-wrap rounded-md px-3 py-2 text-xs leading-5 ${
          isUser
            ? 'bg-text-primary text-text-inverse'
            : 'border border-border-subtle bg-surface-elevated text-text-primary'
        }`}
      >
        {text}
      </div>
    </div>
  )
}

function AssistantThinking({ label }: { label: string }) {
  return (
    <div className="flex justify-start">
      <div className="inline-flex items-center gap-2 rounded-md border border-border-subtle bg-surface-elevated px-3 py-2 text-xs text-text-secondary">
        <Loader2 className="h-3.5 w-3.5 animate-spin text-brand-brown" />
        {label}
      </div>
    </div>
  )
}

function ToolActivityRow({ activity }: { activity: AiToolActivity }) {
  const { t } = useTranslation()
  const isDone = activity.status === 'done'

  return (
    <div className="rounded-md border border-border-subtle bg-surface-elevated px-2.5 py-1.5">
      <div className="flex items-center gap-2 text-[11px] text-text-secondary">
        {isDone ? (
          <span className="h-1.5 w-1.5 rounded-full bg-status-success" />
        ) : (
          <Loader2 className="h-3 w-3 animate-spin text-brand-brown" />
        )}
        <span className="min-w-0 truncate">{toolLabel(activity.name, t)}</span>
      </div>
    </div>
  )
}

function renderGate({
  aiStatusPending,
  aiStatusError,
  aiEnabled,
  userPending,
  isSignedIn,
  t,
}: {
  aiStatusPending: boolean
  aiStatusError: boolean
  aiEnabled: boolean
  userPending: boolean
  isSignedIn: boolean
  t: ReturnType<typeof useTranslation>['t']
}) {
  if (aiStatusPending || userPending) {
    return <PanelNotice icon={Loader2} spin title={t('ai.checking')} />
  }

  if (aiStatusError) {
    return (
      <PanelNotice
        icon={AlertCircle}
        title={t('ai.statusErrorTitle')}
        description={t('ai.statusErrorDescription')}
      />
    )
  }

  if (!aiEnabled) {
    return (
      <PanelNotice
        icon={Sparkles}
        title={t('ai.disabledTitle')}
        description={t('ai.disabledDescription')}
      />
    )
  }

  if (!isSignedIn) {
    return (
      <div className="flex flex-1 flex-col items-center justify-center px-5 py-8 text-center">
        <div className="mb-3 flex h-10 w-10 items-center justify-center rounded-full bg-brand-brown/10">
          <Lock className="h-5 w-5 text-brand-brown" />
        </div>
        <p className="text-sm font-medium text-text-primary">{t('ai.signInTitle')}</p>
        <p className="mt-1 max-w-[220px] text-xs leading-5 text-text-tertiary">
          {t('ai.signInDescription')}
        </p>
        <Link
          to="/login"
          className="mt-3 rounded-md bg-text-primary px-3 py-1.5 text-xs font-medium text-text-inverse transition-colors hover:bg-surface-inverse-hover"
        >
          {t('ai.signIn')}
        </Link>
      </div>
    )
  }

  return null
}

function PanelNotice({
  icon: Icon,
  spin,
  title,
  description,
}: {
  icon: LucideIcon
  spin?: boolean
  title: string
  description?: string
}) {
  return (
    <div className="flex flex-1 flex-col items-center justify-center px-5 py-8 text-center">
      <div className="mb-3 flex h-10 w-10 items-center justify-center rounded-full bg-brand-brown/10">
        <Icon className={`h-5 w-5 text-brand-brown ${spin ? 'animate-spin' : ''}`} />
      </div>
      <p className="text-sm font-medium text-text-primary">{title}</p>
      {description && (
        <p className="mt-1 max-w-[220px] text-xs leading-5 text-text-tertiary">{description}</p>
      )}
    </div>
  )
}

function toolLabel(name: string, t: ReturnType<typeof useTranslation>['t']): string {
  const fallback = name
    .split('_')
    .filter(Boolean)
    .map((part) => `${part.slice(0, 1).toUpperCase()}${part.slice(1)}`)
    .join(' ')

  return t(`ai.tools.${name}`, { defaultValue: fallback })
}

function errorKey(code: AiAssistantErrorCode): string {
  return `ai.errors.${code}`
}
