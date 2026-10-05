# M1 Spec — Homepage (咖啡馆主题)

> Implementation spec for the first frontend milestone. Read
> `docs/frontend-architecture.md` first; it is the law, this file is the task.
> Scope: the anonymous homepage only. **No auth, no login page, no notebook detail page.**

## 1. Concept

CodeCafe 是一家咖啡馆，公共笔记本是菜单。首页不是营销页，**首页就是菜单本身**。
视觉走暖色咖啡馆路线：米白底、深烘文字色、焦糖 accent、衬线标题字体。
主题化文案（见 §5 文案表）是这个设计的灵魂，不许换成中性文案。

## 2. API facts (verified against the backend)

- `GET /api/notebooks/public` — anonymous allowed; returns the public catalog only.
  (Its sibling `GET /api/notebooks` requires auth and returns own + shared notebooks.)
- Query params: `search` (string), `sort` (`UpdatedDesc` | `CreatedDesc` | `TitleAsc`),
  `page` (1-based), `pageSize` (default 20, max 100).
- Response envelope: `{ value, error, isSuccess }`. Success value is
  `PagedResult<NotebookSummaryDto>` = `{ items, page, pageSize, totalCount, hasNextPage }`.
- `NotebookSummaryDto`:
  ```ts
  {
    id: string            // Guid
    title: string
    description: string | null
    slug: string
    visibility: 'Private' | 'Unlisted' | 'Public'
    isFavorite: boolean   // always false for anonymous
    tags: string[]
    pageCount: number
    updatedAtUtc: string  // ISO 8601
  }
  ```
- Failure body: `{ value: null, error: { code, message, kind } }` with 4xx/5xx status.
- Dev server proxy: Vite proxies `/api` → `http://localhost:5258`.

## 3. Dependencies to add

```
pnpm add react-router @tanstack/react-query i18next react-i18next i18next-browser-languagedetector
pnpm add @fontsource-variable/fraunces @fontsource-variable/inter
pnpm add -D tailwindcss @tailwindcss/vite vitest jsdom @testing-library/react @testing-library/jest-dom @testing-library/user-event
```

Do not add anything else. No UI kit, no icon library (inline the two or three SVGs needed:
search icon, globe icon for language, coffee logo mark).

Add npm scripts: `"typecheck": "tsc -b --noEmit"`, `"test": "vitest run"`.

## 4. File / slice layout (FSD)

```
src/
  app/
    main.tsx              # entry (moved from src/)
    providers.tsx         # QueryClientProvider + I18nextProvider + RouterProvider
    router.tsx            # createBrowserRouter, lazy pages, route ErrorBoundary
    styles/index.css      # tailwind + @theme tokens (§6)
  pages/
    home/
      index.ts            # public API of the slice
      HomePage.tsx
  widgets/
    site-header/          # brand mark + language toggle
    site-footer/          # espresso-dark footer, one line of copy
    hero/                 # slogan + big search input
    notebook-catalog/     # sort toggle + card grid + loading/empty/error + load-more
  features/
    search-notebooks/     # debounced search input (300 ms), controlled, syncs upward
    switch-language/      # zh ⇄ en toggle, persists to localStorage
  entities/
    notebook/
      model/types.ts      # NotebookSummary, PagedResult<>, query keys
      api/listNotebooks.ts
      ui/NotebookCard.tsx
      lib/formatRelativeTime.ts   # Intl.RelativeTimeFormat, locale-aware
  shared/
    api/client.ts         # apiFetch: envelope unwrap, ApiError, 30 s timeout
    api/types.ts          # ResultEnvelope, PagedResult, ErrorKind
    i18n/                 # i18next init + en.ts + zh.ts (§5)
    ui/                   # Button, Spinner — minimal, tokens only
```

Import rule: `app → pages → widgets → features → entities → shared`. Never import upward;
never deep-import past a slice's `index.ts`.

## 5. Copy deck (all copy lives in `shared/i18n`, keyed)

| key | zh | en |
|---|---|---|
| `brand.name` | CodeCafe | CodeCafe |
| `hero.title` | 每一本笔记，都是一杯现磨。 | Every notebook, freshly brewed. |
| `hero.subtitle` | 逛逛大家的公共笔记本，找一杯对你口味的。 | Browse public notebooks and find one to your taste. |
| `search.placeholder` | 搜搜今天的菜单… | Search the menu… |
| `search.clear` | 清空 | Clear |
| `catalog.title` | 今日菜单 | Today's Menu |
| `catalog.sort.recent` | 刚出炉 | Freshly brewed |
| `catalog.sort.title` | 按名称 | By name |
| `catalog.loadMore` | 再来一份 | One more serving |
| `catalog.empty.title` | 咖啡师还没上班 | The barista hasn't started yet |
| `catalog.empty.body` | 菜单暂时是空的，过会儿再来看看。 | The menu is empty for now — check back soon. |
| `catalog.error.title` | 厨房出了点状况 | Something went wrong in the kitchen |
| `catalog.error.retry` | 再试一次 | Try again |
| `card.pageCount` | {{count}} 页 | {{count}} pages |
| `card.updatedAt` | 更新于 {{time}} | Updated {{time}} |
| `footer.line` | 用 ❤️ 和咖啡因酿造 | Brewed with ❤️ and caffeine |
| `lang.switchTo` | EN | 中文 |

