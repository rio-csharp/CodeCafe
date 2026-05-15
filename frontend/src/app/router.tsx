import { createBrowserRouter } from 'react-router-dom'
import { LoginPage } from '../features/auth/LoginPage'
import { AiSettingsPage } from '../features/ai/AiSettingsPage'
import { ChatWorkbench } from '../features/chat/ChatWorkbench'
import { NotesPage } from '../features/notes/NotesPage'
import { SettingsPage } from '../features/settings/SettingsPage'
import { LandingPage } from '../features/landing/LandingPage'
import { AppShell } from './AppShell'

export const router = createBrowserRouter([
  {
    path: '/login',
    element: <LoginPage />,
  },
  {
    path: '/',
    element: <LandingPage />,
  },
  {
    path: '/app',
    element: <AppShell />,
    children: [
      { index: true, element: <ChatWorkbench /> },
      { path: 'chat', element: <ChatWorkbench /> },
      { path: 'notes', element: <NotesPage /> },
      { path: 'settings', element: <SettingsPage /> },
      { path: 'settings/ai', element: <AiSettingsPage /> },
    ],
  },
])
