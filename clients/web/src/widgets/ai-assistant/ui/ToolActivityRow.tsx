import { Loader2 } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import type { AiToolActivity } from '@/features/ai-assistant'
import { toolLabel } from '../lib/labels'

interface ToolActivityRowProps {
  activity: AiToolActivity
}

export function ToolActivityRow({ activity }: ToolActivityRowProps) {
  const { t } = useTranslation()
  const isDone = activity.status === 'done'

  return (
    <div className="rounded-md border border-border-subtle bg-surface-elevated px-2.5 py-1.5">
      <div className="flex items-center gap-2 text-[11px] text-text-secondary">
        {isDone ? (
          <span className="h-1.5 w-1.5 rounded-full bg-status-success" />
        ) : (
          <Loader2 className="h-3 w-3 animate-spin text-brand-brown" />
        )}
        <span className="min-w-0 truncate">{toolLabel(activity.name, t)}</span>
      </div>
    </div>
  )
}
