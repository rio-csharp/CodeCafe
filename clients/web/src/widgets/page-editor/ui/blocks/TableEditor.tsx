import { useTranslation } from 'react-i18next'
import type { SpanDto, TableContent } from '@/entities/block'
import type { EditorBlock } from '../../lib/draft'

export interface TableEditorProps {
  block: EditorBlock
  onChange: (content: Record<string, unknown>) => void
}

const CELL_CLASS =
  'w-full bg-transparent px-2 py-1 text-sm text-ink placeholder:text-muted/60 focus:outline focus:outline-2 focus:outline-accent'

const TOOL_CLASS =
  'rounded-md border border-line px-2 py-0.5 text-xs text-muted transition-colors hover:bg-muted-soft hover:text-ink'

/** Plain text of a cell; cell marks survive unless the cell itself is edited. */
function cellText(cell: readonly SpanDto[]): string {
  return cell.map((span) => span.text).join('')
}

/** Replaces a cell's spans with a single plain span, dropping its marks. */
function plainCell(text: string): SpanDto[] {
  return text.length === 0 ? [] : [{ text, marks: [] }]
}

/**
 * Table editing as a plain-text grid: cell edits, add/remove row/column and a
 * header toggle. Cells are single-line rich text in the model, but the editor
 * keeps it simple — editing a cell rewrites it as unstyled text.
 */
export function TableEditor({ block, onChange }: TableEditorProps) {
  const { t } = useTranslation()
  const content = block.content as TableContent
  const columns = content.alignments.length

  const setCell = (target: 'header' | number, column: number, text: string) => {
    if (target === 'header') {
      const header = (content.header ?? []).map((cell, index) =>
        index === column ? plainCell(text) : cell,
      )
      onChange({ ...content, header })
      return
    }
    const rows = content.rows.map((row, rowIndex) =>
      rowIndex === target
        ? row.map((cell, cellIndex) => (cellIndex === column ? plainCell(text) : cell))
        : row,
    )
    onChange({ ...content, rows })
  }

  const addRow = () => {
    onChange({ ...content, rows: [...content.rows, Array.from({ length: columns }, () => [])] })
  }

  const removeRow = () => {
    if (content.rows.length > 0) {
      onChange({ ...content, rows: content.rows.slice(0, -1) })
    }
  }

  const addColumn = () => {
    onChange({
      alignments: [...content.alignments, 'none'],
      header: content.header === null ? null : [...content.header, []],
      rows: content.rows.map((row) => [...row, []]),
    })
  }

  const removeColumn = () => {
    if (columns <= 1) {
      return
    }
    onChange({
      alignments: content.alignments.slice(0, -1),
      header: content.header === null ? null : content.header.slice(0, -1),
      rows: content.rows.map((row) => row.slice(0, -1)),
    })
  }

  const toggleHeader = () => {
    onChange({
      ...content,
      header:
        content.header === null ? Array.from({ length: columns }, () => [] as SpanDto[]) : null,
    })
  }

  const renderCell = (target: 'header' | number, cell: readonly SpanDto[], column: number) => (
    <td key={column} className="border border-line">
      <input
        value={cellText(cell)}
        aria-label={
          target === 'header'
            ? t('editor.tableHeaderCell', { column: column + 1 })
            : t('editor.tableCell', { row: target + 1, column: column + 1 })
        }
        onChange={(event) => {
          setCell(target, column, event.target.value)
        }}
        className={CELL_CLASS}
      />
    </td>
  )

  return (
    <div className="flex flex-col gap-1">
      <table className="w-full border-collapse">
        {content.header !== null ? (
          <thead>
            <tr>{content.header.map((cell, column) => renderCell('header', cell, column))}</tr>
          </thead>
        ) : null}
        <tbody>
          {content.rows.map((row, rowIndex) => (
            <tr key={rowIndex}>{row.map((cell, column) => renderCell(rowIndex, cell, column))}</tr>
          ))}
        </tbody>
      </table>
      <div className="flex flex-wrap gap-1">
        <button type="button" onClick={addRow} className={TOOL_CLASS}>
          {t('editor.addRow')}
        </button>
        <button
          type="button"
          onClick={removeRow}
          disabled={content.rows.length === 0}
          className={`${TOOL_CLASS} disabled:opacity-40`}
        >
          {t('editor.removeRow')}
        </button>
        <button type="button" onClick={addColumn} className={TOOL_CLASS}>
          {t('editor.addColumn')}
        </button>
        <button
          type="button"
          onClick={removeColumn}
          disabled={columns <= 1}
          className={`${TOOL_CLASS} disabled:opacity-40`}
        >
          {t('editor.removeColumn')}
        </button>
        <button
          type="button"
          onClick={toggleHeader}
          aria-pressed={content.header !== null}
          className={TOOL_CLASS}
        >
          {t('editor.toggleHeader')}
        </button>
      </div>
    </div>
  )
}
