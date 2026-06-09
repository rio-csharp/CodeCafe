import { MarkdownRenderer } from '@/shared/ui/MarkdownRenderer'

interface MessageBubbleProps {
  role: 'assistant' | 'user'
  text: string
}

export function MessageBubble({ role, text }: MessageBubbleProps) {
  const isUser = role === 'user'
  const assistantMarkdownClassName =
    'text-inherit prose-headings:text-inherit prose-p:text-inherit prose-strong:text-inherit prose-em:text-inherit prose-li:text-inherit prose-ol:text-inherit prose-ul:text-inherit prose-blockquote:text-inherit prose-code:text-inherit prose-a:text-brand-brown'

  return (
    <div className={`flex ${isUser ? 'justify-end' : 'justify-start'}`}>
      <div
        className={`max-w-[92%] rounded-md px-3 py-2 text-xs leading-5 ${
          isUser
            ? 'bg-brand-brown text-white dark:bg-brand-brown-light dark:text-surface'
            : 'border border-border-subtle bg-surface-elevated text-text-primary'
        }`}
      >
        {isUser ? (
          <span className="whitespace-pre-wrap">{text}</span>
        ) : (
          <MarkdownRenderer className={assistantMarkdownClassName} text={text} />
        )}
      </div>
    </div>
  )
}
