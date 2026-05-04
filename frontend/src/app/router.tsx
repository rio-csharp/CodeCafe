import { createBrowserRouter } from 'react-router-dom'
import { ChatWorkbench } from '../features/chat/ChatWorkbench'
import { NotesPage } from '../features/notes/NotesPage'
import { SettingsPage } from '../features/settings/SettingsPage'
import { AppShell } from './AppShell'

export const router = createBrowserRouter([
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
])
