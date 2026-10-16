# M3 Spec — Auth

> Read `docs/frontend-architecture.md` first. Scope: session plumbing, login/register
> pages, header auth state, a "my notebooks" section on the homepage.
> **No profile editing, no password change, no notebook creation.**

## 1. API facts (verified against the backend)

| Endpoint | Body | Returns |
|---|---|---|
| `POST /api/auth/register` | `{ email, password, displayName }` | `AuthSessionDto` |
| `POST /api/auth/login` | `{ email, password }` | `AuthSessionDto` |
| `POST /api/auth/refresh` | `{ refreshToken }` | `AuthSessionDto` |
| `POST /api/auth/logout` | `{ refreshToken }` | `Result` (no value) |
| `GET /api/auth/me` | — (Bearer) | `{ id, email, displayName }` |

```ts
interface AuthSessionDto {
  user: { id: string; email: string; displayName: string }
  accessToken: string
  accessTokenExpiresAtUtc: string // ISO 8601
  refreshToken: string
}
```

Server validation to mirror in Zod: register — email format, ≤ 256 chars; password
8–128; displayName required, ≤ 40. Login — email format; password non-empty.
Error codes the UI translates: `invalid_credentials` (401, one uniform message for
unknown email / wrong password — do not say which failed),
`email_already_registered` (409), `invalid_refresh_token` (401).
`GET /api/notebooks` (own + shared) requires auth; `/api/notebooks/public` stays anonymous.

## 2. New dependencies

```
pnpm add react-hook-form zod @hookform/resolvers zustand
```

## 3. Session plumbing (the heart of M3)

### Storage rules

- **Access token: memory only** (module-level in `shared/api/session.ts`).
- **Refresh token: `localStorage["codecafe.refresh"]`** — the second and last
  localStorage key besides `codecafe.lang`/`codecafe.theme`.
- Refresh tokens **rotate on every refresh**: always persist the new one.

### `shared/api/session.ts`

```ts
getAccessToken(): string | null
getRefreshToken(): string | null
setSession(dto: AuthSessionDto): void      // stores both tokens + expiry
clearSession(): void                        // wipes both, notifies listeners
refreshSession(): Promise<AuthSessionDto>   // SINGLE-FLIGHT, see below
onSessionCleared(listener): () => void
```

- **Single-flight**: `refreshSession` keeps one in-flight promise; concurrent callers
  await the same promise. A refresh that 401s (`invalid_refresh_token`) clears the
  session and rejects for every waiter.
- A module-level singleton is intentional here — `shared/` is the only layer allowed
  one (see architecture doc).

### `apiFetch` integration (`shared/api/client.ts`)

1. Attach `Authorization: Bearer <token>` when an access token exists.
2. **Lazy-proactive refresh**: before sending, if the token expires within 60 s,
   `await refreshSession()` first. The common path never sees a 401.
3. **Reactive 401 retry**: a request that *had* a token and got 401 awaits
   `refreshSession()`, then retries **once** with the new token. A second 401 means a
   real logout: clear the session and throw. Anonymous requests (no token) never retry.
4. `refreshSession` itself must not recurse through this 401 logic — call `fetch`
   directly or pass an opt-out flag.

### Session state for the UI

`entities/session` slice: Zustand store
`{ status: 'unknown' | 'authenticated' | 'anonymous', user: AuthUser | null }`.

- On app boot (`app/providers.tsx`): if a refresh token exists, `status: 'unknown'`
  while one `refreshSession()` runs → then authenticated / anonymous. No token →
  anonymous immediately.
- Subscribes to `onSessionCleared` → flips to anonymous (header reacts without a
  reload). After login/register, the feature sets authenticated with the returned user.

## 4. Pages & routing

Two new lazy routes: `/login`, `/register`, wrapped in a shared `widgets/auth-layout`
(centered card on the canvas, cafe copy, link to the other page).

- `features/authenticate`: RHF + Zod forms. Client-side rules mirror §1 server rules.
  Server errors map to copy by `error.code`; unknown → generic error copy.
