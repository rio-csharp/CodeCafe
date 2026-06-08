import type { HTMLAttributes } from 'react'
import { Minus, Sparkles } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import type { Notebook } from '@/entities/notebook'
import type { NotebookItem } from '@/entities/notebook-item'

interface AiAssistantHeaderProps {
  variant: 'docked' | 'floating'
  notebook: Notebook
  activePage: NotebookItem | null
  dragHandleClassName?: string
  dragHandleAttributes?: HTMLAttributes<HTMLDivElement>
  onCollapse: () => void
}

export function AiAssistantHeader({
  variant,
  notebook,
  activePage,
  dragHandleClassName,
  dragHandleAttributes,
  onCollapse,
}: AiAssistantHeaderProps) {
  const { t } = useTranslation()
  const isFloating = variant === 'floating'
  const headerClassName = isFloating
    ? `flex select-none items-start justify-between gap-3 border-b border-border-subtle px-4 py-2.5 ${dragHandleClassName ?? ''}`
    : 'flex items-start justify-between gap-3 px-4 py-2.5'

  return (
    <div {...dragHandleAttributes} className={headerClassName}>
      <div className="min-w-0">
        <div className="flex items-center gap-2">
          <Sparkles className="h-4 w-4 shrink-0 text-brand-brown" />
          <span className="truncate text-sm font-medium text-text-primary">{t('ai.title')}</span>
        </div>
        <div className="mt-1 flex items-center gap-1.5">
          <span className="max-w-[138px] truncate rounded-sm bg-surface-elevated px-1.5 py-0.5 text-[10px] text-text-tertiary">
            {activePage?.title ?? notebook.title}
          </span>
          <span className="rounded-sm bg-brand-brown/10 px-1.5 py-0.5 text-[10px] font-medium text-brand-brown">
            {t('ai.readOnly')}
          </span>
        </div>
      </div>
      <button
        type="button"
        onClick={onCollapse}
        onPointerDown={(event) => event.stopPropagation()}
        className="rounded p-1 text-text-tertiary transition-colors hover:bg-surface-hover hover:text-text-primary"
        aria-label={t('ai.collapse')}
        title={t('ai.collapse')}
      >
        <Minus className="h-3.5 w-3.5" />
      </button>
    </div>
  )
}
