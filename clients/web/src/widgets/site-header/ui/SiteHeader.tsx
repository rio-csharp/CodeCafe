import { useTranslation } from 'react-i18next'
import { Link, useLocation } from 'react-router'
import { useSessionStore } from '@/entities/session'
import { LanguageToggle } from '@/features/switch-language'
import { ThemeToggle } from '@/features/switch-theme'
import { buttonClass, Container } from '@/shared/ui'
import { UserMenu } from './UserMenu'

export function SiteHeader() {
  const { t } = useTranslation()
  const location = useLocation()
  const status = useSessionStore((state) => state.status)
  const user = useSessionStore((state) => state.user)

  return (
    <header className="sticky top-0 z-10 border-b border-line bg-card">
      <Container className="flex flex-wrap items-center justify-between gap-3 py-2">
        <div className="flex items-center gap-2">
          <svg className="h-6 w-6 text-accent" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <path
              d="M4 8h11v6a4 4 0 0 1-4 4H8a4 4 0 0 1-4-4V8Z"
              stroke="currentColor"
              strokeWidth="1.6"
              strokeLinejoin="round"
            />
            <path
              d="M15 9.5h2.5a2.5 2.5 0 0 1 0 5H15"
              stroke="currentColor"
              strokeWidth="1.6"
              strokeLinecap="round"
            />
            <path
              d="M7.5 4.5c0-1 1-1.4 1-2.5M11 4.5c0-1 1-1.4 1-2.5"
              stroke="currentColor"
              strokeWidth="1.4"
              strokeLinecap="round"
            />
          </svg>
          <span className="font-display text-base tracking-tight text-ink">{t('brand.name')}</span>
        </div>

        <div className="flex items-center gap-2">
          <ThemeToggle />
          <LanguageToggle />

          {/* `unknown` renders neither state — a boot refresh is still in flight. */}
          {status === 'anonymous' ? (
            <Link
              to="/login"
              state={{ from: location.pathname }}
              className={buttonClass('ghost')}
            >
              {t('header.login')}
            </Link>
          ) : null}

          {status === 'authenticated' && user !== null ? (
            <UserMenu displayName={user.displayName} />
          ) : null}
        </div>
      </Container>
    </header>
  )
}
