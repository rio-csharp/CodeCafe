import type { NoteTreeNode } from './noteTreeBuilder'

export function buildNotesDirectoryPrompt(nodes: NoteTreeNode[]) {
  const lines = flattenTree(nodes)

  return lines.length > 0 ? lines.join('\n') : '(empty)'
}

export function buildNotesAssistantSystemPrompt() {
  return [
    'You are an AI reading assistant for a developer notes workspace.',
    'Answer using the provided notes context.',
    'Prefer concise, accurate answers grounded in the note content.',
    'If the answer is not supported by the provided context, say so clearly.',
    'When useful, reference relevant sections or headings from the current note.',
  ].join(' ')
}

export function buildNotesAssistantContextPrompt({
  currentNoteContent,
  currentNotePath,
  currentNoteTitle,
  directoryTree,
}: {
  currentNoteContent: string
  currentNotePath: string
  currentNoteTitle: string
  directoryTree: string
}) {
  return [
    'Workspace notes directory:',
    directoryTree,
    '',
    `Current note path: ${currentNotePath}`,
    `Current note title: ${currentNoteTitle}`,
    '',
    'Current note content:',
    currentNoteContent.trim() || '(empty)',
    '',
    'Use this as the initial context for the conversation. In later turns, keep relying on this context unless the user asks about something else.',
  ].join('\n')
}

function flattenTree(nodes: NoteTreeNode[], depth = 0): string[] {
  return nodes.flatMap((node) => {
    const indent = '  '.repeat(depth)
    const marker = node.type === 'directory' ? '- ' : '* '
    const line = `${indent}${marker}${node.name}`

    if (node.type === 'directory') {
      return [line, ...flattenTree(node.children, depth + 1)]
    }

    return [line]
  })
}
