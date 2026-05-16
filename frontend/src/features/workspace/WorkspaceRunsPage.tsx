import { useState } from 'react'
import { WorkspaceSidebar } from './WorkspaceSidebar'

const stats = [
  { label: 'Environments', value: '3', sub: '2 healthy', icon: 'env' },
  { label: 'Total Runs', value: '18', sub: 'All time', icon: 'runs' },
  { label: 'Success Rate', value: '83%', sub: '15 successful', icon: 'success' },
  { label: 'Avg. Uptime', value: '99.2%', sub: 'Last 7 days', icon: 'uptime' },
]

const environments = [
  { name: 'Production Preview', url: 'https://codecafe.app', icon: '🌐', iconColor: 'text-accent', uptime: '99.9%', deployedAt: 'May 10, 2024 10:32 AM', latestRun: '#18' },
  { name: 'Staging Preview', url: 'https://staging.codecafe.app', icon: '📦', iconColor: 'text-accent', uptime: '98.7%', deployedAt: 'May 9, 2024 08:15 PM', latestRun: '#17' },
  { name: 'Feature Preview', url: 'feat/auth-improvements', icon: '🔀', iconColor: 'text-warning', uptime: '85.1%', deployedAt: 'May 8, 2024 03:21 PM', latestRun: '#15' },
]

const runHistory = [
  { id: 18, env: 'Production Preview', envIcon: '🌐', status: 'Success', commit: 'a1b2c3d', branch: 'main', triggeredBy: 'Rio', startedAt: 'May 10, 2024 10:32 AM', duration: '2m 34s' },
  { id: 17, env: 'Staging Preview', envIcon: '📦', status: 'Success', commit: 'd4e5f6g', branch: 'main', triggeredBy: 'Rio', startedAt: 'May 9, 2024 08:15 PM', duration: '3m 12s' },
  { id: 16, env: 'Production Preview', envIcon: '🌐', status: 'Failed', commit: 'f7g8h9i', branch: 'main', triggeredBy: 'Rio', startedAt: 'May 9, 2024 07:42 PM', duration: '1m 45s' },
  { id: 15, env: 'Feature Preview', envIcon: '🔀', status: 'Success', commit: 'j1k2l3m', branch: 'feat/auth-improvements', triggeredBy: 'Rio', startedAt: 'May 8, 2024 03:21 PM', duration: '2m 08s' },
  { id: 14, env: 'Staging Preview', envIcon: '📦', status: 'Success', commit: 'n4o5p6q', branch: 'main', triggeredBy: 'Rio', startedAt: 'May 8, 2024 11:09 AM', duration: '2m 56s' },
]

const deploymentSteps = [
  { name: 'Preparing environment', time: '12s', done: true },
  { name: 'Installing dependencies', time: '45s', done: true },
  { name: 'Building application', time: '1m 02s', done: true },
  { name: 'Running tests', time: '18s', done: true },
  { name: 'Deploying to preview', time: '22s', done: true },
  { name: 'Health check', time: '15s', done: true },
  { name: 'Deployment successful', time: '10:34:24 AM', done: true },
]

const logs = [
  { time: '10:32:11', level: 'INFO', message: 'Starting deployment...' },
  { time: '10:32:12', level: 'INFO', message: 'Preparing environment' },
  { time: '10:32:24', level: 'INFO', message: 'Installing dependencies' },
  { time: '10:32:56', level: 'INFO', message: 'Building application' },
  { time: '10:33:58', level: 'INFO', message: 'Running tests' },
  { time: '10:34:16', level: 'INFO', message: 'Deploying to preview' },
  { time: '10:34:21', level: 'INFO', message: 'Health check passed' },
  { time: '10:34:24', level: 'INFO', message: 'Deployment successful' },
]

