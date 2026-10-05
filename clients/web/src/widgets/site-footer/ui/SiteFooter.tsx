import { useTranslation } from 'react-i18next'
import { Container } from '@/shared/ui'

export function SiteFooter() {
  const { t } = useTranslation()

  return (
    <footer className="border-t border-line bg-band">
      <Container className="py-2 text-center text-sm text-band-ink">
        {t('footer.line')}
      </Container>
    </footer>
  )
}
