import type { ComponentProps } from 'react'
import NotebookSettingsFormComponent from './ui/NotebookSettingsForm'
import ErrorBoundary from '@/shared/ui/ErrorBoundary'
import { ErrorFallback } from '@/shared/ui/ErrorBoundary'

function NotebookSettingsForm(props: ComponentProps<typeof NotebookSettingsFormComponent>) {
  return (
    <ErrorBoundary fallback={<ErrorFallback title="Settings Error" description="The settings form failed to load. Try refreshing the page." />}>
      <NotebookSettingsFormComponent {...props} />
    </ErrorBoundary>
  )
}

export default NotebookSettingsForm
export { default as DeleteConfirmSection } from './ui/DeleteConfirmSection'
export { default as SettingsFormActions } from './ui/SettingsFormActions'
export { default as VisibilityField } from './ui/VisibilityField'
