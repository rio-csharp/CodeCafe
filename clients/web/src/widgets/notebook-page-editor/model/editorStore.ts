import { create } from 'zustand'

interface EditorStore {
  editClickedForPath: string | null
  setEditClickedForPath: (path: string | null) => void
}

export const useEditorStore = create<EditorStore>((set) => ({
  editClickedForPath: null,
  setEditClickedForPath: (path) => set({ editClickedForPath: path }),
}))
