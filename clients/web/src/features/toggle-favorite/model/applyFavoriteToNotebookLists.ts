import type { InfiniteData } from '@tanstack/react-query'
import type { Notebook } from '@/entities/notebook'

/**
 * Returns new infinite-list data with the favorite flag and count flipped for
 * the target notebook. Pages and notebooks that don't change keep their
 * identity; the same reference is returned when no notebook matches, so
 * callers can tell a cache write was a no-op.
 */
export function applyFavoriteToNotebookLists(
  data: InfiniteData<Notebook[]> | undefined,
  notebookId: string,
): InfiniteData<Notebook[]> | undefined {
  if (!data) return data
  let found = false
  const pages = data.pages.map((page) => {
    if (!page.some((notebook) => notebook.id === notebookId)) return page
    return page.map((notebook) => {
      if (notebook.id !== notebookId) return notebook
      found = true
      return {
        ...notebook,
        isFavoritedByMe: !notebook.isFavoritedByMe,
        favoriteCount: notebook.favoriteCount + (notebook.isFavoritedByMe ? -1 : 1),
      }
    })
  })
  return found ? { ...data, pages } : data
}
