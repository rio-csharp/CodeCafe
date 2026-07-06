import { Globe, Lock, EyeOff } from 'lucide-react'
import type { NotebookVisibility } from '@/entities/notebook'
import { useTranslation } from 'react-i18next'

const config: Record<
  NotebookVisibility,
  { icon: typeof Globe; className: string }
> = {
  public: {
    icon: Globe,
    className: 'bg-status-success-bg text-status-success border-status-success-border',
  },
  private: {
    icon: Lock,
    className: 'bg-surface-active text-text-secondary border-border-default',
  },
  unlisted: {
    icon: EyeOff,
    className: 'bg-status-favorite-bg text-status-favorite border-status-favorite-border',
  },
}

export default function VisibilityBadge({ visibility }: { visibility: NotebookVisibility }) {
  const { icon: Icon, className } = config[visibility]
  const { t } = useTranslation()
  const labels = {
    private: t('notebook.visibilityPrivate'),
    unlisted: t('notebook.visibilityUnlisted'),
    public: t('notebook.visibilityPublic'),
  } satisfies Record<NotebookVisibility, string>

  return (
    <span
      className={`inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-xs font-medium ${className}`}
    >
      <Icon className="h-3 w-3" />
      {labels[visibility]}
    </span>
  )
}
