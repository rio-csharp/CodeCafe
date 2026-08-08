# Frontend Best Practices

> Universal guidelines for React + TypeScript projects using modern tooling.
> Stack: React 19 + TypeScript + Vite + Tailwind CSS v4 + TanStack Query + Zustand

---

## Philosophy

We optimize for:

- **Readability** — code is read 10x more than it is written
- **Maintainability** — prefer explicit over clever, simple over complex
- **Explicitness** — magic and implicit behavior hide bugs
- **Feature isolation** — a feature module should be self-contained and deletable
- **Predictable state flow** — know where every piece of state lives and why

We do NOT optimize for:

- **Clever abstractions** — if it requires a comment to understand, it is wrong
- **Premature optimization** — measure first, optimize second
- **Over-engineering** — start with the simplest solution that works

---

## 1. Component Design

### One Component, One Responsibility

```tsx
// ❌ Bad smell: a single component doing too much, 300+ lines
function Dashboard() {
  // fetching data, handling charts, managing modals, form logic...
}
```

```tsx
// ✅ Good: split it! One component should do one thing
function Dashboard() {
  return (
    <Layout>
      <StatsSection />
      <RecentActivity />
      <QuickActions />
    </Layout>
  )
}
```

**Review rule of thumb**: Be cautious at 100 lines, must refactor at 150 lines.

---

## 2. Server State vs Client State

| Type | Definition | Tool |
|------|-----------|------|
| **Server State** | Data that lives on the server (API responses, cached data) | TanStack Query |
| **Client State** | UI-only state (modals, themes, form inputs, toggles) | Zustand / `useState` |

### Use TanStack Query for:

- API data fetching
- Caching and background refetching
- Loading, error, and stale states
- Pagination, infinite scroll, optimistic updates

```tsx
// ✅ Good
const { data, isLoading, error } = useQuery({
  queryKey: ['users'],
  queryFn: fetchUsers,
})
```

### Use Zustand or local state for:

- Modal visibility
- Theme (light / dark)
- Sidebar collapsed state
- Temporary UI interactions

```tsx
// ❌ Bad smell: manually syncing server data with useState
const [users, setUsers] = useState([])

useEffect(() => {
  fetch('/api/users').then(r => r.json()).then(setUsers)
}, [])
```

**Review rule of thumb**: If the data comes from an API, it is Server State — use TanStack Query.

---

## 3. State Scope

### Prefer Local State First

Only move state to Zustand when:

- Multiple distant components need it
- Prop drilling becomes painful (3+ layers)
- The state truly belongs to the application level (auth, theme)

```tsx
// ✅ Good: local state is enough here
function LoginForm() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
}
```

```tsx
// ❌ Bad smell: globalizing everything
const useAuthStore = create(() => ({
  email: '',           // only used in LoginForm
  password: '',        // only used in LoginForm
  isSidebarOpen: true, // only used in Layout
}))
```

**Review rule of thumb**: Start local, promote to global only when necessary.

---

## 4. Props Passing

### Avoid Prop Drilling

```tsx
// ❌ Bad smell: passing props through layers, intermediate components don't use them
<App user={user}>
  <Layout user={user}>
    <Sidebar user={user}>
      <UserAvatar user={user} />  // only this component actually uses it
```

```tsx
// ✅ Good: if passing through 3+ layers, use Context or Zustand
// Intermediate components like Layout and Sidebar don't need to know about user
```

**Review rule of thumb**: If the same prop appears 3+ times down the tree, consider state management.

---

## 5. useEffect Guidelines

### Don't Use useEffect for Data Fetching

```tsx
// ❌ Bad smell: manually handling loading/error
useEffect(() => {
  fetch('/api/users')
    .then(r => r.json())
    .then(setUsers)
    .catch(setError)
}, [])
```

```tsx
// ✅ Good: data requests always go through TanStack Query
const { data, isLoading, error } = useQuery({
  queryKey: ['users'],
  queryFn: fetchUsers,
})
```

