import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { Button, Container } from '@/shared/ui'
import { SiteFooter } from '@/widgets/site-footer'
import { SiteHeader } from '@/widgets/site-header'

/**
 * A 404 here can mean "private" or "access-code protected" as easily as
 * "absent", so the copy deliberately claims nothing about existence.
 */
export function NotebookMissingState() {
  const { t } = useTranslation()

  return (
    <div className="flex min-h-dvh flex-col bg-canvas">
      <SiteHeader />

      <main className="flex flex-1 items-center justify-center py-16">
        <Container className="max-w-md text-center">
          <p className="font-display text-2xl text-ink">{t('reader.notebookMissing')}</p>

          <Link
            to="/"
            className="mt-6 inline-block rounded-full border border-line px-5 py-2 text-sm text-ink transition-colors hover:border-accent hover:text-accent-strong focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
          >
            {t('reader.backHome')}
          </Link>
        </Container>
      </main>

      <SiteFooter />
    </div>
  )
}

/** Stays inside the reader chrome so the tree is still there to navigate by. */
export function PageMissingState() {  const { t } = useTranslation()

  return (
    <div className="rounded-2xl border border-line bg-card p-8 text-center">
      <p className="font-display text-xl text-ink">{t('reader.pageMissing')}</p>

      <Link
        to="/"
        className="mt-4 inline-block text-sm text-accent-strong underline underline-offset-2 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
      >
        {t('reader.backHome')}
      </Link>
    </div>
  )
}

export function EmptyNotebookState() {
  const { t } = useTranslation()

  return (
    <div className="rounded-2xl border border-line bg-card p-8 text-center">
      <p className="font-display text-xl text-ink">{t('reader.emptyNotebook')}</p>
    </div>
  )
}

/** A non-404 failure of the page itself: retry without leaving the chrome. */
export function PageErrorState({ onRetry }: { onRetry: () => void }) {
  const { t } = useTranslation()

  return (
    <div className="rounded-2xl border border-line bg-card p-8 text-center">
      <p className="font-display text-xl text-ink">{t('catalog.error.title')}</p>

      <div className="mt-4 flex justify-center">
        <Button onClick={onRetry}>{t('catalog.error.retry')}</Button>
      </div>
    </div>
  )
}

/** A non-404 failure of the notebook itself: there is no chrome to fall back on. */
export function NotebookErrorState({ onRetry }: { onRetry: () => void }) {
  const { t } = useTranslation()

  return (
    <div className="flex min-h-dvh flex-col bg-canvas">
      <SiteHeader />

      <main className="flex flex-1 items-center justify-center py-16">
        <Container className="max-w-md text-center">
          <p className="font-display text-2xl text-ink">{t('catalog.error.title')}</p>

          <div className="mt-6 flex justify-center">
            <Button onClick={onRetry}>{t('catalog.error.retry')}</Button>
          </div>
        </Container>
      </main>

      <SiteFooter />
    </div>
  )
}
