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
    <div className="relative min-h-screen overflow-hidden text-text bg-bg">
      {/* Background layers (bottom to top) */}
      {/* Scanlines */}
      <div className="pointer-events-none absolute inset-0 bg-scanlines" />
      {/* Stars */}
      <div className="pointer-events-none absolute inset-0 bg-stars" />
      {/* Bottom glow */}
      <div className="pointer-events-none absolute bottom-0 left-1/2 h-[600px] w-[1400px] -translate-x-1/2 bg-glow-bottom" />
      {/* Mid glow */}
      <div className="pointer-events-none absolute left-1/2 top-[40%] h-[500px] w-[1200px] -translate-x-1/2 bg-glow-mid" />
      {/* Left arc */}
      <div className="pointer-events-none absolute inset-0 bg-arc-left" />
      {/* Right arc */}
      <div className="pointer-events-none absolute inset-0 bg-arc-right" />
      {/* Top purple glow */}
      <div className="pointer-events-none absolute left-1/2 top-0 h-[700px] w-[1400px] -translate-x-1/2 bg-glow-hero" />
      {/* Top cyan glow */}
      <div className="pointer-events-none absolute left-1/2 top-0 h-[500px] w-[1000px] -translate-x-1/2 bg-glow-hero-cyan" />

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

      <main className="relative">
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
        <section className="relative px-6 pb-16" aria-label="Workspace preview">
          {/* Glow behind preview */}
          <div className="pointer-events-none absolute left-1/2 top-1/2 h-[500px] w-[1200px] -translate-x-1/2 -translate-y-1/2 bg-glow-preview" />
          <div className="relative mx-auto max-w-[1100px]">
            <div className="overflow-hidden rounded-xl border border-border-strong bg-surface shadow-card shadow-glow">
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
          </div>
        </section>

        {/* Features */}
        <section className="relative px-6 py-16" id="features" aria-label="Features">
          <div className="mx-auto grid max-w-[1100px] grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-4">
            <FeatureCard
              variant="memory"
              icon={<MemoryIcon />}
              title="Persistent Project Memory"
              desc="AI remembers your decisions, architecture, and context across sessions and workspaces."
            />
            <FeatureCard
              variant="code"
              icon={<CodebaseIcon />}
              title="AI-Aware Codebase"
              desc="Ask questions, get explanations, and receive intelligent suggestions based on your entire codebase."
            />
            <FeatureCard
              variant="preview"
              icon={<PreviewIcon />}
              title="Safe Preview Environments"
              desc="Run and preview your projects in isolated, secure environments with real-time logs and metrics."
            />
            <FeatureCard
              variant="workflow"
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
            <TrustedLogo label="ASP.NET Core" icon={<DotNetIcon />} />
            <TrustedLogo label="React" icon={<ReactLogoIcon />} />
            <TrustedLogo label="TypeScript" icon={<TypeScriptIcon />} />
            <TrustedLogo label="Docker" icon={<DockerIcon />} />
            <TrustedLogo label="GitHub" icon={<GitHubLogoIcon />} />
            <TrustedLogo label="Vite" icon={<ViteLogoIcon />} />
          </div>
        </section>

        {/* CTA */}
        <section className="relative mx-auto grid max-w-[700px] place-items-center gap-4 px-6 py-20 text-center" aria-label="Call to action">
          {/* CTA background glow */}
          <div className="pointer-events-none absolute inset-0 -z-10 rounded-3xl bg-glow-cta" />
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

const featureVariantClasses = {
  memory: 'border-feature-memory/20 bg-feature-memory/10 text-feature-memory',
  code: 'border-feature-code/20 bg-feature-code/10 text-feature-code',
  preview: 'border-feature-preview/20 bg-feature-preview/10 text-feature-preview',
  workflow: 'border-feature-workflow/20 bg-feature-workflow/10 text-feature-workflow',
}

function FeatureCard({ variant, icon, title, desc }: { variant: keyof typeof featureVariantClasses; icon: React.ReactNode; title: string; desc: string }) {
  return (
    <div className="flex flex-col gap-3.5 rounded-xl border border-border bg-bg/50 p-6 transition hover:border-accent/25 hover:bg-accent/6 hover:-translate-y-0.5">
      <div className={`grid h-11 w-11 place-items-center rounded-lg border text-base font-bold ${featureVariantClasses[variant]}`}>
        {icon}
      </div>
      <h3 className="m-0 text-base font-bold">{title}</h3>
      <p className="m-0 text-sm leading-relaxed text-muted">{desc}</p>
    </div>
  )
}

