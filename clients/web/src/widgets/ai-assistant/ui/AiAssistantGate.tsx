import { AlertCircle, Loader2, Lock, Sparkles } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { PanelNotice } from './PanelNotice'

interface AiAssistantGateProps {
  aiStatusPending: boolean
  aiStatusError: boolean
  aiEnabled: boolean
  userPending: boolean
  isSignedIn: boolean
}

export function AiAssistantGate({
  aiStatusPending,
  aiStatusError,
  aiEnabled,
  userPending,
  isSignedIn,
}: AiAssistantGateProps) {
  const { t } = useTranslation()

  if (aiStatusPending || userPending) {
    return <PanelNotice icon={Loader2} spin title={t('ai.checking')} />
  }

  if (aiStatusError) {
    return (
      <PanelNotice
        icon={AlertCircle}
        title={t('ai.statusErrorTitle')}
        description={t('ai.statusErrorDescription')}
      />
    )
  }

  if (!aiEnabled) {
    return (
      <PanelNotice
        icon={Sparkles}
        title={t('ai.disabledTitle')}
        description={t('ai.disabledDescription')}
      />
    )
  }

  if (!isSignedIn) {
    return (
      <div className="flex flex-1 flex-col items-center justify-center px-5 py-8 text-center">
        <div className="mb-3 flex h-10 w-10 items-center justify-center rounded-full bg-brand-brown/10">
          <Lock className="h-5 w-5 text-brand-brown" />
        </div>
        <p className="text-sm font-medium text-text-primary">{t('ai.signInTitle')}</p>
        <p className="mt-1 max-w-[220px] text-xs leading-5 text-text-tertiary">
          {t('ai.signInDescription')}
        </p>
        <Link
          to="/login"
          className="mt-3 rounded-md bg-text-primary px-3 py-1.5 text-xs font-medium text-text-inverse transition-colors hover:bg-surface-inverse-hover"
        >
          {t('ai.signIn')}
        </Link>
      </div>
    )
  }

  return null
}
