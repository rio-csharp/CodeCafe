import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { RouterProvider, createMemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ChatWorkbench } from '../features/chat/ChatWorkbench'
import { SettingsPage } from '../features/settings/SettingsPage'
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
          { path: 'settings', element: <SettingsPage /> },
        ],
      },
    ],
    { initialEntries: [initialPath] },
  )

  render(<RouterProvider router={router} />)
}

describe('AppShell', () => {
  afterEach(() => {
    vi.restoreAllMocks()
    vi.useRealTimers()
  })

  it('renders the platform shell and chat workbench', () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response('Healthy', { status: 200 }))
    renderRoute()

    expect(screen.getByRole('searchbox', { name: 'Search conversations' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Chat' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Settings' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Deployment review/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /API design/i })).toBeInTheDocument()
    expect(screen.queryByText(/Microsoft Agent Framework/i)).not.toBeInTheDocument()
    expect(screen.queryByText('CodeCafe')).not.toBeInTheDocument()
    expect(screen.queryByText('Notes')).not.toBeInTheDocument()
    expect(screen.queryByText('Guest preview')).not.toBeInTheDocument()
    expect(screen.queryByText('Backend')).not.toBeInTheDocument()
  })

  it('opens and closes a chat session', async () => {
    const user = userEvent.setup()
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response('Healthy', { status: 200 }))
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
    renderRoute()

    await user.click(screen.getByRole('link', { name: 'Settings' }))

    expect(screen.getByRole('heading', { name: 'Application settings' })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Chat' })).not.toBeInTheDocument()
    expect(screen.queryByText(/Microsoft Agent Framework/i)).not.toBeInTheDocument()
  })
})
