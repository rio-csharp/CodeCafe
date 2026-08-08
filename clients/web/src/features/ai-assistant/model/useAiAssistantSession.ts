import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { HttpAgent, type AgentSubscriber } from '@ag-ui/client'
import type { Message, ToolCallArgsEvent, ToolCallResultEvent, ToolCallStartEvent, UserMessage } from '@ag-ui/core'
import { API_BASE_URL, ApiError, clearCsrfToken, fetchCsrfToken } from '@/shared/api/client'
import { createAiContext, createPromptId, getVisibleMessages } from './aiAssistantUtils'
import { clearThread, loadThread, saveThread } from './aiThreadStorage'
import type {
  AiAssistantNotebookContext,
  AiAssistantRunState,
  AiAssistantVisibleMessage,
  AiToolActivity,
} from './types'

export type AiAssistantErrorCode =
  | 'authentication_required'
  | 'connection_failed'
  | 'csrf_failed'
  | 'not_configured'
  | 'rate_limited'
  | 'unknown'

interface UseAiAssistantSessionOptions extends AiAssistantNotebookContext {
  enabled: boolean
  endpointPath: string | null
}

interface AiAssistantError {
  code: AiAssistantErrorCode
  message: string
}

interface ProblemDetails {
  code?: string
  detail?: string
  message?: string
  title?: string
}

const MAX_TOOL_TEXT_LENGTH = 260

export function useAiAssistantSession({
  enabled,
  endpointPath,
  notebook,
  activePage,
}: UseAiAssistantSessionOptions) {
  const { t } = useTranslation()
  const [messages, setMessages] = useState<Message[]>([])
  const [toolActivities, setToolActivities] = useState<AiToolActivity[]>([])
  const [runState, setRunState] = useState<AiAssistantRunState>('idle')
  const [error, setError] = useState<AiAssistantError | null>(null)
  const agentRef = useRef<HttpAgent | null>(null)

  const threadKey = useMemo(
    () => `codecafe:${notebook.slug}:${activePage?.path ?? 'notebook'}`,
    [activePage?.path, notebook.slug],
  )

  useEffect(() => {
    agentRef.current?.abortRun()
    agentRef.current = null

    if (!enabled || !endpointPath) {
      return
    }

    const agent = new HttpAgent({
      url: `${API_BASE_URL}${endpointPath}`,
      threadId: threadKey,
      fetch: aiFetch,
    })

    agentRef.current = agent

    const persisted = loadThread(threadKey)
    if (persisted) {
      agent.setMessages(persisted.messages)
      // Restoring persisted thread from browser storage when the notebook/page
      // context changes is an intentional synchronization with an external store.
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setMessages(persisted.messages)
    } else {
      // No persisted thread for this key — drop the previous page's session
      // so its messages don't bleed into the page the user just opened.
      setMessages([])
      setToolActivities([])
      setError(null)
    }

    return () => {
      agent.abortRun()
      if (agentRef.current === agent) {
        agentRef.current = null
      }
    }
  }, [enabled, endpointPath, threadKey])

  const syncMessages = useCallback((nextMessages: readonly Message[]) => {
    setMessages([...nextMessages])
  }, [])

  const subscriber = useMemo<AgentSubscriber>(
    () => ({
      onMessagesChanged: ({ messages }) => {
        syncMessages(messages)
        saveThread(threadKey, messages)
      },
      onToolCallStartEvent: ({ event }) => {
        setToolActivities((current) => upsertToolStart(current, event))
      },
      onToolCallArgsEvent: ({ event }) => {
        setToolActivities((current) => appendToolArgs(current, event))
      },
      onToolCallResultEvent: ({ event }) => {
        setToolActivities((current) => finishTool(current, event))
      },
      onRunErrorEvent: ({ event }) => {
        setError({ code: 'connection_failed', message: event.message })
      },
    }),
    [syncMessages, threadKey],
  )

  const sendMessage = useCallback(
    async (content: string) => {
      const trimmed = content.trim()
      if (!trimmed || runState === 'running') {
        return
      }

      if (!enabled || !endpointPath || !agentRef.current) {
        setError({
          code: 'not_configured',
          message: t('ai.errors.not_configured'),
        })
        setRunState('error')
        return
      }

      const agent = agentRef.current
      const userMessage: UserMessage = {
        id: createPromptId(),
        role: 'user',
        content: trimmed,
      }

      setError(null)
      setRunState('running')
      setToolActivities([])
      agent.addMessage(userMessage)
      syncMessages(agent.messages)

      try {
        await agent.runAgent(
          {
            context: createAiContext({ notebook, activePage }),
          },
          subscriber,
        )
        syncMessages(agent.messages)
        setRunState('idle')
      } catch (err) {
        if (isAbortError(err)) {
          setRunState('idle')
          return
        }

        setError(toAiAssistantError(err, t))
        setRunState('error')
      }
    },
    [activePage, enabled, endpointPath, notebook, runState, subscriber, syncMessages, t],
  )

  const stop = useCallback(() => {
    agentRef.current?.abortRun()
    setRunState('idle')
  }, [])

  const clear = useCallback(() => {
    agentRef.current?.abortRun()
    agentRef.current?.setMessages([])
    clearThread(threadKey)
    setMessages([])
    setToolActivities([])
    setError(null)
    setRunState('idle')
  }, [threadKey])

  const visibleMessages = useMemo<AiAssistantVisibleMessage[]>(
    () => getVisibleMessages(messages),
    [messages],
  )

  return {
    clear,
    error,
    isRunning: runState === 'running',
    messages: visibleMessages,
    runState,
    sendMessage,
    stop,
    toolActivities,
  }
}

