import { Link } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'

function LogoIcon({ size = 28 }: { size?: number }) {
  return (
    <svg aria-hidden="true" viewBox="0 0 32 32" width={size} height={size}>
      <rect width="32" height="32" rx="7" fill="url(#logo-grad)" />
      <path d="M10 10 L6 16 L10 22" stroke="white" strokeWidth="2.2" fill="none" strokeLinecap="round" strokeLinejoin="round" />
      <path d="M22 10 L26 16 L22 22" stroke="white" strokeWidth="2.2" fill="none" strokeLinecap="round" strokeLinejoin="round" />
      <path d="M14 22 L18 10" stroke="white" strokeWidth="2.2" fill="none" strokeLinecap="round" strokeLinejoin="round" />
      <defs>
        <linearGradient id="logo-grad" x1="0" y1="0" x2="32" y2="32">
          <stop stopColor="#38bdf8" />
          <stop offset="1" stopColor="#818cf8" />
        </linearGradient>
      </defs>
    </svg>
  )
}

function ArrowRightIcon({ size = 18 }: { size?: number }) {
  return (
    <svg aria-hidden="true" viewBox="0 0 24 24" width={size} height={size}>
      <path d="M5 12h14M12 5l7 7-7 7" stroke="currentColor" strokeWidth="2" fill="none" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  )
}

