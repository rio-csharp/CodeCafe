import { Globe, Lock, EyeOff } from 'lucide-react'
import { NOTEBOOK_VISIBILITY_LABELS, type NotebookVisibility } from '@/entities/notebook'

const config: Record<
  NotebookVisibility,
  { label: string; icon: typeof Globe; className: string }
> = {
  public: {
    label: 'Public',
    icon: Globe,
    className: 'bg-status-success-bg text-status-success border-status-success-border',
  },
  private: {
    label: 'Private',
    icon: Lock,
    className: 'bg-surface-active text-text-secondary border-border-default',
  },
  unlisted: {
    label: 'Unlisted',
    icon: EyeOff,
    className: 'bg-status-favorite-bg text-status-favorite border-status-favorite-border',
  },
}

export default function VisibilityBadge({ visibility }: { visibility: NotebookVisibility }) {
  const { label, icon: Icon, className } = config[visibility]
  return (
    <span
      className={`inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-xs font-medium ${className}`}
    >
      <Icon className="h-3 w-3" />
      {NOTEBOOK_VISIBILITY_LABELS[visibility] ?? label}
    </span>
  )
}
