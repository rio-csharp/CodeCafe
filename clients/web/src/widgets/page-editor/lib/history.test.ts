import { describe, expect, it } from 'vitest'
import type { EditorBlock } from './draft'
import {
  applyRedo,
  applyUndo,
  COALESCE_MS,
  emptyHistory,
  HISTORY_LIMIT,
  recordChange,
} from './history'

function paragraph(id: string, text: string): EditorBlock {
  return {
    id,
    isNew: false,
    parentBlockId: null,
    type: 'paragraph',
    content: { spans: [{ text, marks: [] }] },
    sortKey: 'a',
    version: 1,
  }
}

function text(blocks: EditorBlock[], id: string): string {
  const block = blocks.find((entry) => entry.id === id)
  return ((block?.content as { spans: { text: string }[] } | undefined)?.spans ?? [])
    .map((span) => span.text)
    .join('')
}

describe('recordChange', () => {
  it('coalesces same-run changes inside the time window into one step', () => {
    const before = [paragraph('a', '')]
    let history = recordChange(emptyHistory(), before, 'text:a', 1000)
    history = recordChange(history, [paragraph('a', 'h')], 'text:a', 1000 + COALESCE_MS - 1)
    history = recordChange(history, [paragraph('a', 'he')], 'text:a', 1000 + COALESCE_MS)

    expect(history.past).toHaveLength(1)
    const undone = applyUndo(history, [paragraph('a', 'hel')])
    expect(text(undone!.draft, 'a')).toBe('')
  })

  it('starts a new step once the run goes quiet past the window', () => {
    let history = recordChange(emptyHistory(), [paragraph('a', 'x')], 'text:a', 1000)
    history = recordChange(history, [paragraph('a', 'xy')], 'text:a', 1000 + COALESCE_MS + 1)
    expect(history.past).toHaveLength(2)
  })

  it('never coalesces structural changes (run = null)', () => {
    let history = recordChange(emptyHistory(), [paragraph('a', 'x')], null, 1000)
    history = recordChange(history, [paragraph('a', 'y')], null, 1001)
    expect(history.past).toHaveLength(2)
  })

  it('clears the redo stack on a new change', () => {
    let history = recordChange(emptyHistory(), [paragraph('a', 'x')], null, 1000)
    const undone = applyUndo(history, [paragraph('a', 'y')])!
    history = recordChange(undone.history, undone.draft, null, 2000)
    expect(history.future).toHaveLength(0)
  })

  it('caps the stack at HISTORY_LIMIT', () => {
    let history = emptyHistory()
    for (let index = 0; index < HISTORY_LIMIT + 10; index += 1) {
      history = recordChange(history, [paragraph('a', `v${index}`)], null, index * 1000)
    }
    expect(history.past).toHaveLength(HISTORY_LIMIT)
  })

  it('isolates the snapshot from later in-place payload mutation', () => {
    const before = [paragraph('a', 'x')]
    const history = recordChange(emptyHistory(), before, null, 1000)
    ;(before[0]!.content as { spans: { text: string }[] }).spans[0]!.text = 'MUTATED'
    expect(text(history.past[0]!.draft, 'a')).toBe('x')
  })
})

describe('applyUndo / applyRedo', () => {
  it('returns null when there is nothing to undo or redo', () => {
    const current = [paragraph('a', 'x')]
    expect(applyUndo(emptyHistory(), current)).toBeNull()
    expect(applyRedo(emptyHistory(), current)).toBeNull()
  })

  it('undo restores the pre-change draft, redo reapplies the change', () => {
    const v1 = [paragraph('a', 'one')]
    const v2 = [paragraph('a', 'two')]
    const history = recordChange(emptyHistory(), v1, null, 1000)

    const undone = applyUndo(history, v2)!
    expect(text(undone.draft, 'a')).toBe('one')

    const redone = applyRedo(undone.history, undone.draft)!
    expect(text(redone.draft, 'a')).toBe('two')

    const backAgain = applyUndo(redone.history, redone.draft)!
    expect(text(backAgain.draft, 'a')).toBe('one')
  })

  it('redoes multiple undos in reverse order', () => {
    let history = emptyHistory()
    history = recordChange(history, [paragraph('a', 'v1')], null, 1000)
    history = recordChange(history, [paragraph('a', 'v2')], null, 2000)

    const u1 = applyUndo(history, [paragraph('a', 'v3')])!
    const u2 = applyUndo(u1.history, u1.draft)!
    expect(text(u2.draft, 'a')).toBe('v1')

    const r1 = applyRedo(u2.history, u2.draft)!
    expect(text(r1.draft, 'a')).toBe('v2')
    const r2 = applyRedo(r1.history, r1.draft)!
    expect(text(r2.draft, 'a')).toBe('v3')
  })
})