export function LandingPage() {
  const auth = useAuth()
  const isAuthenticated = auth.status === 'authenticated'

  return (
    <div className="min-h-screen text-text bg-bg">
      {/* Header */}
      <header className="sticky top-0 z-50 border-b border-border bg-bg/82 backdrop-blur-xl">
        <div className="mx-auto flex max-w-[1200px] items-center justify-between gap-6 px-6 py-3.5">
          <Link className="inline-flex items-center gap-2.5 text-lg font-bold text-text no-underline" to="/">
            <LogoIcon />
            <span>CodeCafe</span>
          </Link>

          <nav className="hidden items-center gap-7 md:flex" aria-label="Primary">
            <a href="#features" className="text-sm font-medium text-muted no-underline transition-colors hover:text-text">Features</a>
            <a href="#how-it-works" className="text-sm font-medium text-muted no-underline transition-colors hover:text-text">How It Works</a>
            <a href="#pricing" className="text-sm font-medium text-muted no-underline transition-colors hover:text-text">Pricing</a>
            <a href="#docs" className="text-sm font-medium text-muted no-underline transition-colors hover:text-text">Docs</a>
            <Link to="/about" className="text-sm font-medium text-muted no-underline transition-colors hover:text-text">About</Link>
          </nav>

          <div className="flex items-center gap-3">
            {isAuthenticated ? (
              <div className="flex items-center gap-2">
                <div className="h-8 w-8 overflow-hidden rounded-full border border-border bg-accent/10">
                  <img
                    src="https://github.com/rio-csharp.png"
                    alt={auth.username ?? 'User'}
                    className="h-full w-full object-cover"
                    onError={(e) => { (e.target as HTMLImageElement).style.display = 'none' }}
                  />
                </div>
                <span className="hidden text-sm font-medium sm:inline">{auth.username}</span>
                <button
                  className="rounded-lg border border-border px-3 py-1.5 text-xs font-semibold text-muted transition-colors hover:border-accent/40 hover:bg-accent/8 hover:text-text"
                  onClick={() => void auth.logout()}
                  type="button"
                >
                  Sign out
                </button>
              </div>
            ) : (
              <Link
                className="rounded-lg border border-border px-4 py-2 text-sm font-semibold text-text no-underline transition-colors hover:border-accent/40 hover:bg-accent/8"
                to="/login"
              >
                Sign in
              </Link>
            )}
            <Link
              className="rounded-lg bg-gradient-to-br from-accent to-[#818cf8] px-[18px] py-2 text-sm font-semibold text-[#070a12] no-underline transition hover:opacity-92 hover:-translate-y-px"
              to="/workspaces"
            >
              Get Started
            </Link>
          </div>
        </div>
      </header>

      <main>
        {/* Hero */}
        <section className="mx-auto grid max-w-[900px] place-items-center gap-5 px-6 pb-12 pt-16 text-center">
          <div className="inline-flex items-center gap-2 rounded-full border border-accent/24 bg-accent/8 px-3.5 py-1.5 text-xs font-bold tracking-widest text-accent uppercase">
            <span className="text-sm">✦</span>
            AI-NATIVE ENGINEERING WORKSPACE
          </div>

          <h1 className="m-0 text-[clamp(36px,6vw,64px)] font-extrabold leading-[1.1] tracking-tight">
            Build. Run. Remember.
            <br />
            <span className="bg-gradient-to-r from-[#a78bfa] to-accent bg-clip-text text-transparent">
              Evolve with AI.
            </span>
          </h1>

          <p className="m-0 max-w-[640px] text-[17px] leading-relaxed text-muted">
            CodeCafe is an AI-native engineering workspace with persistent project memory.
            <br />
            Understand your codebase, automate tasks, and run projects safely in isolated environments.
          </p>

          <div className="mt-2 flex flex-wrap items-center justify-center gap-3.5">
            <Link
              className="inline-flex items-center gap-2 rounded-xl bg-gradient-to-br from-accent to-[#818cf8] px-6 py-3 text-[15px] font-bold text-[#070a12] no-underline transition hover:opacity-92 hover:-translate-y-px"
              to="/workspaces"
            >
              Get Started
              <ArrowRightIcon />
            </Link>
            <Link
              className="rounded-xl border border-border-strong bg-surface/60 px-6 py-3 text-[15px] font-semibold text-text no-underline transition hover:border-accent/50 hover:bg-accent/10"
              to="/workspaces/codecafe"
            >
              Explore Demo Workspace
            </Link>
          </div>
        </section>

        {/* Preview */}
        <section className="px-6 pb-16" aria-label="Workspace preview">
          <div className="mx-auto max-w-[1100px] overflow-hidden rounded-xl border border-border-strong bg-surface shadow-card shadow-glow">
            <div className="flex items-center gap-2 border-b border-border bg-bg/72 px-3.5 py-2.5">
              <div className="h-2.5 w-2.5 rounded-full bg-[#ff5f57]" />
              <div className="h-2.5 w-2.5 rounded-full bg-[#febc2e]" />
              <div className="h-2.5 w-2.5 rounded-full bg-[#28c840]" />
              <span className="ml-1.5 text-xs font-semibold text-muted">Workspace: CodeCafe</span>
            </div>

            <div className="grid min-h-[420px] grid-cols-[180px_1fr]">
              {/* Sidebar */}
              <div className="flex flex-col gap-4 border-r border-border bg-bg/72 p-3.5">
                <div className="flex items-center gap-2 text-sm font-bold">
                  <LogoIcon size={18} />
                  <span>CodeCafe</span>
                </div>
                <div className="flex flex-col gap-1">
                  <div className="px-1.5 py-1 text-[10px] font-bold tracking-widest text-muted uppercase">Workspaces</div>
                  <div className="flex items-center gap-2 rounded-md bg-accent/10 px-2 py-1.5 text-xs text-text">
                    <span className="inline-flex h-[18px] w-[18px] items-center justify-center rounded bg-accent/20 text-[10px] font-bold">C</span>
                    CodeCafe
                  </div>
                  <div className="flex items-center gap-2 rounded-md px-2 py-1.5 text-xs text-muted">
                    <span className="inline-flex h-[18px] w-[18px] items-center justify-center rounded bg-accent/20 text-[10px] font-bold">T</span>
                    TinyCivilization
                  </div>
                  <div className="flex items-center gap-2 rounded-md px-2 py-1.5 text-xs text-muted">
                    <span className="inline-flex h-[18px] w-[18px] items-center justify-center rounded bg-accent/20 text-[10px] font-bold">I</span>
                    Interview Prep
                  </div>
                </div>
                <div className="flex flex-col gap-1">
                  <div className="px-1.5 py-1 text-[10px] font-bold tracking-widest text-muted uppercase">Navigation</div>
                  <div className="flex items-center gap-2 rounded-md px-2 py-1.5 text-xs text-muted">
                    <svg viewBox="0 0 24 24" width="14" height="14"><circle cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="2" fill="none"/></svg>
                    Overview
                  </div>
                  <div className="flex items-center gap-2 rounded-md px-2 py-1.5 text-xs text-muted">
                    <svg viewBox="0 0 24 24" width="14" height="14"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z" stroke="currentColor" strokeWidth="2" fill="none"/></svg>
                    Chat
                  </div>
                  <div className="flex items-center gap-2 rounded-md px-2 py-1.5 text-xs text-muted">
                    <svg viewBox="0 0 24 24" width="14" height="14"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" stroke="currentColor" strokeWidth="2" fill="none"/><polyline points="14 2 14 8 20 8" stroke="currentColor" strokeWidth="2" fill="none"/></svg>
                    Code
                  </div>
                </div>
              </div>

              {/* Main */}
              <div className="flex flex-col gap-3.5 overflow-hidden p-3.5">
                <div className="flex items-center justify-between gap-3">
                  <div className="inline-flex items-center gap-1.5 rounded-md border border-success/20 bg-success/8 px-2.5 py-1 text-xs font-semibold text-success">
                    <span className="h-1.5 w-1.5 rounded-full bg-success" />
                    Active
                  </div>
                  <div className="text-xs text-muted">Last active 2m ago</div>
                </div>

                <div className="grid grid-cols-2 gap-3">
                  <div className="flex flex-col gap-2.5 rounded-lg border border-border bg-bg/60 p-3">
                    <div className="flex items-center gap-2 text-[11px] font-bold tracking-wider text-muted uppercase">
                      <span className="inline-flex h-[18px] w-[18px] items-center justify-center rounded-full border border-accent/30 text-[10px] font-bold text-accent">1</span>
                      Project Overview
                    </div>
                    <div className="flex items-center gap-2.5">
                      <div className="grid h-9 w-9 place-items-center rounded-lg border border-accent/20 bg-accent/8 text-sm font-bold text-accent">{'</>'}</div>
                      <div>
                        <div className="text-sm font-bold">CodeCafe</div>
                        <div className="text-[11px] leading-snug text-muted">AI-native engineering workspace with persistent project memory.</div>
                      </div>
                    </div>
                  </div>

                  <div className="flex flex-col gap-2.5 rounded-lg border border-border bg-bg/60 p-3">
                    <div className="flex items-center gap-2 text-[11px] font-bold tracking-wider text-muted uppercase">
                      <span className="inline-flex h-[18px] w-[18px] items-center justify-center rounded-full border border-accent/30 text-[10px] font-bold text-accent">2</span>
                      Current Tasks
                    </div>
                    <div className="flex flex-col gap-1.5">
                      <div className="flex items-center gap-2 text-xs">
                        <span className="h-3 w-3 rounded-sm border border-border" />
                        <span>Add workspace persistence</span>
                        <span className="ml-auto rounded px-2 py-0.5 text-[10px] font-bold text-warning border border-warning/20 bg-warning/8">In Progress</span>
                      </div>
                      <div className="flex items-center gap-2 text-xs">
                        <span className="h-3 w-3 rounded-sm border border-border" />
                        <span>Add run logs viewer</span>
                        <span className="ml-auto rounded px-2 py-0.5 text-[10px] font-bold text-muted border border-border bg-bg/50">To Do</span>
                      </div>
                      <div className="flex items-center gap-2 text-xs">
                        <span className="h-3 w-3 rounded-sm border border-border" />
                        <span>Improve memory summarization</span>
                        <span className="ml-auto rounded px-2 py-0.5 text-[10px] font-bold text-muted border border-border bg-bg/50">To Do</span>
                      </div>
                    </div>
                  </div>

                  <div className="col-span-2 flex flex-col gap-2.5 rounded-lg border border-border bg-bg/60 p-3">
                    <div className="flex items-center gap-2 text-[11px] font-bold tracking-wider text-muted uppercase">
                      <span className="inline-flex h-[18px] w-[18px] items-center justify-center rounded-full border border-accent/30 text-[10px] font-bold text-accent">3</span>
                      Workspace Memory
                    </div>
                    <div className="grid grid-cols-3 gap-4">
                      <div>
                        <div className="mb-1.5 text-[11px] font-bold text-muted">Recent Decisions</div>
                        <ul className="m-0 list-disc pl-3.5 text-[11px] leading-relaxed text-muted">
                          <li>Keep monorepo structure</li>
                          <li>Use guest anonymous workspaces</li>
                          <li>Only support safe template runs</li>
                        </ul>
                      </div>
                      <div>
                        <div className="mb-1.5 text-[11px] font-bold text-muted">Known Architecture</div>
                        <ul className="m-0 list-disc pl-3.5 text-[11px] leading-relaxed text-muted">
                          <li>ASP.NET Core backend</li>
                          <li>React + Vite frontend</li>
                          <li>AI orchestration layer</li>
                        </ul>
                      </div>
                      <div>
                        <div className="mb-1.5 text-[11px] font-bold text-muted">Recent Topics</div>
                        <ul className="m-0 list-disc pl-3.5 text-[11px] leading-relaxed text-muted">
                          <li>sandbox security</li>
                          <li>GitHub integration</li>
                          <li>context persistence</li>
                        </ul>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </section>

        {/* Features */}
        <section className="px-6 py-16" id="features" aria-label="Features">
          <div className="mx-auto grid max-w-[1100px] grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-4">
            <FeatureCard
              icon={<MemoryIcon />}
              title="Persistent Project Memory"
              desc="AI remembers your decisions, architecture, and context across sessions and workspaces."
            />
            <FeatureCard
              icon={<CodebaseIcon />}
              title="AI-Aware Codebase"
              desc="Ask questions, get explanations, and receive intelligent suggestions based on your entire codebase."
            />
            <FeatureCard
              icon={<PreviewIcon />}
              title="Safe Preview Environments"
              desc="Run and preview your projects in isolated, secure environments with real-time logs and metrics."
            />
            <FeatureCard
              icon={<WorkflowIcon />}
              title="Engineering Workflow"
              desc="From tasks to code to runs, everything is connected in one AI-native workspace."
            />
          </div>
        </section>

        {/* Trusted by */}
        <section className="px-6 py-12 text-center" aria-label="Trusted by">
          <p className="m-0 mb-6 text-xs font-bold tracking-widest text-muted uppercase">Trusted by developers and teams</p>
          <div className="mx-auto flex max-w-[800px] flex-wrap items-center justify-center gap-8">
            <TrustedLogo color="#512bd4" label="ASP.NET Core" text=".NET" />
            <TrustedLogo color="#61dafb" label="React" text="React" textColor="#0f172a" />
            <TrustedLogo color="#3178c6" label="TypeScript" text="TS" />
            <TrustedLogo color="#2496ed" label="Docker" text="Docker" />
            <TrustedLogo color="#181717" label="GitHub" text="Git" />
            <TrustedLogo icon={<ViteIcon />} label="Vite" />
          </div>
        </section>

        {/* CTA */}
        <section className="mx-auto grid max-w-[700px] place-items-center gap-4 px-6 py-20 text-center" aria-label="Call to action">
          <h2 className="m-0 text-[clamp(24px,4vw,36px)] font-bold leading-snug">
            Your AI-native engineering workspace is just one click away.
          </h2>
          <Link
            className="inline-flex items-center gap-2 rounded-xl bg-gradient-to-br from-accent to-[#818cf8] px-7 py-3.5 text-base font-bold text-[#070a12] no-underline transition hover:opacity-92 hover:-translate-y-px"
            to="/workspaces"
          >
            Get Started for Free
            <ArrowRightIcon />
          </Link>
          <p className="m-0 text-sm text-muted">No credit card required</p>
        </section>
      </main>

      {/* Footer */}
      <footer className="border-t border-border bg-bg/60">
        <div className="mx-auto flex max-w-[1100px] items-center justify-between gap-4 px-6 py-6">
          <div className="flex items-center gap-2.5 text-[15px] font-bold">
            <LogoIcon size={22} />
            <span>CodeCafe</span>
          </div>
          <p className="m-0 text-sm text-muted">© 2026 CodeCafe. All rights reserved.</p>
        </div>
      </footer>
    </div>
  )
}

