import { apiJson } from '../../lib/apiClient'

export type NoteSummary = {
  path: string
  title: string
  updatedAt: string
  sizeBytes: number
}

export type NoteContent = NoteSummary & {
  content: string
}

export function listNotes() {
  return apiJson<NoteSummary[]>('/api/notes')
}

export function readNote(path: string) {
  return apiJson<NoteContent>(`/api/notes/content?path=${encodeURIComponent(path)}`)
}
