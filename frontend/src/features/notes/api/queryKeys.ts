export const notesKeys = {
  all: ['notes'] as const,
  public: (search?: string) => [...notesKeys.all, 'public', search ?? ''] as const,
  mine: (search?: string) => [...notesKeys.all, 'mine', search ?? ''] as const,
  detail: (slug: string) => [...notesKeys.all, 'detail', slug] as const,
  items: (notebookId: string, search?: string, includeArchived?: boolean) =>
    [...notesKeys.all, 'items', notebookId, search ?? '', includeArchived ?? false] as const,
}
