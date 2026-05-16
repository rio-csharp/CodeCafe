import { Link } from 'react-router-dom'

const workspaces = [
  {
    id: 'codecafe',
    name: 'CodeCafe',
    initials: 'CC',
    desc: 'The official CodeCafe showcase project. AI-native engineering workspace built with ASP.NET Core and React.',
    updatedAt: '2m ago',
    branch: 'main',
    status: 'Healthy' as const,
    showcase: true,
  },
  {
    id: 'notes',
    name: 'Notes',
    initials: 'N',
    desc: 'Personal knowledge management workspace. Notes, ideas, and documents.',
    updatedAt: '1d ago',
    branch: 'main',
    status: 'Healthy' as const,
    showcase: false,
  },
  {
    id: 'skills',
    name: 'Skills',
    initials: 'S',
    desc: 'Reusable capabilities and skill definitions for AI agents.',
    updatedAt: '3d ago',
    branch: 'main',
    status: 'Healthy' as const,
    showcase: false,
  },
]

function LogoIcon({ size = 28 }: { size?: number }) {
  return (
    <svg aria-hidden="true" viewBox="0 0 32 32" width={size} height={size}>
      <rect width="32" height="32" rx="7" fill="url(#ws-logo-grad)" />
      <path d="M10 10 L6 16 L10 22" stroke="white" strokeWidth="2.2" fill="none" strokeLinecap="round" strokeLinejoin="round" />
      <path d="M22 10 L26 16 L22 22" stroke="white" strokeWidth="2.2" fill="none" strokeLinecap="round" strokeLinejoin="round" />
      <path d="M14 22 L18 10" stroke="white" strokeWidth="2.2" fill="none" strokeLinecap="round" strokeLinejoin="round" />
      <defs>
        <linearGradient id="ws-logo-grad" x1="0" y1="0" x2="32" y2="32">
          <stop stopColor="#38bdf8" />
          <stop offset="1" stopColor="#818cf8" />
        </linearGradient>
      </defs>
    </svg>
  )
}

