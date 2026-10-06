import type { ReactNode } from 'react'

export interface EmptyShelfProps {
  icon: ReactNode
  title: string
  hint?: string
  action?: ReactNode
}

/** An empty tab should still say something useful — and offer the way forward. */
export function EmptyShelf({ icon, title, hint, action }: EmptyShelfProps) {
  return (
    <div className="col-span-full flex flex-col items-center gap-2 py-16 text-center">
      <div className="grid size-12 place-items-center rounded-full border border-dashed border-line text-muted">
        {icon}
      </div>
      <p className="text-sm font-medium text-ink">{title}</p>
      {hint !== undefined ? <p className="max-w-xs text-xs leading-relaxed text-muted">{hint}</p> : null}
      {action !== undefined ? <div className="mt-2">{action}</div> : null}
    </div>
  )
}
