import type { LucideIcon } from 'lucide-react'
import type { AiDraftIntent } from '@/features/ai-assistant'

export interface QuickAction {
  id: string
  icon: LucideIcon
  label: string
  prompt: string
}

export interface DraftQuickAction {
  id: AiDraftIntent
  icon: LucideIcon
  label: string
  prompt: string
}
