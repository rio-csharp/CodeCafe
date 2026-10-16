import type { ReactNode } from 'react'
import type { BlockNode } from '../model/assembleBlockTree'
import { blockAnchorId } from '../model/outline'
import type {
  AudioContent,
  BlockDto,
  CalloutContent,
  CodeContent,
  HeadingContent,
  ImageContent,
  ListItemContent,
  ParagraphContent,
  QuoteContent,
  TableContent,
  TodoContent,
} from '../model/types'
import { AudioBlock } from './AudioBlock'
import { CalloutBlock } from './CalloutBlock'
import { CodeBlock } from './CodeBlock'
import { DividerBlock } from './DividerBlock'
import { HeadingBlock } from './HeadingBlock'
import { ImageBlock } from './ImageBlock'
import { ListItemBlock } from './ListItemBlock'
import { ParagraphBlock } from './ParagraphBlock'
import { QuoteBlock } from './QuoteBlock'
import { TableBlock } from './TableBlock'
import { TodoBlock } from './TodoBlock'

export interface BlockRendererProps {
  node: BlockNode
  /** 1-based number within the run of consecutive ordered-list siblings. */
  listNumber?: number
}

/** One block plus whatever is nested under it. */
export function BlockRenderer({ node, listNumber }: BlockRendererProps) {
  // A callout's children render inside its box; every other block's children
  // render below with a rail.
  const own =
    node.block.type === 'callout'
      ? renderBlock(node.block, listNumber, renderChildren(node.children))
      : renderBlock(node.block, listNumber)

  // An unknown type is future content: say nothing about it, but keep its
  // children so a new wrapper type cannot swallow a whole subtree.
  if (own === null) {
    if (import.meta.env.DEV) {
      console.warn(`[blocks] unknown block type "${node.block.type}" skipped.`)
    }
    return <>{renderChildren(node.children)}</>
  }

  return (
    <>
      {own}
      {node.block.type === 'callout' ? null : renderChildren(node.children)}
    </>
  )
}

export interface BlockListProps {
  nodes: readonly BlockNode[]
}

/** The assembled forest of a page. */
export function BlockList({ nodes }: BlockListProps) {
  const numbers = listItemNumbers(nodes)
  return (
    <div className="flex flex-col gap-4">
      {nodes.map((node) => (
        <BlockRenderer key={node.block.id} node={node} listNumber={numbers.get(node.block.id)} />
      ))}
    </div>
  )
}

function renderChildren(children: readonly BlockNode[]): ReactNode {
  if (children.length === 0) {
    return null
  }

  const numbers = listItemNumbers(children)
  return (
    <div className="mt-4 ml-4 flex flex-col gap-4 border-l border-line pl-4">
      {children.map((child) => (
        <BlockRenderer key={child.block.id} node={child} listNumber={numbers.get(child.block.id)} />
      ))}
    </div>
  )
}

/** Numbers restart whenever anything but an ordered-list item interrupts the run. */
function listItemNumbers(nodes: readonly BlockNode[]): ReadonlyMap<string, number> {
  const numbers = new Map<string, number>()
  let run = 0
  for (const node of nodes) {
    run = node.block.type === 'numbered-list' ? run + 1 : 0
    if (node.block.type === 'numbered-list') {
      numbers.set(node.block.id, run)
    }
  }
  return numbers
}

/**
 * Payloads are validated on the write side, so the reader trusts the shape and
 * casts instead of re-checking; a malformed payload renders empty rather than
 * throwing, which keeps one bad block from taking down the page.
 */
function renderBlock(block: BlockDto, listNumber?: number, children?: ReactNode): ReactNode {
  switch (block.type) {
    case 'paragraph':
      return <ParagraphBlock content={block.content as ParagraphContent} />
    case 'heading':
      return (
        <HeadingBlock id={blockAnchorId(block.id)} content={block.content as HeadingContent} />
      )
    case 'quote':
      return <QuoteBlock content={block.content as QuoteContent} />
    case 'callout':
      return <CalloutBlock content={block.content as CalloutContent}>{children}</CalloutBlock>
    case 'todo':
      return <TodoBlock content={block.content as TodoContent} />
    case 'bulleted-list':
      return <ListItemBlock content={block.content as ListItemContent} ordered={false} />
    case 'numbered-list':
      return <ListItemBlock content={block.content as ListItemContent} ordered number={listNumber} />
    case 'code':
      return <CodeBlock content={block.content as CodeContent} />
    case 'divider':
      return <DividerBlock />
    case 'table':
      return <TableBlock content={block.content as TableContent} />
    case 'image':
      return <ImageBlock content={block.content as ImageContent} />
    case 'audio':
      return <AudioBlock content={block.content as AudioContent} />
    default:
      return null
  }
}
