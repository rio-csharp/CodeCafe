import type {
  ChatCompletionContentPart,
  ChatCompletionMessageParam,
} from 'openai/resources'
import { getPreferredFormat, type AiProvider, type AiProviderModel } from './aiSettingsStore'

export type ChatMessage = {
  role: 'assistant' | 'system' | 'user'
  text: string
}

type StreamChatResponseParams = {
  maxOutputTokens?: number | null
  messages: ChatMessage[]
  model: AiProviderModel
  onComplete?: () => void
  onDelta: (delta: string) => void
  provider: AiProvider
  signal?: AbortSignal
  systemPrompt?: string
  temperature?: number | null
  topP?: number | null
}

export async function streamChatResponse({
  maxOutputTokens,
  messages,
  model,
  onComplete,
  onDelta,
  provider,
  signal,
  systemPrompt,
  temperature,
  topP,
}: StreamChatResponseParams) {
  const { default: OpenAI } = await import('openai')
  const client = new OpenAI({
    apiKey: provider.apiKey,
    baseURL: normalizeBaseUrl(provider.baseUrl),
    dangerouslyAllowBrowser: true,
    maxRetries: 0,
  })
  const requestMessages = buildRequestMessages(messages, systemPrompt)
  const preferredFormat = getPreferredFormat(provider)

  if (preferredFormat === 'anthropic' || preferredFormat === 'gemini') {
    throw new Error('This format is not wired into the chat runtime yet. Use Chat Completions or Responses for now.')
  }

  if (preferredFormat === 'responses') {
    if (model.supportsStreaming) {
      const stream = await client.responses.create({
        input: toResponsesInput(requestMessages) as never,
        max_output_tokens: maxOutputTokens ?? undefined,
        model: model.modelId,
        stream: true,
        temperature: temperature ?? undefined,
        top_p: topP ?? undefined,
      }, { signal })

      for await (const event of stream) {
        if (event.type === 'response.output_text.delta') {
          onDelta(event.delta)
        }
      }
    } else {
      const response = await client.responses.create({
        input: toResponsesInput(requestMessages) as never,
        max_output_tokens: maxOutputTokens ?? undefined,
        model: model.modelId,
        temperature: temperature ?? undefined,
        top_p: topP ?? undefined,
      }, { signal })

      onDelta(response.output_text)
    }

    onComplete?.()
    return
  }

  if (model.supportsStreaming) {
    const stream = await client.chat.completions.create({
      max_completion_tokens: maxOutputTokens ?? undefined,
      messages: toChatCompletionMessages(requestMessages),
      model: model.modelId,
      stream: true,
      temperature: temperature ?? undefined,
      top_p: topP ?? undefined,
    }, { signal })

    for await (const chunk of stream) {
      const delta = chunk.choices[0]?.delta?.content

      if (typeof delta === 'string' && delta.length > 0) {
        onDelta(delta)
      }
    }
  } else {
    const completion = await client.chat.completions.create({
      max_completion_tokens: maxOutputTokens ?? undefined,
      messages: toChatCompletionMessages(requestMessages),
      model: model.modelId,
      temperature: temperature ?? undefined,
      top_p: topP ?? undefined,
    }, { signal })

    const content = completion.choices[0]?.message?.content ?? ''

    if (typeof content === 'string') {
      onDelta(content)
    }
  }

  onComplete?.()
}

export async function testProviderConnection({
  model,
  provider,
  signal,
}: {
  model?: AiProviderModel | null
  provider: AiProvider
  signal?: AbortSignal
}) {
  const { default: OpenAI } = await import('openai')
  const client = new OpenAI({
    apiKey: provider.apiKey,
    baseURL: normalizeBaseUrl(provider.baseUrl),
    dangerouslyAllowBrowser: true,
    maxRetries: 0,
  })

  try {
    await client.models.list({ signal })

    return {
      message: 'Connection succeeded.',
      ok: true,
    }
  } catch (modelsError) {
    if (!model) {
      return {
        message: getErrorMessage(modelsError),
        ok: false,
      }
    }

    try {
      await streamChatResponse({
        maxOutputTokens: 8,
        messages: [{
          role: 'user',
          text: 'Reply with OK.',
        }],
        model,
        onDelta: () => {},
        provider,
        signal,
        temperature: 0,
        topP: 1,
      })

      return {
        message: 'Connection succeeded.',
        ok: true,
      }
    } catch (chatError) {
      return {
        message: getErrorMessage(chatError),
        ok: false,
      }
    }
  }
}

function buildRequestMessages(
  messages: ChatMessage[],
  systemPrompt: string | undefined,
) {
  if (!systemPrompt?.trim()) {
    return messages
  }

  return [
    {
      role: 'system' as const,
      text: systemPrompt.trim(),
    },
    ...messages,
  ]
}

function toChatCompletionMessages(messages: ChatMessage[]): ChatCompletionMessageParam[] {
  return messages.map((message) => {
    if (message.role === 'assistant') {
      return {
        content: message.text,
        role: 'assistant',
      }
    }

    if (message.role === 'system') {
      return {
        content: message.text,
        role: 'system',
      }
    }

    const content: ChatCompletionContentPart[] = [{
      text: message.text,
      type: 'text',
    }]

    return {
      content,
      role: 'user',
    }
  })
}

function toResponsesInput(messages: ChatMessage[]) {
  return messages.map((message) => {
    if (message.role === 'assistant') {
      return {
        content: [{
          text: message.text,
          type: 'input_text',
        }],
        role: 'assistant',
      }
    }

    if (message.role === 'system') {
      return {
        content: [{
          text: message.text,
          type: 'input_text',
        }],
        role: 'system',
      }
    }

    return {
      content: [{
        text: message.text,
        type: 'input_text' as const,
      }],
      role: 'user',
    }
  })
}

function normalizeBaseUrl(baseUrl: string) {
  return baseUrl.endsWith('/') ? baseUrl : `${baseUrl}/`
}

function getErrorMessage(error: unknown) {
  return error instanceof Error ? error.message : 'Request failed.'
}
