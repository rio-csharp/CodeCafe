import type { QuickAction } from '../lib/types'

interface QuickActionButtonProps {
  action: QuickAction
  disabled: boolean
  onClick: (prompt: string) => void
}

export function QuickActionButton({ action, disabled, onClick }: QuickActionButtonProps) {
  const Icon = action.icon

  return (
    <button
      type="button"
      onClick={() => onClick(action.prompt)}
      disabled={disabled}
      className="flex min-h-10 items-center gap-2 rounded-md border border-border-subtle px-3 py-2 text-left text-xs font-medium text-text-secondary transition-colors hover:border-border-hover hover:bg-surface-hover disabled:cursor-not-allowed disabled:opacity-50"
    >
      <Icon className="h-3.5 w-3.5 shrink-0 text-brand-brown" />
      <span className="min-w-0 truncate">{action.label}</span>
    </button>
  )
}
