import { Link } from 'react-router-dom'

function LogoIcon({ size = 28 }: { size?: number }) {
  return (
    <svg aria-hidden="true" viewBox="0 0 32 32" width={size} height={size}>
      <rect width="32" height="32" rx="7" fill="url(#about-logo-grad)" />
      <path d="M10 10 L6 16 L10 22" stroke="white" strokeWidth="2.2" fill="none" strokeLinecap="round" strokeLinejoin="round" />
      <path d="M22 10 L26 16 L22 22" stroke="white" strokeWidth="2.2" fill="none" strokeLinecap="round" strokeLinejoin="round" />
      <path d="M14 22 L18 10" stroke="white" strokeWidth="2.2" fill="none" strokeLinecap="round" strokeLinejoin="round" />
      <defs>
        <linearGradient id="about-logo-grad" x1="0" y1="0" x2="32" y2="32">
          <stop stopColor="#38bdf8" />
          <stop offset="1" stopColor="#818cf8" />
        </linearGradient>
      </defs>
    </svg>
  )
}

export function AboutPage() {
  return (
    <div className="min-h-screen text-text bg-bg">
      <header className="sticky top-0 z-50 border-b border-border bg-bg/82 backdrop-blur-xl">
        <div className="mx-auto flex max-w-[1200px] items-center justify-between gap-6 px-6 py-3.5">
          <Link className="inline-flex items-center gap-2.5 text-lg font-bold text-text no-underline" to="/">
            <LogoIcon />
            <span>CodeCafe</span>
          </Link>
          <Link
            className="rounded-lg border border-border px-4 py-2 text-sm font-semibold text-text no-underline transition-colors hover:border-accent/40 hover:bg-accent/8"
            to="/"
          >
            Back to Home
          </Link>
        </div>
      </header>

      <main className="mx-auto max-w-[640px] px-6 py-16">
        <div className="mb-8 text-center">
          <div className="mx-auto mb-6 h-20 w-20 overflow-hidden rounded-2xl border border-accent/20">
            <img
              src="https://github.com/rio-csharp.png"
              alt="Yao"
              className="h-full w-full object-cover"
              onError={(e) => {
                const target = e.target as HTMLImageElement
                target.style.display = 'none'
                target.parentElement?.classList.add('grid', 'place-items-center', 'bg-accent/8', 'text-accent')
                const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg')
                svg.setAttribute('viewBox', '0 0 24 24')
                svg.setAttribute('width', '40')
                svg.setAttribute('height', '40')
                svg.setAttribute('fill', 'none')
                svg.setAttribute('stroke', 'currentColor')
                svg.setAttribute('stroke-width', '1.5')
                target.parentElement?.appendChild(svg)
              }}
            />
          </div>
          <h1 className="m-0 text-3xl font-bold tracking-tight">About</h1>
        </div>

        <div className="flex flex-col gap-6 rounded-xl border border-border bg-surface/40 p-8">
          <div className="flex items-center gap-4">
            <div className="h-14 w-14 overflow-hidden rounded-full border border-border bg-accent/10">
              <img
                src="https://github.com/rio-csharp.png"
                alt="Yao"
                className="h-full w-full object-cover"
                onError={(e) => { (e.target as HTMLImageElement).style.display = 'none' }}
              />
            </div>
            <div>
              <h2 className="m-0 text-lg font-bold">Yao</h2>
              <p className="m-0 text-sm text-muted">Creator of CodeCafe</p>
            </div>
          </div>

          <p className="m-0 leading-relaxed text-muted">
            Hi, I&apos;m Yao. I built CodeCafe because I believe engineering work should be
            persistent, contextual, and AI-native. This project is my vision for a workspace
            that remembers your decisions, understands your codebase, and evolves with you
            across every session.
          </p>

          <div className="flex items-center gap-3 pt-2">
            <a
              href="https://github.com/rio-csharp"
              target="_blank"
              rel="noreferrer"
              className="inline-flex items-center gap-2 rounded-lg border border-border px-4 py-2 text-sm font-semibold text-text no-underline transition-colors hover:border-accent/40 hover:bg-accent/8"
            >
              <svg viewBox="0 0 24 24" width="16" height="16" fill="currentColor">
                <path d="M12 2C6.48 2 2 6.59 2 12.25c0 4.53 2.87 8.37 6.84 9.72.5.09.68-.22.68-.49 0-.24-.01-1.03-.01-1.87-2.78.62-3.37-1.21-3.37-1.21-.46-1.2-1.11-1.52-1.11-1.52-.91-.64.07-.63.07-.63 1 .08 1.53 1.06 1.53 1.06.9 1.58 2.35 1.12 2.92.86.09-.67.35-1.12.63-1.38-2.22-.26-4.56-1.15-4.56-5.1 0-1.13.39-2.05 1.03-2.78-.1-.26-.45-1.31.1-2.73 0 0 .84-.28 2.75 1.06A9.3 9.3 0 0 1 12 6.84c.85 0 1.71.12 2.51.35 1.91-1.34 2.75-1.06 2.75-1.06.55 1.42.2 2.47.1 2.73.64.73 1.03 1.65 1.03 2.78 0 3.96-2.34 4.83-4.57 5.09.36.32.68.94.68 1.9 0 1.37-.01 2.47-.01 2.8 0 .27.18.58.69.48A10.27 10.27 0 0 0 22 12.25C22 6.59 17.52 2 12 2Z" />
              </svg>
              GitHub
            </a>
            <a
              href="https://github.com/rio-csharp/CodeCafe"
              target="_blank"
              rel="noreferrer"
              className="inline-flex items-center gap-2 rounded-lg border border-border px-4 py-2 text-sm font-semibold text-text no-underline transition-colors hover:border-accent/40 hover:bg-accent/8"
            >
              <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M9 19c-5 1.5-5-2.5-7-3m14 6v-3.87a3.37 3.37 0 0 0-.94-2.61c3.14-.35 6.44-1.54 6.44-7A5.44 5.44 0 0 0 20 4.77 5.07 5.07 0 0 0 19.91 1S18.73.65 16 2.48a13.38 13.38 0 0 0-7 0C6.27.65 5.09 1 5.09 1A5.07 5.07 0 0 0 5 4.77a5.44 5.44 0 0 0-1.5 3.78c0 5.42 3.3 6.61 6.44 7A3.37 3.37 0 0 0 9 18.13V22" />
              </svg>
              Project Repo
            </a>
          </div>
        </div>
      </main>
    </div>
  )
}
