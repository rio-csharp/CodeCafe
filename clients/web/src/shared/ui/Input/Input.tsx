import type { InputHTMLAttributes, ReactNode } from 'react'

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string
  error?: string
  iconLeft?: ReactNode
}

export function Input({ label, error, iconLeft, className = '', ...props }: InputProps) {
  return (
    <div className="w-full">
      {label && (
        <label className="block text-sm font-medium text-text-primary mb-1">
          {label}
        </label>
      )}
      <div className="relative">
        {iconLeft && (
          <span className="absolute left-3 top-1/2 -translate-y-1/2 text-text-tertiary pointer-events-none">
            {iconLeft}
          </span>
        )}
        <input
          className={`w-full rounded-lg border bg-surface text-text-primary outline-none focus:border-border-hover transition-colors placeholder:text-text-tertiary ${
            error
              ? 'border-status-error-border focus:border-status-error'
              : 'border-border-default'
          } ${iconLeft ? 'pl-10' : 'px-4'} pr-4 py-2.5 text-sm ${className}`}
          {...props}
        />
      </div>
      {error && <p className="mt-1 text-xs text-status-error">{error}</p>}
    </div>
  )
}
