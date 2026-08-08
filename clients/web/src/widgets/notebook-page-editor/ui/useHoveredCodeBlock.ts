import { useEffect, useRef, useState } from 'react'
import type { Editor } from '@tiptap/react'

function getMountedEditorElement(editor: Editor | null): HTMLElement | null {
  if (!editor || editor.isDestroyed) return null

  try {
    return editor.view.dom as HTMLElement
  } catch {
    return null
  }
}

/** Tracks the code block (<pre>) currently hovered inside the editor, for copy-button visibility. */
export function useHoveredCodeBlock(editor: Editor | null) {
  const [hoveredPre, setHoveredPre] = useState<HTMLElement | null>(null)
  const hoveredPreRef = useRef(hoveredPre)
  useEffect(() => { hoveredPreRef.current = hoveredPre }, [hoveredPre])

  useEffect(() => {
    const editorElement = getMountedEditorElement(editor)
    if (!editorElement) return

    const handleMouseOver = (e: MouseEvent) => {
      const pre = (e.target as HTMLElement).closest('pre')
      if (pre && editorElement.contains(pre)) setHoveredPre(pre as HTMLElement)
    }
    const handleMouseOut = (e: MouseEvent) => {
      const pre = (e.target as HTMLElement).closest('pre')
      if (pre && pre === hoveredPreRef.current) {
        const related = e.relatedTarget as HTMLElement | null
        if (!related || !pre.contains(related)) {
          setHoveredPre(null)
        }
      }
    }

    editorElement.addEventListener('mouseover', handleMouseOver)
    editorElement.addEventListener('mouseout', handleMouseOut)

    return () => {
      editorElement.removeEventListener('mouseover', handleMouseOver)
      editorElement.removeEventListener('mouseout', handleMouseOut)
    }
  }, [editor])

  return hoveredPre
}
