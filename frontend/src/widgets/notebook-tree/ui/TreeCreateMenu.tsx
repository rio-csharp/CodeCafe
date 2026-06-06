import { useState, useRef } from 'react'
import { Plus, Folder, FileText } from 'lucide-react'
import { useClickOutside } from '@/shared/hooks/useClickOutside'
import { useTranslation } from 'react-i18next'

interface TreeCreateMenuProps {
  onCreateFolder: () => void
  onCreatePage: () => void
}

export default function TreeCreateMenu({ onCreateFolder, onCreatePage }: TreeCreateMenuProps) {
  const [show, setShow] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)
  const { t } = useTranslation()
  useClickOutside(menuRef, () => setShow(false))

  return (
    <div className="relative" ref={menuRef}>
      <button
        type="button"
        onClick={(e) => { e.stopPropagation(); setShow(!show) }}
        className="p-0.5 text-text-tertiary hover:text-brand-brown rounded transition-colors"
        title={t('notebook.addItem')}
      >
        <Plus className="h-3.5 w-3.5" />
      </button>
      {show && (
        <div className="absolute left-0 top-full mt-1 w-36 rounded-lg border border-border-subtle bg-surface shadow-lg z-50 py-1">
          <button
            type="button"
            onClick={() => { onCreateFolder(); setShow(false) }}
            className="w-full flex items-center gap-2 px-3 py-1.5 text-xs text-text-secondary hover:bg-surface-hover transition-colors"
          >
            <Folder className="h-3.5 w-3.5 text-brand-brown" />
            {t('notebook.newFolder')}
          </button>
          <button
            type="button"
            onClick={() => { onCreatePage(); setShow(false) }}
            className="w-full flex items-center gap-2 px-3 py-1.5 text-xs text-text-secondary hover:bg-surface-hover transition-colors"
          >
            <FileText className="h-3.5 w-3.5 text-text-tertiary" />
            {t('notebook.newPage')}
          </button>
        </div>
      )}
    </div>
  )
}
