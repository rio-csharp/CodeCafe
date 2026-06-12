import { Editor } from '@tiptap/core'
import { createTipTapExtensions } from './tiptapExtensions'

export function getTipTapText(content: Record<string, unknown> | null | undefined): string {
  const extensions = createTipTapExtensions({ editable: false })
  const editor = new Editor({
    extensions,
    content: content ?? { type: 'doc', content: [] },
    editable: false,
  })

  try {
    return editor.getText({ blockSeparator: '\n' })
  } finally {
    editor.destroy()
  }
}
