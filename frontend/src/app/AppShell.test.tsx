import { act, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { RouterProvider, createMemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AiPanel } from '../features/ai/AiPanel'
import { ActivityPanel } from '../features/audit/ActivityPanel'
import { NotesPanel } from '../features/notes/NotesPanel'
import { WorkspacePanel } from '../features/workspaces/WorkspacePanel'
import { AppShell, DashboardPage } from './AppShell'

function renderRoute(initialPath = '/') {
  const router = createMemoryRouter(
    [
      {
        path: '/',
        element: <AppShell />,
        children: [
          { index: true, element: <DashboardPage /> },
          { path: 'notes', element: <NotesPanel /> },
          { path: 'workspace', element: <WorkspacePanel /> },
          { path: 'ai', element: <AiPanel /> },
          { path: 'audit', element: <ActivityPanel /> },
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

  it('renders the platform shell and dashboard modules', () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response('Healthy', { status: 200 }))
    renderRoute()

    expect(screen.getByText('CodeCafe')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: /developer knowledge workspace/i })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Notes' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Workspace' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'AI' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Audit' })).toBeInTheDocument()
  })

  it('navigates to feature routes', async () => {
    const user = userEvent.setup()
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response('Healthy', { status: 200 }))
    renderRoute()

    await user.click(screen.getByRole('link', { name: 'AI' }))

    expect(screen.getByRole('heading', { name: 'AI' })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Notes' })).not.toBeInTheDocument()
  })

  it('shows backend health and refreshes it every five seconds', async () => {
    const setIntervalSpy = vi.spyOn(window, 'setInterval')
    const fetchMock = vi
      .spyOn(globalThis, 'fetch')
      .mockResolvedValueOnce(new Response('Healthy', { status: 200 }))
      .mockResolvedValueOnce(new Response(null, { status: 503 }))

    renderRoute()

    expect(screen.getByText('Checking')).toBeInTheDocument()
    expect(await screen.findByText('Online')).toBeInTheDocument()
    expect(setIntervalSpy).toHaveBeenCalledWith(expect.any(Function), 5_000)

    const refreshHealth = setIntervalSpy.mock.calls[0][0]

    if (typeof refreshHealth !== 'function') {
      throw new Error('Expected the health refresh callback to be a function.')
    }

    await act(async () => {
      await refreshHealth()
    })

    expect(await screen.findByText('Offline')).toBeInTheDocument()
    expect(fetchMock).toHaveBeenCalledTimes(2)
  })
})
