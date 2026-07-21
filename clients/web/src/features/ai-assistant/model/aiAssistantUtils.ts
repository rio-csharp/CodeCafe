import type { Context, Message } from '@ag-ui/core'
import type { AiAssistantNotebookContext, AiAssistantVisibleMessage } from './types'

export function createAiContext({
  notebook,
  activePage,
}: AiAssistantNotebookContext): Context[] {
  const context: Context[] = [
    {
      description: 'Current CodeCafe notebook',
      value: JSON.stringify({
        title: notebook.title,
        slug: notebook.slug,
        visibility: notebook.visibility,
        canEdit: notebook.canEdit ?? false,
        itemCount: notebook.itemCount,
        folderCount: notebook.folderCount,
        pageCount: notebook.pageCount,
      }),
    },
  ]

  if (activePage) {
    context.push({
      description: 'Current CodeCafe notebook page',
      value: JSON.stringify({
        title: activePage.title,
        path: activePage.path,
        type: activePage.type,
        contentFormat: activePage.contentFormat,
        plainTextPreview: activePage.plainTextContent?.slice(0, 1200) ?? null,
      }),
    })
  }

  return context
}

export function getMessageText(message: Message): string {
  if (!('content' in message) || message.content === undefined) {
    return ''
  }

  const content: unknown = message.content

  if (typeof content === 'string') {
    return content
  }

  if (!Array.isArray(content)) {
    return ''
  }

  return content
    .filter(isTextPart)
    .map((part) => part.text)
    .join('')
}

export function getVisibleMessages(messages: Message[]): AiAssistantVisibleMessage[] {
  return messages.filter(
    (message): message is AiAssistantVisibleMessage =>
      (message.role === 'assistant' || message.role === 'user')
      && getMessageText(message).trim().length > 0,
  )
}

export function createPromptId(): string {
  if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
    return crypto.randomUUID()
  }

  return `msg-${Date.now()}-${Math.random().toString(16).slice(2)}`
}

function isTextPart(value: unknown): value is { type: 'text'; text: string } {
  return isRecord(value)
    && value.type === 'text'
    && typeof value.text === 'string'
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}
