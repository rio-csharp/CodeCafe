import { useInfiniteQuery } from '@tanstack/react-query'
import {
  getPublicNotes,
  getMyNotes,
  notesKeys,
} from '@/entities/notebook'

const PAGE_SIZE = 50

export function usePublicNotes(search?: string) {
  const query = useInfiniteQuery({
    queryKey: notesKeys.public(search),
    queryFn: ({ pageParam }) => getPublicNotes(search, PAGE_SIZE, pageParam),
    initialPageParam: 0,
    getNextPageParam: (lastPage, _allPages, lastPageParam) =>
      lastPage.length < PAGE_SIZE ? undefined : lastPageParam + PAGE_SIZE,
  })
  return { ...query, data: query.data?.pages.flat() }
}

export function useMyNotes(search?: string, enabled = true) {
  const query = useInfiniteQuery({
    queryKey: notesKeys.mine(search),
    queryFn: ({ pageParam }) => getMyNotes(search, PAGE_SIZE, pageParam),
    initialPageParam: 0,
    getNextPageParam: (lastPage, _allPages, lastPageParam) =>
      lastPage.length < PAGE_SIZE ? undefined : lastPageParam + PAGE_SIZE,
    enabled,
  })
  return { ...query, data: query.data?.pages.flat() }
}
