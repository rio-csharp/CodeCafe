vi.mock('../features/ai/aiClient', async () => {
  const actual = await vi.importActual<typeof import('../features/ai/aiClient')>('../features/ai/aiClient')

  return {
    ...actual,
    streamChatResponse: vi.fn(actual.streamChatResponse),
  }
})

import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { RouterProvider, createMemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import * as aiClient from '../features/ai/aiClient'
import { AiSettingsPage } from '../features/ai/AiSettingsPage'
import { ChatWorkbench } from '../features/chat/ChatWorkbench'
import { NotesPage } from '../features/notes/NotesPage'
import { SettingsPage } from '../features/settings/SettingsPage'
import { AppShell } from './AppShell'
import { ThemeProvider } from './theme'
import * as runtimeEnvironment from './runtimeEnvironment'

function renderRoute(initialPath = '/') {
  const router = createMemoryRouter(
    [
      {
        path: '/',
        element: <AppShell />,
        children: [
          { index: true, element: <ChatWorkbench /> },
          { path: 'chat', element: <ChatWorkbench /> },
          { path: 'notes', element: <NotesPage /> },
          { path: 'settings', element: <SettingsPage /> },
          { path: 'settings/ai', element: <AiSettingsPage /> },
        ],
      },
    ],
    { initialEntries: [initialPath] },
  )

  render(
    <ThemeProvider>
      <RouterProvider router={router} />
    </ThemeProvider>,
  )
}

function mockApiFetch() {
  return vi.spyOn(globalThis, 'fetch').mockImplementation((input) => {
    const url = String(input)

    if (url.endsWith('/health')) {
      return Promise.resolve(new Response('Healthy', { status: 200 }))
    }

    if (url.endsWith('/api/notes/settings')) {
      return Promise.resolve(Response.json({ rootPath: '/srv/codecafe/notes' }))
    }

    if (url.endsWith('/api/notes')) {
      return Promise.resolve(Response.json([
        {
          path: '01-dotnet-platform/01-dotnet-overview.md',
          title: '01-dotnet-overview',
          updatedAt: '2026-05-03T00:00:00Z',
          sizeBytes: 128,
        },
        {
          path: '01-dotnet-platform/02-clr.md',
          title: '02-clr',
          updatedAt: '2026-05-03T00:00:00Z',
          sizeBytes: 96,
        },
      ]))
    }

    if (url.includes('/api/notes/content?path=')) {
      if (url.includes('02-clr.md')) {
        return Promise.resolve(Response.json({
          path: '01-dotnet-platform/02-clr.md',
          title: '02-clr',
          updatedAt: '2026-05-03T00:00:00Z',
          sizeBytes: 96,
          content: '# Common Language Runtime\n\n## Core Idea\n\nCLR note.',
        }))
      }

      return Promise.resolve(Response.json({
        path: '01-dotnet-platform/01-dotnet-overview.md',
        title: '01-dotnet-overview',
        updatedAt: '2026-05-03T00:00:00Z',
        sizeBytes: 128,
        content: '# .NET Platform Overview\n\n## Core Idea\n\nRead-only note.\n\n## .NET Components\n\nRuntime and SDK.',
      }))
    }

    return Promise.resolve(new Response(null, { status: 404 }))
  })
}

function seedAiSettings() {
  window.localStorage.setItem('codecafe-ai-settings', JSON.stringify({
    defaultModelId: 'model-1',
    defaultProviderId: 'provider-1',
    providers: [
      {
        apiKey: 'sk-test',
        baseUrl: 'https://example.com/v1',
        enabled: true,
        formats: ['chat-completions', 'responses'],
        id: 'provider-1',
        models: [
          {
            defaultMaxOutputTokens: 4096,
            defaultTemperature: 0.7,
            defaultTopP: 1,
            enabled: true,
            id: 'model-1',
            modelId: 'gpt-4.1-mini',
            name: 'GPT 4.1 Mini',
            supportsStreaming: true,
          },
        ],
        name: 'Example provider',
        preferredFormat: 'chat-completions',
      },
    ],
  }))
}

function seedChatSessions() {
  window.localStorage.setItem('codecafe-chat-sessions', JSON.stringify([
    {
      id: 'session-1',
      messages: [
        {
          id: 'message-1',
          role: 'user',
          text: 'Review this deployment plan.',
        },
        {
          id: 'message-2',
          role: 'assistant',
          text: 'Start with health checks and rollback ownership.',
        },
      ],
      modelId: 'model-1',
      providerId: 'provider-1',
      title: 'Deployment review',
      updatedAt: '2026-05-05T00:00:00.000Z',
    },
  ]))
}

describe('AppShell', () => {
  afterEach(() => {
    vi.restoreAllMocks()
    vi.useRealTimers()
    window.localStorage.clear()
  })

  it('renders the platform shell and AI chat workspace', async () => {
    vi.spyOn(runtimeEnvironment, 'isLocalEnvironment').mockReturnValue(true)
    mockApiFetch()
    seedAiSettings()
    seedChatSessions()
    renderRoute()

    expect(screen.getByRole('searchbox', { name: 'Search conversations' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Chat' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Notes' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Settings' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'New' })).toBeInTheDocument()
    expect(screen.getByRole('option', { name: 'GPT 4.1 Mini' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Chat settings' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Delete Deployment review' })).toBeInTheDocument()
  })

  it('opens an existing chat session', async () => {
    const user = userEvent.setup()
    vi.spyOn(runtimeEnvironment, 'isLocalEnvironment').mockReturnValue(true)
    mockApiFetch()
    seedAiSettings()
    seedChatSessions()
    renderRoute()

    await user.click(screen.getByRole('button', { name: 'Open Deployment review' }))

    expect(screen.getByRole('heading', { name: 'Deployment review' })).toBeInTheDocument()
    expect(
      within(screen.getByLabelText('Deployment review messages')).getByText(/rollback ownership/i),
    ).toBeInTheDocument()
  })

  it('opens chat settings and deletes a session', async () => {
    const user = userEvent.setup()
    vi.spyOn(runtimeEnvironment, 'isLocalEnvironment').mockReturnValue(true)
    mockApiFetch()
    seedAiSettings()
    seedChatSessions()
    renderRoute()

    await user.click(screen.getByRole('button', { name: 'Chat settings' }))

    expect(screen.getByRole('textbox', { name: 'System prompt' })).toBeInTheDocument()
    expect(screen.getByRole('spinbutton', { name: 'Temperature' })).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Delete Deployment review' }))

    expect(screen.queryByRole('button', { name: 'Open Deployment review' })).not.toBeInTheDocument()
  })

  it('navigates to settings and AI settings', async () => {
    const user = userEvent.setup()
    vi.spyOn(runtimeEnvironment, 'isLocalEnvironment').mockReturnValue(true)
    mockApiFetch()
    seedAiSettings()
    renderRoute()

    await user.click(screen.getByRole('link', { name: 'Settings' }))

    expect(within(screen.getByLabelText('Settings')).getByText('Settings')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Appearance' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Notes' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'AI' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'E-ink' })).toBeInTheDocument()
    expect(await screen.findByDisplayValue('/srv/codecafe/notes')).toBeInTheDocument()

    await user.click(screen.getByRole('link', { name: /Providers, formats, models, and capabilities/i }))

    expect(screen.getByRole('heading', { name: 'AI' })).toBeInTheDocument()
    expect(screen.getByRole('navigation', { name: 'Providers' })).toBeInTheDocument()
    expect(screen.getByDisplayValue('Example provider')).toBeInTheDocument()
    expect(screen.getByDisplayValue('https://example.com/v1')).toBeInTheDocument()
    expect(screen.getByDisplayValue('GPT 4.1 Mini')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Test' })).toBeInTheDocument()
    expect(screen.getByText('Top-p')).toBeInTheDocument()
    expect(screen.getByText('Max')).toBeInTheDocument()
  })

  it('navigates to notes', async () => {
    const user = userEvent.setup()
    vi.spyOn(runtimeEnvironment, 'isLocalEnvironment').mockReturnValue(true)
    mockApiFetch()
    seedAiSettings()
    renderRoute()

    await user.click(screen.getByRole('link', { name: 'Notes' }))

    expect(screen.getByRole('searchbox', { name: 'Search notes' })).toBeInTheDocument()
    expect(await screen.findByText('Dotnet Platform')).toBeInTheDocument()
    expect(within(screen.getByRole('complementary', { name: 'Notes list' })).getByRole('button', { name: 'Dotnet Overview' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'PDF' })).toHaveAttribute(
      'href',
      'https://github.com/rio-csharp/Notes/releases/download/latest/notes.pdf',
    )
    expect(screen.getByRole('link', { name: 'EPUB' })).toHaveAttribute(
      'href',
      'https://github.com/rio-csharp/Notes/releases/download/latest/notes.epub',
    )
    expect(screen.getByRole('button', { name: 'Open notes AI assistant' })).toBeInTheDocument()
  })

  it('opens the notes AI assistant from the notes reader', async () => {
    const user = userEvent.setup()
    vi.spyOn(runtimeEnvironment, 'isLocalEnvironment').mockReturnValue(true)
    mockApiFetch()
    seedAiSettings()
    renderRoute('/notes')

    await user.click(screen.getByRole('button', { name: 'Open notes AI assistant' }))

    expect(screen.getByRole('region', { name: 'Notes AI assistant' })).toBeInTheDocument()
    expect(screen.getByText('Ask about this note.')).toBeInTheDocument()
    expect(screen.getByRole('textbox', { name: 'Ask AI about this note' })).toBeInTheDocument()
  })

  it('shows notes settings as read-only outside local environments', async () => {
    const user = userEvent.setup()
    vi.spyOn(runtimeEnvironment, 'isLocalEnvironment').mockReturnValue(false)
    mockApiFetch()
    seedAiSettings()
    renderRoute()

    await user.click(screen.getByRole('link', { name: 'Settings' }))

    expect(screen.getByRole('heading', { name: 'Appearance' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Notes' })).toBeInTheDocument()
    expect(await screen.findByDisplayValue('/srv/codecafe/notes')).toBeDisabled()
    expect(screen.getByText('Read-only')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Save' })).not.toBeInTheDocument()
  })

  it('allows multiple chat sessions to stream at the same time', async () => {
    const user = userEvent.setup()
    const pendingStreams: Array<{
      onDelta: (delta: string) => void
      resolve: () => void
    }> = []

    vi.spyOn(runtimeEnvironment, 'isLocalEnvironment').mockReturnValue(true)
    mockApiFetch()
    seedAiSettings()
    vi.mocked(aiClient.streamChatResponse).mockImplementation(({ onDelta, signal }) =>
      new Promise<{ responseId: null }>((resolve, reject) => {
        signal?.addEventListener(
          'abort',
          () => reject(new DOMException('Aborted', 'AbortError')),
          { once: true },
        )

        pendingStreams.push({
          onDelta,
          resolve: () => resolve({ responseId: null }),
        })
      }),
    )

    renderRoute()

    await user.type(screen.getByRole('textbox', { name: 'Message' }), 'First session')
    await user.click(screen.getByRole('button', { name: 'Send message' }))

    await user.click(screen.getByRole('button', { name: 'New' }))
    await user.type(screen.getByRole('textbox', { name: 'Message' }), 'Second session')
    await user.click(screen.getByRole('button', { name: 'Send message' }))

    expect(aiClient.streamChatResponse).toHaveBeenCalledTimes(2)
    expect(screen.getByRole('button', { name: 'Open First session' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Open Second session' })).toBeInTheDocument()

    pendingStreams[0]?.onDelta('Reply one')
    pendingStreams[0]?.resolve()
    pendingStreams[1]?.onDelta('Reply two')
    pendingStreams[1]?.resolve()

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Send message' })).toBeInTheDocument()
    })
  })

  it('aborts an active response when deleting a session', async () => {
    const user = userEvent.setup()
    let aborted = false

    vi.spyOn(runtimeEnvironment, 'isLocalEnvironment').mockReturnValue(true)
    mockApiFetch()
    seedAiSettings()
    vi.mocked(aiClient.streamChatResponse).mockImplementation(({ signal }) =>
      new Promise<{ responseId: null }>((_resolve, reject) => {
        signal?.addEventListener(
          'abort',
          () => {
            aborted = true
            reject(new DOMException('Aborted', 'AbortError'))
          },
          { once: true },
        )
      }),
    )

    renderRoute()

    await user.type(screen.getByRole('textbox', { name: 'Message' }), 'Abort me')
    await user.click(screen.getByRole('button', { name: 'Send message' }))

    await user.click(await screen.findByRole('button', { name: 'Delete Abort me' }))

    await waitFor(() => {
      expect(aborted).toBe(true)
      expect(screen.queryByRole('button', { name: 'Open Abort me' })).not.toBeInTheDocument()
    })

    expect(screen.queryByText('Generation stopped.')).not.toBeInTheDocument()
  })
})
