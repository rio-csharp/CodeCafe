import { createBrowserRouter } from 'react-router-dom'
import { AiPanel } from '../features/ai/AiPanel'
import { ActivityPanel } from '../features/audit/ActivityPanel'
import { NotesPanel } from '../features/notes/NotesPanel'
import { WorkspacePanel } from '../features/workspaces/WorkspacePanel'
import { AppShell, DashboardPage } from './AppShell'

export const router = createBrowserRouter([
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
])
