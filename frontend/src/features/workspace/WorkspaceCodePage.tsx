import { useState } from 'react'
import { WorkspaceSidebar } from './WorkspaceSidebar'

type FileTreeItem = {
  name: string
  type: 'folder' | 'file'
  open?: boolean
  children?: FileTreeItem[]
}

const fileTree: FileTreeItem[] = [
  {
    name: '.github',
    type: 'folder' as const,
    children: [],
  },
  {
    name: 'src',
    type: 'folder' as const,
    open: true,
    children: [
      {
        name: 'CodeCafe.Api',
        type: 'folder' as const,
        open: true,
        children: [
          { name: 'Controllers', type: 'folder' as const, children: [] },
          { name: 'Services', type: 'folder' as const, children: [] },
          { name: 'Models', type: 'folder' as const, children: [] },
          { name: 'Program.cs', type: 'file' as const },
        ],
      },
      {
        name: 'CodeCafe.Core',
        type: 'folder' as const,
        open: false,
        children: [
          { name: 'Entities', type: 'folder' as const, children: [] },
          { name: 'Interfaces', type: 'folder' as const, children: [] },
          { name: 'Services', type: 'folder' as const, children: [] },
          { name: 'CodeCafeContext.cs', type: 'file' as const },
        ],
      },
      {
        name: 'CodeCafe.Infrastructure',
        type: 'folder' as const,
        open: false,
        children: [
          { name: 'Data', type: 'folder' as const, children: [] },
          { name: 'Repositories', type: 'folder' as const, children: [] },
          { name: 'Services', type: 'folder' as const, children: [] },
          { name: 'DependencyInjection.cs', type: 'file' as const },
        ],
      },
      { name: 'CodeCafe.Web', type: 'folder' as const, children: [] },
    ],
  },
  { name: 'tests', type: 'folder' as const, children: [] },
  { name: '.gitignore', type: 'file' as const },
  { name: 'docker-compose.yml', type: 'file' as const },
  { name: 'README.md', type: 'file' as const },
  { name: 'CodeCafe.sln', type: 'file' as const },
]

const codeContent = `using CodeCafe.Api.Extensions;
using CodeCafe.Infrastructure;
using CodeCafe.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<CodeCafeContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Dependency Injection
builder.Services.AddInfrastructure();
builder.Services.AddCore();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CodeCafePolicy",
        policy => policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();`

const suggestions = [
  {
    icon: 'bolt',
    title: 'Add DTOs for API requests',
    tag: 'Improvement',
    tagColor: 'success',
    desc: 'Consider using DTOs to decouple API models from domain entities.',
    file: 'Controllers/WorkspaceController.cs',
  },
  {
    icon: 'shield',
    title: 'Add global exception handling',
    tag: 'Enhancement',
    tagColor: 'accent',
    desc: 'Implement a global exception handler to improve error responses.',
    file: 'Program.cs',
  },
  {
    icon: 'zap',
    title: 'Add caching for workspace queries',
    tag: 'Performance',
    tagColor: 'warning',
    desc: 'Add caching to frequently queried workspace data to improve performance.',
    file: 'Services/WorkspaceService.cs',
  },
]

const recentChanges = [
  { icon: 'git', text: 'Workspace persistence implemented', time: '2h ago' },
  { icon: 'git', text: 'Run logs viewer added', time: '5h ago' },
  { icon: 'warning', text: 'Memory summarization improved', time: '1d ago' },
]

