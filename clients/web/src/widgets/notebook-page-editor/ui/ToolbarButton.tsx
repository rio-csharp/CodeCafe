import type { ReactNode } from 'react'

interface ToolbarButtonProps {
  active?: boolean
  onClick: () => void
  children: ReactNode
  title: string
}

export default function ToolbarButton({ active, onClick, children, title }: ToolbarButtonProps) {
  return (
    <button
      type="button"
      onClick={onClick}
      title={title}
      aria-label={title}
      aria-pressed={active}
      className={`p-1.5 rounded-md transition-colors ${
        active ? 'bg-surface-active text-brand-brown' : 'text-text-secondary hover:bg-surface-hover hover:text-text-primary'
      }`}
    >
      {children}
    </button>
  )
}
