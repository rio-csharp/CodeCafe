import { Link } from 'react-router-dom'
import { FileText, Code, ArrowRight } from 'lucide-react'
import { NotesIllustration } from '@/widgets/dashboard'
import { CodesIllustration } from '@/widgets/dashboard'
import { useTranslation } from 'react-i18next'

const useCards = () => {
  const { t } = useTranslation()
  return [
    {
      to: '/notes',
      label: t('nav.notes'),
      desc: t('features.notebooksDesc'),
      icon: FileText,
      illustration: NotesIllustration,
      btnText: t('dashboard.openNotes'),
    },
    {
      to: '/codes',
      label: t('nav.codes'),
      desc: t('features.codesDesc'),
      icon: Code,
      illustration: CodesIllustration,
      btnText: t('dashboard.seeRoadmap'),
    },
  ] as const
}

export default function DashboardCards() {
  const cards = useCards()
  return (
    <div className="mt-10 grid gap-6 grid-cols-1 lg:grid-cols-2">
      {cards.map(({ to, label, desc, icon: Icon, illustration: Illustration, btnText }) => (
        <div
          key={to}
          className="group relative flex flex-col rounded-2xl border border-border-default bg-surface p-6 sm:p-8 transition-all hover:border-border-hover hover:shadow-sm"
        >
          <div className="flex items-start justify-between gap-6">
            <div className="flex-1 min-w-0">
              <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-surface-active">
                <Icon className="h-6 w-6 text-text-secondary" />
              </div>
              <h2 className="mt-6 text-2xl font-semibold text-text-primary">{label}</h2>
              <p className="mt-2 text-sm text-text-secondary leading-relaxed max-w-xs">{desc}</p>
            </div>
            <Illustration />
          </div>

          <div className="mt-8">
            <Link
              to={to}
              className="inline-flex w-fit items-center justify-center gap-2 whitespace-nowrap rounded-lg bg-brand-brown px-6 py-2.5 text-sm font-medium text-text-inverse transition-opacity hover:opacity-90"
            >
              {btnText}
              <ArrowRight className="h-4 w-4" />
            </Link>
          </div>
        </div>
      ))}
    </div>
  )
}
