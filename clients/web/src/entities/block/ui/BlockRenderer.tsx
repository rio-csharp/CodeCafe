import type { ReactNode } from 'react'
import type { BlockNode } from '../model/assembleBlockTree'
import type {
  AudioContent,
  BlockDto,
  CalloutContent,
  CodeContent,
  HeadingContent,
  ImageContent,
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
import { ParagraphBlock } from './ParagraphBlock'
import { QuoteBlock } from './QuoteBlock'
import { TableBlock } from './TableBlock'
import { TodoBlock } from './TodoBlock'

export interface BlockRendererProps {
  node: BlockNode
}

/** One block plus whatever is nested under it. */
export function BlockRenderer({ node }: BlockRendererProps) {
  const own = renderBlock(node.block)

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
      {renderChildren(node.children)}
    </>
  )
}

export interface BlockListProps {
  nodes: readonly BlockNode[]
}

/** The assembled forest of a page. */
export function BlockList({ nodes }: BlockListProps) {
  return (
    <div className="flex flex-col gap-4">
      {nodes.map((node) => (
        <BlockRenderer key={node.block.id} node={node} />
      ))}
    </div>
  )
}

function renderChildren(children: readonly BlockNode[]): ReactNode {
  if (children.length === 0) {
    return null
  }

  return (
    <div className="mt-4 ml-4 flex flex-col gap-4 border-l border-line pl-4">
      {children.map((child) => (
        <BlockRenderer key={child.block.id} node={child} />
      ))}
    </div>
  )
}

/**
 * Payloads are validated on the write side, so the reader trusts the shape and
 * casts instead of re-checking; a malformed payload renders empty rather than
 * throwing, which keeps one bad block from taking down the page.
 */
function renderBlock(block: BlockDto): ReactNode {
  switch (block.type) {
    case 'paragraph':
      return <ParagraphBlock content={block.content as ParagraphContent} />
    case 'heading':
      return <HeadingBlock content={block.content as HeadingContent} />
    case 'quote':
      return <QuoteBlock content={block.content as QuoteContent} />
    case 'callout':
      return <CalloutBlock content={block.content as CalloutContent} />
    case 'todo':
      return <TodoBlock content={block.content as TodoContent} />
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
