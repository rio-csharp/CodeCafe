import { useWatch, useController } from 'react-hook-form'
import type { Control, FieldValues, Path } from 'react-hook-form'
import type { NotebookVisibility } from '@/entities/notebook'
import { useTranslation } from 'react-i18next'

interface VisibilityFieldProps<T extends FieldValues> {
  control: Control<T>
  name?: Path<T>
}

export default function VisibilityField<T extends FieldValues>({
  control,
  name = 'visibility' as Path<T>,
}: VisibilityFieldProps<T>) {
  const { t } = useTranslation()
  const visibility = useWatch({ control, name })
  const {
    field: { onChange, value },
  } = useController({ control, name })
  const visibilityOptions: { value: NotebookVisibility; label: string }[] = [
    { value: 'private', label: t('notebook.visibilityPrivate') },
    { value: 'unlisted', label: t('notebook.visibilityUnlisted') },
    { value: 'public', label: t('notebook.visibilityPublic') },
  ]
  const helpText = {
    private: t('notebook.visibilityPrivateHelp'),
    unlisted: t('notebook.visibilityUnlistedHelp'),
    public: t('notebook.visibilityPublicHelp'),
  } satisfies Record<NotebookVisibility, string>

  return (
    <div>
      <span className="block text-sm font-medium text-text-primary mb-1">{t('notebook.visibility')}</span>
      <div className="flex items-center gap-3 flex-wrap">
        {visibilityOptions.map((opt) => (
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
      <p className="mt-2 text-xs text-text-tertiary">{helpText[(visibility as NotebookVisibility) ?? 'private']}</p>
    </div>
  )
}
