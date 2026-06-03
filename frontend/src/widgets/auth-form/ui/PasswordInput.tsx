import { useState } from 'react'
import { Eye, EyeOff, Lock } from 'lucide-react'

interface PasswordInputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  error?: string
  ref?: React.Ref<HTMLInputElement>
}

export default function PasswordInput({ error, className, ref, ...rest }: PasswordInputProps) {
  const [show, setShow] = useState(false)

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
          aria-label="Password"
          {...rest}
        />
        <button
          type="button"
          onClick={() => setShow(!show)}
          aria-label={show ? 'Hide password' : 'Show password'}
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