### Don't Use useEffect to Sync Derived State

```tsx
// ❌ Bad smell: using useEffect to keep two states in sync
const [firstName, setFirstName] = useState('')
const [lastName, setLastName] = useState('')
const [fullName, setFullName] = useState('')

useEffect(() => {
  setFullName(firstName + ' ' + lastName)
}, [firstName, lastName])
```

```tsx
// ✅ Good: compute directly
const fullName = firstName + ' ' + lastName
```

**Review rule of thumb**: When you see useEffect, ask "is this side effect actually necessary?"

---

## 6. List Rendering Keys

```tsx
// ❌ Bad smell: using array index as key
{items.map((item, index) => (
  <Card key={index} />   // dangerous! breaks on delete/reorder
))}
```

```tsx
// ✅ Good: use a unique and stable ID
{items.map(item => (
  <Card key={item.id} />
))}
```

**Review rule of thumb**: `key={index}` is wrong 90% of the time. Only acceptable if the list never changes or reorders.

---

## 7. TypeScript Type Safety

```tsx
// ❌ Bad smell: bypassing type checks
const data: any = await response.json()
// @ts-ignore
data.something.wrong()
```

```tsx
// ✅ Good: define explicit types
interface User {
  id: string
  name: string
  email: string
}

const data: User[] = await response.json()
```

**Review rule of thumb**: `any` is tech debt, `@ts-ignore` is a ticking bomb. Always ask for justification.

---

## 8. API Layer

### Never Call fetch/axios Directly Inside Components

All API calls should live in entity modules or a shared API layer.

Allowed direct `fetch` locations:

- Shared API client for authenticated API calls
- Lightweight health checks

```
src/
├── features/
│   └── user-management/
│       ├── UserList.tsx
│       └── api/
│           ├── getUsers.ts
│           ├── createUser.ts
│           └── updateUser.ts
```

```tsx
// ❌ Bad smell: fetch scattered everywhere
function UserList() {
  useEffect(() => {
    fetch('/api/users').then(...)   // don't do this
  }, [])
}
```

```tsx
// ✅ Good: centralized API layer
import { useUsers } from '@/features/user-management/api/getUsers'

function UserList() {
  const { data } = useUsers()   // wraps useQuery
}
```

### API Rules

- Use a shared `apiFetch` wrapper so cookies, JSON parsing, CSRF headers, and retry behavior stay consistent.
- Mutating requests must go through the shared client; do not manually fetch CSRF tokens from feature code.
- Keep API functions small and transport-shaped. React components should call hooks or feature API functions, not assemble URLs and request headers.
- Preserve backend contracts exactly. If the backend returns nullable fields, model them as nullable in TypeScript instead of assuming success data exists.

**Review rule of thumb**: If you see `fetch(` or `axios(` inside a component, request extraction to the API layer.

---

## 9. Forms

### Use React Hook Form + Zod

For any non-trivial form, use:

- **React Hook Form** — performant form handling with minimal re-renders
- **Zod** — schema validation with TypeScript inference

```tsx
// ❌ Bad smell: manual validation with useState chains
const [email, setEmail] = useState('')
const [emailError, setEmailError] = useState('')
const [password, setPassword] = useState('')
const [passwordError, setPasswordError] = useState('')

const handleSubmit = () => {
  if (!email.includes('@')) setEmailError('Invalid email')
  if (password.length < 8) setPasswordError('Too short')
  // ...
}
```

```tsx
// ✅ Good: React Hook Form + Zod
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'

const schema = z.object({
  email: z.string().email(),
  password: z.string().min(8),
})

type FormData = z.infer<typeof schema>

function LoginForm() {
  const { register, handleSubmit, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
  })

  return (
    <form onSubmit={handleSubmit(onSubmit)}>
      <input {...register('email')} />
      {errors.email && <span>{errors.email.message}</span>}
      {/* ... */}
    </form>
  )
}
```

