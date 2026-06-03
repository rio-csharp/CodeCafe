import type { InputHTMLAttributes } from 'react'

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string
  error?: string
}

export function Input({ label, error, className = '', ...props }: InputProps) {
  return (
    <div className="w-full">
      {label && (
        <label className="block text-sm font-medium text-text-primary mb-1">
          {label}
        </label>
      )}
      <input
        className={`w-full rounded-lg border border-border-default px-4 py-2.5 text-sm outline-none focus:border-border-hover transition-colors placeholder:text-text-tertiary ${error ? 'border-status-error-border focus:border-status-error' : ''} ${className}`}
        {...props}
      />
      {error && <p className="mt-1 text-xs text-status-error">{error}</p>}
    </div>
  )
}
