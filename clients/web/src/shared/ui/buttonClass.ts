import type { ButtonSize, ButtonVariant } from './Button'

const BASE_CLASS =
  'inline-flex items-center justify-center gap-2 rounded-xl font-medium transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 disabled:cursor-not-allowed disabled:opacity-60'

const SIZE_CLASS: Record<ButtonSize, string> = {
  md: 'px-5 py-2.5 text-sm',
  sm: 'px-3.5 py-1.5 text-xs',
}

const VARIANT_CLASS: Record<ButtonVariant, string> = {
  primary: 'bg-accent text-on-accent hover:bg-accent-strong focus-visible:outline-accent-strong',
  ghost: 'border border-line text-ink hover:border-accent hover:text-accent-strong focus-visible:outline-accent',
}

/** Shared so a link can wear the button's clothes without a wrapper component. */
export function buttonClass(
  variant: ButtonVariant = 'primary',
  size: ButtonSize = 'md',
  className?: string,
): string {
  return [BASE_CLASS, SIZE_CLASS[size], VARIANT_CLASS[variant], className]
    .filter((value): value is string => Boolean(value))
    .join(' ')
}
