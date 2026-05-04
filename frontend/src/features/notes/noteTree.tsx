import { toDisplayName } from './noteDisplay'
import type { NoteTreeNode } from './noteTreeBuilder'

export function NoteTree({
  activePath,
  nodes,
  onSelect,
}: {
  activePath: string
  nodes: NoteTreeNode[]
  onSelect: (path: string) => void
}) {
  return (
    <ul className="note-tree">
      {nodes.map((node) => (
        <li key={node.path}>
          {node.type === 'directory' ? (
            <details>
              <summary>{node.name}</summary>
              <NoteTree activePath={activePath} nodes={node.children} onSelect={onSelect} />
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
