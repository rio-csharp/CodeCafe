import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'

import type { NotebookItem } from '@/entities/notebook-item'
import { sanitizeTipTapContent } from '@/shared/lib/sanitizeTipTapContent'
import TipTapViewer from '@/shared/ui/TipTapViewer'
import ErrorBoundary from '@/shared/ui/ErrorBoundary'

interface NotebookPageContentProps {
  page: NotebookItem
}

function PlainTextViewer({ text }: { text: string }) {
  const { t } = useTranslation()
  return (
    <div className="prose prose-sm max-w-none">
      <p className="text-xs text-text-tertiary mb-2">{t('common.plainTextFallback')}</p>
      <pre className="whitespace-pre-wrap font-sans text-text-secondary text-sm leading-relaxed">{text}</pre>
    </div>
  )
}

function ErrorDisplay() {
  const { t } = useTranslation()
  return (
    <div className="rounded-xl border border-status-error-border bg-status-error-bg p-6">
      <p className="text-sm font-semibold text-status-error">{t('common.unableToDisplayContent')}</p>
      <p className="mt-1 text-xs text-status-error">{t('common.pageContentRenderError')}</p>
    </div>
  )
}

function NotebookPageContentComponent({ page }: NotebookPageContentProps) {
  const { t } = useTranslation()
  const safeContent = useMemo(
    () => (page.contentJson ? sanitizeTipTapContent(page.contentJson) : null),
    [page.contentJson],
  )
  const hasPlainText = !!page.plainTextContent

  if (!safeContent && !hasPlainText) {
    return (
      <div className="flex items-center justify-center h-64">
        <p className="text-sm text-text-tertiary">{t('common.pageEmpty')}</p>
      </div>
    )
  }

  if (safeContent) {
    return (
      <ErrorBoundary
        fallback={
          hasPlainText ? (
            <PlainTextViewer text={page.plainTextContent!} />
          ) : (
            <ErrorDisplay />
          )
        }
      >
        <TipTapViewer content={safeContent} />
      </ErrorBoundary>
    )
  }

  return <PlainTextViewer text={page.plainTextContent!} />
}

export default function NotebookPageContent(props: NotebookPageContentProps) {
  return (
    <ErrorBoundary fallback={<ErrorDisplay />}>
      <NotebookPageContentComponent {...props} />
    </ErrorBoundary>
  )
}
