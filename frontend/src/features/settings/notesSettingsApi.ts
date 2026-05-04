import { apiJson, apiSend } from '../../lib/apiClient'

export type NotesSettings = {
  rootPath: string
}

export function getNotesSettings() {
  return apiJson<NotesSettings>('/api/notes/settings')
}

export function updateNotesSettings(settings: NotesSettings) {
  return apiSend<NotesSettings>('/api/notes/settings', 'PUT', settings)
}
