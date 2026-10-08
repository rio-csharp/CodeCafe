import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import type { DragEvent, ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { assembleBlockTree, BlockRenderer } from '@/entities/block'
import type { BlockNode, BlockOpWire, SpanDto } from '@/entities/block'
import type { PageDetails } from '@/entities/page'
import {
  emptyParagraphBlock,
  insertBlockAfter,
  mintTempId,
  removeBlock,
  replaceBlockContent,
  toEditorDraft,
} from '../lib/draft'
import type { EditorBlock } from '../lib/draft'
import { diffToOps } from '../lib/ops'
import {
  hoistChildren,
  indentBlock,
  moveBlockInGroup,
  outdentBlock,
  preorderBlocks,
  relocateBlock,
  removeBlocks,
  transferChildren,
} from '../lib/moving'
import { joinSpans, spansPlainText, splitSpansAt } from '../lib/spans'
import { ParagraphEditor } from './blocks/ParagraphEditor'
import { HeadingEditor } from './blocks/HeadingEditor'
import { QuoteEditor } from './blocks/QuoteEditor'
import { TodoEditor } from './blocks/TodoEditor'
import { CalloutEditor } from './blocks/CalloutEditor'
import { ListItemEditor } from './blocks/ListItemEditor'
import { AudioEditor } from './blocks/AudioEditor'
import { CodeEditor } from './blocks/CodeEditor'
import { ImageEditor } from './blocks/ImageEditor'
import { TableEditor } from './blocks/TableEditor'
import { DividerBlock } from '@/entities/block'
import type { SlashTarget } from '../lib/blockTypes'
import { contentForTarget } from '../lib/blockTypes'
import { BlockHandle } from './BlockHandle'

export interface PageEditorProps {
  page: PageDetails
  saving: boolean
  /** Set when the last save attempt failed; shown inline in the editor bar. */
  error?: string | null
  onSave: (title: string, ops: BlockOpWire[]) => void
  onCancel: () => void
}

const TEXT_TYPES = new Set([
  'paragraph',
  'heading',
  'quote',
  'callout',
  'todo',
  'bulleted-list',
  'numbered-list',
])

/** Enter continues the block instead of exiting to a paragraph. */
const ENTER_CONTINUES = new Set(['todo', 'bulleted-list', 'numbered-list'])

function spansOf(block: EditorBlock): SpanDto[] {
  return (block.content as { spans: SpanDto[] }).spans
}

/**
 * The editing shell. Text blocks (paragraph/heading/quote/todo/callout) are
 * editable inline at any depth — the draft is flat, so the tree is derived
 * from parentBlockId and rendered recursively. Other types render read-only
 * from the untouched source tree. Escape cancels, Ctrl/Cmd+Enter saves.
 */
export function PageEditor({ page, saving, error = null, onSave, onCancel }: PageEditorProps) {
  const { t } = useTranslation()
  const [title, setTitle] = useState(page.title)
  // An empty page still needs somewhere to type: start from one fresh paragraph.
  const [draft, setDraft] = useState<EditorBlock[]>(() => {
    const initial = toEditorDraft(page.blocks)
    return initial.some((block) => block.parentBlockId === null) ? initial : [emptyParagraphBlock()]
  })
  const [focusRequest, setFocusRequest] = useState<{ id: string; offset: number } | null>(null)

  // Read-only fallback rendering needs the untouched tree, children included.
  const sourceTree = useMemo(() => assembleBlockTree(page.blocks), [page.blocks])
  const sourceNodes = useMemo(() => {
    const map = new Map<string, BlockNode>()
    const walk = (nodes: BlockNode[]): void => {
      for (const node of nodes) {
        map.set(node.block.id, node)
        walk(node.children)
      }
    }
    walk(sourceTree)
    return map
  }, [sourceTree])

  // The draft is flat; group children under their parent, keeping array order.
  const groups = useMemo(() => {
    const map = new Map<string | null, EditorBlock[]>()
    for (const block of draft) {
      const list = map.get(block.parentBlockId) ?? []
      list.push(block)
      map.set(block.parentBlockId, list)
    }
    return map
  }, [draft])

  // Display order = preorder flatten; navigation runs over this, not the
  // draft array, because a split lands next to its source in the array even
  // when the source's children render in between.
  const visibleBlocks = useMemo(() => preorderBlocks(draft), [draft])

  const rootBlocks = groups.get(null) ?? []
  const textBlocks = visibleBlocks.filter((block) => TEXT_TYPES.has(block.type))

  // An ordered item's number is its position in the run of consecutive ordered
  // siblings — derived per group, never stored.
  const listNumbers = useMemo(() => {
    const numbers = new Map<string, number>()
    for (const list of groups.values()) {
      let run = 0
      for (const block of list) {
        run = block.type === 'numbered-list' ? run + 1 : 0
        if (block.type === 'numbered-list') {
          numbers.set(block.id, run)
        }
      }
    }
    return numbers
  }, [groups])

  // Block-level selection: Escape inside a text block selects it; arrows move,
  // Shift+arrows extend, Backspace/Delete removes the whole subtree set.
  const [blockSelection, setBlockSelection] = useState<{
    anchorId: string
    focusId: string
  } | null>(null)

  // Drag-and-drop: the handle's dragstart stashes the id here; dragover on a
  // block wrapper turns it into a before/after indicator.
  const dragIdRef = useRef<string | null>(null)
  const [dropIndicator, setDropIndicator] = useState<{
    id: string
    position: 'before' | 'after'
  } | null>(null)

  /** Selected ids plus every descendant — deletes cascade, so the tint does too. */
  const selectedIds = useMemo(() => {
    if (blockSelection === null) {
      return null
    }
    const anchorIndex = visibleBlocks.findIndex((entry) => entry.id === blockSelection.anchorId)
    const focusIndex = visibleBlocks.findIndex((entry) => entry.id === blockSelection.focusId)
    if (anchorIndex < 0 || focusIndex < 0) {
      return null
    }
    const from = Math.min(anchorIndex, focusIndex)
    const to = Math.max(anchorIndex, focusIndex)
    const ids = new Set(visibleBlocks.slice(from, to + 1).map((entry) => entry.id))
    let settled = false
    while (!settled) {
      settled = true
      for (const block of draft) {
        if (block.parentBlockId !== null && ids.has(block.parentBlockId) && !ids.has(block.id)) {
          ids.add(block.id)
          settled = false
        }
      }
    }
    return ids
  }, [blockSelection, visibleBlocks, draft])

  const trimmed = title.trim()
  const canSave = trimmed.length > 0 && !saving
  const save = useCallback(() => {
    onSave(trimmed, diffToOps(page.blocks, draft))
  }, [trimmed, page.blocks, draft, onSave])

  useEffect(() => {
    const handler = (event: KeyboardEvent) => {
      // Save works from anywhere, including block-selection mode where focus
      // sits on <body> and never reaches the editor's own handlers.
      if (event.key === 'Enter' && (event.metaKey || event.ctrlKey) && canSave) {
        event.preventDefault()
        save()
        return
      }

      if (event.key === 'Escape') {
        // Selection mode eats the first Escape; the page-level cancel is next.
        if (blockSelection !== null) {
          event.preventDefault()
          setBlockSelection(null)
        } else {
          onCancel()
        }
        return
      }

      if (blockSelection === null || selectedIds === null) {
        return
      }
      const selection = blockSelection
      const anchorIndex = visibleBlocks.findIndex((entry) => entry.id === selection.anchorId)
      const focusIndex = visibleBlocks.findIndex((entry) => entry.id === selection.focusId)
      if (anchorIndex < 0 || focusIndex < 0) {
        setBlockSelection(null)
        return
      }
      const from = Math.min(anchorIndex, focusIndex)
      const to = Math.max(anchorIndex, focusIndex)

      if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
        event.preventDefault()
        const delta = event.key === 'ArrowDown' ? 1 : -1
        if (event.shiftKey) {
          const nextFocus = visibleBlocks[focusIndex + delta]
          if (nextFocus !== undefined) {
            setBlockSelection({ anchorId: selection.anchorId, focusId: nextFocus.id })
          }
        } else {
          const edge = delta === 1 ? to : from
          const next = visibleBlocks[edge + delta]
          if (next !== undefined) {
            setBlockSelection({ anchorId: next.id, focusId: next.id })
          }
        }
        return
      }

      if (event.key === 'Enter') {
        event.preventDefault()
        const target =
          selection.anchorId === selection.focusId
            ? draft.find((entry) => entry.id === selection.anchorId)
            : undefined
        if (target !== undefined && TEXT_TYPES.has(target.type)) {
          setBlockSelection(null)
          setFocusRequest({ id: target.id, offset: spansPlainText(spansOf(target)).length })
        }
        return
      }

      if (event.key === 'Backspace' || event.key === 'Delete') {
        event.preventDefault()
        const after = visibleBlocks
          .slice(to + 1)
          .find((entry) => TEXT_TYPES.has(entry.type) && !selectedIds.has(entry.id))
        const before = visibleBlocks
          .slice(0, from)
          .reverse()
          .find((entry) => TEXT_TYPES.has(entry.type) && !selectedIds.has(entry.id))
        const focusTarget = after ?? before
        const remaining = removeBlocks(draft, selectedIds)
        setBlockSelection(null)
        if (focusTarget !== undefined) {
          setDraft(remaining)
          setFocusRequest({ id: focusTarget.id, offset: 0 })
        } else {
          // Nothing editable survives: leave one fresh paragraph to type into.
          const freshBlock = emptyParagraphBlock()
          setDraft([...remaining, freshBlock])
          setFocusRequest({ id: freshBlock.id, offset: 0 })
        }
      }
    }
    document.addEventListener('keydown', handler)
    return () => {
      document.removeEventListener('keydown', handler)
    }
  }, [blockSelection, selectedIds, visibleBlocks, draft, canSave, onCancel, save])

  const splitBlock = (id: string, offset: number) => {
    const source = draft.find((entry) => entry.id === id)
    if (source === undefined || !TEXT_TYPES.has(source.type)) {
      return
    }
    const [left, right] = splitSpansAt(spansOf(source), offset)
    // Enter inside a heading/quote exits to a paragraph; a to-do or list item
    // continues as the same kind; a callout is a container, so Enter becomes
    // its first child. The new block stays at the same depth otherwise.
    const freshType = ENTER_CONTINUES.has(source.type) ? source.type : 'paragraph'
    const fresh: EditorBlock = {
      id: mintTempId(),
      isNew: true,
      parentBlockId: source.type === 'callout' ? source.id : source.parentBlockId,
      type: freshType,
      content: freshType === 'todo' ? { checked: false, spans: right } : { spans: right },
      sortKey: '',
      version: 0,
    }
    setDraft((current) => {
      const withLeft = replaceBlockContent(current, id, {
        ...(source.content as object),
        spans: left,
      })
      return insertBlockAfter(withLeft, id, fresh)
    })
    setFocusRequest({ id: fresh.id, offset: 0 })
  }

  /**
   * Slash-menu conversion. The backend never changes a block's type on Update,
   * so conversion is delete-and-insert: the old block leaves the draft (a
   * Delete op when it was persisted) and a fresh temp block takes its place —
   * same depth, and the old block's children follow onto the fresh one.
   */
  const transformBlock = (id: string, target: SlashTarget, spans: SpanDto[]) => {
    const source = draft.find((entry) => entry.id === id)
    if (source === undefined) {
      return
    }
    const fresh: EditorBlock = {
      id: mintTempId(),
      isNew: true,
      parentBlockId: source.parentBlockId,
      type: target.type,
      content: contentForTarget(target, spans),
      sortKey: '',
      version: 0,
    }
    setDraft((current) => {
      const index = current.findIndex((entry) => entry.id === id)
      if (index < 0) {
        return current
      }
      const next = [...current]
      next.splice(index, 1, fresh)
      return next.map((entry) =>
        entry.parentBlockId === id ? { ...entry, parentBlockId: fresh.id } : entry,
      )
    })
    if (target.type !== 'divider') {
      setFocusRequest({ id: fresh.id, offset: 0 })
    }
  }

  const mergeBackward = (id: string) => {
    const index = textBlocks.findIndex((entry) => entry.id === id)
    const current = textBlocks[index]
    if (current === undefined) {
      return
    }
    const text = spansPlainText(spansOf(current))
    const hasChildren = (groups.get(id)?.length ?? 0) > 0

    // An empty block under Backspace just goes away; its children are hoisted
    // into its place first (the diff turns that into Move ops + a Delete).
    if (text.length === 0) {
      const previousText = textBlocks[index - 1]
      if (previousText !== undefined) {
        setFocusRequest({ id: previousText.id, offset: spansPlainText(spansOf(previousText)).length })
      }
      setDraft((draftNow) =>
        removeBlock(hasChildren ? hoistChildren(draftNow, id) : draftNow, id),
      )
      return
    }
    // Merging crosses into the immediate VISUAL neighbour only: a divider (or
    // any uneditable block) in between means nothing happens. The merged-away
    // block's children follow its text onto the surviving block.
    const visibleIndex = visibleBlocks.findIndex((entry) => entry.id === id)
    const previous = visibleBlocks[visibleIndex - 1]
    if (previous === undefined || !TEXT_TYPES.has(previous.type)) {
      return
    }
    const junction = spansPlainText(spansOf(previous)).length
    const merged = joinSpans(spansOf(previous), spansOf(current))
    setDraft((draftNow) => {
      const withChildren = hasChildren ? transferChildren(draftNow, id, previous.id) : draftNow
      const content = { ...(previous.content as object), spans: merged }
      return removeBlock(replaceBlockContent(withChildren, previous.id, content), id)
    })
    setFocusRequest({ id: previous.id, offset: junction })
  }

  /** Applies a draft-moving gesture and keeps the caret where it was. */
  const applyMove = (
    id: string,
    offset: number,
    move: (draft: readonly EditorBlock[], id: string) => EditorBlock[] | null,
  ) => {
    const next = move(draft, id)
    if (next === null) {
      return
    }
    setDraft(next)
    setFocusRequest({ id, offset })
  }

  const focusNeighbor = (id: string, direction: -1 | 1) => {
    const index = textBlocks.findIndex((entry) => entry.id === id)
    const target = textBlocks[index + direction]
    if (target === undefined) {
      return
    }
    setFocusRequest({
      id: target.id,
      offset: direction === -1 ? spansPlainText(spansOf(target)).length : 0,
    })
  }

  /**
   * The id of the paragraph to land in below the content: the trailing block
   * when it already is an empty text block, otherwise a freshly appended one.
   */
  const appendOrReuseTrailingParagraph = (): string => {
    const last = rootBlocks[rootBlocks.length - 1]
    if (
      last !== undefined &&
      TEXT_TYPES.has(last.type) &&
      spansPlainText(spansOf(last)).length === 0
    ) {
      return last.id
    }
    const fresh = emptyParagraphBlock()
    setDraft((current) => [...current, fresh])
    return fresh.id
  }

  /** Clicking the empty canvas below the blocks appends (or focuses) a paragraph. */
  const handleCanvasClick = () => {
    setFocusRequest({ id: appendOrReuseTrailingParagraph(), offset: 0 })
  }

  /** ArrowDown with no editable block below behaves like clicking the empty canvas. */
  const focusNextOrAppend = (id: string) => {
    const index = textBlocks.findIndex((entry) => entry.id === id)
    if (textBlocks[index + 1] !== undefined) {
      focusNeighbor(id, 1)
      return
    }
    setFocusRequest({ id: appendOrReuseTrailingParagraph(), offset: 0 })
  }

  /** Deletes one block (subtree included); leaves a paragraph if nothing editable survives. */
  const deleteBlock = (id: string) => {
    const remaining = removeBlocks(draft, new Set([id]))
    setBlockSelection(null)
    if (remaining.some((entry) => TEXT_TYPES.has(entry.type))) {
      setDraft(remaining)
    } else {
      setDraft([...remaining, emptyParagraphBlock()])
    }
  }

  const renderEditorBlock = (block: EditorBlock): ReactNode => {
    const engine = {
      focusOffset: focusRequest?.id === block.id ? focusRequest.offset : null,
      onTransform: (target: SlashTarget, spans: SpanDto[]) => {
        transformBlock(block.id, target, spans)
      },
      onFocusPrevious: () => {
        focusNeighbor(block.id, -1)
      },
      onFocusNext: () => {
        focusNextOrAppend(block.id)
      },
      onFocusHandled: () => {
        setFocusRequest(null)
      },
      onIndent: (offset: number) => {
        applyMove(block.id, offset, indentBlock)
      },
      onOutdent: (offset: number) => {
        applyMove(block.id, offset, outdentBlock)
      },
      onMoveBlock: (direction: -1 | 1, offset: number) => {
        applyMove(block.id, offset, (draftNow, blockId) =>
          moveBlockInGroup(draftNow, blockId, direction),
        )
      },
    }

    const shared = {
      ...engine,
      block,
      onChange: (content: Record<string, unknown>) => {
        setDraft((current) => replaceBlockContent(current, block.id, content))
      },
      onSplit: (offset: number) => {
        splitBlock(block.id, offset)
      },
      onMergeBackward: () => {
        mergeBackward(block.id)
      },
    }

    const children = groups.get(block.id) ?? []
    const childrenList =
      children.length > 0 ? (
        <div className="flex flex-col gap-1">{children.map(renderEditorBlock)}</div>
      ) : null
    // Callout children sit inside the tinted box; every other parent indents
    // its children with a rail on the left.
    const childrenBlock =
      childrenList !== null ? (
        <div className="ml-3 border-l border-line pl-3">{childrenList}</div>
      ) : null

    const editor = (() => {
      switch (block.type) {
        case 'paragraph':
          return <ParagraphEditor {...shared} />
        case 'heading':
          return <HeadingEditor {...shared} />
        case 'quote':
          return <QuoteEditor {...shared} />
        case 'todo':
          return <TodoEditor {...shared} />
        case 'callout':
          // A callout's children live inside the tinted box.
          return <CalloutEditor {...shared}>{childrenList}</CalloutEditor>
        case 'bulleted-list':
        case 'numbered-list':
          return <ListItemEditor {...shared} listNumber={listNumbers.get(block.id)} />
        case 'code':
          return <CodeEditor block={block} onChange={shared.onChange} />
        case 'table':
          return <TableEditor block={block} onChange={shared.onChange} />
        case 'image':
          return <ImageEditor block={block} onChange={shared.onChange} />
        case 'audio':
          return <AudioEditor block={block} onChange={shared.onChange} />
        default:
          return null
      }
    })()

    const selected = selectedIds?.has(block.id) ?? false
    const canTurnInto = TEXT_TYPES.has(block.type)

    const dropPosition = (event: DragEvent<HTMLDivElement>): 'before' | 'after' => {
      const rect = event.currentTarget.getBoundingClientRect()
      return event.clientY < rect.top + rect.height / 2 ? 'before' : 'after'
    }

    return (
      <div
        key={block.id}
        data-selected={selected || undefined}
        onDragOver={(event) => {
          if (dragIdRef.current === null) {
            return
          }
          // The deepest block under the cursor wins; ancestors must not steal it.
          event.stopPropagation()
          event.preventDefault()
          const position = dropPosition(event)
          setDropIndicator((current) =>
            current?.id === block.id && current.position === position
              ? current
              : { id: block.id, position },
          )
        }}
        onDrop={(event) => {
          event.stopPropagation()
          event.preventDefault()
          const dragId = dragIdRef.current
          dragIdRef.current = null
          setDropIndicator(null)
          if (dragId === null) {
            return
          }
          const next = relocateBlock(draft, dragId, block.id, dropPosition(event))
          if (next !== null) {
            setDraft(next)
          }
        }}
        className={`group relative rounded ${selected ? 'bg-accent-soft/70' : ''}`}
      >
        {dropIndicator?.id === block.id ? (
          <div
            aria-hidden
            data-testid="drop-indicator"
            className={`absolute right-0 left-0 z-10 h-0.5 rounded bg-accent ${
              dropIndicator.position === 'before' ? '-top-0.5' : '-bottom-0.5'
            }`}
          />
        ) : null}
        <BlockHandle
          canTurnInto={canTurnInto}
          onTurnInto={(target) => {
            transformBlock(block.id, target, canTurnInto ? spansOf(block) : [])
          }}
          onDelete={() => {
            deleteBlock(block.id)
          }}
          onSelect={() => {
            setBlockSelection({ anchorId: block.id, focusId: block.id })
          }}
          onDragStart={(event) => {
            dragIdRef.current = block.id
            event.dataTransfer.effectAllowed = 'move'
            event.dataTransfer.setData('text/plain', block.id)
            setBlockSelection({ anchorId: block.id, focusId: block.id })
          }}
          onDragEnd={() => {
            dragIdRef.current = null
            setDropIndicator(null)
          }}
        />
        {block.type === 'divider' ? (
          // Editor blocks butt up against each other (the reader's list has
          // gap-4, the editor does not), so the rule needs its own breathing
          // room to stay visible.
          <div className="py-2">
            <DividerBlock />
          </div>
        ) : editor !== null ? (
          <>
            {editor}
            {block.type === 'callout' ? null : childrenBlock}
          </>
        ) : (
          // Unknown types render read-only from the untouched source tree,
          // children included; a fresh one (not slash-creatable yet) is skipped.
          (() => {
            const node = sourceNodes.get(block.id)
            return node !== undefined ? <BlockRenderer node={node} /> : null
          })()
        )}
      </div>
    )
  }

  return (
    <div
      onMouseDown={() => {
        // Any click is a text-editing intent; block selection does not survive it.
        if (blockSelection !== null) {
          setBlockSelection(null)
        }
      }}
    >
      <div className="sticky top-0 z-10 -mx-4 -mt-6 mb-4 flex items-center justify-end gap-2 bg-card/95 px-4 py-2 backdrop-blur-sm sm:-mx-8 sm:px-8 xl:-mx-12 xl:px-12">
        {error !== null ? (
          <span role="alert" className="mr-auto text-xs text-danger">
            {error}
          </span>
        ) : null}
        <button
          type="button"
          onClick={onCancel}
          className="rounded-lg border border-line px-3 py-1.5 text-xs font-medium text-muted transition-colors hover:bg-muted-soft hover:text-ink focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent"
        >
          {t('editor.cancel')}
        </button>
        <button
          type="button"
          disabled={!canSave}
          onClick={save}
          className="rounded-lg bg-accent px-3 py-1.5 text-xs font-medium text-white transition-colors hover:bg-accent-strong focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent disabled:opacity-50"
        >
          {saving ? t('editor.saving') : t('editor.save')}
        </button>
      </div>

      <input
        value={title}
        autoFocus
        aria-label={t('editor.titlePlaceholder')}
        placeholder={t('editor.titlePlaceholder')}
        onChange={(event) => {
          setTitle(event.target.value)
        }}
        className="w-full rounded-md bg-transparent px-2 py-1 text-lg font-semibold text-ink placeholder:text-muted focus:outline focus:outline-2 focus:outline-accent"
      />

      <div className="mt-4 text-sm leading-relaxed">
        {rootBlocks.map(renderEditorBlock)}
        {/** Notion-style canvas: clicking below the content starts a new paragraph. */}
        <div
          role="button"
          tabIndex={-1}
          aria-label={t('editor.appendBlock')}
          onClick={handleCanvasClick}
          className="min-h-40 cursor-text"
        />
      </div>
    </div>
  )
}
