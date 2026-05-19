import { Link } from 'react-router-dom'
import { Folder, FileText } from 'lucide-react'
import type { NotebookItem } from '../../types'

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
          ? 'bg-amber-50/60 text-brand-brown font-medium'
          : isFolder
            ? 'text-gray-500'
            : 'text-gray-600 hover:bg-gray-50 hover:text-black'
      }`}
    >
      {isFolder ? (
        <Folder className="h-3.5 w-3.5 shrink-0 text-brand-brown" />
      ) : (
        <FileText className="h-3.5 w-3.5 shrink-0 text-gray-400" />
      )}
      <span className="truncate">{item.title}</span>
      {isFolder && <span className="text-[10px] text-gray-400 ml-1">(folder)</span>}
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