function FeatureCard({ icon, title, desc }: { icon: React.ReactNode; title: string; desc: string }) {
  return (
    <div className="flex flex-col gap-3.5 rounded-xl border border-border bg-bg/50 p-6 transition hover:border-accent/25 hover:bg-accent/6 hover:-translate-y-0.5">
      <div className="grid h-11 w-11 place-items-center rounded-lg border border-accent/20 bg-accent/8 text-accent">
        {icon}
      </div>
      <h3 className="m-0 text-base font-bold">{title}</h3>
      <p className="m-0 text-sm leading-relaxed text-muted">{desc}</p>
    </div>
  )
}

function TrustedLogo({ color, label, text, textColor = 'white', icon }: { color?: string; label: string; text?: string; textColor?: string; icon?: React.ReactNode }) {
  return (
    <div className="flex items-center gap-2 text-sm font-semibold text-muted">
      {icon ?? (
        <svg viewBox="0 0 24 24" width="24" height="24">
          <circle cx="12" cy="12" r="10" fill={color} />
          {text && <text x="12" y="16" textAnchor="middle" fill={textColor} fontSize="10" fontWeight="bold">{text}</text>}
        </svg>
      )}
      <span>{label}</span>
    </div>
  )
}

function MemoryIcon() {
  return (
    <svg aria-hidden="true" viewBox="0 0 24 24" width="24" height="24">
      <path d="M12 2a10 10 0 1 0 10 10A10 10 0 0 0 12 2zm0 18a8 8 0 1 1 8-8 8 8 0 0 1-8 8z" fill="currentColor"/>
      <path d="M12 6a6 6 0 1 0 6 6 6 6 0 0 0-6-6zm0 10a4 4 0 1 1 4-4 4 4 0 0 1-4 4z" fill="currentColor"/>
    </svg>
  )
}

