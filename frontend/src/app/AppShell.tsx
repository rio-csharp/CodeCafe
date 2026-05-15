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
        <div className="app-nav-main">
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
        </div>

        <div className="app-nav-bottom">
          <a
            aria-label="GitHub"
            className="app-nav-external-link"
            href={repositoryUrl}
            rel="noreferrer"
            title="GitHub"
            target="_blank"
          >
            <svg aria-hidden="true" viewBox="0 0 24 24">
              <path
                d="M12 2C6.48 2 2 6.59 2 12.25c0 4.53 2.87 8.37 6.84 9.72.5.09.68-.22.68-.49 0-.24-.01-1.03-.01-1.87-2.78.62-3.37-1.21-3.37-1.21-.46-1.2-1.11-1.52-1.11-1.52-.91-.64.07-.63.07-.63 1 .08 1.53 1.06 1.53 1.06.9 1.58 2.35 1.12 2.92.86.09-.67.35-1.12.63-1.38-2.22-.26-4.56-1.15-4.56-5.1 0-1.13.39-2.05 1.03-2.78-.1-.26-.45-1.31.1-2.73 0 0 .84-.28 2.75 1.06A9.3 9.3 0 0 1 12 6.84c.85 0 1.71.12 2.51.35 1.91-1.34 2.75-1.06 2.75-1.06.55 1.42.2 2.47.1 2.73.64.73 1.03 1.65 1.03 2.78 0 3.96-2.34 4.83-4.57 5.09.36.32.68.94.68 1.9 0 1.37-.01 2.47-.01 2.8 0 .27.18.58.69.48A10.27 10.27 0 0 0 22 12.25C22 6.59 17.52 2 12 2Z"
                fill="currentColor"
              />
            </svg>
          </a>
        </div>
      </nav>

      <section className="workspace">
        <Outlet />
      </section>
    </main>
  )
}