export function WorkspaceCodePage() {
  const [openFolders, setOpenFolders] = useState<Set<string>>(new Set(['src', 'src/CodeCafe.Api']))
  const [activeFile, setActiveFile] = useState('Program.cs')

  const toggleFolder = (path: string) => {
    const next = new Set(openFolders)
    if (next.has(path)) next.delete(path)
    else next.add(path)
    setOpenFolders(next)
  }

  return (
    <div className="flex min-h-screen bg-bg text-text">
      <WorkspaceSidebar activeItem="Code" />

      <div className="flex flex-1 flex-col">
        {/* Header */}
        <header className="flex items-center justify-between border-b border-border px-6 py-4">
          <div>
            <h1 className="m-0 text-2xl font-bold tracking-tight">Code</h1>
            <p className="m-0 mt-1 text-sm text-muted">Browse and understand your codebase with AI.</p>
          </div>
          <div className="flex items-center gap-3">
            <span className="inline-flex items-center gap-1.5 rounded-full border border-success/20 bg-success/8 px-3 py-1 text-xs font-semibold text-success">
              <span className="h-2 w-2 rounded-full bg-success" />
              AI Context On
            </span>
            <div className="flex items-center gap-2 rounded-lg border border-border bg-bg/60 px-3 py-1.5">
              <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/></svg>
              <input type="text" placeholder="Search files..." className="border-0 bg-transparent text-sm text-text outline-none placeholder:text-muted" />
              <span className="rounded border border-border px-1 py-0.5 text-[10px] text-muted">⌘K</span>
            </div>
            <button className="inline-flex h-8 w-8 items-center justify-center rounded-lg border border-border text-muted transition hover:text-text">
              <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2"><rect x="3" y="3" width="18" height="18" rx="2" ry="2"/><line x1="9" y1="3" x2="9" y2="21"/></svg>
            </button>
            <button className="inline-flex h-8 w-8 items-center justify-center rounded-lg border border-border text-muted transition hover:text-text">
              <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="12" cy="12" r="1"/><circle cx="19" cy="12" r="1"/><circle cx="5" cy="12" r="1"/></svg>
            </button>
          </div>
        </header>

        {/* Main content */}
        <div className="flex flex-1 overflow-hidden">
          {/* File explorer */}
          <aside className="flex w-[240px] shrink-0 flex-col border-r border-border bg-bg/40">
            <div className="flex items-center justify-between px-3 py-2 text-[10px] font-bold tracking-widest text-muted uppercase">
              <span>Explorer</span>
              <div className="flex items-center gap-1">
                <button className="inline-flex h-6 w-6 items-center justify-center rounded text-muted transition hover:text-text"><svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg></button>
                <button className="inline-flex h-6 w-6 items-center justify-center rounded text-muted transition hover:text-text"><svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/></svg></button>
                <button className="inline-flex h-6 w-6 items-center justify-center rounded text-muted transition hover:text-text"><svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><polyline points="23 4 23 10 17 10"/><path d="M20.49 15a9 9 0 1 1-2.12-9.36L23 10"/></svg></button>
              </div>
            </div>
            <div className="flex-1 overflow-auto px-2 pb-4">
              <div className="px-2 py-1 text-[10px] font-bold tracking-widest text-muted uppercase">CODECAFE</div>
              <FileTree items={fileTree} path="" openFolders={openFolders} onToggle={toggleFolder} activeFile={activeFile} onSelect={setActiveFile} />
            </div>
            {/* Git info */}
            <div className="border-t border-border p-3">
              <div className="mb-2 flex items-center justify-between">
                <span className="text-xs font-bold">Git</span>
                <span className="flex items-center gap-1 rounded border border-border bg-bg/60 px-2 py-0.5 text-[11px]">
                  <svg viewBox="0 0 24 24" width="10" height="10" fill="none" stroke="currentColor" strokeWidth="2"><line x1="6" y1="3" x2="6" y2="15"/><circle cx="18" cy="6" r="3"/><circle cx="6" cy="18" r="3"/><path d="M18 9a9 9 0 0 1-9 9"/></svg>
                  main
                </span>
              </div>
              <div className="text-[11px] text-muted">Latest commit</div>
              <div className="flex items-center gap-2 text-xs">
                <span className="font-mono text-accent">a1b2c3d</span>
                <span className="text-muted">3m ago</span>
              </div>
            </div>
          </aside>

          {/* Code editor */}
          <div className="flex flex-1 flex-col overflow-hidden border-r border-border">
            {/* Tabs */}
            <div className="flex items-center border-b border-border">
              <div className="flex items-center gap-2 border-r border-border bg-bg/60 px-4 py-2 text-sm">
                <span className="text-accent">⚡</span>
                <span className="font-medium">Program.cs</span>
                <button className="text-muted transition hover:text-text">×</button>
              </div>
              <div className="flex items-center gap-2 px-4 py-2 text-sm text-muted">
                <span className="text-accent">⚡</span>
                <span>CodeCafeContext.cs</span>
              </div>
            </div>
            {/* Breadcrumb */}
            <div className="flex items-center gap-1 border-b border-border px-4 py-1.5 text-xs text-muted">
              <span>src</span>
              <span>/</span>
              <span>CodeCafe.Api</span>
              <span>/</span>
              <span className="text-text">Program.cs</span>
            </div>
            {/* Code */}
            <div className="flex-1 overflow-auto bg-[#0a0e1a]">
              <pre className="m-0 p-4 text-sm leading-6">
                {codeContent.split('\n').map((line, i) => (
                  <div key={i} className="flex">
                    <span className="mr-4 inline-block w-8 shrink-0 select-none text-right text-muted/40">{i + 1}</span>
                    <CodeLine line={line} />
                  </div>
                ))}
              </pre>
            </div>
            {/* Status bar */}
            <div className="flex items-center justify-between border-t border-border bg-bg/60 px-4 py-1 text-[11px] text-muted">
              <div className="flex items-center gap-4">
                <span>Ln 1, Col 1</span>
                <span>Spaces: 4</span>
                <span>UTF-8</span>
                <span>LF</span>
                <span>C#</span>
              </div>
              <div className="flex items-center gap-1 text-success">
                <svg viewBox="0 0 24 24" width="12" height="12" fill="none" stroke="currentColor" strokeWidth="2"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>
                No issues
              </div>
            </div>
          </div>

          {/* AI Assistant panel */}
          <aside className="hidden w-[320px] shrink-0 flex-col gap-5 overflow-auto bg-bg/40 p-5 xl:flex">
            <div className="flex items-center justify-between">
              <h3 className="m-0 text-sm font-bold">AI Assistant <span className="ml-1 rounded border border-accent/30 bg-accent/8 px-1.5 py-0.5 text-[10px] text-accent">Beta</span></h3>
              <div className="flex items-center gap-1">
                <button className="text-muted transition hover:text-text"><svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><polyline points="23 4 23 10 17 10"/><path d="M20.49 15a9 9 0 1 1-2.12-9.36L23 10"/></svg></button>
                <button className="text-muted transition hover:text-text"><svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg></button>
              </div>
            </div>
            <p className="m-0 text-xs text-muted">I understand your codebase. Here are some suggestions to improve it.</p>

            <div>
              <h4 className="m-0 mb-3 text-xs font-bold text-muted uppercase">Smart Suggestions</h4>
              <div className="flex flex-col gap-3">
                {suggestions.map((s, i) => (
                  <div key={i} className="rounded-lg border border-border bg-surface/30 p-3">
                    <div className="mb-2 flex items-start justify-between gap-2">
                      <div className="flex items-center gap-2 text-sm font-semibold">
                        <span className="text-accent">
                          {s.icon === 'bolt' && '⚡'}
                          {s.icon === 'shield' && '🛡'}
                          {s.icon === 'zap' && '⚡'}
                        </span>
                        {s.title}
                      </div>
                      <span className={`shrink-0 rounded px-1.5 py-0.5 text-[10px] font-bold ${
                        s.tagColor === 'success' ? 'border border-success/20 bg-success/8 text-success' :
                        s.tagColor === 'warning' ? 'border border-warning/20 bg-warning/8 text-warning' :
                        'border border-accent/20 bg-accent/8 text-accent'
                      }`}>{s.tag}</span>
                    </div>
                    <p className="m-0 mb-2 text-xs leading-relaxed text-muted">{s.desc}</p>
                    <div className="mb-2 flex items-center gap-1 text-[11px] text-muted">
                      <svg viewBox="0 0 24 24" width="10" height="10" fill="none" stroke="currentColor" strokeWidth="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>
                      {s.file}
                    </div>
                    <div className="flex items-center justify-between">
                      <button className="rounded border border-border bg-bg/60 px-3 py-1 text-xs font-medium transition hover:bg-accent/8">View Suggestion</button>
                      <button className="text-muted transition hover:text-text"><svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><path d="M12 2L2 7l10 5 10-5-10-5z"/><path d="M2 17l10 5 10-5"/><path d="M2 12l10 5 10-5"/></svg></button>
                    </div>
                  </div>
                ))}
              </div>
            </div>

            <div>
              <div className="mb-3 flex items-center justify-between">
                <h4 className="m-0 text-xs font-bold text-muted uppercase">Recent Changes</h4>
                <a href="#" className="text-xs font-medium text-accent no-underline">View all</a>
              </div>
              <div className="flex flex-col gap-2.5">
                {recentChanges.map((c, i) => (
                  <div key={i} className="flex items-center justify-between gap-2 text-xs">
                    <div className="flex items-center gap-2">
                      <span className={c.icon === 'warning' ? 'text-warning' : 'text-success'}>
                        {c.icon === 'git' && <svg viewBox="0 0 24 24" width="12" height="12" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>}
                        {c.icon === 'warning' && <svg viewBox="0 0 24 24" width="12" height="12" fill="none" stroke="currentColor" strokeWidth="2"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>}
                      </span>
                      <span className="text-muted">{c.text}</span>
                    </div>
                    <span className="shrink-0 text-[11px] text-muted">{c.time}</span>
                  </div>
                ))}
              </div>
            </div>

            <button className="mt-auto flex w-full items-center justify-center gap-2 rounded-lg bg-accent px-4 py-2.5 text-sm font-semibold text-[#070a12] transition hover:opacity-90">
              Ask AI about this codebase
              <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/></svg>
            </button>
          </aside>
        </div>
      </div>
    </div>
  )
}

