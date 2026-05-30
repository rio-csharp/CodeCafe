import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useLayout } from '../../../app/LayoutContext'
import { notesKeys } from '../api/queryKeys'
import {
  getPublicNotes,
  getMyNotes,
  getNotebookBySlug,
  getNotebookItems,
  createNotebook,
  updateNotebook,
  deleteNotebook,
  createNotebookItem,
  updateNotebookItem,
  archiveNotebookItem,
  restoreNotebookItem,
  deleteNotebookItem,
  reorderNotebookItems,
  getFavoriteStatus,
  addFavorite,
  removeFavorite,
} from '../api/notesApi'
import type {
  CreateNotebookRequest,
  UpdateNotebookRequest,
  CreateNotebookItemRequest,
  UpdateNotebookItemRequest,
  ReorderItemsPayload,
  NotebookItem,
} from '../types'

export function usePublicNotes(search?: string) {
  return useQuery({
    queryKey: notesKeys.public(search),
    queryFn: () => getPublicNotes(search),
  })
}

export function useMyNotes(search?: string) {
  const { user } = useLayout()
  return useQuery({
    queryKey: notesKeys.mine(search),
    queryFn: () => getMyNotes(search),
    enabled: !!user,
  })
}

export function useNotebook(slug: string) {
  return useQuery({
    queryKey: notesKeys.detail(slug),
    queryFn: () => getNotebookBySlug(slug),
  })
}

export function useNotebookItems(notebookId: string, search?: string, includeArchived?: boolean) {
  return useQuery({
    queryKey: notesKeys.items(notebookId, search, includeArchived),
    queryFn: () => getNotebookItems(notebookId, search, includeArchived),
    enabled: !!notebookId,
  })
}

export function useCreateNotebook() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateNotebookRequest) => createNotebook(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: notesKeys.all })
    },
  })
}

export function useUpdateNotebook(notebookId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: UpdateNotebookRequest) => updateNotebook(notebookId, data),
    onSuccess: (data) => {
      // If slug changed, write the new data under the new slug key so navigation works
      queryClient.setQueryData(notesKeys.detail(data.slug), data)
      queryClient.invalidateQueries({ queryKey: notesKeys.all })
    },
  })
}

export function useDeleteNotebook() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (notebookId: string) => deleteNotebook(notebookId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: notesKeys.all })
    },
  })
}

// Notebook Item Mutations

export function useCreateNotebookItem(notebookId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateNotebookItemRequest) => createNotebookItem(notebookId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: notesKeys.items(notebookId) })
      queryClient.invalidateQueries({ queryKey: notesKeys.all })
    },
  })
}

export function useUpdateNotebookItem(notebookId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ itemId, data }: { itemId: string; data: UpdateNotebookItemRequest }) =>
      updateNotebookItem(notebookId, itemId, data),
    onSuccess: (data) => {
      // Optimistically update the item in cache so the reader shows new content immediately
      queryClient.setQueryData<NotebookItem[]>(notesKeys.items(notebookId), (old) => {
        if (!old) return old
        return old.map((item) => (item.id === data.id ? data : item))
      })
      queryClient.invalidateQueries({ queryKey: notesKeys.items(notebookId) })
      queryClient.invalidateQueries({ queryKey: notesKeys.all })
    },
  })
}

export function useArchiveNotebookItem(notebookId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (itemId: string) => archiveNotebookItem(notebookId, itemId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: notesKeys.items(notebookId) })
      queryClient.invalidateQueries({ queryKey: notesKeys.all })
    },
  })
}

export function useRestoreNotebookItem(notebookId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (itemId: string) => restoreNotebookItem(notebookId, itemId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: notesKeys.items(notebookId) })
      queryClient.invalidateQueries({ queryKey: notesKeys.all })
    },
  })
}

export function useDeleteNotebookItem(notebookId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (itemId: string) => deleteNotebookItem(notebookId, itemId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: notesKeys.items(notebookId) })
      queryClient.invalidateQueries({ queryKey: notesKeys.all })
    },
  })
}

export function useReorderNotebookItems(notebookId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: ReorderItemsPayload) => reorderNotebookItems(notebookId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: notesKeys.items(notebookId) })
      queryClient.invalidateQueries({ queryKey: notesKeys.all })
    },
  })
}

// Favorite hooks

export function useFavoriteStatus(notebookId: string) {
  return useQuery({
    queryKey: [...notesKeys.all, 'favorite', notebookId],
    queryFn: () => getFavoriteStatus(notebookId),
    enabled: !!notebookId,
  })
}

export function useToggleFavorite() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ notebookId, isFavorited }: { notebookId: string; isFavorited: boolean }) => {
      const result = isFavorited
        ? await removeFavorite(notebookId)
        : await addFavorite(notebookId)
      return result
    },
    onSuccess: (_, { notebookId }) => {
      // Invalidate favorite status
      queryClient.invalidateQueries({ queryKey: [...notesKeys.all, 'favorite', notebookId] })
      // Invalidate notebooks lists and detail to refresh favoriteCount / isFavoritedByMe
      queryClient.invalidateQueries({ queryKey: notesKeys.all })
    },
  })
}
