import { NavLink, Outlet, useNavigate } from 'react-router-dom'

export function AppShell() {
  const navigate = useNavigate()

  const navigationItems = [
    { label: 'Overview', to: '/app', icon: OverviewIcon },
    { label: 'Chat', to: '/app/chat', icon: ChatIcon },
    { label: 'Code', to: '/app/code', icon: CodeIcon },
    { label: 'Runs', to: '/app/runs', icon: RunsIcon },
    { label: 'Notes', to: '/app/notes', icon: NotesIcon },
  ]

  return (
    <main className="grid min-h-screen grid-cols-[200px_1fr] max-[820px]:grid-cols-1 max-[820px]:pb-[76px]">
      <nav
        className="sticky top-0 flex h-screen flex-col gap-1 border-r border-border bg-gradient-to-b from-[rgba(17,26,44,0.94)] to-[rgba(9,14,25,0.96)] p-3 shadow-[inset_-1px_0_0_rgba(56,189,248,0.08)] max-[820px]:fixed max-[820px]:inset-x-0 max-[820px]:bottom-0 max-[820px]:top-auto max-[820px]:z-10 max-[820px]:h-auto max-[820px]:flex-row max-[820px]:justify-stretch max-[820px]:gap-0.5 max-[820px]:border-t max-[820px]:border-r-0 max-[820px]:bg-[rgba(9,14,25,0.96)] max-[820px]:p-2.5"
        aria-label="Primary navigation"
      >
        {/* Logo */}
        <div className="mb-2 flex items-center gap-2 px-2 py-2 max-[820px]:hidden">
          <svg aria-hidden="true" viewBox="0 0 32 32" width="22" height="22">
            <rect width="32" height="32" rx="7" fill="url(#app-logo-grad)" />
            <path d="M10 10 L6 16 L10 22" stroke="white" strokeWidth="2.2" fill="none" strokeLinecap="round" strokeLinejoin="round" />
            <path d="M22 10 L26 16 L22 22" stroke="white" strokeWidth="2.2" fill="none" strokeLinecap="round" strokeLinejoin="round" />
            <path d="M14 22 L18 10" stroke="white" strokeWidth="2.2" fill="none" strokeLinecap="round" strokeLinejoin="round" />
            <defs>
              <linearGradient id="app-logo-grad" x1="0" y1="0" x2="32" y2="32">
                <stop stopColor="#38bdf8" />
                <stop offset="1" stopColor="#818cf8" />
              </linearGradient>
            </defs>
          </svg>
          <span className="text-sm font-bold">CodeCafe</span>
        </div>

        {/* Nav links */}
        <div className="grid gap-1 max-[820px]:flex max-[820px]:flex-1 max-[820px]:min-w-0">
          {navigationItems.map((item) => (
            <div className="relative max-[820px]:flex max-[820px]:flex-1" key={item.to}>
              <NavLink
                className={({ isActive }) =>
                  `relative flex items-center gap-2.5 rounded-lg px-3 py-2 text-sm font-medium text-muted no-underline transition-colors ${
                    isActive
                      ? 'text-text bg-accent/10'
                      : 'hover:text-text hover:bg-accent/8'
                  } max-[820px]:justify-center max-[820px]:px-2 max-[820px]:py-2.5`
                }
                end={item.to === '/app'}
                to={item.to}
              >
                <item.icon className="h-[18px] w-[18px]" />
                <span className="max-[820px]:hidden">{item.label}</span>
              </NavLink>
            </div>
          ))}
        </div>

        {/* Bottom section */}
        <div className="mt-auto flex flex-col gap-2 pt-4 max-[820px]:hidden">
          {/* User */}
          <div className="flex items-center gap-2 rounded-lg border border-border bg-bg/40 px-2 py-2">
            <div className="h-8 w-8 overflow-hidden rounded-full border border-border bg-accent/10">
              <img src="https://github.com/rio-csharp.png" alt="Rio" className="h-full w-full object-cover" onError={(e) => { (e.target as HTMLImageElement).style.display = 'none' }} />
            </div>
            <div className="min-w-0 flex-1">
              <div className="truncate text-xs font-semibold">Rio</div>
              <div className="text-[10px] text-muted">Creator</div>
            </div>
          </div>

          {/* Back to workspaces */}
          <button
            onClick={() => void navigate('/workspaces')}
            className="flex items-center gap-2 rounded-lg px-2 py-2 text-xs font-medium text-muted transition-colors hover:bg-accent/8 hover:text-text"
          >
            <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><path d="M19 12H5"/><path d="M12 19l-7-7 7-7"/></svg>
            Back to Workspaces
          </button>
        </div>
      </nav>

      <section className="min-w-0 p-5 max-[820px]:p-0">
        <Outlet />
      </section>
    </main>
  )
}

function OverviewIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/>
    </svg>
  )
}

function ChatIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/>
    </svg>
  )
}

function CodeIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <polyline points="16 18 22 12 16 6"/><polyline points="8 6 2 12 8 18"/>
    </svg>
  )
}

function RunsIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <polygon points="5 3 19 12 5 21 5 3"/>
    </svg>
  )
}

function NotesIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/><polyline points="10 9 9 9 8 9"/>
    </svg>
  )
}
