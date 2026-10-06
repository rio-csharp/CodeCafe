import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import type { NotebookDetails } from '@/entities/notebook'

export interface NotebookTopBarProps {
  notebook: NotebookDetails
  treeOpen: boolean
  onToggleTree: () => void
  rightOpen: boolean
  onToggleRight: () => void
}

/**
 * The reader's whole horizontal chrome: one strip. Everything else on screen
 * is either a panel or the page being read; page-level actions live in the
 * sticky chrome inside the text column, not here.
 */
export function NotebookTopBar({
  notebook,
  treeOpen,
  onToggleTree,
  rightOpen,
  onToggleRight,
}: NotebookTopBarProps) {
  const { t } = useTranslation()

  return (
    <header className="flex h-14 shrink-0 items-center gap-3 border-b border-line bg-card px-3 sm:px-4">
      {/* A real route back, not history.back(): deep links have nothing to go back to. */}
      <Link
        to="/"
        aria-label={t('reader.backHome')}
        className="grid size-8 shrink-0 place-items-center rounded-full text-muted transition-colors hover:bg-muted-soft hover:text-accent-strong focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
      >
        <svg
          viewBox="0 0 16 16"
          className="size-4"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.6"
          strokeLinecap="round"
          strokeLinejoin="round"
          aria-hidden="true"
        >
          <path d="M9.5 3.5 5 8l4.5 4.5" />
        </svg>
      </Link>

      <h1 className="min-w-0 flex-1 truncate text-sm font-medium text-ink">{notebook.title}</h1>

      <div className="flex shrink-0 items-center gap-1">
        <PanelToggle label={t('reader.contents')} pressed={treeOpen} onClick={onToggleTree}>
          <path d="M3 4.5h10M3 8h10M3 11.5h6" />
        </PanelToggle>

        <PanelToggle label={t('reader.sidePanel')} pressed={rightOpen} onClick={onToggleRight}>
          <path d="M2.5 2.5h11v11h-11z M9.5 2.5v11" />
        </PanelToggle>
      </div>
    </header>
  )
}

/** Toggle button: its accessible name and `aria-pressed` carry all the state. */
function PanelToggle({
  label,
  pressed,
  onClick,
  children,
}: {
  label: string
  pressed: boolean
  onClick: () => void
  children: ReactNode
}) {
  return (
    <button
      type="button"
      aria-pressed={pressed}
      onClick={onClick}
      className={[
        'grid size-8 place-items-center rounded-full transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent',
        pressed ? 'bg-accent-soft text-accent-strong' : 'text-muted hover:bg-muted-soft',
      ].join(' ')}
    >
      <span className="sr-only">{label}</span>
      <svg
        viewBox="0 0 16 16"
        className="size-4"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.6"
        strokeLinecap="round"
        strokeLinejoin="round"
        aria-hidden="true"
      >
        {children}
      </svg>
    </button>
  )
}
