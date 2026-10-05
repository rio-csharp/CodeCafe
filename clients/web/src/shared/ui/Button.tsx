import type { ButtonHTMLAttributes } from 'react'

export type ButtonVariant = 'primary' | 'ghost'

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant
}

const BASE_CLASS =
  'inline-flex items-center justify-center gap-2 rounded-full px-5 py-2.5 text-sm font-medium transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 disabled:cursor-not-allowed disabled:opacity-60'

const VARIANT_CLASS: Record<ButtonVariant, string> = {
  primary: 'bg-accent text-card hover:bg-accent-strong focus-visible:outline-accent-strong',
  ghost: 'border border-line text-ink hover:border-accent hover:text-accent-strong focus-visible:outline-accent',
}

export function Button({ variant = 'primary', className, type = 'button', ...rest }: ButtonProps) {
  const classes = [BASE_CLASS, VARIANT_CLASS[variant], className]
    .filter((value): value is string => Boolean(value))
    .join(' ')

  return <button type={type} className={classes} {...rest} />
}
