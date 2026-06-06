import { useLayout } from '@/shared/model/layoutContext'
import { FileText, Code2, Sparkles, Coffee, Globe } from 'lucide-react'
import { useTranslation } from 'react-i18next'

function FeatureItem({ icon, title, status }: { icon: React.ReactNode; title: string; status: string }) {
  return (
    <div className="flex items-center gap-3">
      <div className="h-9 w-9 rounded-lg bg-surface-hover border border-border-subtle flex items-center justify-center shrink-0">
        {icon}
      </div>
      <div>
        <p className="text-sm font-medium text-text-primary">{title}</p>
        <p className="text-xs text-text-tertiary">{status}</p>
      </div>
    </div>
  )
}

function AboutPage() {
  const { layout } = useLayout()
  const { t } = useTranslation()

  return (
    <div className={layout === 'sidebar' ? 'p-6 sm:p-8 lg:p-12' : 'pt-28 pb-20 lg:pt-32 lg:pb-24'}>
      <div className="mx-auto max-w-2xl px-4 sm:px-6 lg:px-8">
        <div className="flex items-center gap-3 mb-6">
          <Coffee className="h-8 w-8 text-brand-brown" />
          <h1 className="text-3xl font-bold text-text-primary">{t('app.name')}</h1>
        </div>

        <p className="text-text-secondary leading-relaxed">
          {t('about.comingSoon')}
        </p>

        <div className="mt-10">
          <h2 className="text-sm font-semibold text-text-tertiary uppercase tracking-wider mb-4">{t('about.features')}</h2>
          <div className="space-y-4">
            <FeatureItem
              icon={<FileText className="h-4 w-4 text-text-secondary" />}
              title={t('about.featureNotebooks')}
              status={t('about.statusAvailable')}
            />
            <FeatureItem
              icon={<Globe className="h-4 w-4 text-text-secondary" />}
              title={t('about.featureSharing')}
              status={t('about.statusAvailable')}
            />
            <FeatureItem
              icon={<Sparkles className="h-4 w-4 text-text-secondary" />}
              title={t('about.featureMcp')}
              status={t('about.statusAvailable')}
            />
            <FeatureItem
              icon={<Code2 className="h-4 w-4 text-text-secondary" />}
              title={t('about.featureCodes')}
              status={t('about.statusPlanned')}
            />
          </div>
        </div>

        <div className="mt-10">
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
