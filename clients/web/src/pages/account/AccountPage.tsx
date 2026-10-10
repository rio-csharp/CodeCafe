import { useTranslation } from 'react-i18next'
import { Link, Navigate, useLocation } from 'react-router'
import { useSessionStore } from '@/entities/session'
import { ChangePasswordForm } from '@/features/change-password'
import { AccessTokensCard } from '@/features/manage-access-tokens'
import { ProfileForm } from '@/features/update-profile'
import { Container, Spinner } from '@/shared/ui'

/**
 * The account page: profile, password and access tokens as cards on the
 * canvas. Auth-only — anonymous visitors are sent to login with a `from`
 * breadcrumb so they land back here.
 */
export function AccountPage() {
  const { t } = useTranslation()
  const location = useLocation()
  const status = useSessionStore((state) => state.status)

  if (status === 'anonymous') {
    return <Navigate to="/login" state={{ from: location.pathname }} replace />
  }

  return (
    <div className="min-h-dvh bg-canvas">
      <Container width="narrow" className="py-12 sm:py-16">
        <Link
          to="/"
          className="inline-flex items-center gap-1.5 text-sm text-muted transition-colors hover:text-ink"
        >
          <svg
            viewBox="0 0 16 16"
            className="size-3.5"
            fill="none"
            stroke="currentColor"
            strokeWidth="1.6"
            strokeLinecap="round"
            strokeLinejoin="round"
            aria-hidden="true"
          >
            <path d="M9.5 3.5 5 8l4.5 4.5" />
          </svg>
          {t('trash.backHome')}
        </Link>

        <h1 className="mt-6 mb-6 border-b border-line pb-4 text-xl font-semibold text-ink">
          {t('account.title')}
        </h1>

        {status !== 'authenticated' ? (
          <div className="grid place-items-center py-16">
            <Spinner />
            <span className="sr-only">{t('list.loading')}</span>
          </div>
        ) : (
          <div className="flex flex-col gap-6">
            <AccountSection title={t('account.profileTitle')}>
              <ProfileForm />
            </AccountSection>
            <AccountSection title={t('account.passwordTitle')}>
              <ChangePasswordForm />
            </AccountSection>
            <AccountSection title={t('account.tokensTitle')}>
              <AccessTokensCard />
            </AccountSection>
          </div>
        )}
      </Container>
    </div>
  )
}

function AccountSection({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section className="rounded-2xl border border-line bg-card p-6">
      <h2 className="mb-4 text-base font-semibold text-ink">{title}</h2>
      {children}
    </section>
  )
}
