import { useState, useEffect, type RefObject } from 'react'
import type { OutlineHeading } from '@/entities/notebook'

interface NotebookOutlineProps {
  headings: OutlineHeading[]
  scrollContainerRef?: RefObject<HTMLElement | null>
}

export default function NotebookOutline({ headings, scrollContainerRef }: NotebookOutlineProps) {
  const [activeId, setActiveId] = useState<string | null>(null)

  useEffect(() => {
    const container = scrollContainerRef?.current
    if (!container) return

    const updateActive = () => {
      const headingEls = Array.from(container.querySelectorAll('[id^="heading-"]'))
      if (headingEls.length === 0) return

      const scrollTop = container.scrollTop
      let current = headingEls[0]

      for (const el of headingEls) {
        const htmlEl = el as HTMLElement
        if (htmlEl.offsetTop <= scrollTop + 120) {
          current = el
        } else {
          break
        }
      }

      setActiveId(current ? current.id : null)
    }

    container.addEventListener('scroll', updateActive, { passive: true })
    updateActive()

    return () => container.removeEventListener('scroll', updateActive)
  }, [headings, scrollContainerRef])

  const handleClick = (id: string) => {
    const el = document.getElementById(id)
    if (el) {
      el.scrollIntoView({ behavior: 'smooth', block: 'start' })
    }
  }

  if (headings.length === 0) {
    return (
      <div className="px-5 py-6">
        <h3 className="text-[11px] font-semibold uppercase tracking-wider text-text-tertiary mb-3">
          On this page
        </h3>
        <p className="text-xs text-text-tertiary">No headings on this page.</p>
      </div>
    )
  }

  return (
    <div className="px-5 py-6">
      <h3 className="text-[11px] font-semibold uppercase tracking-wider text-text-tertiary mb-3">
        On this page
      </h3>
      <ul className="space-y-2">
        {headings.map((h) => {
          const isActive = h.id === activeId
          return (
            <li key={h.id}>
              <button
                onClick={() => handleClick(h.id)}
                className={`text-left text-[13px] transition-colors w-full truncate ${
                  isActive
                    ? 'text-brand-brown font-medium'
                    : 'text-text-secondary hover:text-brand-brown'
                }`}
                style={{ paddingLeft: `${(h.level - 1) * 14}px` }}
              >
                {h.text}
              </button>
            </li>
          )
        })}
      </ul>
    </div>
  )
}
