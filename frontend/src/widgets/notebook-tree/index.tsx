import type { ComponentProps } from 'react'
import NotebookTreeComponent from './ui/NotebookTree'
import ErrorBoundary from '@/shared/ui/ErrorBoundary'
import { ErrorFallback } from '@/shared/ui/ErrorBoundary'

function NotebookTree(props: ComponentProps<typeof NotebookTreeComponent>) {
  return (
    <ErrorBoundary fallback={<ErrorFallback title="Notebook Tree Error" description="The notebook tree failed to render. Try refreshing the page." />}>
      <NotebookTreeComponent {...props} />
    </ErrorBoundary>
  )
}

export default NotebookTree
export { default as TreeContent } from './ui/TreeContent'
export { default as TreeCreateMenu } from './ui/TreeCreateMenu'
export { default as TreeFolderNode } from './ui/TreeFolderNode'
export { default as TreeItem } from './ui/TreeItem'
export { default as TreeNodeActions } from './ui/TreeNodeActions'
export { default as TreePageNode } from './ui/TreePageNode'
export { default as TreeRenameField } from './ui/TreeRenameField'
export { default as TreeRootActions } from './ui/TreeRootActions'
