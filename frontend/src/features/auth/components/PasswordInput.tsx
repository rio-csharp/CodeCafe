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
        <Lock className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-gray-400" />
        <input
          ref={ref}
          type={show ? 'text' : 'password'}
          className={`w-full rounded-lg border bg-white py-2.5 pl-10 pr-10 text-sm text-gray-900 outline-none transition-colors focus:border-black ${
            error ? 'border-red-300' : 'border-gray-200'
          }`}
          {...rest}
        />
        <button
          type="button"
          onClick={() => setShow(!show)}
          aria-label={show ? 'Hide password' : 'Show password'}
          className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600"
        >
          {show ? (
            <EyeOff className="h-5 w-5" />
          ) : (
            <Eye className="h-5 w-5" />
          )}
        </button>
      </div>
      {error && <p className="mt-1 text-xs text-red-500">{error}</p>}
    </div>
  )
}
