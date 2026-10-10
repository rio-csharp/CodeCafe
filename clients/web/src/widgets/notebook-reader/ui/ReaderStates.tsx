import { useState } from 'react'
import type { FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { Button, Container, Input } from '@/shared/ui'
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
          <p className="text-xl font-semibold text-ink">{t('reader.notebookMissing')}</p>

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

export interface NotebookUnlockStateProps {
  pending: boolean
  /** The last code the reader tried was refused. */
  wrongCode: boolean
  onUnlock: (code: string) => void
}

/**
 * A 403 with `access_code_required` means the notebook exists but is locked:
 * ask for the code instead of showing the deliberately vague 404 state.
 */
export function NotebookUnlockState({ pending, wrongCode, onUnlock }: NotebookUnlockStateProps) {
  const { t } = useTranslation()
  const [code, setCode] = useState('')

  const submit = (event: FormEvent) => {
    event.preventDefault()
    const trimmed = code.trim()
    if (trimmed.length === 0 || pending) {
      return
    }
    onUnlock(trimmed)
  }

  return (
    <div className="flex min-h-dvh flex-col bg-canvas">
      <SiteHeader />

      <main className="flex flex-1 items-center justify-center py-16">
        <Container className="max-w-md text-center">
          <p className="text-xl font-semibold text-ink">{t('reader.unlockTitle')}</p>
          <p className="mt-2 text-sm text-muted">{t('reader.unlockBody')}</p>

          <form className="mt-6 flex flex-col gap-3" onSubmit={submit}>
            <Input
              type="password"
              value={code}
              onChange={(event) => {
                setCode(event.target.value)
              }}
              placeholder={t('reader.unlockPlaceholder')}
              aria-label={t('reader.unlockPlaceholder')}
              autoComplete="off"
              autoFocus
            />

            {wrongCode ? (
              <p role="alert" className="text-sm text-danger">
                {t('reader.unlockWrongCode')}
              </p>
            ) : null}

            <div className="flex justify-center">
              <Button type="submit" disabled={code.trim().length === 0 || pending}>
                {pending ? t('reader.unlocking') : t('reader.unlockSubmit')}
              </Button>
            </div>
          </form>

          <Link
            to="/"
            className="mt-6 inline-block text-sm text-accent-strong underline underline-offset-2 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
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
      <p className="text-lg font-semibold text-ink">{t('reader.pageMissing')}</p>

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
      <p className="text-lg font-semibold text-ink">{t('reader.emptyNotebook')}</p>
    </div>
  )
}

/** A non-404 failure of the page itself: retry without leaving the chrome. */
export function PageErrorState({ onRetry }: { onRetry: () => void }) {
  const { t } = useTranslation()

  return (
    <div className="rounded-2xl border border-line bg-card p-8 text-center">
      <p className="text-lg font-semibold text-ink">{t('list.loadError')}</p>

      <div className="mt-4 flex justify-center">
        <Button onClick={onRetry}>{t('list.retry')}</Button>
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
          <p className="text-xl font-semibold text-ink">{t('list.loadError')}</p>

          <div className="mt-6 flex justify-center">
            <Button onClick={onRetry}>{t('list.retry')}</Button>
          </div>
        </Container>
      </main>

      <SiteFooter />
    </div>
  )
}
