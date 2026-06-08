import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { NotebookItem } from '@/entities/notebook-item'
import { applyCodeBlockLineNumbers } from '@/shared/lib/codeBlockLineNumbers'
import NotebookPageEditor from './NotebookPageEditor'

const mocks = vi.hoisted(() => ({
  applyCodeBlockLineNumbers: vi.fn(),
  useEditor: vi.fn(),
}))

vi.mock('@tiptap/react', () => ({
  EditorContent: () => <div data-testid="editor-content" />,
  useEditor: mocks.useEditor,
}))

vi.mock('@/shared/lib/codeBlockLineNumbers', () => ({
  applyCodeBlockLineNumbers: mocks.applyCodeBlockLineNumbers,
}))

vi.mock('@/shared/lib/tiptapExtensions', () => ({
  createTipTapExtensions: () => [],
}))

vi.mock('./NotebookEditorToolbar', () => ({
  default: () => <div data-testid="editor-toolbar" />,
}))

const page: NotebookItem = {
  id: 'page-1',
  notebookId: 'notebook-1',
  parentId: null,
  type: 'page',
  title: 'Overview',
  slug: 'overview',
  path: 'overview',
  sortOrder: 0,
  contentFormat: 'tiptap_json',
  contentJson: { type: 'doc', content: [] },
  plainTextContent: null,
  isArchived: false,
  archivedAtUtc: null,
  archivedByUserId: null,
  createdAtUtc: '2026-06-01T00:00:00Z',
  updatedAtUtc: null,
}

function createEditorWithUnavailableView() {
  return {
    get view() {
      throw new Error('[tiptap error]: The editor view is not available. Cannot access view[dom].')
    },
    isDestroyed: false,
    getJSON: () => ({ type: 'doc', content: [] }),
    on: vi.fn(),
    off: vi.fn(),
    storage: {
      characterCount: {
        characters: () => 0,
        words: () => 0,
      },
    },
  }
}

describe('NotebookPageEditor', () => {
  afterEach(() => {
    cleanup()
    vi.restoreAllMocks()
  })

  it('does not crash when the TipTap view is not mounted yet', () => {
    mocks.useEditor.mockReturnValue(createEditorWithUnavailableView())
    const requestAnimationFrame = vi
      .spyOn(window, 'requestAnimationFrame')
      .mockImplementation(() => 1)
    vi.spyOn(window, 'cancelAnimationFrame').mockImplementation(() => undefined)

    render(<NotebookPageEditor page={page} onSave={vi.fn()} onCancel={vi.fn()} />)

    expect(screen.getByTestId('editor-content')).toBeInTheDocument()
    expect(requestAnimationFrame).toHaveBeenCalled()
    expect(applyCodeBlockLineNumbers).not.toHaveBeenCalled()
  })
})
