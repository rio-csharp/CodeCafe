import { useWatch, useController } from 'react-hook-form'
import type { Control, FieldValues, Path } from 'react-hook-form'
import {
  NOTEBOOK_VISIBILITY_HELP_TEXT,
  NOTEBOOK_VISIBILITY_LABELS,
  type NotebookVisibility,
} from '@/entities/notebook'

interface VisibilityFieldProps<T extends FieldValues> {
  control: Control<T>
  name?: Path<T>
}

const VISIBILITY_OPTIONS: { value: NotebookVisibility; label: string }[] = [
  { value: 'private', label: NOTEBOOK_VISIBILITY_LABELS.private },
  { value: 'unlisted', label: NOTEBOOK_VISIBILITY_LABELS.unlisted },
  { value: 'public', label: NOTEBOOK_VISIBILITY_LABELS.public },
]

export default function VisibilityField<T extends FieldValues>({
  control,
  name = 'visibility' as Path<T>,
}: VisibilityFieldProps<T>) {
  const visibility = useWatch({ control, name })
  const {
    field: { onChange, value },
  } = useController({ control, name })

  return (
    <div>
      <span className="block text-sm font-medium text-text-primary mb-1">Visibility</span>
      <div className="flex items-center gap-3 flex-wrap">
        {VISIBILITY_OPTIONS.map((opt) => (
          <label
            key={opt.value}
            htmlFor={`visibility-${opt.value}`}
            className={`flex items-center gap-2 rounded-lg border px-4 py-2 text-sm cursor-pointer transition-colors ${
              visibility === opt.value
                ? 'border-brand-brown bg-surface-elevated text-text-primary'
                : 'border-border-default text-text-secondary hover:bg-surface-hover'
            }`}
          >
            <input
              id={`visibility-${opt.value}`}
              type="radio"
              value={opt.value}
              checked={value === opt.value}
              onChange={() => onChange(opt.value)}
              className="accent-brand-brown"
            />
            {opt.label}
          </label>
        ))}
      </div>
      <p className="mt-2 text-xs text-text-tertiary">{NOTEBOOK_VISIBILITY_HELP_TEXT[(visibility as NotebookVisibility) ?? 'private']}</p>
    </div>
  )
}
