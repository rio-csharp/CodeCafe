import { apiFetch } from '@/shared/api'

/** Wire shape of one op; kind travels as the enum name (`JsonStringEnumConverter`). */
export interface BlockOpWire {
  kind: 'Insert' | 'Update' | 'Delete' | 'Move'
  blockId?: string
  tempId?: string
  type?: string
  /** Live block id or a temp id from earlier in the batch; omitted = group head. */
  after?: string
  /** Pins the target group explicitly: `after` is interpreted among this block's
   *  children, and an omitted `after` means FIRST child — the only way into a
   *  childless parent. Omitted = the after-block's own group (or top level). */
  parent?: string
  content?: unknown
  baseVersion?: number
}

export interface BlockOpResult {
  tempId: string | null
  blockId: string | null
  version: number | null
  content: unknown
}

/** All-or-nothing batch; ops run strictly in order, each seeing the earlier ones. */
export function applyBlockOps(
  pageId: string,
  ops: readonly BlockOpWire[],
  dryRun = false,
): Promise<BlockOpResult[]> {
  return apiFetch<BlockOpResult[]>(`/api/pages/${encodeURIComponent(pageId)}/blocks/batch`, {
    method: 'POST',
    body: JSON.stringify({ ops, dryRun }),
  })
}
