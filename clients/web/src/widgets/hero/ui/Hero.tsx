import { useTranslation } from 'react-i18next'
import { SearchInput } from '@/features/search-notebooks'

export interface HeroProps {
  value: string
  onChange: (value: string) => void
}

export function Hero({ value, onChange }: HeroProps) {
  const { t } = useTranslation()

  return (
    <section className="bg-canvas px-4 py-14 text-center sm:px-6 sm:py-20">
      <div className="mx-auto w-full max-w-2xl">
        <h1 className="font-display text-4xl leading-tight text-ink sm:text-5xl">
          {t('hero.title')}
        </h1>
        <p className="mt-4 text-muted">{t('hero.subtitle')}</p>
        <div className="mt-8">
          <SearchInput value={value} onChange={onChange} />
        </div>
      </div>
    </section>
  )
}
