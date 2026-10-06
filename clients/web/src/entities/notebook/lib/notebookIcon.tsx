import type { ReactNode } from 'react'

/**
 * Deterministic variety, not semantics: the same title always gets the same
 * icon, and neighbouring notebooks rarely share one. Hash-picked, like 3.4.
 */
const ICONS: readonly ReactNode[] = [
  // folder
  <path key="f" d="M2.5 4.5a1 1 0 0 1 1-1h3l1.5 2h5.5a1 1 0 0 1 1 1v6a1 1 0 0 1-1 1h-10a1 1 0 0 1-1-1v-8Z" />,
  // code
  <path key="c" d="M5.5 5 2.5 8l3 3M10.5 5l3 3-3 3" />,
  // database
  <path key="d" d="M8 2.5c3 0 5 .8 5 1.9v7.2c0 1-2 1.9-5 1.9s-5-.8-5-1.9V4.4c0-1 2-1.9 5-1.9ZM3 8c0 1 2 1.9 5 1.9s5-.8 5-1.9" />,
  // book
  <path key="b" d="M3 2.5h7a2 2 0 0 1 2 2v9H5a2 2 0 0 1-2-2v-9ZM5.5 5.5h4" />,
  // file
  <path key="t" d="M4 2.5h5.5L12 5v8.5a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1v-10a1 1 0 0 1 1-1ZM9 2.5V5.5H12" />,
  // layers
  <path key="l" d="m8 2 5.5 3L8 8 2.5 5 8 2ZM2.5 8.5 8 11.5l5.5-3M2.5 11.5 8 14.5l5.5-3" />,
]

export function notebookIcon(title: string): ReactNode {
  let hash = 0
  for (let index = 0; index < title.length; index += 1) {
    hash = title.charCodeAt(index) + ((hash << 5) - hash)
  }
  return ICONS[Math.abs(hash) % ICONS.length]
}
