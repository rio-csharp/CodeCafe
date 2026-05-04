import { toDisplayName } from './noteDisplay'

export type NoteOutlineItem = {
  depth: number
  id: string
  text: string
}

export type NoteHeadingInfo = {
  title: string | null
  titleLineIndex: number | null
}

type HastNode = {
  children?: HastNode[]
  properties?: Record<string, unknown>
  tagName?: string
  type?: string
}

export function getNoteHeadingInfo(content: string): NoteHeadingInfo {
  let isInCodeBlock = false
  const lines = content.split('\n')

  for (const [index, line] of lines.entries()) {
    if (line.trim().startsWith('```')) {
      isInCodeBlock = !isInCodeBlock
      continue
    }

    if (isInCodeBlock) {
      continue
    }

    const match = /^#\s+(.+?)\s*#*$/.exec(line)

    if (match) {
      return {
        title: toDisplayName(match[1].replace(/[`*_~[\]()]/g, '').trim()),
        titleLineIndex: index,
      }
    }
  }

  return {
    title: null,
    titleLineIndex: null,
  }
}

export function removeLine(content: string, lineIndex: number | null) {
  if (lineIndex === null) {
    return content
  }

  return content
    .split('\n')
    .filter((_, index) => index !== lineIndex)
    .join('\n')
    .replace(/^\s+/, '')
}

export function buildOutline(content: string, hiddenLineIndex: number | null) {
  const usedIds = new Map<string, number>()
  const outline: NoteOutlineItem[] = []
  let isInCodeBlock = false

  for (const [index, line] of content.split('\n').entries()) {
    if (line.trim().startsWith('```')) {
      isInCodeBlock = !isInCodeBlock
      continue
    }

    if (isInCodeBlock || index === hiddenLineIndex) {
      continue
    }

    const match = /^(#{1,6})\s+(.+?)\s*#*$/.exec(line)

    if (!match) {
      continue
    }

    const depth = match[1].length

    if (depth === 1) {
      continue
    }

    const text = toDisplayName(match[2].replace(/[`*_~[\]()]/g, '').trim())
    const baseId = slugify(text)
    const duplicateCount = usedIds.get(baseId) ?? 0
    const id = duplicateCount === 0 ? baseId : `${baseId}-${duplicateCount + 1}`

    usedIds.set(baseId, duplicateCount + 1)
    outline.push({
      depth,
      id,
      text,
    })
  }

  return outline
}

export function createHeadingIdPlugin(outline: NoteOutlineItem[]) {
  return () => (tree: HastNode) => {
    let headingIndex = 0

    visitHast(tree, (node) => {
      if (node.type !== 'element' || !isHeadingTag(node.tagName)) {
        return
      }

      const item = outline[headingIndex]
      headingIndex += 1

      if (!item) {
        return
      }

      node.properties = {
        ...node.properties,
        dataOutlineId: item.id,
        id: item.id,
      }
    })
  }
}

function slugify(value: string) {
  return (
    value
      .toLowerCase()
      .trim()
      .replace(/[^a-z0-9\u4e00-\u9fa5]+/g, '-')
      .replace(/^-+|-+$/g, '') || 'heading'
  )
}

function visitHast(node: HastNode, visitor: (node: HastNode) => void) {
  visitor(node)

  for (const child of node.children ?? []) {
    visitHast(child, visitor)
  }
}

function isHeadingTag(tagName: string | undefined) {
  return (
    tagName === 'h1' ||
    tagName === 'h2' ||
    tagName === 'h3' ||
    tagName === 'h4' ||
    tagName === 'h5' ||
    tagName === 'h6'
  )
}
