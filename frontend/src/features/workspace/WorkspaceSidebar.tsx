import { Link, useParams } from 'react-router-dom'

const navItems = [
  { label: 'Overview', path: '', icon: OverviewIcon },
  { label: 'Chat', path: 'chat', icon: ChatIcon },
  { label: 'Code', path: 'code', icon: CodeIcon },
  { label: 'Runs', path: 'runs', icon: RunsIcon },
  { label: 'Notes', path: 'notes', icon: NotesIcon },
]

export function WorkspaceSidebar({ activeItem }: { activeItem: string }) {
  const { id } = useParams<{ id: string }>()
  const basePath = `/workspaces/${id}`

  return (
    <aside className="flex w-[200px] shrink-0 flex-col border-r border-border bg-gradient-to-b from-[rgba(17,26,44,0.94)] to-[rgba(9,14,25,0.96)] shadow-[inset_-1px_0_0_rgba(56,189,248,0.08)]">
      {/* Logo */}
      <div className="flex items-center gap-2 px-3 py-3.5">
        <svg aria-hidden="true" viewBox="0 0 32 32" width="22" height="22">
          <rect width="32" height="32" rx="7" fill="url(#wsb-logo-grad)" />
          <path d="M10 10 L6 16 L10 22" stroke="white" strokeWidth="2.2" fill="none" strokeLinecap="round" strokeLinejoin="round" />
          <path d="M22 10 L26 16 L22 22" stroke="white" strokeWidth="2.2" fill="none" strokeLinecap="round" strokeLinejoin="round" />
          <path d="M14 22 L18 10" stroke="white" strokeWidth="2.2" fill="none" strokeLinecap="round" strokeLinejoin="round" />
          <defs>
            <linearGradient id="wsb-logo-grad" x1="0" y1="0" x2="32" y2="32">
              <stop stopColor="#38bdf8" />
              <stop offset="1" stopColor="#818cf8" />
            </linearGradient>
          </defs>
        </svg>
        <span className="text-sm font-bold">CodeCafe</span>
      </div>

      {/* Nav */}
      <nav className="flex flex-1 flex-col gap-1 px-3">
        {navItems.map((item) => {
          const to = item.path ? `${basePath}/${item.path}` : basePath
          const isActive = activeItem === item.label
          return (
            <Link
              key={item.label}
              to={to}
              className={`flex items-center gap-2.5 rounded-lg px-3 py-2 text-sm font-medium no-underline transition-colors ${
                isActive ? 'bg-accent/10 text-text' : 'text-muted hover:bg-accent/8 hover:text-text'
              }`}
            >
              <item.icon />
              <span>{item.label}</span>
            </Link>
          )
        })}
      </nav>

      {/* User + Back */}
      <div className="flex flex-col gap-2 p-3">
        <div className="flex items-center gap-2 rounded-lg border border-border bg-bg/40 px-2 py-2">
          <div className="h-8 w-8 overflow-hidden rounded-full border border-border bg-accent/10">
            <img src="https://github.com/rio-csharp.png" alt="Rio" className="h-full w-full object-cover" onError={(e) => { (e.target as HTMLImageElement).style.display = 'none' }} />
          </div>
          <div className="min-w-0 flex-1">
            <div className="truncate text-xs font-semibold">Rio</div>
            <div className="text-[10px] text-muted">Creator</div>
          </div>
        </div>
        <Link to="/workspaces" className="flex items-center gap-2 rounded-lg px-2 py-2 text-xs font-medium text-muted no-underline transition-colors hover:bg-accent/8 hover:text-text">
          <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><path d="M19 12H5M12 19l-7-7 7-7"/></svg>
          Back to Workspaces
        </Link>
      </div>
    </aside>
  )
}

/* Icons */
function OverviewIcon() {
  return <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/></svg>
}

function ChatIcon() {
  return <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/></svg>
}

function CodeIcon() {
  return <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2"><polyline points="16 18 22 12 16 6"/><polyline points="8 6 2 12 8 18"/></svg>
}

function RunsIcon() {
  return <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2"><polygon points="5 3 19 12 5 21 5 3"/></svg>
}

function NotesIcon() {
  return <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>
}