function TrustedLogo({ label, icon }: { label: string; icon?: React.ReactNode }) {
  return (
    <div className="flex items-center gap-2 text-sm font-semibold text-muted/50">
      {icon}
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

/* Trusted by logos — flat monochrome style matching design */
function DotNetIcon() {
  return (
    <svg viewBox="0 0 24 24" width="22" height="22" fill="none">
      <rect x="2" y="2" width="20" height="20" rx="4" fill="currentColor" opacity="0.12" stroke="currentColor" strokeWidth="1.5"/>
      <text x="12" y="16" textAnchor="middle" fill="currentColor" fontSize="9" fontWeight="bold">.NET</text>
    </svg>
  )
}

function ReactLogoIcon() {
  return (
    <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" strokeWidth="1.5">
      <ellipse cx="12" cy="12" rx="9" ry="3.5" />
      <ellipse cx="12" cy="12" rx="9" ry="3.5" transform="rotate(60 12 12)" />
      <ellipse cx="12" cy="12" rx="9" ry="3.5" transform="rotate(120 12 12)" />
      <circle cx="12" cy="12" r="1.5" fill="currentColor" stroke="none"/>
    </svg>
  )
}

function TypeScriptIcon() {
  return (
    <svg viewBox="0 0 24 24" width="22" height="22" fill="none">
      <rect x="2" y="2" width="20" height="20" rx="3" fill="currentColor" opacity="0.12" stroke="currentColor" strokeWidth="1.5"/>
      <text x="12" y="16" textAnchor="middle" fill="currentColor" fontSize="10" fontWeight="bold">TS</text>
    </svg>
  )
}

function DockerIcon() {
  return (
    <svg viewBox="0 0 24 24" width="22" height="22" fill="currentColor" opacity="0.7">
      <path d="M4 10h2v2H4zm3 0h2v2H7zm3 0h2v2h-2zm-6 3h2v2H4zm3 0h2v2H7zm3 0h2v2h-2zm3 0h2v2h-2zM4 16h2v2H4zm3 0h2v2H7zm3 0h2v2h-2z" />
      <path d="M22 14c0-2.5-2-4-4.5-4H16v-1.5c0-1-.5-1.5-1.5-1.5h-1v2h-1v-2h-1v2h-1v-2h-1v2H9v-2H8v2H7v-2H6c-1 0-1.5.5-1.5 1.5V14c0 3.5 2.5 6 6 6h4c3 0 5.5-2 6.5-5h-1c-1 0-2-.5-2.5-1.5H22z" opacity="0.8"/>
    </svg>
  )
}

function GitHubLogoIcon() {
  return (
    <svg viewBox="0 0 24 24" width="22" height="22" fill="currentColor" opacity="0.7">
      <path d="M12 2C6.48 2 2 6.59 2 12.25c0 4.53 2.87 8.37 6.84 9.72.5.09.68-.22.68-.49 0-.24-.01-1.03-.01-1.87-2.78.62-3.37-1.21-3.37-1.21-.46-1.2-1.11-1.52-1.11-1.52-.91-.64.07-.63.07-.63 1 .08 1.53 1.06 1.53 1.06.9 1.58 2.35 1.12 2.92.86.09-.67.35-1.12.63-1.38-2.22-.26-4.56-1.15-4.56-5.1 0-1.13.39-2.05 1.03-2.78-.1-.26-.45-1.31.1-2.73 0 0 .84-.28 2.75 1.06A9.3 9.3 0 0 1 12 6.84c.85 0 1.71.12 2.51.35 1.91-1.34 2.75-1.06 2.75-1.06.55 1.42.2 2.47.1 2.73.64.73 1.03 1.65 1.03 2.78 0 3.96-2.34 4.83-4.57 5.09.36.32.68.94.68 1.9 0 1.37-.01 2.47-.01 2.8 0 .27.18.58.69.48A10.27 10.27 0 0 0 22 12.25C22 6.59 17.52 2 12 2Z"/>
    </svg>
  )
}

function ViteLogoIcon() {
  return (
    <svg viewBox="0 0 24 24" width="22" height="22">
      <polygon points="12 2 22 8 12 14 2 8" fill="currentColor" opacity="0.7"/>
      <polygon points="12 14 22 8 22 16 12 22" fill="currentColor" opacity="0.45"/>
      <polygon points="12 14 2 8 2 16 12 22" fill="currentColor" opacity="0.25"/>
    </svg>
  )
}
