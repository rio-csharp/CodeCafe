import { act, renderHook } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { notesKeys } from '@/entities/notebook'
import type { NotebookItem } from '@/entities/notebook-item'
import { useDeleteNotebookItem } from './useDeleteNotebookItem'

const mocks = vi.hoisted(() => ({
  deleteNotebookItem: vi.fn(),
}))

vi.mock('@/entities/notebook', async () => {
  const actual = await vi.importActual<typeof import('@/entities/notebook')>('@/entities/notebook')
  return {
    ...actual,
    deleteNotebookItem: mocks.deleteNotebookItem,
  }
})

function makeItem(
  id: string,
  parentId: string | null,
  type: NotebookItem['type'] = 'page',
): NotebookItem {
  return {
    id,
    notebookId: 'notebook-1',
    parentId,
    type,
    title: id,
    slug: id,
    path: id,
    sortOrder: 0,
    contentFormat: type === 'page' ? 'tiptap_json' : null,
    contentJson: null,
    plainTextContent: null,
    isArchived: true,
    archivedAtUtc: '2026-01-01T00:00:00Z',
    archivedByUserId: 'user-1',
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: null,
  }
}

describe('useDeleteNotebookItem', () => {
  beforeEach(() => {
    mocks.deleteNotebookItem.mockReset()
    mocks.deleteNotebookItem.mockResolvedValue(undefined)
  })

  it('immediately removes a deleted folder and its descendants from every cached list', async () => {
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    })
    const folder = makeItem('folder', null, 'folder')
    const child = makeItem('child', folder.id)
    const grandchild = makeItem('grandchild', child.id)
    const sibling = makeItem('sibling', null)
    const fullListKey = notesKeys.items('notebook-1', undefined, true, false)
    const searchListKey = notesKeys.items('notebook-1', 'child', true, false)

    queryClient.setQueryData(fullListKey, [folder, child, grandchild, sibling])
    queryClient.setQueryData(searchListKey, [child, grandchild])

    const wrapper = ({ children }: { children: ReactNode }) => (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    )
    const { result } = renderHook(() => useDeleteNotebookItem('notebook-1'), { wrapper })

    await act(async () => {
      await result.current.mutateAsync(folder.id)
    })

    expect(mocks.deleteNotebookItem).toHaveBeenCalledWith('notebook-1', folder.id)
    expect(queryClient.getQueryData<NotebookItem[]>(fullListKey)).toEqual([sibling])
    expect(queryClient.getQueryData<NotebookItem[]>(searchListKey)).toEqual([])
  })
})
