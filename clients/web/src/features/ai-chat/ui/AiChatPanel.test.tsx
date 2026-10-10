import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { useSessionStore } from '@/entities/session'
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
    useSessionStore.setState({ status: 'anonymous', user: null })
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

  it('shows the provider message for a relay-specific failure instead of a generic one', async () => {
    const user = userEvent.setup()
    replyWith([
      {
        kind: 'error',
        code: 'server_is_overloaded',
        message: 'Our servers are currently overloaded. Please try again later.',
      },
    ])
    render(<AiChatPanel slug="s" onAiChanged={vi.fn()} />)

    await user.type(screen.getByRole('textbox', { name: 'ai.placeholder' }), 'hi')
    await user.keyboard('{Enter}')

    expect(
      await screen.findByText('Our servers are currently overloaded. Please try again later.'),
    ).toBeInTheDocument()
    expect(screen.queryByText('ai.error.unknown')).not.toBeInTheDocument()
  })

  it('falls back to the generic message when an unknown failure carries no message', async () => {
    const user = userEvent.setup()
    replyWith([{ kind: 'error', code: 'mystery_failure', message: '   ' }])
    render(<AiChatPanel slug="s" onAiChanged={vi.fn()} />)

    await user.type(screen.getByRole('textbox', { name: 'ai.placeholder' }), 'hi')
    await user.keyboard('{Enter}')

    expect(await screen.findByText('ai.error.unknown')).toBeInTheDocument()
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

  it('does not show one account the previous account conversation', async () => {
    const user = userEvent.setup()
    replyWith([{ kind: 'text', text: 'private answer' }, { kind: 'done' }])
    useSessionStore.setState({
      status: 'authenticated',
      user: { id: 'user-1', email: 'one@example.com', displayName: 'One' },
    })
    const view = render(<AiChatPanel slug="s" onAiChanged={vi.fn()} />)
    await user.type(screen.getByRole('textbox', { name: 'ai.placeholder' }), 'private question')
    await user.keyboard('{Enter}')
    expect(await screen.findByText('private answer')).toBeInTheDocument()

    useSessionStore.setState({
      status: 'authenticated',
      user: { id: 'user-2', email: 'two@example.com', displayName: 'Two' },
    })
    view.rerender(<AiChatPanel slug="s" onAiChanged={vi.fn()} />)

    expect(screen.queryByText('private answer')).not.toBeInTheDocument()
    expect(screen.getByText('ai.empty')).toBeInTheDocument()
  })
})
