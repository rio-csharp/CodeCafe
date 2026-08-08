import { apiFetch } from '@/shared/api'
import type {
  Notebook,
  NotebookFavorite,
  CreateNotebookRequest,
  UpdateNotebookRequest,
} from '../model/types'
import type {
  NotebookItem,
  CreateNotebookItemRequest,
  UpdateNotebookItemRequest,
  ReorderItemsPayload,
} from '@/entities/notebook-item'

function buildQueryString(params: Record<string, string | undefined>): string {
  const query = new URLSearchParams()
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== '') {
      query.set(key, value)
    }
  })
  const qs = query.toString()
  return qs ? `?${qs}` : ''
}

export async function getPublicNotes(search?: string, limit = 50, offset = 0): Promise<Notebook[]> {
  const params = buildQueryString({ search, limit: String(limit), offset: String(offset) })
  return apiFetch<Notebook[]>(`/api/notes/public${params}`)
}

export async function getMyNotes(search?: string, limit = 50, offset = 0): Promise<Notebook[]> {
  const params = buildQueryString({ search, limit: String(limit), offset: String(offset) })
  return apiFetch<Notebook[]>(`/api/notes/mine${params}`)
}

export async function getNotebookBySlug(slug: string, signal?: AbortSignal): Promise<Notebook> {
  return apiFetch<Notebook>(`/api/notes/${slug}?includeItems=false`, { signal })
}

export async function getNotebookItems(
  notebookId: string,
  search?: string,
  includeArchived?: boolean,
  includeContent?: boolean,
  signal?: AbortSignal,
): Promise<NotebookItem[]> {
  const params = buildQueryString({
    search,
    includeArchived: includeArchived ? 'true' : undefined,
    includeContent: includeContent === undefined ? undefined : String(includeContent),
  })
  return apiFetch<NotebookItem[]>(`/api/notes/${notebookId}/items${params}`, { signal })
}

export async function getNotebookItem(
  notebookId: string,
  itemId: string,
  signal?: AbortSignal,
): Promise<NotebookItem> {
  return apiFetch<NotebookItem>(`/api/notes/${notebookId}/items/${itemId}`, { signal })
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

export async function archiveNotebookItem(
  notebookId: string,
  itemId: string,
): Promise<NotebookItem> {
  return apiFetch<NotebookItem>(`/api/notes/${notebookId}/items/${itemId}/archive`, {
    method: 'POST',
  })
}

export async function restoreNotebookItem(
  notebookId: string,
  itemId: string,
): Promise<NotebookItem> {
  return apiFetch<NotebookItem>(`/api/notes/${notebookId}/items/${itemId}/restore`, {
    method: 'POST',
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
