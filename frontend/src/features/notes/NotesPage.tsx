import { useEffect, useMemo, useRef, useState } from 'react'
import type { MouseEvent } from 'react'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import { listNotes, readNote } from './notesApi'
import type { NoteContent, NoteSummary } from './notesApi'

type NoteTreeNode = {
  children: NoteTreeNode[]
  name: string
  note?: NoteSummary
  path: string
  sortName: string
  type: 'directory' | 'note'
}

type NoteOutlineItem = {
  depth: number
  id: string
  text: string
}

type NoteHeadingInfo = {
  title: string | null
  titleLineIndex: number | null
}

type HastNode = {
  children?: HastNode[]
  properties?: Record<string, unknown>
  tagName?: string
  type?: string
}

export function NotesPage() {
  const previewRef = useRef<HTMLElement | null>(null)
  const [notes, setNotes] = useState<NoteSummary[]>([])
  const [activePath, setActivePath] = useState('')
  const [activeNote, setActiveNote] = useState<NoteContent | null>(null)
  const [query, setQuery] = useState('')
  const [status, setStatus] = useState('')
  const [isLoadingList, setIsLoadingList] = useState(true)
  const [isLoadingNote, setIsLoadingNote] = useState(false)

  const filteredNotes = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase()

    if (!normalizedQuery) {
      return notes
    }

    return notes.filter(
      (note) =>
        note.title.toLowerCase().includes(normalizedQuery) ||
        note.path.toLowerCase().includes(normalizedQuery),
      )
  }, [notes, query])
  const noteTree = useMemo(() => buildNoteTree(filteredNotes), [filteredNotes])
  const headingInfo = useMemo(() => getNoteHeadingInfo(activeNote?.content ?? ''), [activeNote?.content])
  const outline = useMemo(() => buildOutline(activeNote?.content ?? '', headingInfo.titleLineIndex), [activeNote?.content, headingInfo.titleLineIndex])
  const headingIdPlugin = useMemo(() => createHeadingIdPlugin(outline), [outline])
  const readerTitle = headingInfo.title ?? toDisplayName(activeNote?.title ?? '')
  const readerContent = removeLine(activeNote?.content ?? '', headingInfo.titleLineIndex)

  useEffect(() => {
    let ignore = false

    async function loadNotes() {
      try {
        const nextNotes = await listNotes()

        if (ignore) {
          return
        }

        setNotes(nextNotes)
        setActivePath(nextNotes[0]?.path ?? '')
      } catch {
        if (!ignore) {
          setStatus('Unable to load notes. Check the notes root path in Settings.')
        }
      } finally {
        if (!ignore) {
          setIsLoadingList(false)
        }
      }
    }

    void loadNotes()

    return () => {
      ignore = true
    }
  }, [])

  useEffect(() => {
    let ignore = false

    async function loadNote() {
      if (!activePath) {
        setActiveNote(null)
        return
      }

      try {
        setIsLoadingNote(true)
        setStatus('')
        const note = await readNote(activePath)

        if (!ignore) {
          setActiveNote(note)
        }
      } catch {
        if (!ignore) {
          setActiveNote(null)
          setStatus('Unable to read the selected note.')
        }
      } finally {
        if (!ignore) {
          setIsLoadingNote(false)
        }
      }
    }

    void loadNote()

    return () => {
      ignore = true
    }
  }, [activePath])

  return (
    <section className="notes-page" aria-label="Notes">
      <aside className="notes-sidebar" aria-label="Notes list">
        <div className="notes-sidebar-header">
          <input
            aria-label="Search notes"
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Search notes"
            type="search"
            value={query}
          />
        </div>

        <div className="note-list">
          <NoteTree nodes={noteTree} activePath={activePath} onSelect={setActivePath} />

          {!isLoadingList && filteredNotes.length === 0 ? (
            <p className="empty-settings-copy note-list-empty">No notes found.</p>
          ) : null}
        </div>
      </aside>

      <section className="note-workspace" aria-label="Note reader">
        {status ? <p className="settings-status note-status">{status}</p> : null}

        {isLoadingNote ? (
          <section className="notes-empty-panel">
            <h3>Loading note</h3>
          </section>
        ) : activeNote ? (
          <>
            <header className="note-editor-header">
              <h2>{readerTitle}</h2>
              <span aria-label="Read-only note">
                {formatFileSize(activeNote.sizeBytes)} - Read-only - {formatReadingTime(activeNote.content)}
              </span>
            </header>

            <article className="note-preview-pane note-preview-pane-full" aria-label="Markdown preview" ref={previewRef}>
              <div className="note-preview-content">
                <ReactMarkdown rehypePlugins={[headingIdPlugin]} remarkPlugins={[remarkGfm]}>
                  {readerContent}
                </ReactMarkdown>
              </div>
            </article>
          </>
        ) : (
          <section className="notes-empty-panel">
            <h3>{isLoadingList ? 'Loading notes' : 'No note selected'}</h3>
          </section>
        )}
      </section>

      <aside className="note-outline" aria-label="Note outline">
        <div className="note-outline-header">Outline</div>
        {outline.length > 0 ? (
          <nav className="note-outline-list">
            {outline.map((item) => (
              <a
                className={`note-outline-item depth-${Math.min(item.depth, 3)}`}
                href={`#${item.id}`}
                key={item.id}
                onClick={(event) => scrollToHeading(event, item.id, previewRef.current)}
              >
                {item.text}
              </a>
            ))}
          </nav>
        ) : (
          <p className="empty-settings-copy note-outline-empty">No headings found.</p>
        )}
      </aside>
    </section>
  )
}

