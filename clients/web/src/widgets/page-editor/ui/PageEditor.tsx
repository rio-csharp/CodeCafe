import { useMemo, useState } from 'react'
import type { ReactNode } from 'react'
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
  transferChildren,
} from '../lib/moving'
import { joinSpans, spansPlainText, splitSpansAt } from '../lib/spans'
import { ParagraphEditor } from './blocks/ParagraphEditor'
import { HeadingEditor } from './blocks/HeadingEditor'
import { QuoteEditor } from './blocks/QuoteEditor'
import { TodoEditor } from './blocks/TodoEditor'
import { CalloutEditor } from './blocks/CalloutEditor'
import { AudioEditor } from './blocks/AudioEditor'
import { CodeEditor } from './blocks/CodeEditor'
import { ImageEditor } from './blocks/ImageEditor'
import { TableEditor } from './blocks/TableEditor'
import { DividerBlock } from '@/entities/block'
import type { SlashTarget } from '../lib/blockTypes'
import { contentForTarget } from '../lib/blockTypes'

export interface PageEditorProps {
  page: PageDetails
  saving: boolean
  /** Set when the last save attempt failed; shown inline in the editor bar. */
  error?: string | null
  onSave: (title: string, ops: BlockOpWire[]) => void
  onCancel: () => void
}

const TEXT_TYPES = new Set(['paragraph', 'heading', 'quote', 'callout', 'todo'])

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
  const visibleBlocks = useMemo(() => {
    const ordered: EditorBlock[] = []
    const walk = (parentId: string | null): void => {
      for (const block of groups.get(parentId) ?? []) {
        ordered.push(block)
        walk(block.id)
      }
    }
    walk(null)
    return ordered
  }, [groups])

  const rootBlocks = groups.get(null) ?? []
  const textBlocks = visibleBlocks.filter((block) => TEXT_TYPES.has(block.type))

  const splitBlock = (id: string, offset: number) => {
    const source = draft.find((entry) => entry.id === id)
    if (source === undefined || !TEXT_TYPES.has(source.type)) {
      return
    }
    const [left, right] = splitSpansAt(spansOf(source), offset)
    // Enter inside a heading/quote/callout exits to a paragraph; a to-do
    // continues as a to-do, Notion-style. The new block stays at the same depth.
    const freshType = source.type === 'todo' ? 'todo' : 'paragraph'
    const fresh: EditorBlock = {
      id: mintTempId(),
      isNew: true,
      parentBlockId: source.parentBlockId,
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

  const trimmed = title.trim()
  const canSave = trimmed.length > 0 && !saving
  const save = () => {
    onSave(trimmed, diffToOps(page.blocks, draft))
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
    const childrenBlock =
      children.length > 0 ? (
        <div className="ml-3 border-l border-line pl-3">{children.map(renderEditorBlock)}</div>
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
          return <CalloutEditor {...shared} />
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

    return (
      <div key={block.id}>
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
            {childrenBlock}
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
      onKeyDown={(event) => {
        if (event.key === 'Escape') {
          onCancel()
        } else if (event.key === 'Enter' && (event.metaKey || event.ctrlKey) && canSave) {
          save()
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