function CodebaseIcon() {
  return (
    <svg aria-hidden="true" viewBox="0 0 24 24" width="24" height="24">
      <path d="M16 18l6-6-6-6" stroke="currentColor" strokeWidth="2" fill="none" strokeLinecap="round" strokeLinejoin="round"/>
      <path d="M8 6l-6 6 6 6" stroke="currentColor" strokeWidth="2" fill="none" strokeLinecap="round" strokeLinejoin="round"/>
    </svg>
  )
}

function PreviewIcon() {
  return (
    <svg aria-hidden="true" viewBox="0 0 24 24" width="24" height="24">
      <polygon points="5 3 19 12 5 21 5 3" fill="currentColor"/>
    </svg>
  )
}

function WorkflowIcon() {
  return (
    <svg aria-hidden="true" viewBox="0 0 24 24" width="24" height="24">
      <path d="M12 2L2 7l10 5 10-5-10-5z" fill="currentColor"/>
      <path d="M2 17l10 5 10-5" stroke="currentColor" strokeWidth="2" fill="none" strokeLinecap="round" strokeLinejoin="round"/>
      <path d="M2 12l10 5 10-5" stroke="currentColor" strokeWidth="2" fill="none" strokeLinecap="round" strokeLinejoin="round"/>
    </svg>
  )
}

function ViteIcon() {
  return (
    <svg viewBox="0 0 24 24" width="24" height="24">
      <polygon points="12 2 22 8 12 14 2 8" fill="#646cff"/>
      <polygon points="12 14 22 8 22 16 12 22" fill="#646cff" opacity="0.6"/>
      <polygon points="12 14 2 8 2 16 12 22" fill="#646cff" opacity="0.3"/>
    </svg>
  )
}