**Review rule of thumb**: Any form with 3+ fields or validation rules should use React Hook Form + Zod.

---

## 10. Async UI States

### Every Async Page Must Handle All Four States

```tsx
function UserListPage() {
  const { data, isLoading, error, refetch } = useUsers()

  if (isLoading) return <Skeleton />            // 1. Loading
  if (error) return <QueryError onRetry={refetch} />  // 2. Error
  if (!data || data.length === 0) return <EmptyState />  // 3. Empty

  return <UserList users={data} />              // 4. Success
}
```

**Never assume data always exists.**

Query failures should use the shared `QueryError` component
(`@/shared/ui/QueryError`) — icon, friendly message, and a retry button —
instead of rendering a bare `<p>{error.message}</p>`.

**Review rule of thumb**: Every `useQuery` call must have explicit handling for loading, error, empty, and success.

### Mutation Cache Rules

Mutations should leave TanStack Query cache in a predictable state:

- Invalidate affected list/detail keys after create, update, delete operations.
- Use `setQueryData` only when the replacement is complete enough to render immediately.
- If a mutation can change a route identity (e.g. a slug), write the returned object under the new query key before navigating.
- Avoid optimistic updates for complex moves or document writes until conflict handling exists.

---

## 11. Performance Optimization

### Don't Overuse useMemo / useCallback

```tsx
// ❌ Bad smell: wrapping simple calculations in useMemo, often hurts performance
const doubled = useMemo(() => count * 2, [count])
```

```tsx
// ✅ Good: write simple calculations directly
const doubled = count * 2

// Only use useMemo for expensive computations or stable object references
const heavyResult = useMemo(() => expensiveSort(bigList), [bigList])
```

**Review rule of thumb**: Don't "prematurely optimize". Write simple code first, use Profiler to measure, then add memoization if needed.

### Code Splitting with React.lazy

For heavy components that are not needed on initial page load, use `React.lazy()` + `Suspense`:

```tsx
// ✅ Good: editor is only needed when user clicks "Edit"
const RichTextEditor = lazy(() => import('@/widgets/rich-text-editor'))

function DocumentPage() {
  return (
    <Suspense fallback={<EditorSkeleton />}>
      {isEditing && <RichTextEditor content={content} />}
    </Suspense>
  )
}
```

Candidates for lazy loading:
- Rich text editors (TipTap, CKEditor, etc.)
- Heavy data-visualization charts
- Complex modals that are rarely opened
- Admin-only panels

**Review rule of thumb**: If a component pulls in >200KB of dependencies and is not visible on first paint, lazy load it.

---

## 12. Testing

We do **not** practice strict TDD. We practice **"test alongside development"**.

### What Must Be Tested

- **Utility functions** (`shared/lib/`) — pure logic, easy to test, high value
- **State management logic** (feature `model/` or `shared/model/`) — Zustand stores, reducers, selectors
- **API layer transformations** — data parsing, error mapping, request builders
- **Bug fixes** — always write a regression test that reproduces the bug *before* fixing it

### What Is Optional

- Pure presentational components with no logic (visual-only wrappers)
- Simple prop-passing components that delegate everything to children
- One-off UI adjustments (spacing, colors, layout tweaks)

### What We Do Not Test

- Visual pixel-perfect matching (no snapshot testing for UI)
- Third-party library internals
- Static configuration files

### Test Types and Tools

| Type | Tool | When to use |
|------|------|-------------|
| Unit | Vitest | Pure functions, utilities, store logic |
| Integration | Vitest + React Testing Library | Component interaction, form submission flows |
| E2E | Playwright | Critical user paths only (login → create → delete) |

### Example: Utility Function Test

```ts
// src/shared/lib/dateUtils.ts
export function formatDate(date: Date): string {
  const y = date.getFullYear()
  const m = String(date.getMonth() + 1).padStart(2, '0')
  const d = String(date.getDate()).padStart(2, '0')
  return `${y}-${m}-${d}`
}
```

