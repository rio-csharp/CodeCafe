import { Loader2 } from 'lucide-react'
import { useTranslation } from 'react-i18next'

export function AssistantThinking() {
  const { t } = useTranslation()

  return (
    <div className="flex justify-start">
      <div className="inline-flex items-center gap-2 rounded-md border border-border-subtle bg-surface-elevated px-3 py-2 text-xs text-text-secondary">
        <Loader2 className="h-3.5 w-3.5 animate-spin text-brand-brown" />
        {t('ai.thinking')}
      </div>
    </div>
  )
}
