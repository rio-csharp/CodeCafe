import { useTranslation } from 'react-i18next'
import { Container } from '@/shared/ui'

export function SiteFooter() {
  const { t } = useTranslation()

  return (
    <footer className="border-t border-line bg-canvas">
      <Container className="py-6 text-center text-xs text-muted">
        {t('footer.line')}
      </Container>
    </footer>
  )
}
