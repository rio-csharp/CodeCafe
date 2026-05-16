export const categories = [
  { name: 'All Notes', count: 28, icon: 'doc' as const },
  { name: 'Decisions', count: 8, icon: 'decision' as const },
  { name: 'Ideas', count: 6, icon: 'idea' as const },
  { name: 'Plans', count: 7, icon: 'plan' as const },
  { name: 'Research', count: 4, icon: 'research' as const },
  { name: 'Other', count: 3, icon: 'other' as const },
]

export const tags = [
  { name: 'architecture', count: 6 },
  { name: 'mvp', count: 5 },
  { name: 'memory', count: 4 },
  { name: 'preview', count: 3 },
  { name: 'ai', count: 3 },
  { name: 'integration', count: 2 },
]

export interface RecentNote {
  id: number
  title: string
  desc: string
  updatedAt: string
  tag: string
  pinned: boolean
}

export const recentNotes: RecentNote[] = [
  { id: 1, title: 'Phase 1 MVP Plan', desc: 'Core features and scope for the first release...', updatedAt: '1h ago', tag: 'mvp', pinned: true },
  { id: 2, title: 'Workspace Memory Design', desc: 'How we store, retrieve and utilize memory...', updatedAt: '1d ago', tag: 'architecture', pinned: true },
  { id: 3, title: 'Sandbox Security Plan', desc: 'Security considerations for preview environments...', updatedAt: '2d ago', tag: 'security', pinned: true },
  { id: 4, title: 'GitHub Integration Flow', desc: 'OAuth flow and repository synchronization...', updatedAt: '3d ago', tag: 'integration', pinned: false },
  { id: 5, title: 'AI Context Strategy', desc: 'What context to include in AI conversations...', updatedAt: '4d ago', tag: 'ai', pinned: false },
  { id: 6, title: 'Preview Environment Architecture', desc: 'Design of isolated, reproducible environments...', updatedAt: '5d ago', tag: 'preview', pinned: false },
  { id: 7, title: 'Future Ideas', desc: 'Long-term ideas and potential features...', updatedAt: '6d ago', tag: 'ideas', pinned: false },
]

export type NoteSection =
  | { heading: string; type: 'paragraph'; content?: string; list?: string[] }
  | { heading: string; type: 'checklist'; items: { text: string; done: boolean }[] }
  | { heading: string; type: 'bullet'; items: string[] }

export const noteContent = {
  title: 'Phase 1 MVP Plan',
  tag: 'mvp',
  updatedAt: '1h ago',
  author: 'Rio',
  sections: [
    {
      heading: 'Goal',
      type: 'paragraph' as const,
      content: "Build the core platform that demonstrates CodeCafe's value:",
      list: [
        'Persistent project memory',
        'Safe preview environments',
        'AI chat with full workspace context',
        'GitHub integration',
      ],
    },
    {
      heading: 'Scope',
      type: 'checklist' as const,
      items: [
        { text: 'Workspace creation & management', done: true },
        { text: 'Project memory (decisions, context, architecture)', done: true },
        { text: 'Preview environments (isolated & reproducible)', done: false },
        { text: 'Run logs viewer', done: false },
        { text: 'AI chat with workspace context', done: false },
        { text: 'Code browsing', done: false },
        { text: 'GitHub sync (read-only for MVP)', done: false },
      ],
    },
    {
      heading: 'Out of Scope (for now)',
      type: 'bullet' as const,
      items: [
        'Real-time collaborative editing',
        'Advanced CI/CD',
        'Deploy to production',
        'Custom AI model training',
      ],
    },
    {
      heading: 'Success Criteria',
      type: 'bullet' as const,
      items: [
        'User can create a workspace and see persistent memory',
        'User can run a preview and view logs',
        'AI can answer questions about the project accurately',
        'System feels fast, reliable, and intuitive',
      ],
    },
  ] satisfies NoteSection[],
}

export const pinnedNotes = [
  { title: 'Phase 1 MVP Plan', tag: 'mvp' },
  { title: 'Workspace Memory Design', tag: 'architecture' },
  { title: 'Sandbox Security Plan', tag: 'security' },
]

export const recentActivity = [
  { action: 'updated', target: 'Phase 1 MVP Plan', time: '1h ago', icon: 'edit' as const },
  { action: 'pinned', target: 'Workspace Memory Design', time: '1d ago', icon: 'pin' as const },
  { action: 'created', target: 'Sandbox Security Plan', time: '2d ago', icon: 'edit' as const },
  { action: 'summarized', target: 'Preview Environment Architecture', time: '3d ago', icon: 'brain' as const },
  { action: 'added tag', target: '"integration" to GitHub Integration Flow', time: '3d ago', icon: 'tag' as const },
  { action: 'created', target: 'Future Ideas', time: '6d ago', icon: 'edit' as const },
]

export const tagColorMap: Record<string, string> = {
  mvp: 'bg-accent/15 text-accent',
  architecture: 'bg-accent/15 text-accent',
  security: 'bg-success/15 text-success',
  integration: 'bg-accent/15 text-accent',
  ai: 'bg-accent/15 text-accent',
  preview: 'bg-accent/15 text-accent',
  ideas: 'bg-warning/15 text-warning',
}
