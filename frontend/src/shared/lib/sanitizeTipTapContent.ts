const EMPTY_TIPTAP_DOC = { type: 'doc', content: [] } as const

export function createEmptyTipTapDocument(): Record<string, unknown> {
  return {
    type: EMPTY_TIPTAP_DOC.type,
    content: [...EMPTY_TIPTAP_DOC.content],
  }
}

/**
 * Remove JSON nodes that ProseMirror rejects before read-only rendering or
 * editor initialization. Stored TipTap JSON occasionally contains empty text
 * nodes (`{ type: "text", text: "" }`), which throws:
 * RangeError: Empty text nodes are not allowed.
 */
export function sanitizeTipTapContent(content: unknown): Record<string, unknown> {
  if (!isRecord(content)) {
    return createEmptyTipTapDocument()
  }

  const clone = cloneJson(content)
  if (!isRecord(clone)) {
    return createEmptyTipTapDocument()
  }

  const sanitized = sanitizeNode(clone)
  return isRecord(sanitized) ? sanitized : createEmptyTipTapDocument()
}

function sanitizeNode(node: unknown): unknown {
  if (!isRecord(node)) {
    return node
  }

  if (node.type === 'text') {
    if (typeof node.text !== 'string' || node.text.length === 0) {
      return null
    }
  }

  if (Array.isArray(node.content)) {
    node.content = node.content
      .map((child) => sanitizeNode(child))
      .filter((child): child is Record<string, unknown> => isRecord(child))
  }

  return node
}

function cloneJson(value: unknown): unknown {
  try {
    return JSON.parse(JSON.stringify(value)) as unknown
  } catch {
    return null
  }
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}
