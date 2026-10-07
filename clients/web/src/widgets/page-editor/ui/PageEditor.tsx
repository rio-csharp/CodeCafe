import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { assembleBlockTree, BlockList } from '@/entities/block'
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
import { joinSpans, spansPlainText, splitSpansAt } from '../lib/spans'
import { ParagraphEditor } from './blocks/ParagraphEditor'
import { HeadingEditor } from './blocks/HeadingEditor'

export interface PageEditorProps {
  page: PageDetails
  saving: boolean
  /** Set when the last save attempt failed; shown inline in the editor bar. */
  error?: string | null
  onSave: (title: string, ops: BlockOpWire[]) => void
  onCancel: () => void
}

const TEXT_TYPES = new Set(['paragraph', 'heading'])

function spansOf(block: EditorBlock): SpanDto[] {
  return (block.content as { spans: SpanDto[] }).spans
}

/**
 * The editing shell. Text blocks (paragraph/heading) are editable inline;
 * every other type and every nested child still renders read-only through the
 * reader's own components. Escape cancels, Ctrl/Cmd+Enter saves.
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

  // Children render read-only from the untouched tree; the draft drives roots.
  const sourceTree = useMemo(() => assembleBlockTree(page.blocks), [page.blocks])
  const childrenOf = (id: string): BlockNode[] =>
    sourceTree.find((node) => node.block.id === id)?.children ?? []

  const rootBlocks = draft.filter((block) => block.parentBlockId === null)
  const editableRoots = rootBlocks.filter((block) => TEXT_TYPES.has(block.type))

  const splitBlock = (id: string, offset: number) => {
    const source = draft.find((entry) => entry.id === id)
    if (source === undefined || !TEXT_TYPES.has(source.type)) {
      return
    }
    const [left, right] = splitSpansAt(spansOf(source), offset)
    // Enter inside a heading continues as a paragraph, Notion-style.
    const fresh: EditorBlock = {
      id: mintTempId(),
      isNew: true,
      parentBlockId: source.parentBlockId,
      type: source.type === 'heading' ? 'paragraph' : source.type,
      content: { spans: right },
      sortKey: '',
      version: 0,
    }
    setDraft((current) => {
      const withLeft = replaceBlockContent(
        current,
        id,
        source.type === 'heading'
          ? { ...(source.content as { level: number }), spans: left }
          : { spans: left },
      )
      return insertBlockAfter(withLeft, id, fresh)
    })
    setFocusRequest({ id: fresh.id, offset: 0 })
  }

  const mergeBackward = (id: string) => {
    const index = editableRoots.findIndex((entry) => entry.id === id)
    const current = editableRoots[index]
    const previous = editableRoots[index - 1]
    if (current === undefined) {
      return
    }
    const text = spansPlainText(spansOf(current))

    // An empty block under Backspace just goes away, whatever came before it.
    if (text.length === 0) {
      if (previous !== undefined) {
        setFocusRequest({ id: previous.id, offset: spansPlainText(spansOf(previous)).length })
      }
      setDraft((draftNow) => removeBlock(draftNow, id))
      return
    }
    if (previous === undefined || !TEXT_TYPES.has(previous.type)) {
      return
    }
    const junction = spansPlainText(spansOf(previous)).length
    const merged = joinSpans(spansOf(previous), spansOf(current))
    setDraft((draftNow) => {
      const content =
        previous.type === 'heading'
          ? { ...(previous.content as { level: number }), spans: merged }
          : { spans: merged }
      return removeBlock(replaceBlockContent(draftNow, previous.id, content), id)
    })
    setFocusRequest({ id: previous.id, offset: junction })
  }

  const focusNeighbor = (id: string, direction: -1 | 1) => {
    const index = editableRoots.findIndex((entry) => entry.id === id)
    const target = editableRoots[index + direction]
    if (target === undefined) {
      return
    }
    setFocusRequest({
      id: target.id,
      offset: direction === -1 ? spansPlainText(spansOf(target)).length : 0,
    })
  }

  const trimmed = title.trim()
  const canSave = trimmed.length > 0 && !saving
  const save = () => {
    onSave(trimmed, diffToOps(page.blocks, draft))
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
        {rootBlocks.map((block) => {
          const engine = {
            focusOffset: focusRequest?.id === block.id ? focusRequest.offset : null,
            onFocusPrevious: () => {
              focusNeighbor(block.id, -1)
            },
            onFocusNext: () => {
              focusNeighbor(block.id, 1)
            },
            onFocusHandled: () => {
              setFocusRequest(null)
            },
          }

          return (
            <div key={block.id}>
              {block.type === 'paragraph' || block.type === 'heading' ? (
                <>
                  {block.type === 'paragraph' ? (
                    <ParagraphEditor
                      {...engine}
                      block={block}
                      onChange={(content) => {
                        setDraft((current) => replaceBlockContent(current, block.id, content))
                      }}
                      onSplit={(offset) => {
                        splitBlock(block.id, offset)
                      }}
                      onMergeBackward={() => {
                        mergeBackward(block.id)
                      }}
                    />
                  ) : (
                    <HeadingEditor
                      {...engine}
                      block={block}
                      onChange={(content) => {
                        setDraft((current) => replaceBlockContent(current, block.id, content))
                      }}
                      onSplit={(offset) => {
                        splitBlock(block.id, offset)
                      }}
                      onMergeBackward={() => {
                        mergeBackward(block.id)
                      }}
                    />
                  )}
                  <BlockList nodes={childrenOf(block.id)} />
                </>
              ) : (
                // Uneditable types render read-only, children included.
                <BlockList nodes={sourceTree.filter((node) => node.block.id === block.id)} />
              )}
            </div>
          )
        })}
      </div>
    </div>
  )
}
