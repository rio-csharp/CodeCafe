import { describe, it, expect, vi, beforeEach } from 'vitest'
import {
  getPublicNotes,
  getMyNotes,
  getNotebookBySlug,
  getNotebookItems,
  createNotebook,
  updateNotebook,
  deleteNotebook,
} from './notesApi'
import * as apiClient from '../../../lib/apiClient'

vi.mock('../../../lib/apiClient', () => ({
  apiFetch: vi.fn(),
}))

const mockedApiFetch = vi.mocked(apiClient.apiFetch)

beforeEach(() => {
  mockedApiFetch.mockClear()
})

describe('notesApi', () => {
  it('getPublicNotes calls correct endpoint', async () => {
    mockedApiFetch.mockResolvedValue([])
    await getPublicNotes()
    expect(mockedApiFetch).toHaveBeenCalledWith('/api/notes/public')
  })

  it('getMyNotes calls correct endpoint', async () => {
    mockedApiFetch.mockResolvedValue([])
    await getMyNotes()
    expect(mockedApiFetch).toHaveBeenCalledWith('/api/notes/mine')
  })

  it('getNotebookBySlug calls correct endpoint', async () => {
    mockedApiFetch.mockResolvedValue({ id: '1' })
    await getNotebookBySlug('my-notebook')
    expect(mockedApiFetch).toHaveBeenCalledWith('/api/notes/my-notebook')
  })

  it('getNotebookItems calls correct endpoint', async () => {
    mockedApiFetch.mockResolvedValue([])
    await getNotebookItems('nb-1')
    expect(mockedApiFetch).toHaveBeenCalledWith('/api/notes/nb-1/items')
  })

  it('createNotebook sends POST with body', async () => {
    mockedApiFetch.mockResolvedValue({ id: '1' })
    const payload = { title: 'T', description: 'D', visibility: 'public' as const }
    await createNotebook(payload)
    expect(mockedApiFetch).toHaveBeenCalledWith('/api/notes', {
      method: 'POST',
      body: JSON.stringify(payload),
    })
  })

  it('updateNotebook sends PUT with body', async () => {
    mockedApiFetch.mockResolvedValue({ id: '1' })
    const payload = { title: 'Updated' }
    await updateNotebook('nb-1', payload)
    expect(mockedApiFetch).toHaveBeenCalledWith('/api/notes/nb-1', {
      method: 'PUT',
      body: JSON.stringify(payload),
    })
  })

  it('deleteNotebook sends DELETE', async () => {
    mockedApiFetch.mockResolvedValue(undefined)
    await deleteNotebook('nb-1')
    expect(mockedApiFetch).toHaveBeenCalledWith('/api/notes/nb-1', {
      method: 'DELETE',
    })
  })
})
