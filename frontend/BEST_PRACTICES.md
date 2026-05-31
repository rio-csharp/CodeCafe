# CodeCafe Frontend Best Practices

> This guide is for the CodeCafe frontend development team to maintain code consistency and maintainability.
> Tech Stack: React 19 + TypeScript + Vite + Tailwind CSS + TanStack Query + Zustand + TipTap

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
      <RecentNotes />
      <AiChatPanel />
    </Layout>
  )
}
```

**Review rule of thumb**: Be cautious at 100 lines, must refactor at 150 lines.

---

## 2. Server State vs Client State

This is one of the most important concepts in modern React.

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
  queryKey: ['notes'],
  queryFn: fetchNotes,
})
```

### Use Zustand or local state for:

- Modal visibility
- Theme (light / dark)
- Sidebar collapsed state
- Temporary UI interactions

```tsx
// ❌ Bad smell: manually syncing server data with useState
const [notes, setNotes] = useState([])

useEffect(() => {
  fetch('/api/notes').then(r => r.json()).then(setNotes)
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
  fetch('/api/notes')
    .then(r => r.json())
    .then(setNotes)
    .catch(setError)
}, [])
```

```tsx
// ✅ Good: data requests always go through TanStack Query
const { data, isLoading, error } = useQuery({
  queryKey: ['notes'],
  queryFn: fetchNotes,
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
interface Note {
  id: string
  title: string
  content: string
}

const data: Note[] = await response.json()
```

**Review rule of thumb**: `any` is tech debt, `@ts-ignore` is a ticking bomb. Always ask for justification.

---

## 8. API Layer

### Never Call fetch/axios Directly Inside Components

All API calls should live in feature modules or `lib/api`.

Allowed direct `fetch` locations:

- `src/lib/apiClient.ts` for normal authenticated API calls
- `src/lib/api/health.ts` for lightweight health checks

```
src/
├── features/
│   └── notes/
│       ├── NotesPage.tsx
│       ├── NoteCard.tsx
│       └── api/
│           ├── getNotes.ts
│           ├── createNote.ts
│           └── updateNote.ts
```

```tsx
// ❌ Bad smell: fetch scattered everywhere
function NotesPage() {
  useEffect(() => {
    fetch('/api/notes').then(...)   // don't do this
  }, [])
}
```

```tsx
// ✅ Good: centralized API layer
import { useNotes } from '@/features/notes/api/getNotes'

function NotesPage() {
  const { data } = useNotes()   // wraps useQuery
}
```

### CodeCafe API Rules

- Use `apiFetch` for product API calls so cookies, JSON parsing, CSRF headers,
  and retry-on-stale-CSRF behavior stay consistent.
- Mutating requests must go through `apiFetch`; do not manually fetch
  `/api/auth/csrf` from feature code.
- Keep API functions small and transport-shaped. React components should call
  hooks or feature API functions, not assemble URLs and request headers.
- Preserve backend contracts exactly. If the backend returns nullable fields,
  model them as nullable in TypeScript instead of assuming success data exists.
- Treat `updatedAtUtc` as a meaningful contract field. When backend optimistic
  concurrency lands, note/page writes should send the expected timestamp or
  revision from the current item.

**Review rule of thumb**: If you see `fetch(` or `axios(` inside a component, request extraction to the API layer.

### TipTap Content Contract

Notebook page content is TipTap JSON. The frontend owns editor UX, but the
backend owns persistence validation and derived search text.

Rules:

- Use `{ type: 'doc', content: [] }` for an empty page document.
- Do not hand-edit TipTap JSON in components outside editor or content utility
  code.
- Keep outline extraction, plain-text previews, and display helpers in feature
  utilities so page components stay focused.
- Do not make search depend on frontend-generated `plainTextContent`. The
  backend should derive searchable text from saved `contentJson`.
- When adding editor extensions, update content rendering, tests, and backend
  validation expectations together.

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
function NotesPage() {
  const { data, isLoading, error } = useNotes()

  if (isLoading) return <NotesSkeleton />     // 1. Loading
  if (error) return <ErrorFallback error={error} />  // 2. Error
  if (!data || data.length === 0) return <EmptyState />  // 3. Empty

  return <NotesList notes={data} />            // 4. Success
}
```

**Never assume data always exists.**

**Review rule of thumb**: Every `useQuery` call must have explicit handling for loading, error, empty, and success.

### Mutation Cache Rules

Mutations should leave TanStack Query cache in a predictable state:

- Invalidate affected list/detail keys after create, update, delete, favorite,
  and reorder operations.
- Use `setQueryData` only when the replacement is complete enough to render
  immediately.
- If a mutation can change a route identity, such as a notebook slug, write the
  returned object under the new query key before navigating.
- Avoid optimistic updates for tree moves or document writes until conflict
  handling exists; stale page content is better than silently losing edits.

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

---

## 12. Testing

We do **not** practice strict TDD. We practice **"test alongside development"**.

### What Must Be Tested

- **Utility functions** (`lib/`) — pure logic, easy to test, high value
- **State management logic** (`stores/`) — Zustand stores, reducers, selectors
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
// src/lib/dateUtils.ts
export function formatDate(date: Date): string {
  const y = date.getFullYear()
  const m = String(date.getMonth() + 1).padStart(2, '0')
  const d = String(date.getDate()).padStart(2, '0')
  return `${y}-${m}-${d}`
}
```

