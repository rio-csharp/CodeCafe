import { ActivityPanel } from '../features/audit/ActivityPanel'
import { AuthPanel } from '../features/auth/AuthPanel'
import { AiPanel } from '../features/ai/AiPanel'
import { NotesPanel } from '../features/notes/NotesPanel'
import { WorkspacePanel } from '../features/workspaces/WorkspacePanel'
import { BackendHealthStatus } from '../shared/components/BackendHealthStatus'
import { PlatformStatus } from '../shared/components/PlatformStatus'
import { NavLink, Outlet } from 'react-router-dom'

const navigationItems = [
  { label: 'Dashboard', to: '/' },
  { label: 'Notes', to: '/notes' },
  { label: 'Workspace', to: '/workspace' },
  { label: 'AI', to: '/ai' },
  { label: 'Audit', to: '/audit' },
]

export function AppShell() {
  return (
    <main className="app-shell">
      <aside className="sidebar" aria-label="Main navigation">
        <div className="brand">
          <span className="brand-mark">CC</span>
          <div>
            <strong>CodeCafe</strong>
            <span>AI workbench</span>
          </div>
        </div>

        <nav className="nav-list">
          {navigationItems.map((item) => (
            <NavLink className="nav-item" end={item.to === '/'} key={item.to} to={item.to}>
              {item.label}
            </NavLink>
          ))}
        </nav>
      </aside>

      <section className="workspace">
        <header className="topbar">
          <div>
            <p className="eyebrow">Platform foundation</p>
            <h1>Developer knowledge workspace</h1>
          </div>
          <AuthPanel />
        </header>

        <section className="status-grid" aria-label="Platform status">
          <BackendHealthStatus />
          <PlatformStatus label="Frontend" value="React shell" tone="ready" />
          <PlatformStatus label="MAF" value="Planned" />
        </section>

        <Outlet />
      </section>
    </main>
  )
}

export function DashboardPage() {
  return (
    <section className="module-grid" aria-label="Feature modules">
      <NotesPanel />
      <WorkspacePanel />
      <AiPanel />
      <ActivityPanel />
    </section>
  )
}
