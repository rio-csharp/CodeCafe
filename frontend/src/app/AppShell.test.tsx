import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { RouterProvider, createMemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
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

describe('AppShell', () => {
  afterEach(() => {
    vi.restoreAllMocks()
    vi.useRealTimers()
    window.localStorage.clear()
  })

  it('renders the platform shell and chat workbench', () => {
    vi.spyOn(runtimeEnvironment, 'isLocalEnvironment').mockReturnValue(true)
    mockApiFetch()
    renderRoute()

    expect(screen.getByRole('searchbox', { name: 'Search conversations' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Chat' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Notes' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Settings' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Deployment review/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /API design/i })).toBeInTheDocument()
    expect(screen.queryByText(/Microsoft Agent Framework/i)).not.toBeInTheDocument()
    expect(screen.queryByText('CodeCafe')).not.toBeInTheDocument()
    expect(screen.queryByText('Guest preview')).not.toBeInTheDocument()
    expect(screen.queryByText('Backend')).not.toBeInTheDocument()
  })

  it('opens and closes a chat session', async () => {
    const user = userEvent.setup()
    vi.spyOn(runtimeEnvironment, 'isLocalEnvironment').mockReturnValue(true)
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
    vi.spyOn(runtimeEnvironment, 'isLocalEnvironment').mockReturnValue(true)
    mockApiFetch()
    renderRoute()

    await user.click(screen.getByRole('link', { name: 'Settings' }))

    expect(within(screen.getByLabelText('Settings')).getByText('Settings')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Appearance' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Notes' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Dark' })).toHaveAttribute('aria-pressed', 'true')
    expect(screen.getByRole('button', { name: 'Light' })).toHaveAttribute('aria-pressed', 'false')
    expect(await screen.findByDisplayValue('/srv/codecafe/notes')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument()
  })

  it('navigates to notes and notes settings', async () => {
    const user = userEvent.setup()
    vi.spyOn(runtimeEnvironment, 'isLocalEnvironment').mockReturnValue(true)
    mockApiFetch()
    renderRoute()

    await user.click(screen.getByRole('link', { name: 'Notes' }))

    expect(screen.getByRole('searchbox', { name: 'Search notes' })).toBeInTheDocument()
    expect(await screen.findByText('Dotnet Platform')).toBeInTheDocument()
    expect(within(screen.getByRole('complementary', { name: 'Notes list' })).getByRole('button', { name: 'Dotnet Overview' })).toBeInTheDocument()
    expect(await screen.findByRole('article', { name: 'Markdown preview' })).toBeInTheDocument()
    expect(screen.getByRole('complementary', { name: 'Note outline' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: '.NET Platform Overview', level: 2 })).toBeInTheDocument()
    expect(within(screen.getByRole('article', { name: 'Markdown preview' })).queryByRole('heading', { name: '.NET Platform Overview' })).not.toBeInTheDocument()
    expect(within(screen.getByRole('complementary', { name: 'Note outline' })).queryByRole('link', { name: '.NET Platform Overview' })).not.toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Core Idea' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: '.NET Components' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Core Idea', level: 2 })).toHaveAttribute('id', 'core-idea')
    expect(screen.getByRole('heading', { name: '.NET Components', level: 2 })).toHaveAttribute('id', 'net-components')
    const pagination = screen.getByRole('navigation', { name: 'Note pagination' })

    expect(pagination).toBeInTheDocument()
    expect(within(pagination).getByText('1 / 2')).toBeInTheDocument()
    expect(within(pagination).getByRole('button', { name: 'Previous' })).toBeDisabled()

    await user.click(within(pagination).getByRole('button', { name: 'Next' }))

    expect(await screen.findByRole('heading', { name: 'Common Language Runtime', level: 2 })).toBeInTheDocument()
    const updatedPagination = screen.getByRole('navigation', { name: 'Note pagination' })

    expect(within(updatedPagination).getByText('2 / 2')).toBeInTheDocument()
    expect(within(updatedPagination).getByRole('button', { name: 'Next' })).toBeDisabled()
    expect(screen.queryByText('01-dotnet-platform/01-dotnet-overview.md')).not.toBeInTheDocument()
    expect(screen.getByLabelText('Read-only note')).toBeInTheDocument()
    expect(screen.queryByRole('textbox', { name: 'Markdown editor' })).not.toBeInTheDocument()

  })

  it('shows notes settings as read-only outside local environments', async () => {
    const user = userEvent.setup()
    vi.spyOn(runtimeEnvironment, 'isLocalEnvironment').mockReturnValue(false)
    mockApiFetch()
    renderRoute()

    await user.click(screen.getByRole('link', { name: 'Settings' }))

    expect(screen.getByRole('heading', { name: 'Appearance' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Notes' })).toBeInTheDocument()
    expect(await screen.findByDisplayValue('/srv/codecafe/notes')).toBeDisabled()
    expect(screen.getByText('Read-only')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Save' })).not.toBeInTheDocument()
  })
})
