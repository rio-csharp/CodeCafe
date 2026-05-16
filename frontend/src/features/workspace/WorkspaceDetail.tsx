import { WorkspaceSidebar } from './WorkspaceSidebar'

const workspaceData = {
  name: 'CodeCafe',
  description: 'AI-native engineering workspace with persistent project memory.',
  longDesc: 'Build, run, and ship software with the power of AI. Your project context, decisions, and knowledge persist across sessions.',
  createdAt: 'May 10, 2024',
  repository: 'rio-csharp/CodeCafe',
  stack: '.NET \u2022 React \u2022 TypeScript',
  visibility: 'Private',
  currentPhase: 'Phase 1 MVP',
  phaseDesc: 'Building the core platform with memory, preview environments, and AI integration.',
  priorities: [
    { title: 'Workspace persistence', status: 'In Progress' },
    { title: 'Safe preview environments', status: 'To Do' },
    { title: 'GitHub integration', status: 'To Do' },
    { title: 'AI engineering workflow', status: 'To Do' },
  ],
  decisions: [
    'Monorepo structure for scalability',
    'Guest workspaces are read-only',
    'Only safe templates in sandbox',
    'Logs retention: 7 days',
  ],
  architecture: [
    { label: 'Frontend (Vite + React)', level: 0 },
    { label: 'Backend (ASP.NET Core)', level: 0 },
    { label: 'Memory Layer', level: 1 },
    { label: 'Run Service', level: 1 },
  ],
  topics: [
    'sandbox security',
    'workspace persistence',
    'GitHub integration',
    'run logs viewer',
    'AI memory summarization',
  ],
  activities: [
    { icon: 'brain', title: 'Memory updated', desc: 'Added decision: "Only support safe template runs in sandbox"', time: '2m ago' },
    { icon: 'check', title: 'Run completed', desc: 'Preview environment is healthy', time: '5m ago', badge: 'Success' },
    { icon: 'task', title: 'Task created', desc: 'Add run logs viewer', time: '12m ago' },
    { icon: 'git', title: 'Repository synced', desc: 'Fetched latest changes from main', time: '18m ago' },
    { icon: 'note', title: 'Note updated', desc: 'Updated: Sandbox Security Plan', time: '1h ago' },
  ],
  quickNote: {
    title: 'Phase 1 MVP Plan',
    desc: 'Core features for the first release.',
    items: [
      { text: 'Workspace persistence', done: true },
      { text: 'Run logs viewer', done: false },
      { text: 'Memory summarization', done: false },
    ],
    updatedAt: '1h ago',
  },
}

