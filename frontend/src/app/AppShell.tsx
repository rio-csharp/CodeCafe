import { NavLink, Outlet } from 'react-router-dom'

export function AppShell() {
  const navigationItems = [
    { label: 'Chat', to: '/app' },
    { label: 'Notes', to: '/app/notes' },
    { label: 'Settings', to: '/app/settings' },
  ]
  const repositoryUrl = 'https://github.com/rio-csharp/CodeCafe'

  return (
    <main className="grid min-h-screen grid-cols-[112px_1fr] max-[820px]:grid-cols-1 max-[820px]:pb-[76px]">
      <nav
        className="sticky top-0 flex h-screen flex-col gap-3.5 border-r border-border bg-gradient-to-b from-[rgba(17,26,44,0.94)] to-[rgba(9,14,25,0.96)] p-4 shadow-[inset_-1px_0_0_rgba(56,189,248,0.08)] max-[820px]:fixed max-[820px]:inset-x-0 max-[820px]:bottom-0 max-[820px]:top-auto max-[820px]:z-10 max-[820px]:h-auto max-[820px]:flex-row max-[820px]:justify-stretch max-[820px]:gap-2.5 max-[820px]:border-t max-[820px]:border-r-0 max-[820px]:bg-[rgba(9,14,25,0.96)] max-[820px]:p-2.5"
        aria-label="Primary navigation"
      >
        <div className="grid gap-3 max-[820px]:flex-1 max-[820px]:min-w-0">
          <div className="grid gap-2 max-[820px]:flex max-[820px]:flex-1">
            {navigationItems.map((item) => (
              <div className="relative max-[820px]:flex max-[820px]:flex-1" key={item.to}>
                <NavLink
                  className={({ isActive }) =>
                    `relative block rounded-lg px-3 py-2.5 text-center text-sm font-medium text-muted no-underline transition-colors ${
                      isActive
                        ? 'text-text bg-accent/10 shadow-[inset_0_0_0_1px_rgba(56,189,248,0.18),0_10px_28px_rgba(8,145,178,0.1)] before:absolute before:left-[-6px] before:top-2.5 before:bottom-2.5 before:w-0.5 before:rounded-full before:bg-accent-strong before:shadow-[0_0_18px_rgba(34,211,238,0.75)] max-[820px]:before:inset-x-7 max-[820px]:before:top-auto max-[820px]:before:bottom-[-4px] max-[820px]:before:h-0.5 max-[820px]:before:w-auto max-[820px]:before:left-auto'
                        : 'hover:text-text hover:bg-accent/10 hover:shadow-[inset_0_0_0_1px_rgba(56,189,248,0.18),0_10px_28px_rgba(8,145,178,0.1)]'
                    }`
                  }
                  end
                  to={item.to}
                >
                  {item.label}
                </NavLink>
              </div>
            ))}
          </div>
        </div>

        <div className="mt-auto grid gap-3 max-[820px]:hidden">
          <a
            aria-label="GitHub"
            className="inline-flex h-[38px] w-[38px] min-h-[38px] min-w-[38px] items-center justify-center rounded-lg border border-transparent text-muted no-underline transition-colors hover:border-accent/25 hover:bg-accent/8 hover:text-text"
            href={repositoryUrl}
            rel="noreferrer"
            title="GitHub"
            target="_blank"
          >
            <svg aria-hidden="true" viewBox="0 0 24 24" className="h-[18px] w-[18px]">
              <path
                d="M12 2C6.48 2 2 6.59 2 12.25c0 4.53 2.87 8.37 6.84 9.72.5.09.68-.22.68-.49 0-.24-.01-1.03-.01-1.87-2.78.62-3.37-1.21-3.37-1.21-.46-1.2-1.11-1.52-1.11-1.52-.91-.64.07-.63.07-.63 1 .08 1.53 1.06 1.53 1.06.9 1.58 2.35 1.12 2.92.86.09-.67.35-1.12.63-1.38-2.22-.26-4.56-1.15-4.56-5.1 0-1.13.39-2.05 1.03-2.78-.1-.26-.45-1.31.1-2.73 0 0 .84-.28 2.75 1.06A9.3 9.3 0 0 1 12 6.84c.85 0 1.71.12 2.51.35 1.91-1.34 2.75-1.06 2.75-1.06.55 1.42.2 2.47.1 2.73.64.73 1.03 1.65 1.03 2.78 0 3.96-2.34 4.83-4.57 5.09.36.32.68.94.68 1.9 0 1.37-.01 2.47-.01 2.8 0 .27.18.58.69.48A10.27 10.27 0 0 0 22 12.25C22 6.59 17.52 2 12 2Z"
                fill="currentColor"
              />
            </svg>
          </a>
        </div>
      </nav>

      <section className="min-w-0 p-5 max-[820px]:p-0">
        <Outlet />
      </section>
    </main>
  )
}
