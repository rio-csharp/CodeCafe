import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { RouterProvider, createMemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ChatWorkbench } from '../features/chat/ChatWorkbench'
import { NotesPage } from '../features/notes/NotesPage'
import { AiSettingsPage, NotesSettingsPage, SettingsPage } from '../features/settings/SettingsPage'
import { AppShell } from './AppShell'

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
          { path: 'settings/notes', element: <NotesSettingsPage /> },
        ],
      },
    ],
    { initialEntries: [initialPath] },
  )

  render(<RouterProvider router={router} />)
}

const aiProviders = [
  {
    apiKey: null,
    baseUrl: 'https://api.openai.com/v1',
    builtIn: true,
    enabled: false,
    id: 'openai',
    models: [],
    name: 'OpenAI',
  },
  {
    apiKey: null,
    baseUrl: 'https://api.deepseek.com',
    builtIn: true,
    enabled: false,
    id: 'deepseek',
    models: [],
    name: 'DeepSeek',
  },
]

function mockApiFetch(providers: unknown[] = aiProviders) {
  return vi.spyOn(globalThis, 'fetch').mockImplementation((input) => {
    const url = String(input)

    if (url.endsWith('/health')) {
      return Promise.resolve(new Response('Healthy', { status: 200 }))
    }

    if (url.endsWith('/api/ai/providers')) {
      return Promise.resolve(Response.json(providers))
    }

    if (url.endsWith('/api/notes/settings')) {
      return Promise.resolve(Response.json({ rootPath: '/srv/codecafe/notes' }))
    }

    return Promise.resolve(new Response(null, { status: 404 }))
  })
}

describe('AppShell', () => {
  afterEach(() => {
    vi.restoreAllMocks()
    vi.useRealTimers()
  })

  it('renders the platform shell and chat workbench', () => {
    mockApiFetch()
    renderRoute()

    expect(screen.getByRole('searchbox', { name: 'Search conversations' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Chat' })).toBeInTheDocument()
    expect(screen.getAllByRole('link', { name: 'Notes' })).toHaveLength(2)
    expect(screen.getByRole('link', { name: 'Settings' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'AI' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Deployment review/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /API design/i })).toBeInTheDocument()
    expect(screen.queryByText(/Microsoft Agent Framework/i)).not.toBeInTheDocument()
    expect(screen.queryByText('CodeCafe')).not.toBeInTheDocument()
    expect(screen.queryByText('Guest preview')).not.toBeInTheDocument()
    expect(screen.queryByText('Backend')).not.toBeInTheDocument()
  })

  it('opens and closes a chat session', async () => {
    const user = userEvent.setup()
    mockApiFetch()
    renderRoute()

    await user.click(screen.getByRole('button', { name: /Deployment review/i }))

    expect(screen.getByRole('heading', { name: 'Deployment review' })).toBeInTheDocument()
    expect(screen.getByText(/Review the deployment workflow/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Back' })).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Back' }))

    expect(screen.getByRole('searchbox', { name: 'Search conversations' })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Deployment review' })).not.toBeInTheDocument()
  })

  it('navigates to settings', async () => {
    const user = userEvent.setup()
    mockApiFetch()
    renderRoute()

    await user.click(screen.getByRole('link', { name: 'Settings' }))

    expect(screen.getByRole('heading', { name: 'Application settings' })).toBeInTheDocument()
    expect(screen.getByText('Select a settings section from the sidebar.')).toBeInTheDocument()

    await user.click(screen.getByRole('link', { name: 'AI' }))

    expect(screen.getByRole('navigation', { name: 'Providers' })).toBeInTheDocument()
    expect(await screen.findByRole('button', { name: 'OpenAI' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'DeepSeek' })).toBeInTheDocument()
    expect(screen.getByDisplayValue('https://api.openai.com/v1')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Add provider' })).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'DeepSeek' }))

    expect(screen.getByDisplayValue('https://api.deepseek.com')).toBeInTheDocument()
    expect(screen.queryByRole('tab', { name: 'Providers' })).not.toBeInTheDocument()
    expect(screen.queryByRole('tab', { name: 'Models' })).not.toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Chat' })).not.toBeInTheDocument()
    expect(screen.queryByText(/Microsoft Agent Framework/i)).not.toBeInTheDocument()
  })

  it('navigates to notes and notes settings', async () => {
    const user = userEvent.setup()
    mockApiFetch()
    renderRoute()

    await user.click(screen.getAllByRole('link', { name: 'Notes' })[0])

    expect(screen.getByRole('heading', { name: 'Notes' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'No notes yet' })).toBeInTheDocument()

    await user.click(screen.getAllByRole('link', { name: 'Notes' })[1])

    expect(screen.getByRole('heading', { name: 'Notes settings' })).toBeInTheDocument()
    expect(await screen.findByDisplayValue('/srv/codecafe/notes')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save notes settings' })).toBeInTheDocument()
  })
})
