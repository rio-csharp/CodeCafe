import { describe, expect, it } from 'vitest'
import { MARKDOWN_FILE_MAX_BYTES, readMarkdownFile } from './markdownFile'

describe('readMarkdownFile', () => {
  it('reads the file text and keeps its name', async () => {
    const file = new File(['# Hello\n'], 'notes.md', { type: 'text/markdown' })

    const read = await readMarkdownFile(file)

    expect(read).toEqual({ ok: true, fileName: 'notes.md', markdown: '# Hello\n' })
  })

  it('rejects files over the client-side limit without reading them', async () => {
    const file = new File(['x'], 'huge.md')
    Object.defineProperty(file, 'size', { value: MARKDOWN_FILE_MAX_BYTES + 1 })

    const read = await readMarkdownFile(file)

    expect(read).toEqual({ ok: false, reason: 'tooLarge' })
  })

  it('reports unreadable files instead of throwing', async () => {
    const file = new File(['x'], 'broken.md')
    Object.defineProperty(file, 'text', {
      value: () => Promise.reject(new Error('disk gone')),
    })

    const read = await readMarkdownFile(file)

    expect(read).toEqual({ ok: false, reason: 'unreadable' })
  })
})
