import { useEffect } from 'react'
import type { Editor } from '@tiptap/react'

/**
 * Syncs editor content when the external page content changes (e.g. after a
 * save roundtrip or when navigating between pages). This must run after the
 * editor instance is ready and be guarded against destroying user edits:
 * user typing only mutates the editor view, never the external content JSON.
 */
export function useEditorContentSync(editor: Editor | null, safeContent: Record<string, unknown>) {
  useEffect(() => {
    if (!editor || editor.isDestroyed) return
    const current = editor.getJSON()
    if (JSON.stringify(current) !== JSON.stringify(safeContent)) {
      editor.commands.setContent(safeContent, { emitUpdate: false })
    }
  }, [editor, safeContent])
}
