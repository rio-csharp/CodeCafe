import { useState } from 'react'
import { WorkspaceSidebar } from './WorkspaceSidebar'

const staticMessages = [
  {
    role: 'user' as const,
    name: 'You',
    time: '10:42 AM',
    content: 'What are we currently building in CodeCafe?',
  },
  {
    role: 'assistant' as const,
    name: 'CodeCafe AI',
    time: '10:42 AM',
    content: `We are building **CodeCafe**, an *AI-native engineering workspace* with persistent project memory.

Here's a summary of what we're building:

**Project Goal**

Create a platform that helps engineers plan, build, run, and evolve software projects with the power of AI.

**Core Capabilities**
- Persistent project memory that retains decisions, architecture, and context
- AI chat that understands your entire codebase and workspace
- Code browsing with AI insights and intelligent suggestions
- Safe preview environments with real-time logs
- GitHub integration and automated task management

**Current Phase** [Phase 1 MVP]

Building the core platform with memory, preview environments, and AI integration.

**Top Priorities**
1. Workspace persistence [In Progress]
2. Safe preview environments [To Do]
3. GitHub integration [To Do]
4. AI engineering workflow [To Do]

Would you like me to dive deeper into any of these areas?`,
  },
]

const contextData = {
  memory: [
    'Architectural decisions',
    'Project goals & principles',
    'Tech stack rationale',
    'Constraints & preferences',
    'API design decisions',
  ],
  tasks: [
    { title: 'Add workspace persistence', status: 'In Progress' },
    { title: 'Add run logs viewer', status: 'To Do' },
    { title: 'Improve memory summarization', status: 'To Do' },
    { title: 'Add preview deployment', status: 'To Do' },
  ],
  files: [
    { name: 'README.md', icon: 'doc' },
    { name: 'docker-compose.yml', icon: 'doc' },
    { name: 'Program.cs', icon: 'code' },
    { name: 'AppShell.tsx', icon: 'code' },
    { name: 'WorkspaceOverview.tsx', icon: 'code' },
  ],
  repo: {
    name: 'rio-csharp/CodeCafe',
    branch: 'main',
    updatedAt: '2m ago',
  },
}

