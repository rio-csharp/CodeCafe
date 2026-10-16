import type { TableAlignment, TableContent } from '../model/types'
import { SpanRenderer } from './SpanRenderer'

const ALIGNMENT_CLASS: Record<TableAlignment, string> = {
  none: '',
  left: 'text-left',
  center: 'text-center',
  right: 'text-right',
}

const CELL_CLASS = 'border-b border-line px-3 py-2 align-top'

export function TableBlock({ content }: { content: TableContent }) {
  return (
    <div className="overflow-x-auto rounded-xl border border-line">
      <table className="w-full border-collapse text-sm">
        {content.header === null ? null : (
          <thead>
            <tr>
              {content.header.map((cell, column) => (
                <th
                  key={column}
                  scope="col"
                  className={`${CELL_CLASS} font-semibold text-ink ${alignmentAt(content.alignments, column)}`}
                >
                  <SpanRenderer spans={cell} />
                </th>
              ))}
            </tr>
          </thead>
        )}

        <tbody>
          {content.rows.map((row, index) => (
            <tr key={index} className="odd:bg-muted-soft/50">
              {row.map((cell, column) => (
                <td
                  key={column}
                  className={`${CELL_CLASS} text-ink ${alignmentAt(content.alignments, column)}`}
                >
                  <SpanRenderer spans={cell} />
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

/** A missing column simply gets no alignment — the grid stays rectangular. */
function alignmentAt(alignments: TableAlignment[], column: number): string {
  return ALIGNMENT_CLASS[alignments[column] ?? 'none']
}
