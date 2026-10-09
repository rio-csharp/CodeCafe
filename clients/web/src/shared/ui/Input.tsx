import type { ComponentPropsWithRef } from 'react'

export interface InputProps extends ComponentPropsWithRef<'input'> {
  invalid?: boolean
}

const BASE_CLASS =
  'h-11 w-full rounded-xl border bg-card px-4 text-ink placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent'

const STATE_CLASS: Record<'valid' | 'invalid', string> = {
  valid: 'border-line focus:border-accent',
  invalid: 'border-danger focus:border-danger focus:ring-danger',
}

export function Input({ invalid = false, className, ...rest }: InputProps) {
  const classes = [BASE_CLASS, STATE_CLASS[invalid ? 'invalid' : 'valid'], className]
    .filter((value): value is string => Boolean(value))
    .join(' ')

  return <input className={classes} aria-invalid={invalid || undefined} {...rest} />
}
