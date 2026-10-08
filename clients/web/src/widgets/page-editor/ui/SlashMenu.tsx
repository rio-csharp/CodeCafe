import { useTranslation } from 'react-i18next'
import type { SlashItem } from '../lib/blockTypes'

export interface SlashMenuProps {
  items: readonly SlashItem[]
  activeIndex: number
  onPick: (item: SlashItem) => void
  onHover: (index: number) => void
}

/** The Notion-style "/" menu; the host text block handles the keyboard. */
export function SlashMenu({ items, activeIndex, onPick, onHover }: SlashMenuProps) {
  const { t } = useTranslation()

  return (
    <div
      role="listbox"
      aria-label={t('editor.slash.label')}
      className="absolute left-2 top-full z-20 mt-1 w-56 overflow-hidden rounded-xl border border-line bg-card py-1 shadow-lg"
    >
      {items.length === 0 ? (
        <p className="px-3 py-1.5 text-xs text-muted">{t('editor.slash.empty')}</p>
      ) : (
        items.map((item, index) => (
          <button
            key={item.id}
            type="button"
            role="option"
            aria-selected={index === activeIndex}
            // Keep focus (and the slash query) in the editor while picking.
            onMouseDown={(event) => {
              event.preventDefault()
            }}
            onClick={() => {
              onPick(item)
            }}
            onMouseEnter={() => {
              onHover(index)
            }}
            className={`flex w-full items-center gap-2 px-3 py-1.5 text-left text-sm transition-colors ${
              index === activeIndex ? 'bg-muted-soft text-ink' : 'text-muted'
            }`}
          >
            {t(`editor.slash.${item.id}`)}
          </button>
        ))
      )}
    </div>
  )
}
