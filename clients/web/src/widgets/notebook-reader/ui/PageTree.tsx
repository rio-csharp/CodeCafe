import { useMemo, useState } from 'react'
import type { DragEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import type { PageTreeNode } from '@/entities/notebook'
import type { MovePageData } from '@/entities/page'
import { dropPositionAt, resolveDrop } from '../lib/dragDrop'
import type { DropPosition } from '../lib/dragDrop'
import { ancestorPathsOf, normalizePagePath, searchVisibleTree, toPageHref, visibleTree } from '../lib/tree'
import type { PageSearchMatch } from '../lib/tree'
import { PageNodeMenu } from './PageNodeMenu'

export interface PageTreeProps {
  slug: string
  roots: readonly PageTreeNode[]
  /** The page currently open, in the node's own path form. */
  activePath?: string | null
  /** Title substring; non-empty swaps the tree for flat search results. */
  filter?: string
  /** Writers get drag-and-drop, node menus and archived pages; readers don't. */
  canWrite?: boolean
  /** Per-node subpage creation; writers only, hidden otherwise. */
  onAddChild?: (parentPath: string) => void
  /** Per-node markdown import, offered in the node menu; writers only. */
  onImportSubpage?: (node: PageTreeNode) => void
  /** Drag-and-drop target resolution; the mutation lives with the caller. */
  onMovePage?: (pageId: string, data: MovePageData) => void
  /** Archive/unarchive from the node menu; writers only. */
  onToggleArchive?: (node: PageTreeNode) => void
  /** Opens the delete confirm dialog for the node; writers only. */
  onDeletePage?: (node: PageTreeNode) => void
  /** Lets the mobile drawer close itself once a page is chosen. */
  onNavigate?: () => void
}

interface DropIndicator {
  id: string
  position: DropPosition
}

/** Drag state shared by every row, so any row can answer "may I land here?". */
interface TreeDnd {
  draggingId: string | null
  indicator: DropIndicator | null
  onDragStart: (id: string) => void
  onDragEnd: () => void
  onDragOver: (node: PageTreeNode, event: DragEvent<HTMLDivElement>) => void
  onDrop: (node: PageTreeNode, event: DragEvent<HTMLDivElement>) => void
}

export function PageTree({
  slug,
  roots,
  activePath = null,
  filter = '',
  canWrite = false,
  onAddChild,
  onImportSubpage,
  onMovePage,
  onToggleArchive,
  onDeletePage,
  onNavigate,
}: PageTreeProps) {
  const { t } = useTranslation()
  const nodes = useMemo(
    () => visibleTree(roots, { includeArchived: canWrite }),
    [roots, canWrite],
  )
  const expanded = useMemo(() => ancestorPathsOf(roots, activePath), [roots, activePath])
  const active = activePath === null ? null : normalizePagePath(activePath)
  const searching = filter.trim().length > 0
  const matches = useMemo(
    () => (searching ? searchVisibleTree(nodes, filter) : []),
    [nodes, filter, searching],
  )

  const [draggingId, setDraggingId] = useState<string | null>(null)
  const [indicator, setIndicator] = useState<DropIndicator | null>(null)
  // The flat search result view has no hierarchy to drop into.
  const dndEnabled = canWrite && onMovePage !== undefined && !searching

  const positionFromEvent = (event: DragEvent<HTMLDivElement>): DropPosition => {
    const rect = event.currentTarget.getBoundingClientRect()
    const ratio = rect.height > 0 ? (event.clientY - rect.top) / rect.height : 0.5
    return dropPositionAt(ratio)
  }

  const dnd: TreeDnd = {
    draggingId,
    indicator,
    onDragStart: setDraggingId,
    onDragEnd: () => {
      setDraggingId(null)
      setIndicator(null)
    },
    onDragOver: (node, event) => {
      if (draggingId === null || draggingId === node.id) {
        return
      }
      const position = positionFromEvent(event)
      const drop = resolveDrop(nodes, draggingId, node.id, position)
      if (drop === null) {
        // Not a legal landing spot: no indicator, and the browser keeps its
        // refusal cursor because we deliberately skip preventDefault.
        setIndicator((current) => (current?.id === node.id ? null : current))
        return
      }
      event.preventDefault()
      if (event.dataTransfer !== null) {
        event.dataTransfer.dropEffect = 'move'
      }
      setIndicator({ id: node.id, position })
    },
    onDrop: (node, event) => {
      if (draggingId === null) {
        return
      }
      event.preventDefault()
      const drop = resolveDrop(nodes, draggingId, node.id, positionFromEvent(event))
      const dragged = draggingId
      setDraggingId(null)
      setIndicator(null)
      if (drop !== null) {
        onMovePage?.(dragged, drop)
      }
    },
  }

  if (nodes.length === 0) {
    return null
  }

  if (searching) {
    return (
      <SearchResults slug={slug} matches={matches} active={active} onNavigate={onNavigate} />
    )
  }

  return (
    <nav
      aria-label={t('reader.contents')}
      className="text-sm"
      onDragLeave={(event) => {
        // Leaving the strip entirely clears any stale indicator; moving
        // between rows is re-decided by the next row's dragover.
        if (!event.currentTarget.contains(event.relatedTarget as Node | null)) {
          setIndicator(null)
        }
      }}
    >
      <ul className="flex flex-col gap-1">
        {nodes.map((node) => (
          <TreeItem
            key={node.id}
            slug={slug}
            node={node}
            active={active}
            expanded={expanded}
            depth={0}
            canWrite={canWrite}
            dnd={dndEnabled ? dnd : undefined}
            onAddChild={onAddChild}
            onImportSubpage={onImportSubpage}
            onToggleArchive={onToggleArchive}
            onDeletePage={onDeletePage}
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
                  node.isArchived ? 'opacity-60' : '',
                ].join(' ')}
              >
                <span className="block truncate">
                  {node.title}
                  {node.isArchived ? <ArchivedBadge /> : null}
                </span>
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

/** Only writers ever see archived nodes; the badge explains the dimming. */
function ArchivedBadge() {
  const { t } = useTranslation()
  return (
    <span className="ml-1.5 inline-block shrink-0 rounded-full bg-muted-soft px-1.5 py-0.5 align-middle text-[10px] font-medium tracking-wide text-muted uppercase">
      {t('reader.archivedBadge')}
    </span>
  )
}

interface TreeItemProps {
  slug: string
  node: PageTreeNode
  active: string | null
  expanded: ReadonlySet<string>
  depth: number
  canWrite: boolean
  dnd?: TreeDnd
  onAddChild?: (parentPath: string) => void
  onImportSubpage?: (node: PageTreeNode) => void
  onToggleArchive?: (node: PageTreeNode) => void
  onDeletePage?: (node: PageTreeNode) => void
  onNavigate?: () => void
}

function TreeItem({
  slug,
  node,
  active,
  expanded,
  depth,
  canWrite,
  dnd,
  onAddChild,
  onImportSubpage,
  onToggleArchive,
  onDeletePage,
  onNavigate,
}: TreeItemProps) {
  const { t } = useTranslation()
  const self = normalizePagePath(node.path)
  const hasChildren = node.children.length > 0
  const [open, setOpen] = useState(false)

  // Ancestors of the active page are open no matter what the local toggle says.
  const isOpen = hasChildren && (open || expanded.has(self))
  const indicator = dnd?.indicator?.id === node.id ? dnd.indicator.position : null
  const showMenu = canWrite && onToggleArchive !== undefined && onDeletePage !== undefined

  return (
    <li>
      <div
        className={[
          'group relative flex items-center gap-1 rounded',
          indicator === 'inside' ? 'bg-accent-soft' : '',
          dnd?.draggingId === node.id ? 'opacity-50' : '',
        ].join(' ')}
        style={{ paddingLeft: `${depth * 0.75}rem` }}
        onDragOver={
          dnd !== undefined
            ? (event) => {
                dnd.onDragOver(node, event)
              }
            : undefined
        }
        onDrop={
          dnd !== undefined
            ? (event) => {
                dnd.onDrop(node, event)
              }
            : undefined
        }
      >
        {indicator === 'before' ? (
          <span
            aria-hidden="true"
            className="pointer-events-none absolute inset-x-1 -top-0.5 h-0.5 rounded-full bg-accent"
          />
        ) : null}
        {indicator === 'after' ? (
          <span
            aria-hidden="true"
            className="pointer-events-none absolute inset-x-1 -bottom-0.5 h-0.5 rounded-full bg-accent"
          />
        ) : null}

        {dnd !== undefined ? (
          <button
            type="button"
            draggable
            aria-label={t('reader.dragPage', { title: node.title })}
            title={t('reader.dragPage', { title: node.title })}
            onDragStart={(event) => {
              event.dataTransfer.setData('text/plain', node.id)
              event.dataTransfer.effectAllowed = 'move'
              dnd.onDragStart(node.id)
            }}
            onDragEnd={dnd.onDragEnd}
            className="grid size-5 shrink-0 cursor-grab place-items-center rounded text-muted transition-all hover:text-ink focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent active:cursor-grabbing sm:opacity-0 sm:group-hover:opacity-100 sm:group-focus-within:opacity-100"
          >
            <svg viewBox="0 0 10 16" className="size-3" fill="currentColor" aria-hidden="true">
              <circle cx="2.5" cy="2.5" r="1.4" />
              <circle cx="7.5" cy="2.5" r="1.4" />
              <circle cx="2.5" cy="8" r="1.4" />
              <circle cx="7.5" cy="8" r="1.4" />
              <circle cx="2.5" cy="13.5" r="1.4" />
              <circle cx="7.5" cy="13.5" r="1.4" />
            </svg>
          </button>
        ) : null}

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
            node.isArchived ? 'opacity-60' : '',
          ].join(' ')}
        >
          {node.title}
          {node.isArchived ? <ArchivedBadge /> : null}
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

        {showMenu ? (
          <PageNodeMenu
            node={node}
            onToggleArchive={onToggleArchive}
            onDelete={onDeletePage}
            onImportSubpage={onImportSubpage}
          />
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
              canWrite={canWrite}
              dnd={dnd}
              onAddChild={onAddChild}
              onImportSubpage={onImportSubpage}
              onToggleArchive={onToggleArchive}
              onDeletePage={onDeletePage}
              onNavigate={onNavigate}
            />
          ))}
        </ul>
      ) : null}
    </li>
  )
}
