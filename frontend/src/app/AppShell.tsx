import { NavLink, Outlet } from 'react-router-dom'

const navigationItems = [
  { label: 'Chat', to: '/' },
  { label: 'Settings', to: '/settings' },
]

export function AppShell() {
  return (
    <main className="app-shell">
      <nav className="app-nav" aria-label="Primary navigation">
        {navigationItems.map((item) => (
          <NavLink className="app-nav-item" end={item.to === '/'} key={item.to} to={item.to}>
            {item.label}
          </NavLink>
        ))}
      </nav>

      <section className="workspace">
        <Outlet />
      </section>
    </main>
  )
}
