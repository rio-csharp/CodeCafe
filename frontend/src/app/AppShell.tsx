import { NavLink, Outlet } from 'react-router-dom'

export function AppShell() {
  const navigationItems = [
    { label: 'Chat', to: '/' },
    { label: 'Notes', to: '/notes' },
    { label: 'Settings', to: '/settings' },
  ]

  return (
    <main className="app-shell">
      <nav className="app-nav" aria-label="Primary navigation">
        {navigationItems.map((item) => {
          return (
            <div className="app-nav-group" key={item.to}>
              <NavLink className="app-nav-item" end to={item.to}>
                {item.label}
              </NavLink>
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
