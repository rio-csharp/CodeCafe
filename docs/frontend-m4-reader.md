# M4 Spec — Notebook reader

> Read `docs/frontend-architecture.md` first. Scope: the read-only notebook reader —
> routing, page tree, block rendering. **No editing, no creation, no favorites,
> no access-code prompt, no AI.**

## 1. API facts (verified against the backend)

| Call | Auth | Returns |
|---|---|---|
| `GET /api/notebooks/{idOrSlug}` | anonymous OK | `NotebookDetailsDto` |
| `GET /api/notebooks/{idOrSlug}/tree` | anonymous OK | `{ notebookId, roots: PageTreeNode[] }` |
| `GET /api/notebooks/{idOrSlug}/pages/by-path?path={path}` | anonymous OK | `PageDetailsDto` (blocks embedded) |

```ts
interface NotebookDetailsDto {
  id: string; title: string; description: string | null; slug: string
  visibility: 'Private' | 'Unlisted' | 'Public'
  hasAccessCode: boolean; tags: string[]; pageCount: number
  createdAtUtc: string; updatedAtUtc: string
  isOwner: boolean; canWrite: boolean   // both false for anonymous
  // shares omitted here; not needed in M4
}

interface PageTreeNode {
  id: string; title: string; path: string; sortOrder: number
  isArchived: boolean; isFavorite: boolean; children: PageTreeNode[]
}

interface PageDetailsDto {
  id: string; notebookId: string; title: string; path: string
  isArchived: boolean; isFavorite: boolean
  blocks: BlockDto[]                     // FLAT list — see §3
  createdAtUtc: string; updatedAtUtc: string
}

interface BlockDto {
  id: string; parentBlockId: string | null
  type: string                            // see §4
  content: unknown                        // per-type payload, see §4
  sortKey: string                         // LexoRank — ordinal string compare
  version: number; updatedAtUtc: string
}
```

A 404 on any of these may mean "private", not "absent" — never claim non-existence.

## 2. Routes & navigation

```
/notebooks/:slug          → reader; if the tree has pages, redirect to the first
                            (depth-first) page's path; else the empty state
/notebooks/:slug/*        → the page at that path (splat = the page path, decoded)
```

- `NotebookCard` becomes a `Link` to `/notebooks/{slug}` (full-card, visible focus ring).
- Tree nodes link to `/notebooks/{slug}/{node.path}` — use the `path` strings from the
  API verbatim (they may contain CJK); encode with `encodeURIComponent` per segment.
- The active page is highlighted in the tree; ancestors auto-expand.
- `<title>` = `{page.title} · {notebook.title} · CodeCafe` (and the notebook title
  alone on the notebook root).

## 3. Block tree assembly (`entities/block`)

`PageDetailsDto.blocks` is flat. Build the render tree:

- Group by `parentBlockId` (null = root level).
- Sort siblings by `sortKey` with **ordinal string comparison** (`a < b`, not locale).
- Pure function `assembleBlockTree(blocks): BlockNode[]` — fully unit-tested
  (ordering, nesting, orphan blocks appended at root sorted by sortKey rather than dropped).

## 4. Block rendering (`entities/block/ui`)

Content payloads (from the backend contract, camelCase on the wire):

| type | payload | render |
|---|---|---|
| `paragraph` | `{ spans }` | `<p>` |
| `heading` | `{ level: 1-6, spans }` | `<h1>`–`<h6>` |
| `quote` | `{ spans }` | bordered quote, muted text |
| `callout` | `{ variant, spans }` | soft-tinted box; variant ∈ primary/success/danger/warning/info/muted |
| `todo` | `{ checked, spans }` | read-only checkbox (disabled) + text, strike-through when checked |
| `code` | `{ code, language }` | `<pre><code>`, monospace card; no syntax highlighting in M4 |
| `divider` | `{}` | `<hr>` with line token |
| `table` | `{ alignments, header: spans[] \| null, rows: spans[][] }` | real `<table>` in an `overflow-x-auto` wrapper; alignment left/center/right per column |
| `image` | `{ url, alt, isDecorative }` | rounded `<img>`, lazy-loaded |
| `audio` | `{ url, mimeType }` | native `<audio controls>` |

Unknown `type` → render nothing but log a dev-mode warning (forward compatibility).