function NoteTree({
  activePath,
  nodes,
  onSelect,
}: {
  activePath: string
  nodes: NoteTreeNode[]
  onSelect: (path: string) => void
}) {
  return (
    <ul className="note-tree">
      {nodes.map((node) => (
        <li key={node.path}>
          {node.type === 'directory' ? (
            <details>
              <summary>{node.name}</summary>
              <NoteTree activePath={activePath} nodes={node.children} onSelect={onSelect} />
            </details>
          ) : (
            <button
              aria-current={node.path === activePath ? 'true' : undefined}
              className="note-list-item"
              onClick={() => onSelect(node.path)}
              type="button"
            >
              <span>
                <strong>{toDisplayName(node.note?.title ?? '')}</strong>
              </span>
            </button>
          )}
        </li>
      ))}
    </ul>
  )
}

function buildNoteTree(notes: NoteSummary[]) {
  const root: NoteTreeNode[] = []

  for (const note of notes) {
    const segments = note.path.split('/').filter(Boolean)
    let siblings = root

    segments.forEach((segment, index) => {
      const isLast = index === segments.length - 1
      const nodePath = segments.slice(0, index + 1).join('/')

      if (isLast) {
        siblings.push({
          children: [],
          name: toDisplayName(note.title),
          note,
          path: note.path,
          sortName: segment,
          type: 'note',
        })
        return
      }

      let directory = siblings.find((node) => node.type === 'directory' && node.path === nodePath)

      if (!directory) {
        directory = {
          children: [],
          name: toDisplayName(segment),
          path: nodePath,
          sortName: segment,
          type: 'directory',
        }
        siblings.push(directory)
      }

      siblings = directory.children
    })
  }

  return sortTree(root)
}

function sortTree(nodes: NoteTreeNode[]): NoteTreeNode[] {
  return nodes
    .map((node) => ({
      ...node,
      children: sortTree(node.children),
    }))
    .sort((left, right) => {
      if (left.type !== right.type) {
        return left.type === 'directory' ? -1 : 1
      }

      return left.sortName.localeCompare(right.sortName, undefined, {
        numeric: true,
        sensitivity: 'base',
      })
    })
}