export function WorkspaceChatPage() {
  const [input, setInput] = useState('')

  return (
    <div className="flex min-h-screen bg-bg text-text">
      <WorkspaceSidebar activeItem="Chat" />

      {/* Main chat area */}
      <div className="flex flex-1 flex-col">
        {/* Header */}
        <header className="flex items-center justify-between border-b border-border px-6 py-4">
          <div>
            <h1 className="m-0 text-2xl font-bold tracking-tight">Chat</h1>
            <p className="m-0 mt-1 text-sm text-muted">Ask anything about your workspace. Get answers with full project context.</p>
          </div>
          <div className="flex items-center gap-3">
            <span className="inline-flex items-center gap-1.5 rounded-full border border-success/20 bg-success/8 px-3 py-1 text-xs font-semibold text-success">
              <span className="h-2 w-2 rounded-full bg-success" />
              Workspace Context On
            </span>
            <button className="inline-flex items-center gap-1.5 rounded-lg border border-border px-3 py-1.5 text-sm font-medium text-muted transition hover:text-text">
              <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/></svg>
              New Chat
            </button>
            <button className="inline-flex h-8 w-8 items-center justify-center rounded-lg border border-border text-muted transition hover:text-text">
              <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="12" cy="12" r="1"/><circle cx="19" cy="12" r="1"/><circle cx="5" cy="12" r="1"/></svg>
            </button>
          </div>
        </header>

        {/* Messages + Context layout */}
        <div className="flex flex-1 overflow-hidden">
          {/* Messages */}
          <div className="flex flex-1 flex-col overflow-hidden">
            <div className="flex-1 overflow-auto px-6 py-6">
              <div className="mx-auto max-w-[800px] flex flex-col gap-5">
                {staticMessages.map((msg, i) => (
                  <div key={i} className={`flex gap-3 ${msg.role === 'user' ? '' : ''}`}>
                    <div className="shrink-0">
                      {msg.role === 'user' ? (
                        <div className="h-9 w-9 overflow-hidden rounded-full border border-border">
                          <img src="https://github.com/rio-csharp.png" alt="You" className="h-full w-full object-cover" />
                        </div>
                      ) : (
                        <div className="grid h-9 w-9 place-items-center rounded-full bg-accent/15 text-accent">
                          <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2"><path d="M12 2a10 10 0 1 0 10 10A10 10 0 0 0 12 2z"/><path d="M12 6a6 6 0 1 0 6 6 6 6 0 0 0-6-6z"/></svg>
                        </div>
                      )}
                    </div>
                    <div className="min-w-0 flex-1">
                      <div className="flex items-center gap-2">
                        <span className="text-sm font-semibold">{msg.name}</span>
                        <span className="text-xs text-muted">{msg.time}</span>
                      </div>
                      <div className={`mt-1 rounded-xl p-4 text-sm leading-relaxed ${
                        msg.role === 'user'
                          ? 'bg-surface/40'
                          : 'border border-border bg-surface/30'
                      }`}>
                        <MarkdownContent content={msg.content} />
                        {msg.role === 'assistant' && (
                          <div className="mt-3 flex items-center gap-2 border-t border-border pt-2">
                            <button className="text-muted transition hover:text-text">
                              <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><rect x="9" y="9" width="13" height="13" rx="2" ry="2"/><path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"/></svg>
                            </button>
                            <button className="text-muted transition hover:text-text">
                              <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><path d="M14 9V5a3 3 0 0 0-3-3l-4 9v11h11.28a2 2 0 0 0 2-1.7l1.38-9a2 2 0 0 0-2-2.3zM7 22H4a2 2 0 0 1-2-2v-7a2 2 0 0 1 2-2h3"/></svg>
                            </button>
                            <button className="text-muted transition hover:text-text">
                              <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><path d="M10 15v4a3 3 0 0 0 3 3l4-9V2H5.72a2 2 0 0 0-2 1.7l-1.38 9a2 2 0 0 0 2 2.3zm7-13h3a2 2 0 0 1 2 2v7a2 2 0 0 1-2 2h-3"/></svg>
                            </button>
                          </div>
                        )}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </div>

            {/* Composer */}
            <div className="border-t border-border px-6 py-4">
              <div className="mx-auto max-w-[800px]">
                <div className="flex items-end gap-3 rounded-xl border border-border bg-surface/40 p-3">
                  <div className="flex items-center gap-1 text-muted">
                    <button className="inline-flex h-8 w-8 items-center justify-center rounded-lg transition hover:bg-accent/8 hover:text-text">
                      <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2"><path d="M21.44 11.05l-9.19 9.19a6 6 0 0 1-8.49-8.49l9.19-9.19a4 4 0 0 1 5.66 5.66l-9.2 9.19a2 2 0 0 1-2.83-2.83l8.49-8.48"/></svg>
                    </button>
                    <button className="inline-flex h-8 w-8 items-center justify-center rounded-lg transition hover:bg-accent/8 hover:text-text">
                      <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/></svg>
                    </button>
                    <button className="inline-flex h-8 w-8 items-center justify-center rounded-lg transition hover:bg-accent/8 hover:text-text">
                      <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2"><path d="M10.5 6L8 9h8l-2.5-3"/><path d="M12 9v10"/><path d="M8 19h8"/></svg>
                    </button>
                  </div>
                  <textarea
                    value={input}
                    onChange={(e) => setInput(e.target.value)}
                    placeholder="Ask anything about your workspace..."
                    rows={1}
                    className="max-h-[120px] min-h-[20px] flex-1 resize-none border-0 bg-transparent py-1.5 text-sm text-text outline-none placeholder:text-muted"
                    onKeyDown={(e) => {
                      if (e.key === 'Enter' && !e.shiftKey) {
                        e.preventDefault()
                      }
                    }}
                  />
                  <button className="grid h-9 w-9 shrink-0 place-items-center rounded-lg bg-accent text-[#070a12] transition hover:opacity-90">
                    <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2"><polygon points="5 3 19 12 5 21 5 3"/></svg>
                  </button>
                </div>
                <p className="mt-2 text-center text-xs text-muted">AI answers based on your workspace context</p>
              </div>
            </div>
          </div>

          {/* Right context panel */}
          <aside className="hidden w-[320px] shrink-0 flex-col gap-5 overflow-auto border-l border-border bg-bg/40 p-5 xl:flex">
            {/* Workspace Context toggle */}
            <div className="flex items-center justify-between">
              <h3 className="m-0 text-sm font-bold">Workspace Context</h3>
              <button className="relative inline-flex h-5 w-9 items-center rounded-full bg-success">
                <span className="absolute left-[18px] h-3.5 w-3.5 rounded-full bg-white" />
              </button>
            </div>
            <p className="m-0 text-xs text-muted">AI has full awareness of your workspace.</p>

            {/* Memory */}
            <div className="rounded-lg border border-border bg-surface/30 p-4">
              <div className="mb-3 flex items-center justify-between">
                <div className="flex items-center gap-2 text-sm font-bold">
                  <BrainIcon />
                  Memory <span className="text-xs font-normal text-muted">(328 items)</span>
                </div>
                <a href="#" className="text-xs font-medium text-accent no-underline">View all</a>
              </div>
              <ul className="m-0 flex flex-col gap-2 pl-4 text-xs text-muted">
                {contextData.memory.map((item, i) => (
                  <li key={i} className="leading-snug">{item}</li>
                ))}
              </ul>
            </div>

            {/* Relevant Tasks */}
            <div className="rounded-lg border border-border bg-surface/30 p-4">
              <div className="mb-3 flex items-center justify-between">
                <div className="flex items-center gap-2 text-sm font-bold">
                  <span className="text-sm">💡</span>
                  Relevant Tasks
                </div>
                <a href="#" className="text-xs font-medium text-accent no-underline">View all</a>
              </div>
              <div className="flex flex-col gap-2.5">
                {contextData.tasks.map((task, i) => (
                  <div key={i} className="flex items-center justify-between gap-2 text-xs">
                    <span className="text-muted">{task.title}</span>
                    <span className={`shrink-0 rounded px-1.5 py-0.5 text-[10px] font-bold ${
                      task.status === 'In Progress' ? 'border border-success/20 bg-success/8 text-success' : 'border border-border bg-bg/50 text-muted'
                    }`}>{task.status}</span>
                  </div>
                ))}
              </div>
            </div>

            {/* Relevant Files */}
            <div className="rounded-lg border border-border bg-surface/30 p-4">
              <div className="mb-3 flex items-center justify-between">
                <div className="flex items-center gap-2 text-sm font-bold">
                  <span className="text-sm">📁</span>
                  Relevant Files
                </div>
                <a href="#" className="text-xs font-medium text-accent no-underline">View all</a>
              </div>
              <div className="flex flex-col gap-2">
                {contextData.files.map((file, i) => (
                  <div key={i} className="flex items-center gap-2 text-xs text-muted">
                    <FileIcon type={file.icon} />
                    <span>{file.name}</span>
                  </div>
                ))}
              </div>
            </div>

            {/* Connected Repo */}
            <div className="rounded-lg border border-border bg-surface/30 p-4">
              <h3 className="m-0 mb-3 text-sm font-bold">Connected Repo</h3>
              <div className="flex items-center gap-2 text-sm">
                <svg viewBox="0 0 24 24" width="16" height="16" fill="currentColor"><path d="M12 2C6.48 2 2 6.59 2 12.25c0 4.53 2.87 8.37 6.84 9.72.5.09.68-.22.68-.49 0-.24-.01-1.03-.01-1.87-2.78.62-3.37-1.21-3.37-1.21-.46-1.2-1.11-1.52-1.11-1.52-.91-.64.07-.63.07-.63 1 .08 1.53 1.06 1.53 1.06.9 1.58 2.35 1.12 2.92.86.09-.67.35-1.12.63-1.38-2.22-.26-4.56-1.15-4.56-5.1 0-1.13.39-2.05 1.03-2.78-.1-.26-.45-1.31.1-2.73 0 0 .84-.28 2.75 1.06A9.3 9.3 0 0 1 12 6.84c.85 0 1.71.12 2.51.35 1.91-1.34 2.75-1.06 2.75-1.06.55 1.42.2 2.47.1 2.73.64.73 1.03 1.65 1.03 2.78 0 3.96-2.34 4.83-4.57 5.09.36.32.68.94.68 1.9 0 1.37-.01 2.47-.01 2.8 0 .27.18.58.69.48A10.27 10.27 0 0 0 22 12.25C22 6.59 17.52 2 12 2Z"/></svg>
                <span className="font-medium">{contextData.repo.name}</span>
              </div>
              <div className="mt-2 flex items-center justify-between text-xs text-muted">
                <div className="flex items-center gap-1">
                  <svg viewBox="0 0 24 24" width="12" height="12" fill="none" stroke="currentColor" strokeWidth="2"><line x1="6" y1="3" x2="6" y2="15"/><circle cx="18" cy="6" r="3"/><circle cx="6" cy="18" r="3"/><path d="M18 9a9 9 0 0 1-9 9"/></svg>
                  {contextData.repo.branch}
                </div>
                <span>Updated {contextData.repo.updatedAt}</span>
              </div>
            </div>
          </aside>
        </div>
      </div>
    </div>
  )
}

/* Simple markdown renderer for static content */
function MarkdownContent({ content }: { content: string }) {
  const lines = content.split('\n')
  const elements: React.ReactNode[] = []
  let inList = false
  let listItems: React.ReactNode[] = []

  const flushList = () => {
    if (inList && listItems.length > 0) {
      elements.push(<ul key={`list-${elements.length}`} className="m-0 my-2 flex flex-col gap-1 pl-5">{listItems}</ul>)
      listItems = []
      inList = false
    }
  }

  lines.forEach((line, i) => {
    const trimmed = line.trim()
    if (!trimmed) {
      flushList()
      return
    }

    // Heading
    if (trimmed.startsWith('**') && trimmed.endsWith('**') && !trimmed.includes('- ')) {
      flushList()
      const text = trimmed.slice(2, -2)
      elements.push(<div key={i} className="mt-3 font-bold">{text}</div>)
      return
    }

    // List item
    if (trimmed.startsWith('- ')) {
      inList = true
      const text = trimmed.slice(2)
      listItems.push(<li key={i} className="leading-snug"><InlineMarkdown text={text} /></li>)
      return
    }

    // Numbered list
    if (/^\d+\.\s/.test(trimmed)) {
      inList = true
      const text = trimmed.replace(/^\d+\.\s/, '')
      listItems.push(<li key={i} className="leading-snug"><InlineMarkdown text={text} /></li>)
      return
    }

    // Badge in brackets at end
    const badgeMatch = trimmed.match(/^(.+?)\s*\[(.+?)\]$/)
    if (badgeMatch) {
      flushList()
      const [, text, badge] = badgeMatch
      const badgeClass = badge === 'In Progress'
        ? 'border-success/20 bg-success/8 text-success'
        : 'border-border bg-bg/50 text-muted'
      elements.push(
        <div key={i} className="flex items-center justify-between gap-2">
          <span>{text}</span>
          <span className={`shrink-0 rounded border px-1.5 py-0.5 text-[10px] font-bold ${badgeClass}`}>{badge}</span>
        </div>
      )
      return
    }

    flushList()
    elements.push(<p key={i} className="m-0 my-1"><InlineMarkdown text={trimmed} /></p>)
  })

  flushList()
  return <>{elements}</>
}

function InlineMarkdown({ text }: { text: string }) {
  // Very simple inline markdown: **bold**, *italic*, `code`
  const parts = text.split(/(\*\*.*?\*\*|\*.*?\*|`.*?`)/g)
  return (
    <>
      {parts.map((part, i) => {
        if (part.startsWith('**') && part.endsWith('**')) {
          return <strong key={i}>{part.slice(2, -2)}</strong>
        }
        if (part.startsWith('*') && part.endsWith('*') && !part.startsWith('**')) {
          return <em key={i}>{part.slice(1, -1)}</em>
        }
        if (part.startsWith('`') && part.endsWith('`')) {
          return <code key={i} className="rounded bg-bg/60 px-1 py-0.5 text-xs">{part.slice(1, -1)}</code>
        }
        return <span key={i}>{part}</span>
      })}
    </>
  )
}

function BrainIcon() {
  return <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2"><path d="M12 2a10 10 0 1 0 10 10A10 10 0 0 0 12 2z"/><path d="M12 6a6 6 0 1 0 6 6 6 6 0 0 0-6-6z"/></svg>
}

function FileIcon({ type }: { type: string }) {
  if (type === 'code') {
    return <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><polyline points="16 18 22 12 16 6"/><polyline points="8 6 2 12 8 18"/></svg>
  }
  return <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>
}
