import { useState } from 'react'
import { Sparkles, Minus } from 'lucide-react'

export default function AiAssistant() {
  const [collapsed, setCollapsed] = useState(false)

  if (collapsed) {
    return (
      <div className="border-t border-border-subtle px-4 py-3">
        <button
          onClick={() => setCollapsed(false)}
          className="flex items-center gap-2 w-full text-left hover:bg-surface-hover rounded-md px-2 py-1.5 transition-colors"
        >
          <Sparkles className="h-4 w-4 text-brand-brown" />
          <span className="text-sm font-medium text-text-primary">AI Assistant</span>
        </button>
      </div>
    )
  }

  return (
    <div className="border-t border-border-subtle flex flex-col shrink-0">
      <div className="flex items-center justify-between px-4 py-2.5">
        <div className="flex items-center gap-2">
          <Sparkles className="h-4 w-4 text-brand-brown" />
          <span className="text-sm font-medium text-text-primary">AI Assistant</span>
        </div>
        <button
          onClick={() => setCollapsed(true)}
          className="p-1 text-text-tertiary hover:text-text-primary hover:bg-surface-hover rounded transition-colors"
        >
          <Minus className="h-3.5 w-3.5" />
        </button>
      </div>

      <div className="px-4 pb-4 flex flex-col items-center justify-center py-10 text-center">
        <div className="h-10 w-10 rounded-full bg-brand-brown/10 flex items-center justify-center mb-3">
          <Sparkles className="h-5 w-5 text-brand-brown" />
        </div>
        <p className="text-sm font-medium text-text-secondary">Coming soon</p>
        <p className="text-xs text-text-tertiary mt-1 max-w-[200px]">
          AI-powered assistance for reading, writing, and understanding your notes.
        </p>
      </div>
    </div>
  )
}
