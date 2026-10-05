import type { HTMLAttributes } from 'react'

/**
 * The one page-gutter rule: every section uses this instead of repeating
 * `mx-auto w-full max-w-6xl px-4 sm:px-6`.
 */
const CONTAINER_CLASS = 'mx-auto w-full max-w-6xl px-4 sm:px-6'

export interface ContainerProps extends HTMLAttributes<HTMLDivElement> {}

export function Container({ className, children, ...rest }: ContainerProps) {
  const classes = [CONTAINER_CLASS, className]
    .filter((value): value is string => Boolean(value))
    .join(' ')

  return (
    <div className={classes} {...rest}>
      {children}
    </div>
  )
}
