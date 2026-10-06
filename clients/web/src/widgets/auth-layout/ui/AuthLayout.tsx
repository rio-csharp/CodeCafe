import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'

export interface AuthLayoutProps {
  title: string
  /** The link to the other auth page, rendered under the card. */
  footer: ReactNode
  children: ReactNode
}

/** Centered card on the canvas: brand, one form, and a way across. */
export function AuthLayout({ title, footer, children }: AuthLayoutProps) {
  const { t } = useTranslation()

  return (
    <div className="flex min-h-dvh flex-col bg-canvas">
      <main className="flex flex-1 items-center justify-center px-4 py-12 sm:px-6">
        {/* Not `Container`: the card needs its own width, and two max-w-* classes
            on one element would fight in the stylesheet rather than in the markup. */}
        <div className="w-full max-w-md">
          <div className="rounded-3xl border border-line bg-card p-8 shadow-sm">
            <div className="text-center">
              <Link
                to="/"
                className="font-display text-2xl tracking-tight text-ink hover:text-accent-strong"
              >
                {t('brand.name')}
              </Link>
              <p className="mt-1 text-sm text-muted">{t('auth.tagline')}</p>
            </div>

            <h1 className="mt-6 text-center text-xl font-semibold text-ink">{title}</h1>

            <div className="mt-6">{children}</div>

            <div className="mt-6 text-center text-sm text-muted">{footer}</div>
          </div>
        </div>
      </main>
    </div>
  )
}
