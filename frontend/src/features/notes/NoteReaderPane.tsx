import { MarkdownContent } from '../../components/MarkdownContent'
import { formatFileSize, formatReadingTime } from './noteDisplay'
import { NotePagination } from './NotePagination'
import type { NoteContent, NoteSummary } from './notesApi'
import type { Pluggable } from 'unified'

export function NoteReaderPane({
  activeNote,
  activeNoteIndex,
  headingIdPlugin,
  isEinkMode,
  isLoadingList,
  isLoadingNote,
  nextNote,
  noteCount,
  onCloseMobileReader,
  onMove,
  onTurnReaderPage,
  previousNote,
  previewRef,
  readerContent,
  readerTitle,
  status,
}: {
  activeNote: NoteContent | null
  activeNoteIndex: number
  headingIdPlugin: Pluggable
  isEinkMode: boolean
  isLoadingList: boolean
  isLoadingNote: boolean
  nextNote: NoteSummary | null
  noteCount: number
  onCloseMobileReader: () => void
  onMove: (path: string) => void
  onTurnReaderPage: (direction: 'next' | 'previous') => void
  previousNote: NoteSummary | null
  previewRef: React.RefObject<HTMLElement | null>
  readerContent: string
  readerTitle: string
  status: string
}) {
  return (
    <section className="note-workspace" aria-label="Note reader">
      {status ? <p className="settings-status note-status">{status}</p> : null}

      {isLoadingNote ? (
        <section className="notes-empty-panel">
          <h3>Loading note</h3>
        </section>
      ) : activeNote ? (
        <>
          <header className="note-editor-header">
            <button className="note-reader-back-button" onClick={onCloseMobileReader} type="button">
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
                  onClick={() => onTurnReaderPage('previous')}
                  tabIndex={-1}
                  type="button"
                />
                <button
                  className="note-page-turn-zone next"
                  onClick={() => onTurnReaderPage('next')}
                  tabIndex={-1}
                  type="button"
                />
              </div>
            ) : null}
            <div className="note-preview-content">
              <MarkdownContent rehypePlugins={[headingIdPlugin]}>{readerContent}</MarkdownContent>

              <NotePagination
                activeIndex={activeNoteIndex}
                className="note-reader-pagination mobile-pagination"
                label="Mobile note pagination"
                noteCount={noteCount}
                nextNote={nextNote}
                onMove={onMove}
                previousNote={previousNote}
              />
            </div>
          </article>

          <NotePagination
            activeIndex={activeNoteIndex}
            className="note-reader-pagination desktop-pagination"
            label="Note pagination"
            noteCount={noteCount}
            nextNote={nextNote}
            onMove={onMove}
            previousNote={previousNote}
          />
        </>
      ) : (
        <section className="notes-empty-panel">
          <h3>{isLoadingList ? 'Loading notes' : 'No note selected'}</h3>
        </section>
      )}
    </section>
  )
}
