import type { ComponentProps } from 'react'
import NotebookOutlineComponent from './ui/NotebookOutline'
import ErrorBoundary from '@/shared/ui/ErrorBoundary'
import { ErrorFallback } from '@/shared/ui/ErrorBoundary'

function NotebookOutline(props: ComponentProps<typeof NotebookOutlineComponent>) {
  return (
    <ErrorBoundary fallback={<ErrorFallback title="Outline Error" description="The page outline failed to render. Try refreshing the page." />}>
      <NotebookOutlineComponent {...props} />
    </ErrorBoundary>
  )
}

export default NotebookOutline
