import { useEffect, useMemo, useRef, useState } from 'react'
import type { MouseEvent } from 'react'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import { useTheme } from '../../app/useTheme'
import { NotesAiAssistant } from './NotesAiAssistant'
import { formatFileSize, formatReadingTime, toDisplayName } from './noteDisplay'
import { buildOutline, createHeadingIdPlugin, getNoteHeadingInfo, removeLine } from './noteMarkdown'
import { NoteTree } from './noteTree'
import { buildNoteTree } from './noteTreeBuilder'
import { listNotes, readNote } from './notesApi'
import type { NoteContent, NoteSummary } from './notesApi'

const bookDownloadLinks = [
  {
    href: 'https://github.com/rio-csharp/Notes/releases/download/latest/notes.pdf',
    label: 'PDF',
  },
  {
    href: 'https://github.com/rio-csharp/Notes/releases/download/latest/notes.epub',
    label: 'EPUB',
  },
] as const

export function NotesPage() {
  const { theme } = useTheme()
  const previewRef = useRef<HTMLElement | null>(null)
  const [notes, setNotes] = useState<NoteSummary[]>([])
  const [activePath, setActivePath] = useState('')
  const [activeNote, setActiveNote] = useState<NoteContent | null>(null)
  const [query, setQuery] = useState('')
  const [status, setStatus] = useState('')
  const [isLoadingList, setIsLoadingList] = useState(true)
  const [isLoadingNote, setIsLoadingNote] = useState(false)
  const [isNotesAiOpen, setIsNotesAiOpen] = useState(false)
  const [isMobileReaderOpen, setIsMobileReaderOpen] = useState(false)

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
  const activeNoteIndex = filteredNotes.findIndex((note) => note.path === activePath)
  const isEinkMode = theme === 'e-ink'
  const previousNote = activeNoteIndex > 0 ? filteredNotes[activeNoteIndex - 1] : null
  const nextNote =
    activeNoteIndex >= 0 && activeNoteIndex < filteredNotes.length - 1
      ? filteredNotes[activeNoteIndex + 1]
      : null

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
    document.body.classList.toggle('notes-reader-open', isMobileReaderOpen)

    return () => {
      document.body.classList.remove('notes-reader-open')
    }
  }, [isMobileReaderOpen])

  useEffect(() => {
    function handlePopState() {
      setIsMobileReaderOpen(false)
    }

    window.addEventListener('popstate', handlePopState)

    return () => {
      window.removeEventListener('popstate', handlePopState)
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

  function selectNote(path: string) {
    setActivePath(path)
    openMobileReader()
    scrollPreviewToTop(previewRef.current)
  }

  function openMobileReader() {
    if (isMobileViewport() && !isMobileReaderOpen) {
      window.history.pushState({ codecafeNotesReader: true }, '', window.location.href)
    }

    setIsMobileReaderOpen(true)
  }

  function moveToNote(path: string) {
    setActivePath(path)
    scrollPreviewToTop(previewRef.current)
  }

  function turnReaderPage(direction: 'next' | 'previous') {
    const preview = previewRef.current

    if (!preview) {
      return
    }

    const viewportHeight = preview.clientHeight
    const currentTop = preview.scrollTop
    const maxTop = Math.max(0, preview.scrollHeight - viewportHeight)
    const pageStep = Math.max(120, viewportHeight - 48)
    const targetTop =
      direction === 'next'
        ? Math.min(maxTop, currentTop + pageStep)
        : Math.max(0, currentTop - pageStep)

    const isAtBoundary =
      direction === 'next'
        ? currentTop >= maxTop - 4
        : currentTop <= 4

    if (isAtBoundary) {
      const boundaryNote = direction === 'next' ? nextNote : previousNote

      if (boundaryNote) {
        moveToNote(boundaryNote.path)
      }

      return
    }

    preview.scrollTo({
      behavior: 'auto',
      top: targetTop,
    })
  }

  return (
    <section className={`notes-page${isMobileReaderOpen ? ' mobile-reader-open' : ''}`} aria-label="Notes">
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
          <NoteTree nodes={noteTree} activePath={activePath} onSelect={selectNote} />

          {!isLoadingList && filteredNotes.length === 0 ? (
            <p className="empty-settings-copy note-list-empty">No notes found.</p>
          ) : null}
        </div>

        <div className="notes-downloads" aria-label="Download notes book">
          {bookDownloadLinks.map((link) => (
            <a
              className="notes-download-link"
              href={link.href}
              key={link.label}
              rel="noreferrer"
              target="_blank"
            >
              {link.label}
            </a>
          ))}
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
              <button className="note-reader-back-button" onClick={() => setIsMobileReaderOpen(false)} type="button">
                Back
              </button>
              <h2>{readerTitle}</h2>
              <span aria-label="Read-only note">
                {formatFileSize(activeNote.sizeBytes)} - Read-only - {formatReadingTime(activeNote.content)}
              </span>
            </header>

            <article className="note-preview-pane note-preview-pane-full" aria-label="Markdown preview" ref={previewRef}>
              {isEinkMode ? (
                <div className="note-page-turn-zones" aria-hidden="true">
                  <button
                    className="note-page-turn-zone previous"
                    onClick={() => turnReaderPage('previous')}
                    tabIndex={-1}
                    type="button"
                  />
                  <button
                    className="note-page-turn-zone next"
                    onClick={() => turnReaderPage('next')}
                    tabIndex={-1}
                    type="button"
                  />
                </div>
              ) : null}
              <div className="note-preview-content">
                <ReactMarkdown rehypePlugins={[headingIdPlugin]} remarkPlugins={[remarkGfm]}>
                  {readerContent}
                </ReactMarkdown>

                <NotePagination
                  activeIndex={activeNoteIndex}
                  className="note-reader-pagination mobile-pagination"
                  label="Mobile note pagination"
                  noteCount={filteredNotes.length}
                  nextNote={nextNote}
                  onMove={moveToNote}
                  previousNote={previousNote}
                />
              </div>
            </article>

            <NotePagination
              activeIndex={activeNoteIndex}
              className="note-reader-pagination desktop-pagination"
              label="Note pagination"
              noteCount={filteredNotes.length}
              nextNote={nextNote}
              onMove={moveToNote}
              previousNote={previousNote}
            />
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

      <button
        aria-expanded={isNotesAiOpen}
        aria-label="Open notes AI assistant"
        className="notes-ai-fab"
        onClick={() => setIsNotesAiOpen(true)}
        type="button"
      >
        AI
      </button>

      <NotesAiAssistant
        currentNote={activeNote}
        currentNoteTitle={readerTitle}
        isOpen={isNotesAiOpen}
        noteTree={noteTree}
        onClose={() => setIsNotesAiOpen(false)}
      />
    </section>
  )
}

function NotePagination({
  activeIndex,
  className,
  label,
  noteCount,
  nextNote,
  onMove,
  previousNote,
}: {
  activeIndex: number
  className: string
  label: string
  noteCount: number
  nextNote: NoteSummary | null
  onMove: (path: string) => void
  previousNote: NoteSummary | null
}) {
  return (
    <nav className={className} aria-label={label}>
      <button disabled={!previousNote} onClick={() => previousNote && onMove(previousNote.path)} type="button">
        Previous
      </button>
      <span>
        {activeIndex + 1 > 0 ? activeIndex + 1 : 0} / {noteCount}
      </span>
      <button disabled={!nextNote} onClick={() => nextNote && onMove(nextNote.path)} type="button">
        Next
      </button>
    </nav>
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

function isMobileViewport() {
  return globalThis.matchMedia?.('(max-width: 820px)').matches ?? false
}

function scrollPreviewToTop(scrollContainer: HTMLElement | null) {
  if (typeof scrollContainer?.scrollTo === 'function') {
    scrollContainer.scrollTo({ top: 0 })
  }
}