export function WorkspaceRunsPage() {
  const [selectedRun, setSelectedRun] = useState(runHistory[0])
  const [detailOpen, setDetailOpen] = useState(true)

  const selectedRunData = runHistory.find((r) => r.id === selectedRun.id) ?? runHistory[0]

  return (
    <div className="flex min-h-screen bg-bg text-text">
      <WorkspaceSidebar activeItem="Runs" />

      <div className="flex flex-1 flex-col">
        {/* Header */}
        <header className="flex items-center justify-between border-b border-border px-6 py-4">
          <div>
            <h1 className="m-0 text-2xl font-bold tracking-tight">Runs</h1>
            <p className="m-0 mt-1 text-sm text-muted">Manage and monitor your preview environments and run history.</p>
          </div>
          <div className="flex items-center gap-3">
            <button className="inline-flex items-center gap-1.5 rounded-lg bg-accent px-4 py-2 text-sm font-semibold text-[#070a12] transition hover:opacity-90">
              <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/></svg>
              New Run
            </button>
            <button className="inline-flex items-center gap-1.5 rounded-lg border border-border px-3 py-2 text-sm font-medium text-muted transition hover:text-text">
              <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="12" cy="12" r="10"/><path d="M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>
              How runs work?
            </button>
          </div>
        </header>

        {/* Content */}
        <div className="flex flex-1 overflow-hidden">
          {/* Main */}
          <div className="flex flex-1 flex-col overflow-auto p-6">
            {/* Stats */}
            <div className="mb-6 grid grid-cols-2 gap-4 lg:grid-cols-4">
              {stats.map((s, i) => (
                <div key={i} className="flex items-center gap-3 rounded-xl border border-border bg-surface/40 p-4">
                  <div className="grid h-10 w-10 shrink-0 place-items-center rounded-lg border border-border bg-bg/60 text-muted">
                    {s.icon === 'env' && <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2"><rect x="2" y="3" width="20" height="14" rx="2" ry="2"/><line x1="8" y1="21" x2="16" y2="21"/><line x1="12" y1="17" x2="12" y2="21"/></svg>}
                    {s.icon === 'runs' && <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2"><polygon points="5 3 19 12 5 21 5 3"/></svg>}
                    {s.icon === 'success' && <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>}
                    {s.icon === 'uptime' && <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>}
                  </div>
                  <div>
                    <div className="text-[11px] text-muted">{s.label}</div>
                    <div className="text-xl font-bold">{s.value}</div>
                    <div className={`text-[11px] ${s.icon === 'success' ? 'text-success' : 'text-muted'}`}>{s.sub}</div>
                  </div>
                </div>
              ))}
            </div>

            {/* Environments */}
            <div className="mb-6 rounded-xl border border-border bg-surface/40 p-5">
              <div className="mb-4 flex items-center justify-between">
                <h2 className="m-0 text-base font-bold">Environments</h2>
                <button className="inline-flex items-center gap-1.5 rounded-lg border border-border px-3 py-1.5 text-xs font-medium text-muted transition hover:text-text">
                  <svg viewBox="0 0 24 24" width="12" height="12" fill="none" stroke="currentColor" strokeWidth="2"><rect x="2" y="3" width="20" height="14" rx="2" ry="2"/><line x1="8" y1="21" x2="16" y2="21"/><line x1="12" y1="17" x2="12" y2="21"/></svg>
                  Manage Environments
                </button>
              </div>
              <div className="flex flex-col gap-3">
                {environments.map((env, i) => (
                  <div key={i} className="flex items-center gap-4 rounded-lg border border-border bg-bg/60 px-4 py-3">
                    <div className={`grid h-10 w-10 shrink-0 place-items-center rounded-lg bg-accent/10 text-lg ${env.iconColor}`}>
                      {env.icon}
                    </div>
                    <div className="min-w-0 flex-1">
                      <div className="text-sm font-semibold">{env.name}</div>
                      <a href={`https://${env.url.replace(/^https?:\/\//, '')}`} target="_blank" rel="noreferrer" className="inline-flex items-center gap-1 text-xs text-accent no-underline">
                        {env.url} ↗
                      </a>
                    </div>
                    <div className="hidden min-w-[80px] text-center sm:block">
                      <div className="text-[11px] text-muted">Uptime</div>
                      <div className="text-sm font-bold">{env.uptime}</div>
                    </div>
                    <div className="hidden min-w-[140px] text-center md:block">
                      <div className="text-[11px] text-muted">Last Deployed</div>
                      <div className="text-xs">{env.deployedAt}</div>
                    </div>
                    <div className="hidden min-w-[60px] text-center sm:block">
                      <div className="text-[11px] text-muted">Latest Run</div>
                      <button onClick={() => { setSelectedRun(runHistory.find((r) => r.id === parseInt(env.latestRun.replace('#', ''))) ?? runHistory[0]); setDetailOpen(true) }} className="text-xs font-bold text-accent">{env.latestRun}</button>
                    </div>
                    <div className="flex items-center gap-2">
                      <button className="rounded-lg border border-border bg-bg/60 px-3 py-1.5 text-xs font-medium transition hover:bg-accent/8">View Logs</button>
                      <button className="inline-flex h-8 w-8 items-center justify-center rounded-lg border border-border text-muted transition hover:text-text">
                        <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="12" cy="12" r="1"/><circle cx="19" cy="12" r="1"/><circle cx="5" cy="12" r="1"/></svg>
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            </div>

            {/* Run History */}
            <div className="rounded-xl border border-border bg-surface/40 p-5">
              <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                <h2 className="m-0 text-base font-bold">Run History</h2>
                <div className="flex items-center gap-2">
                  <div className="flex items-center gap-2 rounded-lg border border-border bg-bg/60 px-3 py-1.5">
                    <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/></svg>
                    <input type="text" placeholder="Search runs..." className="border-0 bg-transparent text-sm text-text outline-none placeholder:text-muted" />
                  </div>
                  <button className="rounded-lg border border-border bg-bg/60 px-3 py-1.5 text-xs font-medium text-muted">All Environments</button>
                  <button className="rounded-lg border border-border bg-bg/60 px-3 py-1.5 text-xs font-medium text-muted">All Statuses</button>
                </div>
              </div>

              <div className="overflow-x-auto">
                <table className="w-full text-left text-sm">
                  <thead>
                    <tr className="border-b border-border text-xs text-muted">
                      <th className="pb-2 pr-4 font-medium">Run</th>
                      <th className="pb-2 pr-4 font-medium">Environment</th>
                      <th className="pb-2 pr-4 font-medium">Status</th>
                      <th className="pb-2 pr-4 font-medium">Commit</th>
                      <th className="pb-2 pr-4 font-medium">Triggered By</th>
                      <th className="pb-2 pr-4 font-medium">Started At</th>
                      <th className="pb-2 pr-4 font-medium">Duration</th>
                      <th className="pb-2 font-medium"></th>
                    </tr>
                  </thead>
                  <tbody>
                    {runHistory.map((run) => (
                      <tr
                        key={run.id}
                        className={`cursor-pointer border-b border-border transition-colors ${selectedRun.id === run.id ? 'bg-accent/5' : 'hover:bg-accent/4'}`}
                        onClick={() => { setSelectedRun(run); setDetailOpen(true) }}
                      >
                        <td className="py-3 pr-4">
                          <div className="flex items-center gap-2">
                            <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><polygon points="5 3 19 12 5 21 5 3"/></svg>
                            <span className="font-semibold">#{run.id}</span>
                          </div>
                        </td>
                        <td className="py-3 pr-4">
                          <div className="flex items-center gap-2">
                            <span>{run.envIcon}</span>
                            <span>{run.env}</span>
                          </div>
                        </td>
                        <td className="py-3 pr-4">
                          <span className={`inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-[10px] font-bold ${
                            run.status === 'Success' ? 'border-success/20 bg-success/8 text-success' : 'border-danger/20 bg-danger/8 text-danger'
                          }`}>
                            <span className={`h-1.5 w-1.5 rounded-full ${run.status === 'Success' ? 'bg-success' : 'bg-danger'}`} />
                            {run.status}
                          </span>
                        </td>
                        <td className="py-3 pr-4">
                          <div className="font-mono text-xs text-accent">{run.commit}</div>
                          <div className="flex items-center gap-1 text-[11px] text-muted">
                            <svg viewBox="0 0 24 24" width="10" height="10" fill="none" stroke="currentColor" strokeWidth="2"><line x1="6" y1="3" x2="6" y2="15"/><circle cx="18" cy="6" r="3"/><circle cx="6" cy="18" r="3"/><path d="M18 9a9 9 0 0 1-9 9"/></svg>
                            {run.branch}
                          </div>
                        </td>
                        <td className="py-3 pr-4">
                          <div className="flex items-center gap-2">
                            <div className="h-5 w-5 overflow-hidden rounded-full">
                              <img src="https://github.com/rio-csharp.png" alt="Rio" className="h-full w-full object-cover" />
                            </div>
                            <span>{run.triggeredBy}</span>
                          </div>
                        </td>
                        <td className="py-3 pr-4 text-xs text-muted">{run.startedAt}</td>
                        <td className="py-3 pr-4 text-xs text-muted">{run.duration}</td>
                        <td className="py-3">
                          <button className="text-muted transition hover:text-text">
                            <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="12" cy="12" r="1"/><circle cx="19" cy="12" r="1"/><circle cx="5" cy="12" r="1"/></svg>
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {/* Pagination */}
              <div className="mt-4 flex items-center justify-center gap-1">
                <button className="inline-flex h-8 w-8 items-center justify-center rounded-lg border border-border text-muted transition hover:text-text">
                  <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><polyline points="15 18 9 12 15 6"/></svg>
                </button>
                <button className="inline-flex h-8 w-8 items-center justify-center rounded-lg bg-accent text-sm font-bold text-[#070a12]">1</button>
                <button className="inline-flex h-8 w-8 items-center justify-center rounded-lg border border-border text-sm text-muted transition hover:text-text">2</button>
                <button className="inline-flex h-8 w-8 items-center justify-center rounded-lg border border-border text-sm text-muted transition hover:text-text">3</button>
                <span className="px-1 text-muted">...</span>
                <button className="inline-flex h-8 w-8 items-center justify-center rounded-lg border border-border text-muted transition hover:text-text">
                  <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><polyline points="9 18 15 12 9 6"/></svg>
                </button>
              </div>
            </div>
          </div>

          {/* Right detail panel */}
          {detailOpen && (
            <aside className="flex w-[320px] shrink-0 flex-col gap-5 overflow-auto border-l border-border bg-bg/40 p-5">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <span className="text-sm font-bold">Run #{selectedRunData.id}</span>
                  <span className={`rounded-full border px-2 py-0.5 text-[10px] font-bold ${
                    selectedRunData.status === 'Success' ? 'border-success/20 bg-success/8 text-success' : 'border-danger/20 bg-danger/8 text-danger'
                  }`}>{selectedRunData.status}</span>
                </div>
                <button onClick={() => setDetailOpen(false)} className="text-muted transition hover:text-text">
                  <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
                </button>
              </div>

              {/* Overview */}
              <div>
                <h3 className="m-0 mb-3 text-xs font-bold text-muted uppercase">Overview</h3>
                <div className="flex flex-col gap-2.5 text-xs">
                  <div className="flex justify-between"><span className="text-muted">Environment</span><span className="flex items-center gap-1"><span>{selectedRunData.envIcon}</span>{selectedRunData.env}</span></div>
                  <div className="flex justify-between"><span className="text-muted">URL</span><a href="#" className="text-accent no-underline">https://codecafe.app ↗</a></div>
                  <div className="flex justify-between"><span className="text-muted">Commit</span><span className="font-mono text-accent">{selectedRunData.commit}</span></div>
                  <div className="flex justify-between"><span className="text-muted">Triggered By</span><span className="flex items-center gap-1"><img src="https://github.com/rio-csharp.png" alt="" className="h-4 w-4 rounded-full" />{selectedRunData.triggeredBy}</span></div>
                  <div className="flex justify-between"><span className="text-muted">Started At</span><span>{selectedRunData.startedAt}</span></div>
                  <div className="flex justify-between"><span className="text-muted">Duration</span><span>{selectedRunData.duration}</span></div>
                </div>
              </div>

              {/* Deployment Steps */}
              <div>
                <h3 className="m-0 mb-3 text-xs font-bold text-muted uppercase">Deployment Steps</h3>
                <div className="flex flex-col gap-2">
                  {deploymentSteps.map((step, i) => (
                    <div key={i} className="flex items-center justify-between text-xs">
                      <div className="flex items-center gap-2">
                        <span className="grid h-4 w-4 place-items-center rounded-full bg-success/10 text-success">
                          <svg viewBox="0 0 24 24" width="10" height="10" fill="none" stroke="currentColor" strokeWidth="3"><polyline points="20 6 9 17 4 12"/></svg>
                        </span>
                        <span>{step.name}</span>
                      </div>
                      <span className="text-muted">{step.time}</span>
                    </div>
                  ))}
                </div>
              </div>

              {/* Logs */}
              <div>
                <div className="mb-3 flex items-center justify-between">
                  <h3 className="m-0 text-xs font-bold text-muted uppercase">Logs</h3>
                  <a href="#" className="text-xs font-medium text-accent no-underline">View full logs</a>
                </div>
                <div className="rounded-lg border border-border bg-[#0a0e1a] p-3 font-mono text-[11px] leading-relaxed">
                  {logs.map((log, i) => (
                    <div key={i} className="flex gap-2">
                      <span className="text-muted/60">{log.time}</span>
                      <span className="text-success">{log.level}</span>
                      <span className="text-[#c9d1d9]">{log.message}</span>
                    </div>
                  ))}
                </div>
              </div>

              <button className="mt-auto flex w-full items-center justify-center gap-2 rounded-lg border border-border bg-bg/60 px-4 py-2.5 text-sm font-medium transition hover:bg-accent/8">
                <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><polygon points="5 3 19 12 5 21 5 3"/></svg>
                Re-run this deployment
              </button>
            </aside>
          )}
        </div>
      </div>
    </div>
  )
}
