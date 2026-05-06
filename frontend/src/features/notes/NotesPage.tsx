import { useEffect, useMemo, useRef, useState } from 'react'
import { useTheme } from '../../app/useTheme'
import { NoteOutline } from './NoteOutline'
import { NoteReaderPane } from './NoteReaderPane'
import { NotesAiAssistant } from './NotesAiAssistant'
import { toDisplayName } from './noteDisplay'
import { buildOutline, createHeadingIdPlugin, getNoteHeadingInfo, removeLine } from './noteMarkdown'
import { NotesSidebar } from './NotesSidebar'
import { buildNoteTree } from './noteTreeBuilder'
import {
  getAncestorPaths,
  loadNotesWorkspaceState,
  mergeExpandedDirectories,
  persistWorkspaceBeforeNavigation,
  rememberScrollPosition,
  saveNotesWorkspaceState,
  type NotesWorkspaceState,
} from './notesWorkspaceState'
import { listNotes, readNote } from './notesApi'
import type { NoteContent, NoteSummary } from './notesApi'

export function NotesPage() {
  const { theme } = useTheme()
  const initialWorkspaceState = useMemo(() => loadNotesWorkspaceState(), [])
  const previewRef = useRef<HTMLElement | null>(null)
  const isRestoringScrollRef = useRef(false)
  const [workspaceState, setWorkspaceState] = useState<NotesWorkspaceState>(initialWorkspaceState)
  const scrollTopByPathRef = useRef<Record<string, number>>(initialWorkspaceState.scrollTopByPath)
  const [notes, setNotes] = useState<NoteSummary[]>([])
  const [savedActivePath] = useState(() => initialWorkspaceState.activePath)
  const [initialRestorePath, setInitialRestorePath] = useState(() => initialWorkspaceState.activePath)
  const [activePath, setActivePath] = useState(() => initialWorkspaceState.activePath ?? '')
  const [activeNote, setActiveNote] = useState<NoteContent | null>(null)
  const [query, setQuery] = useState('')
  const [status, setStatus] = useState('')
  const [isLoadingList, setIsLoadingList] = useState(true)
  const [isLoadingNote, setIsLoadingNote] = useState(false)
  const [isNotesAiOpen, setIsNotesAiOpen] = useState(false)
  const [isMobileReaderOpen, setIsMobileReaderOpen] = useState(false)
  const expandedPaths = useMemo(
    () => new Set(workspaceState.expandedDirectories),
    [workspaceState.expandedDirectories],
  )

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
    saveNotesWorkspaceState({
      ...workspaceState,
      scrollTopByPath: scrollTopByPathRef.current,
    })
  }, [workspaceState])

  useEffect(() => {
    let ignore = false

    async function loadNotes() {
      try {
        const nextNotes = await listNotes()

        if (ignore) {
          return
        }

        setNotes(nextNotes)
        setActivePath((currentPath) => {
          const preferredPath = currentPath || savedActivePath || ''

          if (preferredPath && nextNotes.some((note) => note.path === preferredPath)) {
            setWorkspaceState((currentState) => ({
              ...currentState,
              expandedDirectories: mergeExpandedDirectories(
                currentState.expandedDirectories,
                getAncestorPaths(preferredPath),
              ),
            }))
            return preferredPath
          }

          setWorkspaceState((currentState) => ({
            ...currentState,
            activePath: nextNotes[0]?.path ?? null,
          }))
          return nextNotes[0]?.path ?? ''
        })
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
  }, [savedActivePath])

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

  useEffect(() => {
    if (!activeNote || !previewRef.current) {
      return
    }

    if (!initialRestorePath || activeNote.path !== initialRestorePath) {
      return
    }

    const preview = previewRef.current
    const scrollTop = scrollTopByPathRef.current[activeNote.path] ?? 0
    isRestoringScrollRef.current = true
    const restoreId = window.requestAnimationFrame(() => {
      scrollPreviewToPosition(preview, scrollTop)
      setInitialRestorePath(null)
      isRestoringScrollRef.current = false
    })

    return () => {
      window.cancelAnimationFrame(restoreId)
      isRestoringScrollRef.current = false
    }
  }, [activeNote, initialRestorePath])

  useEffect(() => {
    const preview = previewRef.current

    if (!preview || !activePath) {
      return
    }

    const handleScroll = () => {
      if (isRestoringScrollRef.current) {
        return
      }

      scrollTopByPathRef.current = {
        ...scrollTopByPathRef.current,
        [activePath]: preview.scrollTop,
      }
    }

    preview.addEventListener('scroll', handleScroll, { passive: true })

    return () => {
      preview.removeEventListener('scroll', handleScroll)
    }
  }, [activePath, activeNote])

  useEffect(() => {
    const flushWorkspaceState = () => {
      saveNotesWorkspaceState({
        ...workspaceState,
        scrollTopByPath: scrollTopByPathRef.current,
      })
    }

    const handleVisibilityChange = () => {
      if (document.visibilityState === 'hidden') {
        flushWorkspaceState()
      }
    }

    window.addEventListener('pagehide', flushWorkspaceState)
    window.addEventListener('beforeunload', flushWorkspaceState)
    document.addEventListener('visibilitychange', handleVisibilityChange)

    return () => {
      window.removeEventListener('pagehide', flushWorkspaceState)
      window.removeEventListener('beforeunload', flushWorkspaceState)
      document.removeEventListener('visibilitychange', handleVisibilityChange)
    }
  }, [workspaceState])

  function selectNote(path: string) {
    const nextExpandedDirectories = mergeExpandedDirectories(
      workspaceState.expandedDirectories,
      getAncestorPaths(path),
    )
    persistWorkspaceBeforeNavigation({
      activePath: path,
      currentPath: activePath,
      expandedDirectories: nextExpandedDirectories,
      scrollContainer: previewRef.current,
      scrollTopByPathRef,
      workspaceState,
    })
    rememberScrollPosition(
      activePath,
      previewRef.current,
      scrollTopByPathRef,
      workspaceState,
    )
    setInitialRestorePath(null)
    setWorkspaceState((currentState) => ({
      ...currentState,
      activePath: path,
      expandedDirectories: nextExpandedDirectories,
    }))
    setActivePath(path)
    openMobileReader()
    scrollPreviewToPosition(previewRef.current, 0)
  }

  function openMobileReader() {
    if (isMobileViewport() && !isMobileReaderOpen) {
      window.history.pushState({ codecafeNotesReader: true }, '', window.location.href)
    }

    setIsMobileReaderOpen(true)
  }

  function moveToNote(path: string) {
    const nextExpandedDirectories = mergeExpandedDirectories(
      workspaceState.expandedDirectories,
      getAncestorPaths(path),
    )
    persistWorkspaceBeforeNavigation({
      activePath: path,
      currentPath: activePath,
      expandedDirectories: nextExpandedDirectories,
      scrollContainer: previewRef.current,
      scrollTopByPathRef,
      workspaceState,
    })
    rememberScrollPosition(
      activePath,
      previewRef.current,
      scrollTopByPathRef,
      workspaceState,
    )
    setInitialRestorePath(null)
    setWorkspaceState((currentState) => ({
      ...currentState,
      activePath: path,
      expandedDirectories: nextExpandedDirectories,
    }))
    setActivePath(path)
    scrollPreviewToPosition(previewRef.current, 0)
  }

  function toggleDirectory(path: string, isOpen: boolean) {
    setWorkspaceState((currentState) => {
      const nextExpandedDirectories = isOpen
        ? Array.from(new Set([...currentState.expandedDirectories, path]))
        : currentState.expandedDirectories.filter((entry) => entry !== path)

      if (nextExpandedDirectories === currentState.expandedDirectories) {
        return currentState
      }

      return {
        ...currentState,
        expandedDirectories: nextExpandedDirectories,
      }
    })
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

  function handleOutlineItemClick(id: string) {
    const scrollContainer = previewRef.current
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

  return (
    <section className={`notes-page${isMobileReaderOpen ? ' mobile-reader-open' : ''}`} aria-label="Notes">
      <NotesSidebar
        activePath={activePath}
        expandedPaths={expandedPaths}
        isLoadingList={isLoadingList}
        nodes={noteTree}
        onQueryChange={setQuery}
        onSelect={selectNote}
        onToggleDirectory={toggleDirectory}
        query={query}
      />

      <NoteReaderPane
        activeNote={activeNote}
        activeNoteIndex={activeNoteIndex}
        headingIdPlugin={headingIdPlugin}
        isEinkMode={isEinkMode}
        isLoadingList={isLoadingList}
        isLoadingNote={isLoadingNote}
        nextNote={nextNote}
        noteCount={filteredNotes.length}
        onCloseMobileReader={() => setIsMobileReaderOpen(false)}
        onMove={moveToNote}
        onTurnReaderPage={turnReaderPage}
        previousNote={previousNote}
        previewRef={previewRef}
        readerContent={readerContent}
        readerTitle={readerTitle}
        status={status}
      />

      <NoteOutline items={outline} onItemClick={handleOutlineItemClick} />

      <NotesAiAssistant
        currentNote={activeNote}
        currentNoteTitle={readerTitle}
        isOpen={isNotesAiOpen}
        onClose={() => setIsNotesAiOpen(false)}
        onOpen={() => setIsNotesAiOpen(true)}
      />
    </section>
  )
}

function escapeCssIdentifier(value: string) {
  return globalThis.CSS?.escape(value) ?? value.replace(/["\\]/g, '\\$&')
}

function isMobileViewport() {
  return globalThis.matchMedia?.('(max-width: 820px)').matches ?? false
}

function scrollPreviewToPosition(scrollContainer: HTMLElement | null, top: number) {
  if (!scrollContainer) {
    return
  }

  if (typeof scrollContainer.scrollTo === 'function') {
    scrollContainer.scrollTo({ top })
    return
  }

  scrollContainer.scrollTop = top
}
