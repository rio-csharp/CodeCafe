import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { streamChat } from '../api/streamChat'
import type { StreamChatParams } from '../api/streamChat'
import { resetChatStores } from '../model/store'
import { AiChatPanel } from './AiChatPanel'

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}))

vi.mock('../api/streamChat', () => {
  class AiChatHttpError extends Error {
    readonly status: number
    readonly code: string

    constructor(status: number, code: string, message: string) {
      super(message)
      this.status = status
      this.code = code
    }
  }
  return { streamChat: vi.fn(), AiChatHttpError }
})

function replyWith(events: Record<string, unknown>[]) {
  vi.mocked(streamChat).mockImplementation(async ({ onEvent }: StreamChatParams) => {
    for (const event of events) {
      onEvent(event as never)
    }
  })
}

describe('AiChatPanel', () => {
  beforeEach(() => {
    resetChatStores()
    vi.mocked(streamChat).mockReset()
  })

  it('shows the empty state before any message', () => {
    render(<AiChatPanel slug="s" onAiChanged={vi.fn()} />)

    expect(screen.getByText('ai.empty')).toBeInTheDocument()
  })

  it('sends on Enter and streams the reply into a bubble', async () => {
    const user = userEvent.setup()
    replyWith([
      { kind: 'text', text: 'Here' },
      { kind: 'text', text: ' you go.' },
      { kind: 'done' },
    ])
    render(<AiChatPanel slug="s" onAiChanged={vi.fn()} />)

    await user.type(screen.getByRole('textbox', { name: 'ai.placeholder' }), 'summarize')
    await user.keyboard('{Enter}')

    expect(await screen.findByText('summarize')).toBeInTheDocument()
    expect(await screen.findByText('Here you go.')).toBeInTheDocument()
  })

  it('shows friendly activity labels for tool calls, never technical names', async () => {
    const user = userEvent.setup()
    replyWith([
      { kind: 'tool-call', tool: 'get_page', callId: 'c1' },
      { kind: 'tool-result', callId: 'c1' },
      { kind: 'text', text: 'Done reading.' },
      { kind: 'done' },
    ])
    render(<AiChatPanel slug="s" onAiChanged={vi.fn()} />)

    await user.type(screen.getByRole('textbox', { name: 'ai.placeholder' }), 'read it')
    await user.keyboard('{Enter}')

    expect(await screen.findByText('ai.toolDone.get_page')).toBeInTheDocument()
    expect(screen.queryByText('get_page')).not.toBeInTheDocument()
  })

  it('shows the error banner with a clear action when the history is too long', async () => {
    const user = userEvent.setup()
    replyWith([
      { kind: 'error', code: 'history_too_long', message: 'too long' },
      { kind: 'error', code: 'history_too_long', message: 'still too long' },
    ])
    render(<AiChatPanel slug="s" onAiChanged={vi.fn()} />)

    await user.type(screen.getByRole('textbox', { name: 'ai.placeholder' }), 'hi')
    await user.keyboard('{Enter}')

    expect(await screen.findByText('ai.error.history_too_long')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'ai.clear' })).toBeInTheDocument()
  })

  it('clears the conversation with a two-click button', async () => {
    const user = userEvent.setup()
    replyWith([{ kind: 'text', text: 'answer' }, { kind: 'done' }])
    render(<AiChatPanel slug="s" onAiChanged={vi.fn()} />)

    await user.type(screen.getByRole('textbox', { name: 'ai.placeholder' }), 'hi')
    await user.keyboard('{Enter}')
    expect(await screen.findByText('answer')).toBeInTheDocument()

    // The first click only arms the button; the conversation is still there.
    await user.click(screen.getByRole('button', { name: 'ai.clear' }))
    expect(screen.getByText('answer')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'ai.clearConfirm' }))
    expect(screen.queryByText('answer')).not.toBeInTheDocument()
    expect(screen.getByText('ai.empty')).toBeInTheDocument()
  })
})
