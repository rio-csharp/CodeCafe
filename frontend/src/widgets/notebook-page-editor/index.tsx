import type { ComponentProps } from 'react'
import NotebookPageEditorComponent from './ui/NotebookPageEditor'
import ErrorBoundary from '@/shared/ui/ErrorBoundary'
import { ErrorFallback } from '@/shared/ui/ErrorBoundary'

function NotebookPageEditor(props: ComponentProps<typeof NotebookPageEditorComponent>) {
  return (
    <ErrorBoundary fallback={<ErrorFallback title="Editor Error" description="The page editor failed to load. Your changes are safe — try refreshing." />}>
      <NotebookPageEditorComponent {...props} />
    </ErrorBoundary>
  )
}

export default NotebookPageEditor
export { default as NotebookEditorToolbar } from './ui/NotebookEditorToolbar'
export { default as ToolbarButton } from './ui/ToolbarButton'
export { default as ToolbarColorControls } from './ui/ToolbarColorControls'
export { default as ToolbarGroup } from './ui/ToolbarGroup'
export { default as ToolbarLanguageSelect } from './ui/ToolbarLanguageSelect'
