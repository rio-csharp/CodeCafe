import type { HTMLAttributes } from 'react'

/**
 * The one page-gutter rule. `wide` is reserved for reading surfaces that need
 * the room — everything else keeps `standard` so the site stays aligned.
 */
const CONTAINER_CLASS = 'mx-auto w-full px-4 sm:px-6'

const WIDTH_CLASS = {
  standard: 'max-w-6xl',
  wide: 'max-w-screen-2xl',
} as const

export type ContainerWidth = keyof typeof WIDTH_CLASS

export interface ContainerProps extends HTMLAttributes<HTMLDivElement> {
  width?: ContainerWidth
}

export function Container({ className, width = 'standard', children, ...rest }: ContainerProps) {
  const classes = [CONTAINER_CLASS, WIDTH_CLASS[width], className]
    .filter((value): value is string => Boolean(value))
    .join(' ')

  return (
    <div className={classes} {...rest}>
      {children}
    </div>
  )
}
