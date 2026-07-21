import { useLayout } from '@/shared/model/layoutContext'
import { FileText, Code2, Sparkles, Globe } from 'lucide-react'
import { LogoMark } from '@/shared/ui/icons'
import { useTranslation } from 'react-i18next'

function FeatureCard({ icon, title, description, status, available }: {
  icon: React.ReactNode
  title: string
  description: string
  status: string
  available: boolean
}) {
  return (
    <div className="rounded-xl border border-border-default bg-surface p-5 transition-shadow hover:shadow-sm">
      <div className="flex items-center justify-between">
        <div className="h-9 w-9 rounded-lg bg-brand-brown/10 flex items-center justify-center shrink-0 text-brand-brown-text">
          {icon}
        </div>
        <span className={`rounded-full px-2.5 py-0.5 text-[11px] font-medium ${
          available
            ? 'bg-status-success-bg text-status-success'
            : 'bg-surface-active text-text-tertiary'
        }`}>
          {status}
        </span>
      </div>
      <p className="mt-3 text-sm font-semibold text-text-primary">{title}</p>
      <p className="mt-1 text-xs text-text-secondary leading-relaxed">{description}</p>
    </div>
  )
}

function AboutPage() {
  const { layout } = useLayout()
  const { t } = useTranslation()

  return (
    <div className={layout === 'sidebar' ? 'p-6 sm:p-8 lg:p-12' : 'pt-28 pb-20 lg:pt-32 lg:pb-24'}>
      <div className="mx-auto max-w-2xl px-4 sm:px-6 lg:px-8">
        {/* Brand header */}
        <div className="flex flex-col items-center text-center">
          <LogoMark className="h-16 w-16 text-text-primary" />
          <h1 className="mt-4 text-3xl font-bold text-text-primary tracking-tight">{t('app.name')}</h1>
          <p className="mt-2 text-text-secondary leading-relaxed max-w-md">
            {t('about.tagline')}
          </p>
          <span className="mt-3 inline-flex items-center gap-1.5 rounded-full border border-border-default px-3 py-1 text-xs text-text-tertiary">
            <Globe className="h-3.5 w-3.5" />
            {t('app.domain')}
          </span>
        </div>

        <div className="mt-12">
          <h2 className="text-sm font-semibold text-text-tertiary uppercase tracking-wider mb-4">{t('about.features')}</h2>
          <div className="grid sm:grid-cols-2 gap-4">
            <FeatureCard
              icon={<FileText className="h-4 w-4" />}
              title={t('about.featureNotebooks')}
              description={t('about.featureNotebooksDesc')}
              status={t('about.statusAvailable')}
              available
            />
            <FeatureCard
              icon={<Globe className="h-4 w-4" />}
              title={t('about.featureSharing')}
              description={t('about.featureSharingDesc')}
              status={t('about.statusAvailable')}
              available
            />
            <FeatureCard
              icon={<Sparkles className="h-4 w-4" />}
              title={t('about.featureMcp')}
              description={t('about.featureMcpDesc')}
              status={t('about.statusAvailable')}
              available
            />
            <FeatureCard
              icon={<Code2 className="h-4 w-4" />}
              title={t('about.featureCodes')}
              description={t('about.featureCodesDesc')}
              status={t('about.statusPlanned')}
              available={false}
            />
          </div>
        </div>

        <div className="mt-12">
          <h2 className="text-sm font-semibold text-text-tertiary uppercase tracking-wider mb-4">{t('about.techStack')}</h2>
          <div className="flex flex-wrap gap-2">
            {['React 19', 'Vite', 'Tailwind CSS', 'TipTap', 'TanStack Query', 'ASP.NET Core 10', 'OpenIddict', 'PostgreSQL', 'MCP'].map(
              (tech) => (
                <span key={tech} className="rounded-full bg-surface-hover border border-border-subtle px-3 py-1 text-xs text-text-secondary">
                  {tech}
                </span>
              ),
            )}
          </div>
        </div>
      </div>
    </div>
  )
}

export default AboutPage
