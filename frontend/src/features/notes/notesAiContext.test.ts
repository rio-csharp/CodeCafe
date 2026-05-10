import { describe, expect, it } from 'vitest'
import {
  buildNotesAssistantContextPrompt,
  buildNotesAssistantSystemPrompt,
} from './notesAiContext'

describe('notesAiContext', () => {
  it('allows general technical knowledge when the note does not cover a question', () => {
    const prompt = buildNotesAssistantSystemPrompt()

    expect(prompt).toContain('primary context')
    expect(prompt).toContain('broader technical knowledge')
    expect(prompt).toContain('adding from general knowledge')
    expect(prompt).toContain('provide the best general answer you can')
  })

  it('keeps the current note as the main context while allowing explicit supplementation', () => {
    const contextPrompt = buildNotesAssistantContextPrompt({
      currentNoteContent: '## Core Idea\nPinned objects stay in place.',
      currentNoteTitle: 'Common Language Runtime',
    })

    expect(contextPrompt).toContain('Current note title: Common Language Runtime')
    expect(contextPrompt).toContain('Current note content:')
    expect(contextPrompt).toContain('primary context')
    expect(contextPrompt).toContain('general technical knowledge')
  })
})
