export const notesKeys = {
  all: ['notes'] as const,
  public: (search?: string) => [...notesKeys.all, 'public', search ?? ''] as const,
  mine: (search?: string) => [...notesKeys.all, 'mine', search ?? ''] as const,
  detail: (slug: string) => [...notesKeys.all, 'detail', slug] as const,
  itemsRoot: (notebookId: string) => [...notesKeys.all, 'items', notebookId] as const,
  items: (notebookId: string, search?: string, includeArchived?: boolean, includeContent?: boolean) =>
    [...notesKeys.itemsRoot(notebookId), search ?? '', includeArchived ?? false, includeContent ?? true] as const,
  item: (notebookId: string, itemId: string) =>
    [...notesKeys.itemsRoot(notebookId), 'item', itemId] as const,
  favorite: (notebookId: string) => [...notesKeys.all, 'favorite', notebookId] as const,
}
