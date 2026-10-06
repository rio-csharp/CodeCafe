import { useState } from 'react'
import type { ReactNode } from 'react'

export interface RightPanelTab {
  id: string
  label: string
  icon: ReactNode
  content: ReactNode
}

export interface RightPanelProps {
  tabs: readonly RightPanelTab[]
}

/**
 * The right column is a tabbed panel, not a single widget: the outline lives
 * here today, and pinned surfaces like chat join as more tabs later. Tabs are
 * identified by id so callers never depend on order.
 */
export function RightPanel({ tabs }: RightPanelProps) {
  const [activeId, setActiveId] = useState<string | undefined>(tabs[0]?.id)
  const active = tabs.find((tab) => tab.id === activeId) ?? tabs[0]

  if (active === undefined) {
    return null
  }

  return (
    <div className="flex h-full min-h-0 flex-col">
      <div role="tablist" className="flex shrink-0 gap-1 border-b border-line px-2 pt-2">
        {tabs.map((tab) => {
          const selected = tab.id === active.id
          return (
            <button
              key={tab.id}
              type="button"
              role="tab"
              aria-selected={selected}
              onClick={() => {
                setActiveId(tab.id)
              }}
              className={[
                'flex items-center gap-1.5 rounded-t-md border-b-2 px-2.5 py-1.5 text-xs font-medium transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent',
                selected
                  ? 'border-accent text-accent-strong'
                  : 'border-transparent text-muted hover:bg-muted-soft hover:text-ink',
              ].join(' ')}
            >
              <svg
                viewBox="0 0 16 16"
                className="size-3.5"
                fill="none"
                stroke="currentColor"
                strokeWidth="1.6"
                strokeLinecap="round"
                strokeLinejoin="round"
                aria-hidden="true"
              >
                {tab.icon}
              </svg>
              {tab.label}
            </button>
          )
        })}
      </div>

      <div role="tabpanel" className="min-h-0 flex-1 overflow-y-auto">
        {active.content}
      </div>
    </div>
  )
}