async function aiFetch(url: string, requestInit: RequestInit): Promise<Response> {
  let response = await fetchWithCsrf(url, requestInit)

  if (response.status === 400 && await isInvalidCsrfResponse(response.clone())) {
    clearCsrfToken()
    response = await fetchWithCsrf(url, requestInit)
  }

  if (!response.ok) {
    throw await createApiError(response)
  }

  return response
}

async function fetchWithCsrf(url: string, requestInit: RequestInit): Promise<Response> {
  const headers = new Headers(requestInit.headers)
  headers.set('X-CSRF-TOKEN', await fetchCsrfToken())

  return fetch(url, {
    ...requestInit,
    credentials: 'include',
    headers,
  })
}

async function createApiError(response: Response): Promise<ApiError> {
  const details = await readProblemDetails(response)
  const code = details.code ?? details.title
  const message = details.detail
    ?? details.message
    ?? `Request failed with status ${response.status}`

  return new ApiError(response.status, message, code)
}

async function readProblemDetails(response: Response): Promise<ProblemDetails> {
  const text = await response.text().catch(() => '')
  if (!text.trim()) {
    return {}
  }

  try {
    const parsed: unknown = JSON.parse(text)
    if (!isRecord(parsed)) {
      return {}
    }

    return {
      code: getString(parsed, 'code'),
      detail: getString(parsed, 'detail'),
      message: getString(parsed, 'message'),
      title: getString(parsed, 'title'),
    }
  } catch {
    return { message: text }
  }
}

async function isInvalidCsrfResponse(response: Response): Promise<boolean> {
  const details = await readProblemDetails(response)
  return details.code === 'invalid_csrf_token' || details.title === 'invalid_csrf_token'
}

function toAiAssistantError(err: unknown, t: (key: string) => string): AiAssistantError {
  if (err instanceof ApiError) {
    if (err.status === 401) {
      return { code: 'authentication_required', message: err.message }
    }

    if (err.status === 404) {
      return { code: 'not_configured', message: err.message }
    }

    if (err.status === 429) {
      return { code: 'rate_limited', message: err.message }
    }

    if (err.code === 'invalid_csrf_token') {
      return { code: 'csrf_failed', message: err.message }
    }

    return { code: 'connection_failed', message: err.message }
  }

  if (err instanceof Error) {
    return { code: 'connection_failed', message: err.message }
  }

  return { code: 'unknown', message: t('ai.errors.unknown') }
}

function isAbortError(err: unknown): boolean {
  return err instanceof DOMException && err.name === 'AbortError'
}

function upsertToolStart(
  activities: AiToolActivity[],
  event: ToolCallStartEvent,
): AiToolActivity[] {
  const next = activities.filter((activity) => activity.id !== event.toolCallId)
  return [
    ...next,
    {
      id: event.toolCallId,
      name: event.toolCallName,
      status: 'running',
    },
  ]
}

function appendToolArgs(
  activities: AiToolActivity[],
  event: ToolCallArgsEvent,
): AiToolActivity[] {
  return activities.map((activity) =>
    activity.id === event.toolCallId
      ? {
          ...activity,
          args: truncateToolText(`${activity.args ?? ''}${event.delta}`),
        }
      : activity,
  )
}

function finishTool(
  activities: AiToolActivity[],
  event: ToolCallResultEvent,
): AiToolActivity[] {
  return activities.map((activity) =>
    activity.id === event.toolCallId
      ? {
          ...activity,
          status: 'done',
          result: truncateToolText(event.content),
        }
      : activity,
  )
}

function truncateToolText(value: string): string {
  if (value.length <= MAX_TOOL_TEXT_LENGTH) {
    return value
  }

  return `${value.slice(0, MAX_TOOL_TEXT_LENGTH)}...`
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}

function getString(record: Record<string, unknown>, key: string): string | undefined {
  const value = record[key]
  return typeof value === 'string' ? value : undefined
}
