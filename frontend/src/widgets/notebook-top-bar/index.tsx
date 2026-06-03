import type { ComponentProps } from 'react'
import NotebookTopBarComponent from './ui/NotebookTopBar'
import ErrorBoundary from '@/shared/ui/ErrorBoundary'
import { ErrorFallback } from '@/shared/ui/ErrorBoundary'

function NotebookTopBar(props: ComponentProps<typeof NotebookTopBarComponent>) {
  return (
    <ErrorBoundary fallback={<ErrorFallback title="Top Bar Error" description="The notebook top bar failed to render. Try refreshing the page." />}>
      <NotebookTopBarComponent {...props} />
    </ErrorBoundary>
  )
}

export default NotebookTopBar
export { default as TopBarActionMenu } from './ui/TopBarActionMenu'
