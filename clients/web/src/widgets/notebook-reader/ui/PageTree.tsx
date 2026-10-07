import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import type { PageTreeNode } from '@/entities/notebook'
import { ancestorPathsOf, normalizePagePath, searchVisibleTree, toPageHref, visibleTree } from '../lib/tree'
import type { PageSearchMatch } from '../lib/tree'

export interface PageTreeProps {
  slug: string
  roots: readonly PageTreeNode[]
  /** The page currently open, in the node's own path form. */
  activePath?: string | null
  /** Title substring; non-empty swaps the tree for flat search results. */
  filter?: string
  /** Per-node subpage creation; writers only, hidden otherwise. */
  onAddChild?: (parentPath: string) => void
  /** Lets the mobile drawer close itself once a page is chosen. */
  onNavigate?: () => void
}

export function PageTree({ slug, roots, activePath = null, filter = '', onAddChild, onNavigate }: PageTreeProps) {
  const { t } = useTranslation()
  const nodes = useMemo(() => visibleTree(roots), [roots])
  const expanded = useMemo(() => ancestorPathsOf(roots, activePath), [roots, activePath])
  const active = activePath === null ? null : normalizePagePath(activePath)
  const searching = filter.trim().length > 0
  const matches = useMemo(
    () => (searching ? searchVisibleTree(nodes, filter) : []),
    [nodes, filter, searching],
  )

  if (nodes.length === 0) {
    return null
  }

  if (searching) {
    return (
      <SearchResults slug={slug} matches={matches} active={active} onNavigate={onNavigate} />
    )
  }

  return (
    <nav aria-label={t('reader.contents')} className="text-sm">
      <ul className="flex flex-col gap-1">
        {nodes.map((node) => (
          <TreeItem
            key={node.id}
            slug={slug}
            node={node}
            active={active}
            expanded={expanded}
            depth={0}
            onAddChild={onAddChild}
            onNavigate={onNavigate}
          />
        ))}
      </ul>
    </nav>
  )
}

/** Flat title matches, each with its ancestor trail for context. */
function SearchResults({
  slug,
  matches,
  active,
  onNavigate,
}: {
  slug: string
  matches: readonly PageSearchMatch[]
  active: string | null
  onNavigate?: () => void
}) {
  const { t } = useTranslation()

  if (matches.length === 0) {
    return <p className="px-2 py-1 text-sm text-muted">{t('reader.noMatchingPages')}</p>
  }

  return (
    <nav aria-label={t('reader.searchPages')} className="text-sm">
      <ul className="flex flex-col gap-1">
        {matches.map(({ node, trail }) => {
          const self = normalizePagePath(node.path)
          const isActive = self === active
          return (
            <li key={node.id}>
              <Link
                to={toPageHref(slug, node.path)}
                aria-current={isActive ? 'page' : undefined}
                onClick={onNavigate}
                className={[
                  'block rounded px-2 py-1 transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent',
                  isActive
                    ? 'bg-accent-soft font-medium text-accent-strong'
                    : 'text-ink hover:bg-muted-soft hover:text-accent-strong',
                ].join(' ')}
              >
                <span className="block truncate">{node.title}</span>
                {trail.length > 0 ? (
                  <span className="block truncate text-xs font-normal text-muted">
                    {trail.join(' / ')}
                  </span>
                ) : null}
              </Link>
            </li>
          )
        })}
      </ul>
    </nav>
  )
}

interface TreeItemProps {
  slug: string
  node: PageTreeNode
  active: string | null
  expanded: ReadonlySet<string>
  depth: number
  onAddChild?: (parentPath: string) => void
  onNavigate?: () => void
}

function TreeItem({ slug, node, active, expanded, depth, onAddChild, onNavigate }: TreeItemProps) {
  const { t } = useTranslation()
  const self = normalizePagePath(node.path)
  const hasChildren = node.children.length > 0
  const [open, setOpen] = useState(false)

  // Ancestors of the active page are open no matter what the local toggle says.
  const isOpen = hasChildren && (open || expanded.has(self))

  return (
    <li>
      <div
        className="group flex items-center gap-1"
        style={{ paddingLeft: `${depth * 0.75}rem` }}
      >
        {hasChildren ? (
          <button
            type="button"
            aria-expanded={isOpen}
            aria-label={t('reader.toggleBranch', { title: node.title })}
            onClick={() => {
              setOpen((current) => !current)
            }}
            className="grid size-5 shrink-0 place-items-center rounded text-muted hover:text-ink focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
          >
            <svg
              viewBox="0 0 12 12"
              className={`size-3 transition-transform ${isOpen ? 'rotate-90' : ''}`}
              fill="currentColor"
              aria-hidden="true"
            >
              <path d="M4 2.5 8 6l-4 3.5V2.5Z" />
            </svg>
          </button>
        ) : (
          <span className="size-5 shrink-0" aria-hidden="true" />
        )}

        <Link
          to={toPageHref(slug, node.path)}
          aria-current={self === active ? 'page' : undefined}
          onClick={onNavigate}
          className={[
            'min-w-0 flex-1 truncate rounded px-2 py-1 transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent',
            self === active
              ? 'bg-accent-soft font-medium text-accent-strong'
              : 'text-ink hover:bg-muted-soft hover:text-accent-strong',
          ].join(' ')}
        >
          {node.title}
        </Link>

        {onAddChild !== undefined ? (
          <button
            type="button"
            aria-label={t('reader.addSubpage', { title: node.title })}
            title={t('reader.addSubpage', { title: node.title })}
            onClick={() => {
              onAddChild(node.path)
            }}
            className="grid size-5 shrink-0 place-items-center rounded text-muted transition-all hover:bg-accent-soft hover:text-accent-strong focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent sm:opacity-0 sm:group-hover:opacity-100 sm:group-focus-within:opacity-100"
          >
            <svg viewBox="0 0 16 16" className="size-3" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" aria-hidden="true">
              <path d="M8 3.5v9M3.5 8h9" />
            </svg>
          </button>
        ) : null}
      </div>

      {isOpen ? (
        <ul className="mt-1 flex flex-col gap-1">
          {node.children.map((child) => (
            <TreeItem
              key={child.id}
              slug={slug}
              node={child}
              active={active}
              expanded={expanded}
              depth={depth + 1}
              onAddChild={onAddChild}
              onNavigate={onNavigate}
            />
          ))}
        </ul>
      ) : null}
    </li>
  )
}