export function WorkspaceDetail() {
  return (
    <div className="flex min-h-screen bg-bg text-text">
      <WorkspaceSidebar activeItem="Overview" />

      {/* Main */}
      <main className="flex-1 overflow-auto p-6 lg:p-8">
        {/* Header */}
        <div className="mb-6 flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h1 className="m-0 text-2xl font-bold tracking-tight">Overview</h1>
            <p className="m-0 mt-1 text-sm text-muted">Get a quick snapshot of CodeCafe and what we&apos;re working on.</p>
          </div>
          <div className="inline-flex items-center gap-1.5 self-start rounded-full border border-success/20 bg-success/8 px-3 py-1 text-xs font-semibold text-success">
            <span className="h-2 w-2 rounded-full bg-success" />
            Workspace Active
          </div>
        </div>

        {/* Content */}
        <div className="grid grid-cols-1 gap-6 xl:grid-cols-[1fr_320px]">
          {/* Left column */}
          <div className="flex flex-col gap-6">
            {/* Hero card */}
            <div className="flex flex-col gap-5 rounded-xl border border-border bg-surface/40 p-5 sm:flex-row sm:items-start">
              <div className="grid h-16 w-16 shrink-0 place-items-center rounded-xl bg-accent/10 text-2xl text-accent">
                {'</>'}
              </div>
              <div className="flex-1">
                <h2 className="m-0 text-xl font-bold">{workspaceData.name}</h2>
                <p className="m-0 mt-1 text-sm text-accent">{workspaceData.description}</p>
                <p className="m-0 mt-2 text-sm leading-relaxed text-muted">{workspaceData.longDesc}</p>
              </div>
              <div className="flex shrink-0 flex-col gap-3 text-sm">
                <InfoRow icon={<CalendarIcon />} label="Created" value={workspaceData.createdAt} />
                <InfoRow icon={<GitIcon />} label="Repository" value={workspaceData.repository} link />
                <InfoRow icon={<StackIcon />} label="Primary Stack" value={workspaceData.stack} />
                <InfoRow icon={<EyeIcon />} label="Visibility" value={<span className="rounded bg-accent/15 px-2 py-0.5 text-xs font-semibold text-accent">{workspaceData.visibility}</span>} />
              </div>
            </div>

            {/* Current Focus */}
            <div className="rounded-xl border border-border bg-surface/40 p-5">
              <div className="mb-4 flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
                <div className="flex items-center gap-2">
                  <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="12" cy="12" r="10"/><path d="M12 6v6l4 2"/></svg>
                  <div>
                    <h3 className="m-0 text-base font-bold">Current Focus</h3>
                    <p className="m-0 text-xs text-muted">What we&apos;re currently working on.</p>
                  </div>
                </div>
                <a href="#" className="text-sm font-medium text-accent no-underline">View all tasks <span>→</span></a>
              </div>
              <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                {/* Phase */}
                <div className="rounded-lg border border-border bg-bg/60 p-4">
                  <div className="mb-3 text-xs text-muted">Current Phase</div>
                  <span className="inline-block rounded bg-accent/15 px-2.5 py-1 text-xs font-bold text-accent">{workspaceData.currentPhase}</span>
                  <p className="m-0 mt-3 text-sm leading-relaxed text-muted">{workspaceData.phaseDesc}</p>
                </div>
                {/* Priorities */}
                <div className="rounded-lg border border-border bg-bg/60 p-4">
                  <div className="mb-3 text-xs font-semibold text-muted">Top Priorities</div>
                  <div className="flex flex-col gap-2.5">
                    {workspaceData.priorities.map((p, i) => (
                      <div key={i} className="flex items-center justify-between gap-3 text-sm">
                        <div className="flex items-center gap-2">
                          <span className={`inline-block h-3.5 w-3.5 rounded-sm border ${p.status === 'In Progress' ? 'border-accent bg-accent/20' : 'border-border'}`} />
                          <span>{p.title}</span>
                        </div>
                        <span className={`rounded px-2 py-0.5 text-[10px] font-bold ${p.status === 'In Progress' ? 'border border-success/20 bg-success/8 text-success' : 'border border-border bg-bg/50 text-muted'}`}>{p.status}</span>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            </div>

            {/* Workspace Memory */}
            <div className="rounded-xl border border-border bg-surface/40 p-5">
              <div className="mb-4 flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
                <div className="flex items-center gap-2">
                  <BrainIcon />
                  <div>
                    <h3 className="m-0 text-base font-bold">Workspace Memory</h3>
                    <p className="m-0 text-xs text-muted">Key knowledge that AI remembers about this project.</p>
                  </div>
                </div>
                <a href="#" className="text-sm font-medium text-accent no-underline">View all memory <span>→</span></a>
              </div>
              <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
                {/* Recent Decisions */}
                <div className="rounded-lg border border-border bg-bg/60 p-4">
                  <div className="mb-3 flex items-center gap-2 text-xs font-semibold text-muted">
                    <span className="inline-flex h-5 w-5 items-center justify-center rounded-full bg-accent/10 text-[10px]">💡</span>
                    Recent Decisions
                  </div>
                  <ul className="m-0 flex flex-col gap-2 pl-4 text-sm text-muted">
                    {workspaceData.decisions.map((d, i) => (
                      <li key={i} className="leading-snug">{d}</li>
                    ))}
                  </ul>
                  <div className="mt-3 text-[11px] text-muted">Updated 2m ago</div>
                </div>
                {/* Known Architecture */}
                <div className="rounded-lg border border-border bg-bg/60 p-4">
                  <div className="mb-3 flex items-center gap-2 text-xs font-semibold text-muted">
                    <span className="inline-flex h-5 w-5 items-center justify-center rounded-full bg-accent/10 text-[10px]">🏗</span>
                    Known Architecture
                  </div>
                  <div className="flex flex-col gap-2">
                    {workspaceData.architecture.map((a, i) => (
                      <div
                        key={i}
                        className={`rounded border border-border bg-bg/80 px-3 py-1.5 text-center text-xs ${a.level === 1 ? 'ml-4' : ''}`}
                      >
                        {a.label}
                      </div>
                    ))}
                  </div>
                  <div className="mt-3 text-[11px] text-muted">Updated 1d ago</div>
                </div>
                {/* Recent Topics */}
                <div className="rounded-lg border border-border bg-bg/60 p-4">
                  <div className="mb-3 flex items-center gap-2 text-xs font-semibold text-muted">
                    <span className="inline-flex h-5 w-5 items-center justify-center rounded-full bg-accent/10 text-[10px]">#</span>
                    Recent Topics
                  </div>
                  <div className="flex flex-wrap gap-1.5">
                    {workspaceData.topics.map((t, i) => (
                      <span key={i} className="rounded border border-border bg-bg/80 px-2 py-1 text-xs text-muted">{t}</span>
                    ))}
                  </div>
                  <div className="mt-3 text-[11px] text-muted">Updated 3m ago</div>
                </div>
              </div>
            </div>
          </div>

          {/* Right sidebar */}
          <div className="flex flex-col gap-6">
            {/* Recent Activity */}
            <div className="rounded-xl border border-border bg-surface/40 p-5">
              <div className="mb-1">
                <h3 className="m-0 flex items-center gap-2 text-base font-bold">
                  <ActivityIcon />
                  Recent Activity
                </h3>
                <p className="m-0 text-xs text-muted">See what&apos;s been happening.</p>
              </div>
              <div className="mt-4 flex flex-col gap-4">
                {workspaceData.activities.map((a, i) => (
                  <div key={i} className="flex items-start gap-3">
                    <div className={`mt-0.5 grid h-8 w-8 shrink-0 place-items-center rounded-full text-xs ${
                      a.icon === 'check' ? 'bg-success/10 text-success' :
                      a.icon === 'brain' ? 'bg-accent/10 text-accent' :
                      a.icon === 'task' ? 'bg-warning/10 text-warning' :
                      a.icon === 'git' ? 'bg-muted/10 text-muted' :
                      'bg-accent/10 text-accent'
                    }`}>
                      {a.icon === 'brain' && <BrainIconSmall />}
                      {a.icon === 'check' && <CheckIconSmall />}
                      {a.icon === 'task' && <TaskIconSmall />}
                      {a.icon === 'git' && <GitIconSmall />}
                      {a.icon === 'note' && <NoteIconSmall />}
                    </div>
                    <div className="min-w-0 flex-1">
                      <div className="flex items-center justify-between gap-2">
                        <span className="text-sm font-semibold">{a.title}</span>
                        <span className="shrink-0 text-[11px] text-muted">{a.time}</span>
                      </div>
                      <p className="m-0 mt-0.5 text-xs leading-relaxed text-muted">{a.desc}</p>
                      {a.badge && <span className="mt-1 inline-block rounded border border-success/20 bg-success/8 px-1.5 py-0.5 text-[10px] font-bold text-success">{a.badge}</span>}
                    </div>
                  </div>
                ))}
              </div>
              <a href="#" className="mt-4 inline-block text-sm font-medium text-accent no-underline">View all activity <span>→</span></a>
            </div>

            {/* Quick Notes */}
            <div className="rounded-xl border border-border bg-surface/40 p-5">
              <div className="mb-1">
                <h3 className="m-0 flex items-center gap-2 text-base font-bold">
                  <NoteIcon />
                  Quick Notes
                </h3>
                <p className="m-0 text-xs text-muted">Jot down important things.</p>
              </div>
              <div className="mt-4 rounded-lg border border-border bg-bg/60 p-4">
                <h4 className="m-0 text-sm font-bold">{workspaceData.quickNote.title}</h4>
                <p className="m-0 mt-1 text-xs text-muted">{workspaceData.quickNote.desc}</p>
                <div className="mt-3 flex flex-col gap-2">
                  {workspaceData.quickNote.items.map((item, i) => (
                    <div key={i} className="flex items-center gap-2 text-sm">
                      <span className={`inline-block h-3.5 w-3.5 rounded-sm border ${item.done ? 'border-success bg-success/20' : 'border-border'}`} />
                      <span className={item.done ? 'text-muted line-through' : ''}>{item.text}</span>
                    </div>
                  ))}
                </div>
                <div className="mt-3 text-[11px] text-muted">Updated {workspaceData.quickNote.updatedAt}</div>
              </div>
              <button className="mt-4 flex w-full items-center justify-center gap-1.5 rounded-lg bg-accent px-4 py-2.5 text-sm font-semibold text-[#070a12] transition hover:opacity-90">
                <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/></svg>
                New Note
              </button>
            </div>
          </div>
        </div>
      </main>
    </div>
  )
}

/* Info row for hero card */
function InfoRow({ icon, label, value, link }: { icon: React.ReactNode; label: string; value: React.ReactNode; link?: boolean }) {
  return (
    <div className="flex items-start gap-2">
      <div className="mt-0.5 text-muted">{icon}</div>
      <div>
        <div className="text-[11px] text-muted">{label}</div>
        {link ? (
          <a href={`https://github.com/${value}`} target="_blank" rel="noreferrer" className="text-sm font-medium text-accent no-underline">{value} ↗</a>
        ) : (
          <div className="text-sm font-medium">{value}</div>
        )}
      </div>
    </div>
  )
}

/* Icons */
function CalendarIcon() {
  return <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2"><rect x="3" y="4" width="18" height="18" rx="2" ry="2"/><line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/></svg>
}

function GitIcon() {
  return <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2"><line x1="6" y1="3" x2="6" y2="15"/><circle cx="18" cy="6" r="3"/><circle cx="6" cy="18" r="3"/><path d="M18 9a9 9 0 0 1-9 9"/></svg>
}

function StackIcon() {
  return <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2"><rect x="2" y="3" width="20" height="14" rx="2" ry="2"/><line x1="8" y1="21" x2="16" y2="21"/><line x1="12" y1="17" x2="12" y2="21"/></svg>
}

function EyeIcon() {
  return <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg>
}

function BrainIcon() {
  return <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2"><path d="M12 2a10 10 0 1 0 10 10A10 10 0 0 0 12 2z"/><path d="M12 6a6 6 0 1 0 6 6 6 6 0 0 0-6-6z"/></svg>
}

function ActivityIcon() {
  return <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2"><polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/></svg>
}

function NoteIcon() {
  return <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>
}

function BrainIconSmall() {
  return <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><path d="M12 2a10 10 0 1 0 10 10A10 10 0 0 0 12 2z"/><path d="M12 6a6 6 0 1 0 6 6 6 6 0 0 0-6-6z"/></svg>
}

function CheckIconSmall() {
  return <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>
}

function TaskIconSmall() {
  return <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><path d="M9 11l3 3L22 4"/><path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11"/></svg>
}

function GitIconSmall() {
  return <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><line x1="6" y1="3" x2="6" y2="15"/><circle cx="18" cy="6" r="3"/><circle cx="6" cy="18" r="3"/><path d="M18 9a9 9 0 0 1-9 9"/></svg>
}

function NoteIconSmall() {
  return <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>
}
