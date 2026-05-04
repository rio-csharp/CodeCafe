import { NavLink, Outlet } from 'react-router-dom'
import { isLocalEnvironment } from './runtimeEnvironment'

export function AppShell() {
  const navigationItems = [
    { label: 'Chat', to: '/' },
    { label: 'Notes', to: '/notes' },
    {
      children: [
        { label: 'AI', to: '/settings/ai' },
        ...(isLocalEnvironment() ? [{ label: 'Notes', to: '/settings/notes' }] : []),
      ],
      label: 'Settings',
      to: '/settings',
    },
  ]

  return (
    <main className="app-shell">
      <nav className="app-nav" aria-label="Primary navigation">
        {navigationItems.map((item) => {
          const children = 'children' in item ? item.children : undefined

          return (
            <div className="app-nav-group" key={item.to}>
              <NavLink className="app-nav-item" end to={item.to}>
                {item.label}
              </NavLink>
              {children ? (
                <div className="app-subnav">
                  {children.map((child) => (
                    <NavLink className="app-subnav-item" key={child.to} to={child.to}>
                      {child.label}
                    </NavLink>
                  ))}
                </div>
              ) : null}
            </div>
          )
        })}
      </nav>

      <section className="workspace">
        <Outlet />
      </section>
    </main>
  )
}
