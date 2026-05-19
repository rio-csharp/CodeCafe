import { useState, useEffect } from 'react'
import type { OutlineHeading } from '../../utils/extractOutline'

interface NotebookOutlineProps {
  headings: OutlineHeading[]
}

export default function NotebookOutline({ headings }: NotebookOutlineProps) {
  const [activeId, setActiveId] = useState<string | null>(null)

  useEffect(() => {
    const main = document.querySelector('main')
    if (!main) return

    const updateActive = () => {
      const headingEls = Array.from(main.querySelectorAll('[id^="heading-"]'))
      if (headingEls.length === 0) return

      const scrollTop = main.scrollTop
      let current: Element | null = headingEls[0]

      for (const el of headingEls) {
        if ((el as HTMLElement).offsetTop <= scrollTop + 120) {
          current = el
        } else {
          break
        }
      }

      setActiveId(current ? current.id : null)
    }

    main.addEventListener('scroll', updateActive, { passive: true })
    updateActive()

    return () => main.removeEventListener('scroll', updateActive)
  }, [headings])

  const handleClick = (id: string) => {
    const el = document.getElementById(id)
    if (el) {
      el.scrollIntoView({ behavior: 'smooth', block: 'start' })
    }
  }

  if (headings.length === 0) {
    return (
      <div className="px-5 py-6">
        <h3 className="text-[11px] font-semibold uppercase tracking-wider text-gray-400 mb-3">
          On this page
        </h3>
        <p className="text-xs text-gray-400">No headings on this page.</p>
      </div>
    )
  }

  return (
    <div className="px-5 py-6">
      <h3 className="text-[11px] font-semibold uppercase tracking-wider text-gray-400 mb-3">
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
                    : 'text-gray-600 hover:text-brand-brown'
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
