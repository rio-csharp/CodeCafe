import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

export interface FieldProps {
  id: string
  label: string
  /** An i18n key produced by the Zod schema; translated here. */
  errorKey?: string
  children: ReactNode
}

export function Field({ id, label, errorKey, children }: FieldProps) {
  const { t } = useTranslation()

  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={id} className="text-sm font-medium text-ink">
        {label}
      </label>

      {children}

      {errorKey === undefined ? null : (
        <p id={`${id}-error`} role="alert" className="text-sm text-accent-strong">
          {t(errorKey)}
        </p>
      )}
    </div>
  )
}
