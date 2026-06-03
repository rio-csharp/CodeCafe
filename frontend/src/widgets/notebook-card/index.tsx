import type { ComponentProps } from 'react'
import NotebookCardComponent from './ui/NotebookCard'
import ErrorBoundary from '@/shared/ui/ErrorBoundary'
import { ErrorFallback } from '@/shared/ui/ErrorBoundary'

function NotebookCard(props: ComponentProps<typeof NotebookCardComponent>) {
  return (
    <ErrorBoundary fallback={<ErrorFallback title="Notebook Card Error" description="The notebook card failed to render. Try refreshing the page." />}>
      <NotebookCardComponent {...props} />
    </ErrorBoundary>
  )
}

export default NotebookCard
export { default as NotebookCardMenu } from './ui/NotebookCardMenu'
export { default as SkeletonGrid } from './ui/SkeletonGrid'
export { default as VisibilityBadge } from './ui/VisibilityBadge'