Language: `i18next-browser-languagedetector`, fallback `en`, user choice persisted in
`localStorage` (`codecafe.lang`). `formatRelativeTime` must follow the active locale.

## 6. Design tokens (Tailwind v4 `@theme` in `app/styles/index.css`)

```css
@theme {
  --color-cream: #faf6f0;        /* page background */
  --color-paper: #fffdf9;        /* card surface */
  --color-espresso: #2b2118;     /* primary text */
  --color-roast: #4a382a;        /* headings, footer bg */
  --color-mocha: #7a6a5a;        /* secondary text */
  --color-caramel: #c07a3e;      /* accent: links, buttons, focus rings */
  --color-caramel-deep: #9c5f2c; /* accent hover */
  --color-latte: #e5d9c8;        /* borders, dividers */
  --font-display: "Fraunces Variable", Georgia, serif;
  --font-body: "Inter Variable", system-ui, sans-serif;
}
```

Rules: semantic tokens only in components (`bg-cream`, `text-espresso`, `border-latte`),
never raw hex/arbitrary palette classes. Headings `font-display`; body `font-body`.
Import the two fontsource packages once in `main.tsx`.

## 7. Page composition

```
<SiteHeader />           sticky, paper bg, bottom latte border
  <Hero />               cream bg, display-serif title, subtitle, <SearchInput />
  <NotebookCatalog />    section title + sort toggle; grid: 1 / 2 / 3 cols (sm/md/lg),
                         cards on paper, latte border, caramel hover ring
<SiteFooter />           roast bg, cream text
```

- **NotebookCard**: title (display serif), description (2-line clamp, omit when null),
  tags as small latte chips, footer row: page count + relative updated time.
  Cards are plain `<article>` in M1 — **not links** (the reader page is a later milestone).
- **Search**: debounce 300 ms; changing search resets pagination. Empty query = full catalog.
- **Sort toggle**: two options (`UpdatedDesc` ↔ "刚出炉", `TitleAsc` ↔ "按名称");
  `UpdatedDesc` default. Changing sort resets pagination.
- **Pagination**: `useInfiniteQuery`, `getNextPageParam` from `hasNextPage`/`page`,
  "再来一份" button while `hasNextPage`.
- **States**: skeleton cards while loading (animate-pulse, paper blocks);
  themed empty state; themed error state with retry (`queryClient.resetQueries`).
- Responsive: header collapses gracefully, search input full-width on mobile.

## 8. Data flow

`entities/notebook/api/listNotebooks.ts` builds the query string (`search`, `sort`,
`page`, `pageSize=12`) and calls `apiFetch<PagedResult<NotebookSummary>>`.
Query key: `['notebooks', { search, sort }]` (page lives in the infinite query).
`shared/api/client.ts`:

- `apiFetch<T>(path, init?)` — JSON only, unwraps `value`, throws `ApiError` on
  `!isSuccess` or non-2xx; 204/empty body → `undefined`.
- `AbortSignal.timeout(30_000)` combined with caller signal via `AbortSignal.any`.
- No `Authorization` header logic in M1 (anonymous only).

## 9. Tests (Vitest + Testing Library)

- `shared/api/client`: unwraps success envelope; throws `ApiError` with `status/code/kind`
  on failure envelope; throws on non-JSON error response.
- `entities/notebook`: `formatRelativeTime` in both locales; `NotebookCard` renders title,
  tags, page count; description omitted when null.
- `widgets/notebook-catalog`: renders items from a mocked query; shows the themed empty
  state on `items: []`; error state renders the retry button.
- `features/search-notebooks`: debounce emits once for rapid typing.

## 10. Acceptance criteria

1. `pnpm lint && pnpm typecheck && pnpm test && pnpm build` all pass.
2. With the backend running and zero public notebooks: themed empty state.
3. With public notebooks in the DB: cards render real data; search filters;
   sort switches; "再来一份" loads the next page until `hasNextPage` is false.
4. Language toggle switches every visible string between zh/en without reload
   (choice survives reload).
5. No auth code, no routes beyond `/`, no `localStorage` usage except `codecafe.lang`.
6. All import boundaries respected; no deep imports.

## 11. Out of scope (do NOT build)

Login/register, notebook detail/reader, favorites UI, tag filtering UI, dashboard,
access-code prompt, AI anything, dark mode.