```ts
// src/lib/dateUtils.test.ts
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

it('should not crash when notes list is empty', () => {
  // This test was added after bug #123
  expect(() => render(<NotesList notes={[]} />)).not.toThrow()
})
```

### Rules

1. **Logic changes without tests will not be merged.**
2. **Fixing a bug requires a regression test.**
3. **Do not chase 100% coverage.** Aim for confidence, not metrics.
4. **Tests should be as easy to delete as the code they test.** If a feature is removed, its tests go too.

**Review rule of thumb**: If a PR touches `lib/`, `stores/`, or `api/` without test files, request them.

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
// ❌ Traditional: go hunt for .note-card definition in some CSS file
<div className="note-card">

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

Prefer Tailwind utilities for static styles. Inline styles are acceptable only
for runtime values Tailwind cannot know ahead of time, such as editor-selected
colors, calculated tree indentation, CSS variables, or third-party library
integration points.

Keep inline styles small:

```tsx
// ✅ Acceptable: dynamic value from editor state
<Type style={{ color: currentColor }} />

// ❌ Bad smell: static styles belong in className
<div style={{ padding: 16, borderRadius: 8, background: '#fff' }} />
```

**Review rule of thumb**: `style={{ ... }}` should explain a dynamic runtime
value. Static visual design belongs in Tailwind classes.

---

## 14. Error Handling

### Use Error Boundaries for Unexpected Crashes

Never leave the entire app blank on runtime errors. Always show user-friendly fallback UI.

```tsx
// ❌ Bad smell: uncaught error crashes the whole app
function NotesPage() {
  const notes = useNotes()
  return <div>{notes.map(...)}</div>  // if notes is undefined, white screen
}
```

```tsx
// ✅ Good: wrap route-level sections with error boundaries
<ErrorBoundary fallback={<ErrorFallback />}>
  <NotesPage />
</ErrorBoundary>
```

**Review rule of thumb**: Route-level and feature-level boundaries prevent one bug from crashing the entire application.

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

## 17. Project Directory Structure

```
frontend/
├── src/
│   ├── app/              # app entry, router config, global providers
│   ├── components/       # generic UI components (Button, Input, Modal...)
│   ├── features/         # feature-based modules (notes, auth, chat...)
│   │   └── notes/
│   │       ├── NotesPage.tsx
│   │       ├── NoteCard.tsx
│   │       └── api/
│   │           ├── getNotes.ts
│   │           └── createNote.ts
│   ├── lib/              # utility functions, API client setup
│   ├── hooks/            # reusable custom hooks
│   ├── stores/           # Zustand global state
│   ├── main.tsx          # entry point
│   └── index.css         # global Tailwind import + minimal base styles
├── index.html
├── package.json
├── vite.config.ts
└── tsconfig.json
```

**Principles**:
- Modules under `features/` are self-contained (components, API, state, types together)
- `components/` is for pure UI with zero business logic only
- Don't create a `utils/` junk drawer, organize by functionality in `lib/`

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
| Mutating API call bypasses `apiFetch` | Require shared CSRF/credentials handling |
| Mutation does not refresh affected query keys | Add invalidation or complete cache replacement |
| Page write relies on frontend `plainTextContent` | Treat `contentJson` as source of truth |
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

## 19. Unified Error Message Extraction

Don't repeat `err instanceof Error ? err.message : '...'` in every mutation onError handler.

Create a shared utility:

```ts
// src/lib/errorUtils.ts
export function getErrorMessage(err: unknown, fallback: string): string {
  return err instanceof Error ? err.message : fallback
}
```

Use it:
```tsx
onError: (err) => showToast(getErrorMessage(err, 'Failed to save page'), 'error')
```

**Review rule of thumb**: If you see `err instanceof Error` more than once in a file, use the shared helper.

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
import { useToast } from '@/components/ui/useToast'

// ❌ Bad smell
import { useToast } from '../../../../components/ui/useToast'
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
export const PROSE_CONTENT_CLASSES =
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
