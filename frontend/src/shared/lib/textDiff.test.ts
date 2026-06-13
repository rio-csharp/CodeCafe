import { describe, expect, it } from 'vitest'
import { diffTextByLine } from './textDiff'

describe('diffTextByLine', () => {
  it('groups added and removed lines', () => {
    const result = diffTextByLine('alpha\nbeta\ngamma', 'alpha\nbravo\ngamma\ndelta')

    expect(result.summary).toEqual({ added: 2, removed: 1 })
    expect(result.segments).toEqual([
      { type: 'unchanged', lines: ['alpha'] },
      { type: 'removed', lines: ['beta'] },
      { type: 'added', lines: ['bravo'] },
      { type: 'unchanged', lines: ['gamma'] },
      { type: 'added', lines: ['delta'] },
    ])
  })

  it('ignores trailing newline differences caused by inconsistent text extraction', () => {
    const result = diffTextByLine('alpha\nbeta', 'alpha\nbravo\n')

    expect(result.summary).toEqual({ added: 1, removed: 1 })
    expect(result.segments).toEqual([
      { type: 'unchanged', lines: ['alpha'] },
      { type: 'removed', lines: ['beta'] },
      { type: 'added', lines: ['bravo'] },
    ])
  })
})