export function WorkspaceSelectPage() {
  return (
    <div className="relative min-h-screen overflow-hidden text-text bg-bg">
      {/* Subtle background glow */}
      <div className="pointer-events-none absolute left-1/2 top-0 h-[500px] w-[1000px] -translate-x-1/2 bg-glow-hero" />
      {/* Dot pattern */}
      <div className="pointer-events-none absolute inset-0 bg-dot-pattern opacity-[0.08]" />

      {/* Header */}
      <header className="border-b border-border bg-bg/82 backdrop-blur-xl">
        <div className="mx-auto flex max-w-[1200px] items-center justify-between gap-6 px-6 py-3.5">
          <Link className="inline-flex items-center gap-2.5 text-lg font-bold text-text no-underline" to="/">
            <LogoIcon />
            <span>CodeCafe</span>
          </Link>

          <div className="flex items-center gap-6">
            <a href="#" className="flex items-center gap-2 text-sm font-medium text-muted no-underline transition-colors hover:text-text">
              <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2"><path d="M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z"/><path d="M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z"/></svg>
              Docs
            </a>
            <a href="https://github.com/rio-csharp/CodeCafe" target="_blank" rel="noreferrer" className="flex items-center gap-2 text-sm font-medium text-muted no-underline transition-colors hover:text-text">
              <svg viewBox="0 0 24 24" width="18" height="18" fill="currentColor"><path d="M12 2C6.48 2 2 6.59 2 12.25c0 4.53 2.87 8.37 6.84 9.72.5.09.68-.22.68-.49 0-.24-.01-1.03-.01-1.87-2.78.62-3.37-1.21-3.37-1.21-.46-1.2-1.11-1.52-1.11-1.52-.91-.64.07-.63.07-.63 1 .08 1.53 1.06 1.53 1.06.9 1.58 2.35 1.12 2.92.86.09-.67.35-1.12.63-1.38-2.22-.26-4.56-1.15-4.56-5.1 0-1.13.39-2.05 1.03-2.78-.1-.26-.45-1.31.1-2.73 0 0 .84-.28 2.75 1.06A9.3 9.3 0 0 1 12 6.84c.85 0 1.71.12 2.51.35 1.91-1.34 2.75-1.06 2.75-1.06.55 1.42.2 2.47.1 2.73.64.73 1.03 1.65 1.03 2.78 0 3.96-2.34 4.83-4.57 5.09.36.32.68.94.68 1.9 0 1.37-.01 2.47-.01 2.8 0 .27.18.58.69.48A10.27 10.27 0 0 0 22 12.25C22 6.59 17.52 2 12 2Z"/></svg>
              GitHub
            </a>
            <div className="flex items-center gap-2">
              <div className="h-8 w-8 overflow-hidden rounded-full border border-border bg-accent/10">
                <img src="https://github.com/rio-csharp.png" alt="Rio" className="h-full w-full object-cover" onError={(e) => { (e.target as HTMLImageElement).style.display = 'none' }} />
              </div>
              <span className="text-sm font-medium">Rio</span>
              <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><path d="M6 9l6 6 6-6"/></svg>
            </div>
          </div>
        </div>
      </header>

      <main className="mx-auto max-w-[1200px] px-6 py-10">
        {/* Hero */}
        <div className="mb-10 text-center">
          <p className="m-0 mb-2 text-sm text-muted">Welcome back, Rio 👋</p>
          <h1 className="m-0 mb-3 text-3xl font-bold tracking-tight">Select a workspace to continue</h1>
          <p className="m-0 mx-auto max-w-[560px] text-sm leading-relaxed text-muted">
            Every workspace is a persistent environment with memory, context, and safe execution. Pick one or create a new workspace to get started.
          </p>
        </div>

        {/* Main grid */}
        <div className="grid grid-cols-[280px_1fr] gap-6">
          {/* Left info panel */}
          <div className="flex flex-col gap-5">
            <div className="rounded-xl border border-border bg-surface/50 p-5">
              <h3 className="m-0 mb-2 text-base font-bold">What is a Workspace?</h3>
              <p className="m-0 mb-4 text-sm leading-relaxed text-muted">
                A workspace is your AI-native engineering environment. It remembers your project, understands your codebase, and evolves with you.
              </p>

              <div className="flex flex-col gap-3">
                <InfoItem
                  icon={<MemoryIcon />}
                  title="Persistent Memory"
                  desc="AI remembers decisions, architecture, and context."
                />
                <InfoItem
                  icon={<CodeIcon />}
                  title="Codebase Aware"
                  desc="Connect your repo and chat with full context."
                />
                <InfoItem
                  icon={<RunIcon />}
                  title="Safe Runs"
                  desc="Run and preview your project in isolated environments."
                />
                <InfoItem
                  icon={<AIIcon />}
                  title="AI Copilot"
                  desc="Get intelligent suggestions and automate engineering tasks."
                />
              </div>

              <a href="#" className="mt-4 inline-flex items-center gap-1 text-sm font-medium text-accent no-underline">
                Learn more <span>→</span>
              </a>
            </div>
          </div>

          {/* Right workspace grid */}
          <div className="flex flex-col gap-5">
            <div className="flex items-center justify-between">
              <h2 className="m-0 text-lg font-bold">Your Workspaces</h2>
              <div className="flex items-center gap-3">
                <button className="inline-flex h-8 w-8 items-center justify-center rounded-md border border-border text-muted transition hover:text-text">
                  <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2"><rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/></svg>
                </button>
                <button className="inline-flex h-8 w-8 items-center justify-center rounded-md border border-border text-muted transition hover:text-text">
                  <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2"><line x1="3" y1="6" x2="21" y2="6"/><line x1="3" y1="12" x2="21" y2="12"/><line x1="3" y1="18" x2="21" y2="18"/></svg>
                </button>
                <div className="flex items-center gap-2 rounded-md border border-border bg-bg/60 px-3 py-1.5">
                  <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/></svg>
                  <input
                    type="text"
                    placeholder="Search workspace..."
                    className="border-0 bg-transparent text-sm text-text outline-none placeholder:text-muted"
                  />
                </div>
                <button className="inline-flex items-center gap-1.5 rounded-md border border-border px-3 py-1.5 text-sm font-medium text-muted transition hover:text-text">
                  <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><line x1="4" y1="21" x2="4" y2="14"/><line x1="4" y1="10" x2="4" y2="3"/><line x1="12" y1="21" x2="12" y2="12"/><line x1="12" y1="8" x2="12" y2="3"/><line x1="20" y1="21" x2="20" y2="16"/><line x1="20" y1="12" x2="20" y2="3"/><line x1="1" y1="14" x2="7" y2="14"/><line x1="9" y1="8" x2="15" y2="8"/><line x1="17" y1="16" x2="23" y2="16"/></svg>
                  Filter
                </button>
              </div>
            </div>

            <div className="grid grid-cols-3 gap-4">
              {workspaces.map((ws) => (
                <div
                  key={ws.id}
                  className={`relative flex flex-col gap-4 rounded-xl border p-5 transition ${
                    ws.showcase
                      ? 'border-accent/30 bg-accent/5'
                      : 'border-border bg-surface/40 hover:border-accent/20 hover:bg-accent/4'
                  }`}
                >
                  {ws.showcase && (
                    <span className="absolute -top-2 left-4 rounded bg-accent px-2 py-0.5 text-[10px] font-bold text-[#070a12]">
                      Showcase
                    </span>
                  )}
                  <div className="flex items-start gap-3">
                    <div className={`grid h-11 w-11 place-items-center rounded-lg text-base font-bold ${
                      ws.showcase ? 'bg-accent/15 text-accent' : 'bg-accent/10 text-accent'
                    }`}>
                      {ws.initials}
                    </div>
                    <div className="min-w-0">
                      <div className="flex items-center gap-1.5">
                        <h3 className="m-0 text-base font-bold">{ws.name}</h3>
                        {ws.showcase && <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" className="text-accent"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>}
                      </div>
                      <p className="m-0 mt-1 text-xs leading-relaxed text-muted">{ws.desc}</p>
                    </div>
                  </div>

                  <div className="mt-auto flex items-center justify-between text-xs text-muted">
                    <span>Updated {ws.updatedAt}</span>
                    <div className="flex items-center gap-1">
                      <svg viewBox="0 0 24 24" width="12" height="12" fill="none" stroke="currentColor" strokeWidth="2"><line x1="6" y1="3" x2="6" y2="15"/><circle cx="18" cy="6" r="3"/><circle cx="6" cy="18" r="3"/><path d="M18 9a9 9 0 0 1-9 9"/></svg>
                      {ws.branch}
                    </div>
                    <div className="flex items-center gap-1 text-success">
                      <span className="h-2 w-2 rounded-full bg-success" />
                      {ws.status}
                    </div>
                  </div>

                  <div className="flex items-center gap-2">
                    <Link
                      className={`flex-1 rounded-lg py-2 text-center text-sm font-semibold no-underline transition ${
                        ws.showcase
                          ? 'bg-accent text-[#070a12] hover:opacity-90'
                          : 'border border-border bg-bg/60 text-text hover:border-accent/30'
                      }`}
                      to={`/workspaces/${ws.id}`}
                    >
                      Open Workspace
                    </Link>
                    <button className="inline-flex h-8 w-8 items-center justify-center rounded-lg border border-border text-muted transition hover:text-text">
                      <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="12" cy="12" r="1"/><circle cx="19" cy="12" r="1"/><circle cx="5" cy="12" r="1"/></svg>
                    </button>
                  </div>
                </div>
              ))}
            </div>

            {/* Create new workspace */}
            <div className="flex items-center gap-4 rounded-xl border border-dashed border-border bg-surface/20 px-6 py-5">
              <div className="grid h-11 w-11 place-items-center rounded-full border border-accent/30 text-accent">
                <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2"><line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/></svg>
              </div>
              <div className="flex-1">
                <h3 className="m-0 text-sm font-bold">Create New Workspace</h3>
                <p className="m-0 text-xs text-muted">Start a new workspace from scratch.</p>
              </div>
              <button className="inline-flex items-center gap-1 rounded-lg bg-accent px-4 py-2 text-sm font-semibold text-[#070a12] transition hover:opacity-90">
                <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/></svg>
                Create Workspace
              </button>
            </div>
          </div>
        </div>
      </main>

      {/* Bottom tip */}
      <div className="border-t border-border bg-bg/60">
        <div className="mx-auto flex max-w-[1200px] items-center justify-between px-6 py-3.5">
          <div className="flex items-center gap-2 text-sm text-muted">
            <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2" className="text-accent"><path d="M13 2L3 14h9l-1 8 10-12h-9l1-8z"/></svg>
            <span className="font-semibold text-text">Tip:</span>
            <span>Workspaces are isolated and secure</span>
            <span className="text-xs">Each workspace runs in its own environment with separate memory, configurations, and execution sandboxes.</span>
          </div>
          <a href="#" className="flex items-center gap-1 text-sm font-medium text-muted no-underline transition hover:text-text">
            <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><path d="M12 20h9"/><path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z"/></svg>
            Manage all workspaces
          </a>
        </div>
      </div>
    </div>
  )
}

function InfoItem({ icon, title, desc }: { icon: React.ReactNode; title: string; desc: string }) {
  return (
    <div className="flex items-start gap-2.5">
      <div className="mt-0.5 text-muted">{icon}</div>
      <div>
        <div className="text-xs font-semibold">{title}</div>
        <div className="text-[11px] leading-relaxed text-muted">{desc}</div>
      </div>
    </div>
  )
}

function MemoryIcon() {
  return <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2"><path d="M12 2a10 10 0 1 0 10 10A10 10 0 0 0 12 2z"/><path d="M12 6a6 6 0 1 0 6 6 6 6 0 0 0-6-6z"/></svg>
}

function CodeIcon() {
  return <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2"><path d="M16 18l6-6-6-6"/><path d="M8 6l-6 6 6 6"/></svg>
}

function RunIcon() {
  return <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2"><polygon points="5 3 19 12 5 21 5 3"/></svg>
}

function AIIcon() {
  return <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2"><path d="M12 2L2 7l10 5 10-5-10-5z"/><path d="M2 17l10 5 10-5"/><path d="M2 12l10 5 10-5"/></svg>
}
