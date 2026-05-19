import { apiFetch } from '../../../lib/apiClient'
import type {
  Notebook,
  NotebookItem,
  NotebookFavorite,
  CreateNotebookRequest,
  UpdateNotebookRequest,
  CreateNotebookItemRequest,
  UpdateNotebookItemRequest,
  ReorderItemsPayload,
} from '../types'

export async function getPublicNotes(search?: string): Promise<Notebook[]> {
  const params = search ? `?search=${encodeURIComponent(search)}` : ''
  return apiFetch<Notebook[]>(`/api/notes/public${params}`)
}

export async function getMyNotes(search?: string): Promise<Notebook[]> {
  const params = search ? `?search=${encodeURIComponent(search)}` : ''
  return apiFetch<Notebook[]>(`/api/notes/mine${params}`)
}

export async function getNotebookBySlug(slug: string): Promise<Notebook> {
  return apiFetch<Notebook>(`/api/notes/${slug}`)
}

export async function getNotebookItems(
  notebookId: string,
  search?: string,
): Promise<NotebookItem[]> {
  const params = search ? `?search=${encodeURIComponent(search)}` : ''
  return apiFetch<NotebookItem[]>(`/api/notes/${notebookId}/items${params}`)
}

export async function createNotebook(data: CreateNotebookRequest): Promise<Notebook> {
  return apiFetch<Notebook>('/api/notes', {
    method: 'POST',
    body: JSON.stringify(data),
  })
}

export async function updateNotebook(
  notebookId: string,
  data: UpdateNotebookRequest,
): Promise<Notebook> {
  return apiFetch<Notebook>(`/api/notes/${notebookId}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  })
}

export async function deleteNotebook(notebookId: string): Promise<void> {
  return apiFetch<void>(`/api/notes/${notebookId}`, {
    method: 'DELETE',
  })
}

// Notebook Item APIs

export async function createNotebookItem(
  notebookId: string,
  data: CreateNotebookItemRequest,
): Promise<NotebookItem> {
  return apiFetch<NotebookItem>(`/api/notes/${notebookId}/items`, {
    method: 'POST',
    body: JSON.stringify(data),
  })
}

export async function updateNotebookItem(
  notebookId: string,
  itemId: string,
  data: UpdateNotebookItemRequest,
): Promise<NotebookItem> {
  return apiFetch<NotebookItem>(`/api/notes/${notebookId}/items/${itemId}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  })
}

export async function deleteNotebookItem(
  notebookId: string,
  itemId: string,
): Promise<void> {
  return apiFetch<void>(`/api/notes/${notebookId}/items/${itemId}`, {
    method: 'DELETE',
  })
}

export async function reorderNotebookItems(
  notebookId: string,
  data: ReorderItemsPayload,
): Promise<{ items: NotebookItem[] }> {
  return apiFetch<{ items: NotebookItem[] }>(`/api/notes/${notebookId}/items/reorder`, {
    method: 'PUT',
    body: JSON.stringify(data),
  })
}

// Favorite APIs

export async function getFavoriteStatus(notebookId: string): Promise<NotebookFavorite> {
  return apiFetch<NotebookFavorite>(`/api/notes/${notebookId}/favorite`)
}

export async function addFavorite(notebookId: string): Promise<NotebookFavorite> {
  return apiFetch<NotebookFavorite>(`/api/notes/${notebookId}/favorite`, {
    method: 'POST',
  })
}

export async function removeFavorite(notebookId: string): Promise<NotebookFavorite> {
  return apiFetch<NotebookFavorite>(`/api/notes/${notebookId}/favorite`, {
    method: 'DELETE',
  })
}