/* File tree recursive component */
function FileTree({
  items,
  path,
  openFolders,
  onToggle,
  activeFile,
  onSelect,
}: {
  items: FileTreeItem[]
  path: string
  openFolders: Set<string>
  onToggle: (p: string) => void
  activeFile: string
  onSelect: (name: string) => void
}) {
  return (
    <div className="flex flex-col">
      {items.map((item) => {
        const itemPath = path ? `${path}/${item.name}` : item.name
        const isOpen = openFolders.has(itemPath)
        const isActive = item.type === 'file' && activeFile === item.name

        return (
          <div key={itemPath}>
            <button
              onClick={() => {
                if (item.type === 'folder') onToggle(itemPath)
                else onSelect(item.name)
              }}
              className={`flex w-full items-center gap-1.5 rounded px-2 py-1 text-left text-xs transition-colors ${
                isActive ? 'bg-accent/15 text-accent' : 'text-muted hover:bg-accent/8 hover:text-text'
              }`}
              style={{ paddingLeft: `${(path.split('/').length) * 12 + 8}px` }}
            >
              {item.type === 'folder' && (
                <span className="text-muted">
                  {isOpen ? (
                    <svg viewBox="0 0 24 24" width="10" height="10" fill="none" stroke="currentColor" strokeWidth="3"><polyline points="6 9 12 15 18 9"/></svg>
                  ) : (
                    <svg viewBox="0 0 24 24" width="10" height="10" fill="none" stroke="currentColor" strokeWidth="3"><polyline points="9 18 15 12 9 6"/></svg>
                  )}
                </span>
              )}
              <FileIcon type={item.type} name={item.name} />
              <span className="truncate">{item.name}</span>
            </button>
            {item.type === 'folder' && isOpen && item.children && (
              <FileTree items={item.children} path={itemPath} openFolders={openFolders} onToggle={onToggle} activeFile={activeFile} onSelect={onSelect} />
            )}
          </div>
        )
      })}
    </div>
  )
}