**Spans**: `{ text, marks }` where marks compose. Mark kinds:
`bold, italic, underline, strike, code, kbd, sup, sub`,
`link { href }` → `<a href target="_blank" rel="noopener noreferrer">` (href is
server-validated http/https/mailto),
`color { name }` / `highlight { name }` → palette classes (name ∈ the closed
PaletteColor set), `abbr { title }` → `<abbr title>`.

Nested blocks render indented under their parent.

## 5. Design tokens: status colors (new)

Callouts and color/highlight marks need a status palette that does not exist yet.
Add to **both** themes in `index.css`, following the same saturation discipline as M2
(no eye-care yellow, no neon):

`--color-success`, `--color-danger`, `--color-warning`, `--color-info`
plus soft backgrounds `--color-success-soft` etc. for callout bodies and highlight
marks. Text-on-soft must stay ≥ 4.5:1 in both themes. `primary` maps to the existing
`accent`; `muted` maps to `muted`/`line`.

## 6. Layout & states

```
SiteHeader (existing)
NotebookHeader            title, description, tags, page count, updated time
<Container> flex:
  <aside> PageTree        md+ sticky sidebar (~w-64), collapsible sections
  <main>  PageContent     max-w-3xl, blocks
SiteFooter (existing)
```

- Mobile (`<md`): the tree hides behind a "目录/Contents" disclosure toggle in the
  notebook header, not a permanent sidebar.
- **Archived pages** (`isArchived`) are hidden from the reader tree.
- Loading: tree skeleton rows + content skeleton paragraphs.
- Notebook 404 → themed state: "这个笔记本不存在，或者它还没公开" /
  "This notebook doesn't exist, or it isn't public yet." + link home.
- Page 404 (valid notebook, bad path) → themed "这一页找不到了" state **inside** the
  chrome (tree stays visible so the user can navigate away).
- Empty notebook (tree has no non-archived pages) → themed empty state.
- Access code: if `hasAccessCode` and the details call 404s for an anonymous user,
  the generic 404 state covers it — **do not build the access-code prompt** (M-later).

## 7. Slice layout

```
pages/notebook/            route glue (params, redirect-to-first-page, 404)
widgets/notebook-reader/   NotebookHeader, PageTree (+toggle), chrome layout
entities/notebook/         + getNotebookDetails, getNotebookTree, types
entities/page/             PageDetails types, getPageByPath
entities/block/            model (types, assembleBlockTree), ui (BlockRenderer,
                           SpanRenderer, one component per type)
```

Query keys: `['notebooks','details',slug]`, `['notebooks','tree',slug]`,
`['pages','by-path',slug,path]` — no collisions with `'public'`/`'mine'`.

## 8. i18n keys (both locales)

`reader.contents` (目录/Contents), `reader.emptyNotebook` (这本菜单还是空白 /
themed), `reader.notebookMissing` (§6 copy), `reader.pageMissing` (这一页找不到了 /
This page is missing), `reader.backHome` (回首页 / Back home), `reader.pageCount`
(reuse `card.pageCount`).

## 9. Tests

- `assembleBlockTree`: ordering by sortKey (ordinal, e.g. `Z` before `a`), nesting,
  orphan handling, empty list.
- `SpanRenderer`: every mark kind renders the right element; marks compose;
  link gets `rel="noopener noreferrer"`; unknown mark is ignored gracefully.
- `BlockRenderer`: one snapshot-ish assertion per block type (role/text), unknown
  type renders nothing.
- `PageTree`: nesting, active highlight, archived nodes hidden, ancestors expanded.
- Route: `/notebooks/:slug` redirects to the first page; bad slug → missing state;
  bad path → page-missing state with the tree still rendered.
- `NotebookCard` links to the notebook route.

## 10. Acceptance criteria

1. `pnpm lint && pnpm typecheck && pnpm test && pnpm build` all green.
2. Clicking a card on the homepage opens the reader with real data; tree and content
   match the API responses; deep links (copy URL → new tab) land on the same page.
3. Every block type present in the local database renders readably in **both** themes;
   unknown types don't crash the page.
4. 404 states use the exact non-committal copy; mobile shows the tree toggle.
5. No mutations anywhere: the reader issues only GETs.

## 11. Out of scope

Editing/creating anything, favorites, access-code prompt, page revisions,
syntax highlighting, table editing, print styles, SEO meta beyond `<title>`,
reader-specific keyboard shortcuts.
