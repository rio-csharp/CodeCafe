import { Link } from 'react-router-dom'
import { Folder, FileText } from 'lucide-react'
import type { NotebookItem } from '@/entities/notebook-item'

interface SearchResultItemProps {
  item: NotebookItem
  notebookSlug: string
  activePath: string | null
}

export default function SearchResultItem({ item, notebookSlug, activePath }: SearchResultItemProps) {
  const isActive = item.type === 'page' && item.path === activePath
  const isFolder = item.type === 'folder'

  const content = (
    <div
      className={`flex items-center gap-2 px-3 py-1.5 text-[13px] rounded-md transition-colors ${
        isActive
          ? 'bg-status-favorite-bg/60 text-brand-brown font-medium'
          : isFolder
            ? 'text-text-secondary'
            : 'text-text-secondary hover:bg-surface-hover hover:text-text-primary'
      }`}
    >
      {isFolder ? (
        <Folder className="h-3.5 w-3.5 shrink-0 text-brand-brown" />
      ) : (
        <FileText className="h-3.5 w-3.5 shrink-0 text-text-tertiary" />
      )}
      <span className="truncate">{item.title}</span>
      {isFolder && <span className="text-[10px] text-text-tertiary ml-1">(folder)</span>}
    </div>
  )

  if (isFolder) {
    return content
  }

  return (
    <Link to={`/notes/${notebookSlug}/${item.path}`}>
      {content}
    </Link>
  )
}
