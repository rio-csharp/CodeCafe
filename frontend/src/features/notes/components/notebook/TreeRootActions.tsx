import { useState, useRef } from 'react'
import { Plus, Folder, FileText } from 'lucide-react'
import { useClickOutside } from '../../../../hooks/useClickOutside'

interface TreeRootActionsProps {
  onCreateRoot: (type: 'folder' | 'page') => void
}

export default function TreeRootActions({ onCreateRoot }: TreeRootActionsProps) {
  const [showRootCreate, setShowRootCreate] = useState(false)
  const rootMenuRef = useRef<HTMLDivElement>(null)
  useClickOutside(rootMenuRef, () => setShowRootCreate(false))

  return (
    <div className="px-4 pb-2">
      <div className="relative" ref={rootMenuRef}>
        <button
          type="button"
          onClick={() => setShowRootCreate(!showRootCreate)}
          className="w-full flex items-center justify-center gap-1.5 rounded-lg border border-dashed border-gray-200 px-3 py-1.5 text-xs text-gray-500 hover:border-gray-300 hover:text-gray-700 hover:bg-gray-50 transition-colors"
        >
          <Plus className="h-3.5 w-3.5" />
          Add folder or page
        </button>
        {showRootCreate && (
          <div className="absolute left-0 right-0 top-full mt-1 rounded-lg border border-gray-100 bg-white shadow-lg z-50 py-1">
            <button
              type="button"
              onClick={() => onCreateRoot('folder')}
              className="w-full flex items-center gap-2 px-3 py-1.5 text-xs text-gray-700 hover:bg-gray-50 transition-colors"
            >
              <Folder className="h-3.5 w-3.5 text-brand-brown" />
              New folder
            </button>
            <button
              type="button"
              onClick={() => onCreateRoot('page')}
              className="w-full flex items-center gap-2 px-3 py-1.5 text-xs text-gray-700 hover:bg-gray-50 transition-colors"
            >
              <FileText className="h-3.5 w-3.5 text-gray-400" />
              New page
            </button>
          </div>
        )}
      </div>
    </div>
  )
}
