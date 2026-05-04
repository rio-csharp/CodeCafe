import { toDisplayName } from './noteDisplay'
import type { NoteSummary } from './notesApi'

export type NoteTreeNode = {
  children: NoteTreeNode[]
  name: string
  note?: NoteSummary
  path: string
  sortName: string
  type: 'directory' | 'note'
}

export function buildNoteTree(notes: NoteSummary[]) {
  const root: NoteTreeNode[] = []

  for (const note of notes) {
    const segments = note.path.split('/').filter(Boolean)
    let siblings = root

    segments.forEach((segment, index) => {
      const isLast = index === segments.length - 1
      const nodePath = segments.slice(0, index + 1).join('/')

      if (isLast) {
        siblings.push({
          children: [],
          name: toDisplayName(note.title),
          note,
          path: note.path,
          sortName: segment,
          type: 'note',
        })
        return
      }

      let directory = siblings.find((node) => node.type === 'directory' && node.path === nodePath)

      if (!directory) {
        directory = {
          children: [],
          name: toDisplayName(segment),
          path: nodePath,
          sortName: segment,
          type: 'directory',
        }
        siblings.push(directory)
      }

      siblings = directory.children
    })
  }

  return sortTree(root)
}

function sortTree(nodes: NoteTreeNode[]): NoteTreeNode[] {
  return nodes
    .map((node) => ({
      ...node,
      children: sortTree(node.children),
    }))
    .sort((left, right) => {
      if (left.type !== right.type) {
        return left.type === 'directory' ? -1 : 1
      }

      return left.sortName.localeCompare(right.sortName, undefined, {
        numeric: true,
        sensitivity: 'base',
      })
    })
}
