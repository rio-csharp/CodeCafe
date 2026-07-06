import { useQuery } from '@tanstack/react-query'
import {
  getPublicNotes,
  getMyNotes,
  notesKeys,
} from '@/entities/notebook'

export function usePublicNotes(search?: string) {
  return useQuery({
    queryKey: notesKeys.public(search),
    queryFn: () => getPublicNotes(search),
  })
}

export function useMyNotes(search?: string, enabled = true) {
  return useQuery({
    queryKey: notesKeys.mine(search),
    queryFn: () => getMyNotes(search),
    enabled,
  })
}