```ts
// src/shared/lib/dateUtils.test.ts
import { describe, it, expect } from 'vitest'
import { formatDate } from './dateUtils'

describe('formatDate', () => {
  it('returns YYYY-MM-DD format', () => {
    expect(formatDate(new Date('2025-01-15'))).toBe('2025-01-15')
  })

  it('pads single-digit month and day', () => {
    expect(formatDate(new Date('2025-03-09'))).toBe('2025-03-09')
  })
})
```

### Example: Regression Test for Bug Fixes

```ts
// ❌ Bad: fix the bug without a test
// ✅ Good: write the failing test first, then fix

it('should not crash when list is empty', () => {
  // This test was added after bug #123
  expect(() => render(<ItemList items={[]} />)).not.toThrow()
})
```

### Rules

1. **Logic changes without tests will not be merged.**
2. **Fixing a bug requires a regression test.**
3. **Do not chase 100% coverage.** Aim for confidence, not metrics.
4. **Tests should be as easy to delete as the code they test.** If a feature is removed, its tests go too.

**Review rule of thumb**: If a PR touches `shared/lib/`, `shared/api/`, or feature `model/` without test files, request them.

---

## 13. Styling (Tailwind CSS)

### Avoid Large Global CSS Files

The most common pitfall in traditional React projects: **one giant CSS file with thousands of lines dumping every component's styles.**

| Problem | Consequence |
|---------|-------------|
| Global naming collisions | Two components define `.title`, one overrides the other |
| Dead code paranoia | Afraid to delete CSS because you don't know what's still using it |
| Loading overhead | User visits home page but downloads CSS for the entire site |
| Maintenance fear | Changing one button style breaks three other pages |

### Prefer Tailwind Utilities

```tsx
// ❌ Traditional: go hunt for .card definition in some CSS file
<div className="card">

// ✅ Tailwind: styles live right in the component, self-documenting
<div className="rounded-lg border p-4 shadow-sm hover:shadow-md transition-shadow">
```

**Benefits**:
- Only used classes are generated (5KB~15KB is common)
- Delete component = styles deleted automatically, zero dead code
- No need to invent class names, no collision risk

### When Global CSS is Acceptable

Avoid large global CSS files, but small global styles are fine for:

- CSS reset / base styles
- Markdown rendering styles
- Syntax highlighting themes
- Third-party library overrides
- Global animations
- Typography font imports

```css
/* index.css — keep this minimal */
@import "tailwindcss";

body {
  font-family: 'Inter', system-ui, sans-serif;
}
```

**Review rule of thumb**: If a CSS rule only applies to one component, it belongs in that component (as Tailwind classes). If it applies to the entire app, `index.css` is acceptable.

### Inline Style Exceptions

Prefer Tailwind utilities for static styles. Inline styles are acceptable only for runtime values Tailwind cannot know ahead of time, such as editor-selected colors, calculated indentation, CSS variables, or third-party library integration points.

Keep inline styles small:

```tsx
// ✅ Acceptable: dynamic value from editor state
<Type style={{ color: currentColor }} />

// ❌ Bad smell: static styles belong in className
<div style={{ padding: 16, borderRadius: 8, background: '#fff' }} />
```

**Review rule of thumb**: `style={{ ... }}` should explain a dynamic runtime value. Static visual design belongs in Tailwind classes.

### Design Tokens

When your project defines Design Tokens (via Tailwind v4 `@theme` or CSS variables), prefer them over hardcoded Tailwind default colors.

```tsx
// ❌ Bad smell: hardcoded Tailwind colors
<button className="border-gray-200 text-gray-700 hover:bg-gray-50">

// ✅ Good: semantic Design Tokens
<button className="border-border-default text-text-secondary hover:bg-surface-hover">
```

Define tokens in your global CSS:

