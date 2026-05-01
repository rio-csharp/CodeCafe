import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { RouterProvider, createMemoryRouter } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
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
  it('renders the platform shell and dashboard modules', () => {
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
    renderRoute()

    await user.click(screen.getByRole('link', { name: 'AI' }))

    expect(screen.getByRole('heading', { name: 'AI' })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Notes' })).not.toBeInTheDocument()
  })
})
