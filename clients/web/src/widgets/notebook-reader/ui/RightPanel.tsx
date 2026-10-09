import { useId, useRef, useState } from 'react'
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
  const instanceId = useId()
  const [activeId, setActiveId] = useState<string | undefined>(tabs[0]?.id)
  const tabRefs = useRef<Array<HTMLButtonElement | null>>([])
  const active = tabs.find((tab) => tab.id === activeId) ?? tabs[0]

  if (active === undefined) {
    return null
  }

  const activeIndex = tabs.findIndex((tab) => tab.id === active.id)
  const tabId = (id: string) => `${instanceId}-tab-${id}`
  const panelId = (id: string) => `${instanceId}-panel-${id}`
  const activateAt = (index: number) => {
    const tab = tabs[index]
    if (tab === undefined) {
      return
    }
    setActiveId(tab.id)
    tabRefs.current[index]?.focus()
  }

  return (
    <div className="flex h-full min-h-0 flex-col">
      <div role="tablist" className="flex shrink-0 gap-1 border-b border-line px-2 pt-2">
        {tabs.map((tab, index) => {
          const selected = tab.id === active.id
          return (
            <button
              key={tab.id}
              ref={(element) => {
                tabRefs.current[index] = element
              }}
              id={tabId(tab.id)}
              type="button"
              role="tab"
              aria-selected={selected}
              aria-controls={panelId(tab.id)}
              tabIndex={selected ? 0 : -1}
              onClick={() => {
                setActiveId(tab.id)
              }}
              onKeyDown={(event) => {
                let nextIndex: number | null = null
                if (event.key === 'ArrowRight') {
                  nextIndex = (activeIndex + 1) % tabs.length
                } else if (event.key === 'ArrowLeft') {
                  nextIndex = (activeIndex - 1 + tabs.length) % tabs.length
                } else if (event.key === 'Home') {
                  nextIndex = 0
                } else if (event.key === 'End') {
                  nextIndex = tabs.length - 1
                }
                if (nextIndex !== null) {
                  event.preventDefault()
                  activateAt(nextIndex)
                }
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

      <div
        id={panelId(active.id)}
        role="tabpanel"
        aria-labelledby={tabId(active.id)}
        tabIndex={0}
        className="min-h-0 flex-1 overflow-y-auto"
      >
        {active.content}
      </div>
    </div>
  )
}