```css
@theme {
  --color-surface: #ffffff;
  --color-surface-hover: #f9fafb;
  --color-border-default: #e5e7eb;
  --color-text-primary: #111827;
  --color-text-secondary: #6b7280;
  --color-status-error: #dc2626;
  --color-status-success: #16a34a;
}
```

**When to keep hardcoded colors**:
- Decorative / illustrative elements (skeleton placeholders, blur backgrounds)
- Third-party library overrides where tokens don't apply
- Shadows with opacity variants

**Review rule of thumb**: If your project has Design Tokens, prefer them over raw `gray-*`, `red-*`, `green-*` Tailwind classes for semantic styling.

---

## 14. Error Handling

### Use Error Boundaries for Unexpected Crashes

Never leave the entire app blank on runtime errors. Always show user-friendly fallback UI.

```tsx
// ❌ Bad smell: uncaught error crashes the whole app
function DataPage() {
  const data = useData()
  return <div>{data.map(...)}</div>  // if data is undefined, white screen
}
```

```tsx
// ✅ Good: wrap route-level sections with error boundaries
<ErrorBoundary fallback={<ErrorFallback />}>
  <DataPage />
</ErrorBoundary>
```

### Widget-Level Error Boundaries

In addition to the global boundary, wrap complex widgets that have independent failure modes:

```tsx
function DataTable(props: DataTableProps) {
  return (
    <ErrorBoundary fallback={<ErrorFallback title="Table Error" description="Failed to load." />}>
      <DataTableComponent {...props} />
    </ErrorBoundary>
  )
}
```

Widgets that should have dedicated boundaries:
- Complex data grids or trees with recursive rendering
- Rich text editors with many extensions
- Heavy visualization components
- Form panels with async mutations

**Review rule of thumb**: Route-level and widget-level boundaries prevent one bug from crashing the entire application.

---

## 15. Accessibility (a11y)

Accessibility is not optional — it is a core part of product quality.

- Use **semantic HTML** — `<nav>`, `<main>`, `<article>`, `<button>`
- Buttons should always be real `<button>` elements, not `<div onClick>`
- Every `<input>` needs an associated `<label>`
- Every `<img>` needs meaningful `alt` text
- Use `aria-*` attributes only when semantic HTML is insufficient

```tsx
// ❌ Bad smell: div pretending to be a button
<div onClick={handleClick} className="btn">Click me</div>

// ✅ Good: proper semantic element
<button onClick={handleClick} className="btn">Click me</button>
```

```tsx
// ❌ Bad smell: input without label
<input placeholder="Email" />

// ✅ Good: labeled input
<label htmlFor="email">Email</label>
<input id="email" type="email" />
```

**Review rule of thumb**: If you can click it, it should be a `<button>`. If it takes text input, it needs a label.

---

## 16. Event Handling

### No Long Logic Inside JSX

```tsx
// ❌ Bad smell: long logic embedded in JSX
<button onClick={() => {
  if (confirm('Sure?')) {
    deleteItem(id)
    navigate('/')
    toast.success('Deleted')
  }
}}>
  Delete
</button>
```

```tsx
// ✅ Good: extract it, JSX should only describe "what"
<button onClick={handleDelete}>Delete</button>

const handleDelete = () => {
  if (!confirm('Sure?')) return
  deleteItem(id)
  navigate('/')
  toast.success('Deleted')
}
```

**Review rule of thumb**: If you see 3+ lines of logic inside `{}` in JSX, extract it to a function.

---

## 17. Project Directory Structure (Feature-Sliced Design)

We follow **Feature-Sliced Design (FSD)**, a scalable frontend architecture standard.

```
src/
  app/              # entry point, router, global providers, global styles
  pages/            # route pages (thin, only compose widgets)
  widgets/          # independent reusable UI blocks
  features/         # user-triggered interactions / use cases
  entities/         # business domain models
  shared/           # pure infrastructure
```

