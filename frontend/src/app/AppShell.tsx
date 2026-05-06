import { NavLink, Outlet } from 'react-router-dom'

export function AppShell() {
  const navigationItems = [
    { label: 'Chat', to: '/' },
    { label: 'Notes', to: '/notes' },
    { label: 'Settings', to: '/settings' },
  ]
  const repositoryUrl = 'https://github.com/rio-csharp/CodeCafe'

  return (
    <main className="app-shell">
      <nav className="app-nav" aria-label="Primary navigation">
        <div className="app-nav-links">
          {navigationItems.map((item) => {
            return (
              <div className="app-nav-group" key={item.to}>
                <NavLink className="app-nav-item" end to={item.to}>
                  {item.label}
                </NavLink>
              </div>
            )
          })}
        </div>

        <a
          className="app-nav-external-link"
          href={repositoryUrl}
          rel="noreferrer"
          target="_blank"
        >
          GitHub
        </a>
      </nav>

      <section className="workspace">
        <Outlet />
      </section>
    </main>
  )
}
