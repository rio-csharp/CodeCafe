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
      className={`p-1.5 rounded-md transition-colors ${
        active ? 'bg-stone-100 text-brand-brown' : 'text-gray-500 hover:bg-gray-50 hover:text-black'
      }`}
    >
      {children}
    </button>
  )
}
