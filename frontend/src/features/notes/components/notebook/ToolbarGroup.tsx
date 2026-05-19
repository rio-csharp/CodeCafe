import type { ReactNode } from 'react'

interface ToolbarGroupProps {
  children: ReactNode
  showDivider?: boolean
}

export default function ToolbarGroup({ children, showDivider = false }: ToolbarGroupProps) {
  return (
    <>
      {children}
      {showDivider && <div className="w-px h-5 bg-gray-200 mx-1" />}
    </>
  )
}