- Success → `navigate(location.state?.from ?? '/', { replace: true })`.
- Already-authenticated users hitting `/login|/register` redirect to `/`.
- Failed login does **not** clear the password field (don't punish retyping the email).

## 5. Header auth state (`widgets/site-header`)

- `anonymous`: ghost button "登录" → `/login`, with `state: { from: currentPath }`.
- `authenticated`: display name (muted text) + "退出" ghost button.
  Logout calls `POST /auth/logout` with the refresh token (fire-and-forget is fine —
  clear the local session regardless of the response) and stays on the current page.
- `unknown`: render neither (avoid a flash of the wrong state).

## 6. Homepage: "my notebooks" section

Authenticated users get a section **above** Today's Menu:

- Title `我的笔记本 / My notebooks`, data from `GET /api/notebooks` (own + shared),
  default sort `UpdatedDesc`, first page of 12 — **no search/sort/load-more in M3**
  (known limitation; a full dashboard is a later milestone).
- Reuse `NotebookCard`. Loading: 3 skeleton cards. Empty: themed copy
  ("你的菜单还是空的" / "Your shelf is empty"). Error: same themed retry pattern as
  the catalog.
- Anonymous users see exactly the M1 homepage — no empty section, no layout shift.
- Query key namespace must not collide with the public catalog:
  `['notebooks', 'mine']` vs `['notebooks', 'public', { search, sort }]` — rename the
  existing catalog key to include `'public'`.

## 7. i18n copy (both locales, keyed under `auth.*` / `header.*` / `myNotebooks.*`)

Cafe-flavored but clear. Required keys: `auth.loginTitle` (回到咖啡馆 / Welcome back),
`auth.registerTitle` (加入咖啡馆 / Join the cafe), `auth.email`, `auth.password`,
`auth.displayName`, `auth.submitLogin` (登录 / Sign in),
`auth.submitRegister` (注册 / Sign up), `auth.toRegister` (还没账号？来一杯 /
New here? Grab a cup), `auth.toLogin` (已有账号？登录 / Have an account? Sign in),
`auth.error.invalidCredentials`, `auth.error.emailTaken`,
`auth.error.generic`, field validation messages, `header.login`, `header.logout`,
`myNotebooks.title`, `myNotebooks.empty`.

## 8. Tests

- `shared/api/session`: single-flight (two concurrent calls → one refresh request);
  rotation (new refresh token persisted); failed refresh clears the session and
  rejects all waiters.
- `shared/api/client`: attaches the bearer header; 401-with-token → refresh → retry
  once; second 401 → session cleared + throw; anonymous request never retries;
  proactive refresh fires when expiry < 60 s away (inject a clock).
- `features/authenticate`: validation messages render; submit posts and navigates;
  `invalid_credentials` maps to its copy; `email_already_registered` maps to its copy.
- `widgets/site-header`: anonymous vs authenticated rendering; logout clears the store.
- Homepage: section absent when anonymous, present with items when authenticated.

## 9. Acceptance criteria

1. `pnpm lint && pnpm typecheck && pnpm test && pnpm build` all green.
2. Register → redirected home, header shows the display name, "我的笔记本" shows the
   empty state. Reload → still authenticated (silent refresh on boot).
3. Login with wrong password → `invalid_credentials` copy; existing email on register
   → `email_already_registered` copy.
4. Logout → header flips to the login button, "我的笔记本" disappears, no reload.
5. Let an access token expire (or fake it in devtools) → next request silently
   refreshes; deleting the refresh token → next authed action ends anonymous.
6. Anonymous homepage is unchanged from M1/M2.
7. localStorage holds exactly three keys: `codecafe.lang`, `codecafe.theme`,
   `codecafe.refresh`.

## 10. Out of scope

Profile editing, password change, notebook creation/editing, notebook detail pages,
favorites, route guards beyond the login/register redirect, "remember me" UI,
token refresh on a timer (the lazy-proactive check in `apiFetch` covers it).