### FSD Layer Dependency Rule

Layers can only import from **lower** layers:

```
app → pages → widgets → features → entities → shared
```

- `app` can import anything
- `pages` can import `widgets`, `features`, `entities`, `shared`
- `widgets` can import `features`, `entities`, `shared`
- `features` can import `entities`, `shared`
- `entities` can import `shared` only
- `shared` cannot import any other layer

**Documented exception**: `entities/notebook` may import `entities/notebook-item`.
`notebook-item` is a sub-domain of `notebook` (its types are only meaningful in
the context of a notebook), so `entities/notebook` re-exports the item types
through its own public API and other layers import them from `@/entities/notebook`.
This is the only sanctioned cross-slice import inside `entities/`; any new one
needs an explicit note here.

### Public API (Barrel Exports)

Every slice **must** expose a public API through `index.ts`. External code **must** only import through the `index.ts`:

```tsx
// ✅ Good: import through public API
import { useLogin } from '@/features/authenticate'

// ❌ Bad smell: deep import into slice internals
import { useLogin } from '@/features/authenticate/model/useLogin'
```

**Principles**:
- `shared/` is for pure infrastructure with zero business logic
- `entities/` owns types and pure domain logic (no UI)
- `features/` are self-contained user actions (can be deleted without breaking the app)
- `widgets/` are complex UI compositions that may cross feature boundaries
- `pages/` are thin routing shells that only compose widgets
- `app/` is the composition root — no business logic here

### Zustand Store Placement

| Scope | Location | Examples |
|-------|----------|----------|
| Global UI | `shared/model/` | Toast queue, theme store |
| Widget-local | `widgets/<name>/model/` | Sidebar state, editor mode |
| Feature-local | `features/<name>/model/` | Form drafts, wizard step state |

Global stores go in `shared/model/`; anything scoped to a single widget or feature stays in that slice's `model/` directory.

---

## 18. Quick Review Checklist

| When you see... | Action... |
|-----------------|-----------|
| Component > 150 lines | Request refactor/split |
| `useState` + manual `fetch` | Suggest TanStack Query |
| Same prop passed 3+ layers deep | Suggest Context / Zustand |
| State in Zustand that only one component uses | Suggest local state |
| `key={index}` | Require unique ID |
| `: any` / `@ts-ignore` | Require proper types |
| `fetch(` or `axios(` inside a component | Request extraction to API layer |
| Mutating API call bypasses shared client | Require shared CSRF/credentials handling |
| Mutation does not refresh affected query keys | Add invalidation or complete cache replacement |
| Form with 3+ fields without React Hook Form | Suggest RHF + Zod |
| Async UI missing loading/error/empty states | Request explicit handling |
| `useMemo` wrapping simple calculations | Suggest removal |
| `style={{` for static styling | Suggest Tailwind; allow narrow dynamic runtime values |
| Long logic inside JSX | Suggest extraction to function |
| Large `App.css` or `global.css` | Suggest Tailwind + component-local styles |
| `!important` usage | Usually a specificity war loser, question it |
| `<div onClick>` | Require `<button>` |
| `<input>` without `<label>` | Require associated label |
| Missing Error Boundary at route level | Request addition |
| Hardcoded Tailwind colors where Design Tokens exist | Replace with semantic tokens |
| Component imports >200KB and is conditionally rendered | Suggest `React.lazy()` + `Suspense` |
| Deep import into slice internals (`features/foo/model/bar`) | Import through `index.ts` only |
| Upper layer importing upper layer (`entities/` → `features/`) | Fix dependency direction per FSD rules |
| Business logic in `pages/` | Move to `features/` or `widgets/`; pages should be thin shells |
| Domain logic mixed with UI in `entities/` | Move UI to `widgets/`; entities own types and pure logic only |

---

## 19. Unified Error Message Extraction

Don't repeat `err instanceof Error ? err.message : '...'` in every mutation onError handler.

