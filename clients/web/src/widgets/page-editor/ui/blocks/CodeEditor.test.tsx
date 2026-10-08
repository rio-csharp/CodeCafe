import { useState } from 'react'
import { fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { EditorBlock } from '../../lib/draft'
import { CodeEditor } from './CodeEditor'

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}))

/** Stateful parent: the input's value lives in the block, like in the editor. */
function Harness({ initial }: { initial: string }) {
  const [block, setBlock] = useState<EditorBlock>({
    id: 'code-1',
    isNew: false,
    parentBlockId: null,
    type: 'code',
    content: { code: 'const x = 1', language: initial },
    sortKey: 'a',
    version: 1,
  })
  return (
    <CodeEditor
      block={block}
      onChange={(content) => {
        setBlock({ ...block, content })
      }}
    />
  )
}

function combobox() {
  return screen.getByRole('combobox', { name: 'editor.codeLanguage' })
}

describe('CodeEditor language combobox', () => {
  it('suggests languages on focus and filters while typing', async () => {
    const user = userEvent.setup()
    render(<Harness initial="" />)

    await user.click(combobox())
    // The suggestion list is capped; the full grammar set is larger.
    expect(screen.getAllByRole('option').length).toBe(8)

    await user.type(combobox(), 'type')
    expect(screen.getAllByRole('option').map((option) => option.textContent)).toEqual([
      'typescript',
    ])
  })

  it('picks a suggestion by click', async () => {
    const user = userEvent.setup()
    render(<Harness initial="c" />)

    await user.click(combobox())
    await user.click(screen.getByRole('option', { name: 'csharp' }))

    expect(combobox()).toHaveValue('csharp')
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument()
  })

  it('picks a suggestion by keyboard: ArrowDown opens, Enter selects', () => {
    render(<Harness initial="jav" />)

    fireEvent.keyDown(combobox(), { key: 'ArrowDown' })
    fireEvent.keyDown(combobox(), { key: 'Enter' })

    expect(combobox()).toHaveValue('java')
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument()
  })

  it('keeps free input working: an unknown language simply has no suggestions', async () => {
    const user = userEvent.setup()
    render(<Harness initial="" />)

    await user.click(combobox())
    await user.type(combobox(), 'brainfuck')

    expect(combobox()).toHaveValue('brainfuck')
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument()
  })
})
