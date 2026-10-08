import { useEffect, useRef, useState, useSyncExternalStore } from 'react'
import { useTranslation } from 'react-i18next'
import { getChatStore } from '../model/store'
import type { ChatMessage } from '../model/store'

export interface AiChatPanelProps {
  slug: string
  /** The AI edited the notebook during the turn — refetch page and tree. */
  onAiChanged: () => void
}

/**
 * The notebook's AI conversation. Streaming text lands character-grouped into
 * the open assistant bubble; tool calls show as one quiet activity line —
 * "正在阅读页面…" while running, a past-tense summary when done — because the
 * user cares about the reply, not the plumbing.
 */
export function AiChatPanel({ slug, onAiChanged }: AiChatPanelProps) {
  const { t } = useTranslation()
  const store = getChatStore(slug)
  const state = useSyncExternalStore(store.subscribe, store.getState)
  const [input, setInput] = useState('')
  const scrollRef = useRef<HTMLDivElement>(null)

  // Follow the stream: keep the latest content in view while it grows.
  useEffect(() => {
    const element = scrollRef.current
    if (element !== null) {
      element.scrollTop = element.scrollHeight
    }
  }, [state.messages])

  const send = () => {
    const text = input.trim()
    if (text === '') {
      return
    }
    setInput('')
    void store.send(text, onAiChanged)
  }

  return (
    <div className="flex h-full min-h-0 flex-col">
      <div ref={scrollRef} className="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto p-4">
        {state.messages.length === 0 ? (
          <p className="text-xs text-muted">{t('ai.empty')}</p>
        ) : (
          state.messages.map((message) => (
            <MessageBubble key={message.id} message={message} />
          ))
        )}
        {state.streaming && state.messages[state.messages.length - 1]?.text === '' &&
        state.messages[state.messages.length - 1]?.activities.length === 0 ? (
          <p className="text-xs text-muted">{t('ai.thinking')}</p>
        ) : null}
      </div>

      {state.error !== null ? (
        <div className="mx-4 mb-2 flex items-center justify-between gap-2 rounded-lg bg-danger-soft px-3 py-2">
          <p className="text-xs text-danger">{t(errorKeyOf(state.error))}</p>
          {state.error.code === 'history_too_long' ? (
            <button
              type="button"
              onClick={() => {
                store.clear()
              }}
              className="shrink-0 text-xs font-medium text-danger underline underline-offset-2"
            >
              {t('ai.clear')}
            </button>
          ) : null}
        </div>
      ) : null}

      <div className="flex shrink-0 items-end gap-2 border-t border-line p-3">
        <textarea
          value={input}
          rows={1}
          aria-label={t('ai.placeholder')}
          placeholder={t('ai.placeholder')}
          onChange={(event) => {
            setInput(event.target.value)
          }}
          onKeyDown={(event) => {
            if (event.key === 'Enter' && !event.shiftKey && !event.nativeEvent.isComposing) {
              event.preventDefault()
              send()
            }
          }}
          className="min-w-0 flex-1 resize-none rounded-lg border border-line bg-transparent px-3 py-2 text-sm text-ink placeholder:text-muted focus:outline focus:outline-2 focus:outline-accent"
        />
        {state.streaming ? (
          <button
            type="button"
            onClick={() => {
              store.stop()
            }}
            className="shrink-0 rounded-lg border border-line px-3 py-2 text-xs font-medium text-muted transition-colors hover:bg-muted-soft hover:text-ink focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent"
          >
            {t('ai.stop')}
          </button>
        ) : (
          <button
            type="button"
            disabled={input.trim() === ''}
            onClick={send}
            className="shrink-0 rounded-lg bg-accent px-3 py-2 text-xs font-medium text-white transition-colors hover:bg-accent-strong focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent disabled:opacity-50"
          >
            {t('ai.send')}
          </button>
        )}
      </div>
    </div>
  )
}

function MessageBubble({ message }: { message: ChatMessage }) {
  if (message.role === 'user') {
    return (
      <p className="max-w-[85%] self-end rounded-2xl rounded-br-sm bg-accent px-3 py-2 text-sm whitespace-pre-wrap text-white">
        {message.text}
      </p>
    )
  }
  return (
    <div className="flex max-w-full flex-col gap-1 self-start">
      <ActivityLine message={message} />
      {message.text !== '' ? (
        <p className="text-sm whitespace-pre-wrap text-ink">{message.text}</p>
      ) : null}
    </div>
  )
}

/** The process line: what the assistant is doing / did, in friendly words. */
function ActivityLine({ message }: { message: ChatMessage }) {
  const { t } = useTranslation()
  if (message.activities.length === 0) {
    return null
  }
  const running = message.activities.filter((activity) => !activity.done)
  if (running.length > 0) {
    return (
      <p className="flex items-center gap-1.5 text-xs text-muted">
        <span className="inline-block size-2 animate-pulse rounded-full bg-accent" />
        {uniqueTools(running.map((activity) => activity.tool))
          .map((tool) => t(toolKey(tool, 'ai.toolRunning')))
          .join(' · ')}
      </p>
    )
  }
  return (
    <p className="text-xs text-muted">
      {uniqueTools(message.activities.map((activity) => activity.tool))
        .map((tool) => t(toolKey(tool, 'ai.toolDone')))
        .join(' · ')}
    </p>
  )
}

const KNOWN_TOOLS = new Set([
  'get_notebook_tree',
  'search_pages',
  'get_page',
  'get_page_by_path',
  'create_page',
  'rename_page',
  'move_page',
  'apply_block_ops',
])

/** Unknown tools fall back to a generic label — never a raw technical name. */
function toolKey(tool: string, family: 'ai.toolRunning' | 'ai.toolDone'): string {
  return KNOWN_TOOLS.has(tool) ? `${family}.${tool}` : `${family}.fallback`
}

const KNOWN_ERROR_CODES = new Set([
  'ai_disabled',
  'ai_not_configured',
  'history_too_long',
  'ai_provider_failed',
  'ai_stream_failed',
  'notebook_not_found',
])

function errorKeyOf(error: { code: string; status?: number }): string {
  if (KNOWN_ERROR_CODES.has(error.code)) {
    return `ai.error.${error.code}`
  }
  if (error.status === 401) {
    return 'ai.error.unauthorized'
  }
  if (error.status === 429) {
    return 'ai.error.rate_limited'
  }
  return 'ai.error.unknown'
}

function uniqueTools(tools: string[]): string[] {
  return [...new Set(tools)]
}
