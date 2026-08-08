import { useRef } from 'react'
import { useMessagesAutoScroll } from './useMessagesAutoScroll'

interface AssistantViewStateOptions {
  aiStatusPending: boolean
  aiStatusError: boolean
  userPending: boolean
  aiEnabled: boolean
  isSignedIn: boolean
  /** Conversation state that should keep the message list scrolled to the end. */
  watch: readonly unknown[]
}

/**
 * View-state plumbing for the assistant panel: auto-scrolls the message list
 * and decides whether the gate (loading / disabled / signed-out) blocks the
 * conversation UI.
 */
export function useAssistantViewState({
  aiStatusPending,
  aiStatusError,
  userPending,
  aiEnabled,
  isSignedIn,
  watch,
}: AssistantViewStateOptions) {
  const messagesEndRef = useRef<HTMLDivElement>(null)
  useMessagesAutoScroll(messagesEndRef, watch)

  const isGateBlocking = aiStatusPending || userPending || aiStatusError || !aiEnabled || !isSignedIn

  return { messagesEndRef, isGateBlocking }
}
