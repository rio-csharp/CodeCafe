import { useEffect, useMemo, useRef, useState } from 'react'
import type { MouseEvent } from 'react'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import { formatFileSize, formatReadingTime, toDisplayName } from './noteDisplay'
import { buildOutline, createHeadingIdPlugin, getNoteHeadingInfo, removeLine } from './noteMarkdown'
import { NoteTree } from './noteTree'
import { buildNoteTree } from './noteTreeBuilder'
import { listNotes, readNote } from './notesApi'
import type { NoteContent, NoteSummary } from './notesApi'

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
  const outline = useMemo(
    () => buildOutline(activeNote?.content ?? '', headingInfo.titleLineIndex),
    [activeNote?.content, headingInfo.titleLineIndex],
  )
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

function escapeCssIdentifier(value: string) {
  return globalThis.CSS?.escape(value) ?? value.replace(/["\\]/g, '\\$&')
}
