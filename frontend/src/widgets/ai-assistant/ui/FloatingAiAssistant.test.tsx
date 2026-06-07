import { cleanup, fireEvent, render, screen } from '@testing-library/react'
import type { HTMLAttributes } from 'react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { Notebook } from '@/entities/notebook'
import type { NotebookItem } from '@/entities/notebook-item'
import FloatingAiAssistant from './FloatingAiAssistant'

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => translations[key] ?? key,
  }),
}))

vi.mock('./AiAssistant', () => ({
  default: ({ dragHandleProps, onCollapse }: {
    dragHandleProps?: HTMLAttributes<HTMLDivElement>
    onCollapse?: () => void
  }) => (
    <div data-testid="assistant">
      <div data-testid="drag-handle" {...dragHandleProps}>Drag</div>
      <button type="button" onClick={onCollapse}>Minimize</button>
    </div>
  ),
}))

const translations: Record<string, string> = {
  'ai.dragHandle': 'Drag AI Assistant',
  'ai.open': 'Open AI Assistant',
  'ai.title': 'AI Assistant',
}

const notebook: Notebook = {
  id: 'notebook-1',
  ownerId: 'user-1',
  title: 'Architecture Notes',
  slug: 'architecture-notes',
  description: null,
  visibility: 'private',
  isPublished: false,
  authorDisplayName: 'Yao',
  itemCount: 3,
  folderCount: 1,
  pageCount: 2,
  favoriteCount: 0,
  isFavoritedByMe: false,
  lastActivityAtUtc: '2026-06-01T00:00:00Z',
  createdAtUtc: '2026-06-01T00:00:00Z',
  updatedAtUtc: null,
  publishedAtUtc: null,
  canEdit: true,
}

const activePage: NotebookItem = {
  id: 'page-1',
  notebookId: 'notebook-1',
  parentId: null,
  type: 'page',
  title: 'Overview',
  slug: 'overview',
  path: 'overview',
  sortOrder: 0,
  contentFormat: 'tiptap_json',
  contentJson: null,
  plainTextContent: null,
  isArchived: false,
  archivedAtUtc: null,
  archivedByUserId: null,
  createdAtUtc: '2026-06-01T00:00:00Z',
  updatedAtUtc: null,
}

describe('FloatingAiAssistant', () => {
  afterEach(() => {
    cleanup()
    setViewport(1280, 720)
  })

  it('opens as a floating panel on desktop viewports', () => {
    setViewport(1000, 800)

    render(<FloatingAiAssistant notebook={notebook} activePage={activePage} />)

    const panel = screen.getByTestId('assistant').parentElement
    expect(panel).toHaveStyle({ height: '560px', left: '596px', top: '216px', width: '380px' })
  })

  it('keeps dragged panels inside the viewport', () => {
    setViewport(1000, 800)

    render(<FloatingAiAssistant notebook={notebook} activePage={activePage} />)

    fireEvent.pointerDown(screen.getByTestId('drag-handle'), {
      button: 0,
      clientX: 600,
      clientY: 220,
      pointerId: 1,
    })
    fireEvent.pointerMove(window, {
      clientX: -500,
      clientY: -500,
      pointerId: 1,
    })

    expect(screen.getByTestId('assistant').parentElement).toHaveStyle({ left: '16px', top: '16px' })
  })

  it('starts minimized on compact viewports and can be opened', () => {
    setViewport(500, 700)

    render(<FloatingAiAssistant notebook={notebook} activePage={activePage} />)

    expect(screen.queryByTestId('assistant')).not.toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Open AI Assistant' }))

    expect(screen.getByTestId('assistant')).toBeInTheDocument()
  })
})

function setViewport(width: number, height: number) {
  Object.defineProperty(window, 'innerWidth', {
    configurable: true,
    value: width,
  })
  Object.defineProperty(window, 'innerHeight', {
    configurable: true,
    value: height,
  })
}
