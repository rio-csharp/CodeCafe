import { MarkdownRenderer } from '@/shared/ui/MarkdownRenderer'

interface MessageBubbleProps {
  role: 'assistant' | 'user'
  text: string
}

export function MessageBubble({ role, text }: MessageBubbleProps) {
  const isUser = role === 'user'

  return (
    <div className={`flex ${isUser ? 'justify-end' : 'justify-start'}`}>
      <div
        className={`max-w-[92%] rounded-md px-3 py-2 text-xs leading-5 ${
          isUser
            ? 'bg-text-primary text-text-inverse'
            : 'border border-border-subtle bg-surface-elevated text-text-primary'
        }`}
      >
        {isUser ? (
          <span className="whitespace-pre-wrap">{text}</span>
        ) : (
          <MarkdownRenderer text={text} />
        )}
      </div>
    </div>
  )
}
