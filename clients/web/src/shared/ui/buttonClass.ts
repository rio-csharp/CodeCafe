import type { ButtonVariant } from './Button'

const BASE_CLASS =
  'inline-flex items-center justify-center gap-2 rounded-full px-5 py-2.5 text-sm font-medium transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 disabled:cursor-not-allowed disabled:opacity-60'

const VARIANT_CLASS: Record<ButtonVariant, string> = {
  primary: 'bg-accent text-card hover:bg-accent-strong focus-visible:outline-accent-strong',
  ghost: 'border border-line text-ink hover:border-accent hover:text-accent-strong focus-visible:outline-accent',
}

/** Shared so a link can wear the button's clothes without a wrapper component. */
export function buttonClass(variant: ButtonVariant = 'primary', className?: string): string {
  return [BASE_CLASS, VARIANT_CLASS[variant], className]
    .filter((value): value is string => Boolean(value))
    .join(' ')
}
