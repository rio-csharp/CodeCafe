import { createBrowserRouter } from 'react-router-dom'
import { ChatWorkbench } from '../features/chat/ChatWorkbench'
import { AiSettingsPage, SettingsPage } from '../features/settings/SettingsPage'
import { AppShell } from './AppShell'

export const router = createBrowserRouter([
  {
    path: '/',
    element: <AppShell />,
    children: [
      { index: true, element: <ChatWorkbench /> },
      { path: 'chat', element: <ChatWorkbench /> },
      { path: 'settings', element: <SettingsPage /> },
      { path: 'settings/ai', element: <AiSettingsPage /> },
    ],
  },
])
