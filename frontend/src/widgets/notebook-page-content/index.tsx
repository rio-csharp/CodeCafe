import type { ComponentProps } from 'react'
import NotebookPageContentComponent from './ui/NotebookPageContent'
import ErrorBoundary from '@/shared/ui/ErrorBoundary'
import { ErrorFallback } from '@/shared/ui/ErrorBoundary'

function NotebookPageContent(props: ComponentProps<typeof NotebookPageContentComponent>) {
  return (
    <ErrorBoundary fallback={<ErrorFallback title="Page Content Error" description="The page content failed to render. Try refreshing the page." />}>
      <NotebookPageContentComponent {...props} />
    </ErrorBoundary>
  )
}

export default NotebookPageContent
