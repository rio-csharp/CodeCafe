import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Eye, EyeOff, Lock } from 'lucide-react'

interface PasswordInputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  error?: string
  ref?: React.Ref<HTMLInputElement>
}

export default function PasswordInput({ error, className, ref, ...rest }: PasswordInputProps) {
  const { t } = useTranslation()
  const [show, setShow] = useState(false)
  // Fallback accessible name only when the caller provides no label of its own
  // (via id + <label htmlFor>, aria-label, or aria-labelledby) — a hardcoded
  // aria-label would win over an external <label>.
  const fallbackAriaLabel =
    rest.id || rest['aria-label'] || rest['aria-labelledby'] ? undefined : t('auth.password')

  return (
    <div className={className}>
      <div className="relative">
        <Lock className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-text-tertiary" />
        <input
          ref={ref}
          type={show ? 'text' : 'password'}
          className={`w-full rounded-lg border bg-surface py-2.5 pl-10 pr-10 text-sm text-text-primary outline-none transition-colors focus:border-border-focus ${
            error ? 'border-status-error-border' : 'border-border-default'
          }`}
          aria-label={fallbackAriaLabel}
          {...rest}
        />
        <button
          type="button"
          onClick={() => setShow(!show)}
          aria-label={show ? t('auth.hidePassword') : t('auth.showPassword')}
          className="absolute right-3 top-1/2 -translate-y-1/2 text-text-tertiary hover:text-text-secondary"
        >
          {show ? (
            <EyeOff className="h-5 w-5" />
          ) : (
            <Eye className="h-5 w-5" />
          )}
        </button>
      </div>
      {error && <p className="mt-1 text-xs text-status-error">{error}</p>}
    </div>
  )
}