function getNoteHeadingInfo(content: string): NoteHeadingInfo {
  let isInCodeBlock = false
  const lines = content.split('\n')

  for (const [index, line] of lines.entries()) {
    if (line.trim().startsWith('```')) {
      isInCodeBlock = !isInCodeBlock
      continue
    }

    if (isInCodeBlock) {
      continue
    }

    const match = /^#\s+(.+?)\s*#*$/.exec(line)

    if (match) {
      return {
        title: toDisplayName(match[1].replace(/[`*_~[\]()]/g, '').trim()),
        titleLineIndex: index,
      }
    }
  }

  return {
    title: null,
    titleLineIndex: null,
  }
}

function removeLine(content: string, lineIndex: number | null) {
  if (lineIndex === null) {
    return content
  }

  return content
    .split('\n')
    .filter((_, index) => index !== lineIndex)
    .join('\n')
    .replace(/^\s+/, '')
}

function buildOutline(content: string, hiddenLineIndex: number | null) {
  const usedIds = new Map<string, number>()
  const outline: NoteOutlineItem[] = []
  let isInCodeBlock = false

  for (const [index, line] of content.split('\n').entries()) {
    if (line.trim().startsWith('```')) {
      isInCodeBlock = !isInCodeBlock
      continue
    }

    if (isInCodeBlock || index === hiddenLineIndex) {
      continue
    }

    const match = /^(#{1,6})\s+(.+?)\s*#*$/.exec(line)

    if (!match) {
      continue
    }

    const depth = match[1].length

    if (depth === 1) {
      continue
    }

    const text = toDisplayName(match[2].replace(/[`*_~[\]()]/g, '').trim())
    const baseId = slugify(text)
    const duplicateCount = usedIds.get(baseId) ?? 0
    const id = duplicateCount === 0 ? baseId : `${baseId}-${duplicateCount + 1}`

    usedIds.set(baseId, duplicateCount + 1)
    outline.push({
      depth,
      id,
      text,
    })
  }

  return outline
}

function slugify(value: string) {
  return (
    value
      .toLowerCase()
      .trim()
      .replace(/[^a-z0-9\u4e00-\u9fa5]+/g, '-')
      .replace(/^-+|-+$/g, '') || 'heading'
  )
}

function scrollToHeading(event: MouseEvent<HTMLAnchorElement>, id: string, scrollContainer: HTMLElement | null) {
  event.preventDefault()

  const heading = scrollContainer?.querySelector<HTMLElement>(`#${escapeCssIdentifier(id)}`)

  if (!heading || !scrollContainer) {
    return
  }

  const headingRect = heading.getBoundingClientRect()
  const containerRect = scrollContainer.getBoundingClientRect()
  const computedStyle = globalThis.getComputedStyle(scrollContainer)
  const scrollOffset =
    Number.parseFloat(computedStyle.scrollPaddingTop) ||
    Number.parseFloat(computedStyle.paddingTop) ||
    0
  const targetTop = scrollContainer.scrollTop + headingRect.top - containerRect.top - scrollOffset
  scrollContainer.scrollTo({
    behavior: 'auto',
    top: Math.max(0, targetTop),
  })
}

function createHeadingIdPlugin(outline: NoteOutlineItem[]) {
  return () => (tree: HastNode) => {
    let headingIndex = 0

    visitHast(tree, (node) => {
      if (node.type !== 'element' || !isHeadingTag(node.tagName)) {
        return
      }

      const item = outline[headingIndex]
      headingIndex += 1

      if (!item) {
        return
      }

      node.properties = {
        ...node.properties,
        dataOutlineId: item.id,
        id: item.id,
      }
    })
  }
}

function visitHast(node: HastNode, visitor: (node: HastNode) => void) {
  visitor(node)

  for (const child of node.children ?? []) {
    visitHast(child, visitor)
  }
}

function isHeadingTag(tagName: string | undefined) {
  return (
    tagName === 'h1' ||
    tagName === 'h2' ||
    tagName === 'h3' ||
    tagName === 'h4' ||
    tagName === 'h5' ||
    tagName === 'h6'
  )
}

function escapeCssIdentifier(value: string) {
  return globalThis.CSS?.escape(value) ?? value.replace(/["\\]/g, '\\$&')
}

function toDisplayName(value: string) {
  const withoutExtension = value.replace(/\.(md|markdown|txt)$/i, '')
  const withoutOrderPrefix = withoutExtension.replace(/^\d+[\s._-]+/, '')

  return withoutOrderPrefix
    .replace(/[-_]+/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()
    .replace(/\b\w/g, (letter) => letter.toUpperCase())
}

function formatFileSize(sizeBytes: number) {
  if (sizeBytes < 1024) {
    return `${sizeBytes} B`
  }

  return `${(sizeBytes / 1024).toFixed(1)} KB`
}

function formatReadingTime(content: string) {
  const latinWordCount = content.match(/[A-Za-z0-9]+(?:[-'][A-Za-z0-9]+)*/g)?.length ?? 0
  const cjkCharacterCount = content.match(/[\u4e00-\u9fff]/g)?.length ?? 0
  const estimatedWords = latinWordCount + cjkCharacterCount / 2
  const minutes = Math.max(1, Math.ceil(estimatedWords / 240))

  return `${minutes} min read`
}
