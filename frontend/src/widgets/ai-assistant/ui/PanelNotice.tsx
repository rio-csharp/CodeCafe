import type { LucideIcon } from 'lucide-react'

interface PanelNoticeProps {
  icon: LucideIcon
  spin?: boolean
  title: string
  description?: string
}

export function PanelNotice({ icon: Icon, spin, title, description }: PanelNoticeProps) {
  return (
    <div className="flex flex-1 flex-col items-center justify-center px-5 py-8 text-center">
      <div className="mb-3 flex h-10 w-10 items-center justify-center rounded-full bg-brand-brown/10">
        <Icon className={`h-5 w-5 text-brand-brown ${spin ? 'animate-spin' : ''}`} />
      </div>
      <p className="text-sm font-medium text-text-primary">{title}</p>
      {description && (
        <p className="mt-1 max-w-[220px] text-xs leading-5 text-text-tertiary">{description}</p>
      )}
    </div>
  )
}
