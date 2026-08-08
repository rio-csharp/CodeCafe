import type { TextDiffSegment } from '@/shared/lib/textDiff'

export function DiffSegment({ segment }: { segment: TextDiffSegment }) {
  const className = segment.type === 'added'
    ? 'bg-status-success-bg text-text-primary'
    : segment.type === 'removed'
      ? 'bg-status-error-bg text-text-primary'
      : 'bg-surface text-text-secondary'
  const prefix = segment.type === 'added' ? '+' : segment.type === 'removed' ? '-' : ' '

  return (
    <div className={className}>
      {segment.lines.map((line, index) => (
        <div key={index} className="grid grid-cols-[2rem_1fr] gap-2 px-3 py-0.5">
          <span className="select-none text-right text-text-tertiary">{prefix}</span>
          <span className="whitespace-pre-wrap break-words">{line || ' '}</span>
        </div>
      ))}
    </div>
  )
}
