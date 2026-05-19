import { Globe, Lock, EyeOff } from 'lucide-react'
import type { NotebookVisibility } from '../types'

const config: Record<
  NotebookVisibility,
  { label: string; icon: typeof Globe; className: string }
> = {
  public: {
    label: 'Public',
    icon: Globe,
    className: 'bg-green-50 text-green-700 border-green-200',
  },
  private: {
    label: 'Private',
    icon: Lock,
    className: 'bg-gray-100 text-gray-600 border-gray-200',
  },
  unlisted: {
    label: 'Unlisted',
    icon: EyeOff,
    className: 'bg-amber-50 text-amber-700 border-amber-200',
  },
}

export default function VisibilityBadge({ visibility }: { visibility: NotebookVisibility }) {
  const { label, icon: Icon, className } = config[visibility]
  return (
    <span
      className={`inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-xs font-medium ${className}`}
    >
      <Icon className="h-3 w-3" />
      {label}
    </span>
  )
}
