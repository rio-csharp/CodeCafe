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
  currentNoteTitle,
}: {
  currentNoteContent: string
  currentNoteTitle: string
}) {
  return [
    `Current note title: ${currentNoteTitle}`,
    '',
    'Current note content:',
    currentNoteContent.trim() || '(empty)',
    '',
    'Use this as the initial context for the conversation. In later turns, keep relying on this context unless the user asks about something else.',
  ].join('\n')
}
