import { useTranslation } from 'react-i18next'
import type { AiAssistantErrorCode } from '@/features/ai-assistant'

export function toolLabel(name: string, t: ReturnType<typeof useTranslation>['t']): string {
  const fallback = name
    .split('_')
    .filter(Boolean)
    .map((part) => `${part.slice(0, 1).toUpperCase()}${part.slice(1)}`)
    .join(' ')

  return t(`ai.tools.${name}`, { defaultValue: fallback })
}

export function errorKey(code: AiAssistantErrorCode): string {
  return `ai.errors.${code}`
}
