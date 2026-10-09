import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'

export interface AuthLayoutProps {
  title: string
  /** The link to the other auth page, rendered under the card. */
  footer: ReactNode
  children: ReactNode
}

/** One focused form, with a quiet brand panel on larger screens. */
export function AuthLayout({ title, footer, children }: AuthLayoutProps) {
  const { t } = useTranslation()

  return (
    <div className="flex min-h-dvh flex-col bg-canvas">
      <main className="flex flex-1 items-center justify-center px-4 py-12 sm:px-6">
        <div className="grid w-full max-w-5xl items-center gap-12 lg:grid-cols-2 lg:gap-20">
          <aside className="hidden py-12 lg:block">
            <p className="text-xs font-semibold tracking-[0.2em] text-accent-strong uppercase">{t('home.overline')}</p>
            <p className="mt-6 max-w-sm font-display text-5xl leading-tight text-ink">{t('home.coverTitle')}</p>
            <p className="mt-6 max-w-sm text-lg leading-relaxed text-muted">{t('home.workspaceHint')}</p>
            <div aria-hidden="true" className="mt-10 flex items-center gap-3 text-accent-strong">
              <span className="h-px w-12 bg-line" /><span className="text-sm">{t('home.coverCaption')}</span>
            </div>
          </aside>
          <div className="mx-auto w-full max-w-md">
          <div className="rounded-3xl border border-line bg-card p-6 shadow-xl shadow-line/30 sm:p-10">
            <div className="text-center">
              <Link
                to="/"
                className="font-display text-3xl tracking-tight text-ink hover:text-accent-strong"
              >
                {t('brand.name')}
              </Link>
              <p className="mt-1 text-sm text-muted">{t('auth.tagline')}</p>
            </div>

            <h1 className="mt-8 text-center text-2xl font-semibold text-ink">{title}</h1>

            <div className="mt-6">{children}</div>

            <div className="mt-6 text-center text-sm text-muted">{footer}</div>
          </div>
          </div>
        </div>
      </main>
    </div>
  )
}
