import { useWatch, useController } from 'react-hook-form'
import type { Control, FieldValues, Path } from 'react-hook-form'
import type { NotebookVisibility } from '../../types'

interface VisibilityFieldProps<T extends FieldValues> {
  control: Control<T>
  name?: Path<T>
}

const VISIBILITY_OPTIONS: { value: NotebookVisibility; label: string }[] = [
  { value: 'private', label: 'Private' },
  { value: 'unlisted', label: 'Unlisted' },
  { value: 'public', label: 'Public' },
]

const visibilityHelp: Record<NotebookVisibility, string> = {
  public: 'This notebook will be published and visible to everyone.',
  unlisted: 'Only people with the link can access this notebook.',
  private: 'Only you can access this notebook.',
}

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
      <span className="block text-sm font-medium text-black mb-1">Visibility</span>
      <div className="flex items-center gap-3 flex-wrap">
        {VISIBILITY_OPTIONS.map((opt) => (
          <label
            key={opt.value}
            htmlFor={`visibility-${opt.value}`}
            className={`flex items-center gap-2 rounded-lg border px-4 py-2 text-sm cursor-pointer transition-colors ${
              visibility === opt.value
                ? 'border-brand-brown bg-stone-50 text-black'
                : 'border-gray-200 text-gray-600 hover:bg-gray-50'
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
      <p className="mt-2 text-xs text-gray-400">{visibilityHelp[(visibility as NotebookVisibility) ?? 'private']}</p>
    </div>
  )
}
