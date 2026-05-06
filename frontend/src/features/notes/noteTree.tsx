import { toDisplayName } from './noteDisplay'
import type { NoteTreeNode } from './noteTreeBuilder'

export function NoteTree({
  activePath,
  expandedPaths,
  nodes,
  onToggleDirectory,
  onSelect,
}: {
  activePath: string
  expandedPaths: ReadonlySet<string>
  nodes: NoteTreeNode[]
  onToggleDirectory: (path: string, isOpen: boolean) => void
  onSelect: (path: string) => void
}) {
  return (
    <ul className="note-tree">
      {nodes.map((node) => (
        <li key={node.path}>
          {node.type === 'directory' ? (
            <details
              onToggle={(event) =>
                onToggleDirectory(node.path, (event.currentTarget as HTMLDetailsElement).open)
              }
              open={expandedPaths.has(node.path)}
            >
              <summary>{node.name}</summary>
              <NoteTree
                activePath={activePath}
                expandedPaths={expandedPaths}
                nodes={node.children}
                onSelect={onSelect}
                onToggleDirectory={onToggleDirectory}
              />
            </details>
          ) : (
            <button
              aria-current={node.path === activePath ? 'true' : undefined}
              className="note-list-item"
              onClick={() => onSelect(node.path)}
              type="button"
            >
              <span>
                <strong>{toDisplayName(node.note?.title ?? '')}</strong>
              </span>
            </button>
          )}
        </li>
      ))}
    </ul>
  )
}