function FileIcon({ type, name }: { type: string; name: string }) {
  if (type === 'folder') {
    return <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/></svg>
  }
  const ext = name.split('.').pop()?.toLowerCase()
  if (ext === 'cs') return <span className="text-accent">⚡</span>
  if (ext === 'md') return <span className="text-muted">📄</span>
  if (ext === 'yml' || ext === 'yaml') return <span className="text-muted">🐳</span>
  if (ext === 'sln') return <span className="text-accent">🔧</span>
  if (name === '.gitignore') return <span className="text-muted">🚫</span>
  return <span className="text-muted">📄</span>
}

/* Simple syntax highlighter */
function CodeLine({ line }: { line: string }) {
  // Very simple C# syntax highlighting
  const keywords = ['using', 'var', 'var', 'new', 'return', 'if', 'else', 'for', 'foreach', 'while', 'class', 'interface', 'namespace', 'public', 'private', 'static', 'void', 'async', 'await', 'await', 'true', 'false', 'null']
  const types = ['string', 'int', 'bool', 'double', 'float', 'decimal', 'DateTime', 'Task', 'List', 'Dictionary', 'IEnumerable']

  let remaining = line

  // Comments
  if (remaining.trim().startsWith('//')) {
    return <span className="text-muted/60">{line}</span>
  }

  // String literals
  const stringMatch = remaining.match(/^(.*?)("(?:[^"\\]|\\.)*")(.*)$/)
  if (stringMatch) {
    const [, before, str, after] = stringMatch
    return (
      <>
        {before && <CodeLine line={before} />}
        <span className="text-[#a5d6ff]">{str}</span>
        {after && <CodeLine line={after} />}
      </>
    )
  }

  // Simple tokenization
  const words = remaining.split(/([\s\(\)\[\]\{\}\.;,=<>+\-*/&|!?:])/)
  return (
    <>
      {words.map((word, i) => {
        if (!word) return null
        if (/^[\s\(\)\[\]\{\}\.;,=<>+\-*/&|!?:]+$/.test(word)) {
          return <span key={i} className="text-muted/70">{word}</span>
        }
        if (keywords.includes(word)) {
          return <span key={i} className="text-[#ff7b72]">{word}</span>
        }
        if (types.includes(word)) {
          return <span key={i} className="text-[#79c0ff]">{word}</span>
        }
        if (/^[A-Z][a-zA-Z0-9_]*$/.test(word)) {
          return <span key={i} className="text-[#ffa657]">{word}</span>
        }
        if (word.startsWith('"') && word.endsWith('"')) {
          return <span key={i} className="text-[#a5d6ff]">{word}</span>
        }
        return <span key={i} className="text-[#c9d1d9]">{word}</span>
      })}
    </>
  )
}
