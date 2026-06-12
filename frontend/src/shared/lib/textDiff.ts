export type TextDiffSegmentType = 'added' | 'removed' | 'unchanged'

export interface TextDiffSegment {
  type: TextDiffSegmentType
  lines: string[]
}

export interface TextDiffSummary {
  added: number
  removed: number
}

export interface TextDiffResult {
  segments: TextDiffSegment[]
  summary: TextDiffSummary
}

export function diffTextByLine(before: string | null | undefined, after: string | null | undefined): TextDiffResult {
  const beforeLines = splitLines(before ?? '')
  const afterLines = splitLines(after ?? '')
  const table = buildLcsTable(beforeLines, afterLines)
  const segments: TextDiffSegment[] = []
  let added = 0
  let removed = 0

  function push(type: TextDiffSegmentType, line: string) {
    const previous = segments.at(-1)
    if (previous?.type === type) {
      previous.lines.push(line)
      return
    }

    segments.push({ type, lines: [line] })
  }

  let i = beforeLines.length
  let j = afterLines.length
  const reversed: Array<{ type: TextDiffSegmentType; line: string }> = []

  while (i > 0 || j > 0) {
    if (i > 0 && j > 0 && beforeLines[i - 1] === afterLines[j - 1]) {
      reversed.push({ type: 'unchanged', line: beforeLines[i - 1] })
      i--
      j--
    } else if (j > 0 && (i === 0 || table[i][j - 1] >= table[i - 1][j])) {
      reversed.push({ type: 'added', line: afterLines[j - 1] })
      added++
      j--
    } else if (i > 0) {
      reversed.push({ type: 'removed', line: beforeLines[i - 1] })
      removed++
      i--
    }
  }

  for (const entry of reversed.reverse()) {
    push(entry.type, entry.line)
  }

  return {
    segments,
    summary: { added, removed },
  }
}

function splitLines(value: string): string[] {
  if (!value) {
    return []
  }

  return value.replace(/\r\n/g, '\n').split('\n')
}

function buildLcsTable(beforeLines: string[], afterLines: string[]): number[][] {
  const table = Array.from({ length: beforeLines.length + 1 }, () =>
    Array.from({ length: afterLines.length + 1 }, () => 0),
  )

  for (let i = 1; i <= beforeLines.length; i++) {
    for (let j = 1; j <= afterLines.length; j++) {
      table[i][j] = beforeLines[i - 1] === afterLines[j - 1]
        ? table[i - 1][j - 1] + 1
        : Math.max(table[i - 1][j], table[i][j - 1])
    }
  }

  return table
}