Create a shared utility:

```ts
// src/shared/lib/errorUtils.ts
export function getErrorMessage(err: unknown, fallback: string): string {
  return err instanceof Error ? err.message : fallback
}
```

Use it:
```tsx
onError: (err) => showToast(getErrorMessage(err, 'Failed to save'), 'error')
```

For messages rendered directly in the UI (error pages, inline form errors),
use `getDisplayErrorMessage(err, t, fallback)` from the same module instead.
It maps known backend error codes to localized `errors.<code>` strings,
passes through 4xx messages (authored for users), and never leaks 5xx
ProblemDetails `detail` (internal paths/field names) into the UI.

**Review rule of thumb**: If you see `err instanceof Error` more than once in a file, use the shared helper. Raw `error.message` from an API error should never be rendered directly.

---

## 20. Import Path Aliases

Avoid `../../../` hell. Configure Vite + TypeScript path aliases so cross-feature imports are readable.

```ts
// vite.config.ts
resolve: {
  alias: {
    '@': path.resolve(__dirname, './src'),
  },
}
```

```json
// tsconfig.app.json
"baseUrl": ".",
"paths": {
  "@/*": ["src/*"]
}
```

```tsx
// ✅ Good
import { useToast } from '@/shared/ui/Toast'

// ❌ Bad smell
import { useToast } from '../../../../shared/ui/Toast'
```

**Review rule of thumb**: If an import path contains `../../..`, replace it with `@/`.

---

## 21. Named Constants over Magic Values

Any number or string whose meaning isn't obvious from the surrounding code should be named.

```ts
// ✅ Good
const TREE_INDENT_PER_LEVEL = 14
const TREE_INDENT_BASE = 10
const paddingLeft = level * TREE_INDENT_PER_LEVEL + TREE_INDENT_BASE

// ❌ Bad smell: what do 14 and 10 mean?
const paddingLeft = level * 14 + 10
```

Keep constants close to where they are used (file-level). Promote to shared modules only when used by 3+ files.

**Review rule of thumb**: If you have to scroll to understand what a literal means, give it a name.

---

## 22. React 19 Feature Adoption

We use React 19. Prefer new features over legacy patterns:

- **ref as a regular prop** — no more `forwardRef`
  ```tsx
  // ✅ Good (React 19)
  function Input({ ref, ...props }: { ref?: React.Ref<HTMLInputElement> }) {
    return <input ref={ref} {...props} />
  }

  // ❌ Legacy
  const Input = forwardRef<HTMLInputElement, Props>((props, ref) => ...)
  ```

- **`use()` for reading Context / Promises** — in Server Components or async boundaries

**Review rule of thumb**: New code should not use `forwardRef`. Refactor existing usages opportunistically.

---

## 23. Organizing Long Tailwind className Strings

If a `className` exceeds 3 lines, extract it to a named constant or shared module.

```ts
// ✅ Good: extracted constant
export const PROSE_CLASSES =
  'prose prose-sm max-w-none ' +
  'prose-headings:font-semibold prose-headings:text-black ' +
  '...'

// ❌ Bad smell: 10+ lines of className inline in JSX, duplicated in 2+ files
<div className="prose prose-sm max-w-none prose-headings:font-semibold ...">
```

This prevents drift between reader and editor styles, and makes changes a single-point update.

**Review rule of thumb**: `className` longer than your thumb is too long. Extract it.

---

## Tech Stack

| Library | Purpose |
|---------|---------|
| React ^19 | UI library |
| TypeScript ~6.0 | Type safety |
| Vite ^8 | Build tool |
| Tailwind CSS ^4 | Styling |
| React Router ^7 | Routing |
| TanStack Query ^5 | Server state |
| Zustand ^5 | Client state |
| React Hook Form | Form handling |
| Zod | Schema validation |
| Vitest | Unit and integration testing |
| React Testing Library | Component interaction testing |
| Playwright | E2E testing |
