# M2 Spec — Foundations: theming & responsive layout

> Read `docs/frontend-architecture.md` first. Scope: dark mode + theme switcher +
> token refactor + responsive consolidation. **No new pages, no auth, no new features.**
> The homepage must look identical in light mode after this milestone.

## 1. Theme model

Three-way: **system (default) → light → dark**, cycled by a header toggle.

- Storage key `codecafe.theme`: `'light' | 'dark'`; **absent means system**.
- When in system mode, follow `prefers-color-scheme` **and react to OS changes live**
  (`matchMedia('(prefers-color-scheme: dark)')` change listener). In explicit modes the
  listener is detached/ignored.
- The resolved theme is applied as `class="dark"` on `<html>` and by setting
  `document.documentElement.style.colorScheme` to match (native scrollbars/inputs follow).

### FOUC prevention (required)

`index.html` gets a tiny inline `<script>` in `<head>`, before the module script, that
reads `localStorage['codecafe.theme']` and applies the `dark` class before first paint.
It must be dependency-free and fail silently.

## 2. Tailwind v4 mechanics (verified against official docs)

`app/styles/index.css`:

```css
@import 'tailwindcss';

@custom-variant dark (&:where(.dark, .dark *));
```

Theme tokens flip via CSS variable override — utilities like `bg-canvas` reference
`var(--color-canvas)`, so re-defining the variable under `.dark` re-themes every
component **without touching any `dark:` classes in markup**:

```css
@theme {
  /* semantic tokens, light values */
  --color-canvas: ...;
  ...
}

@layer theme {
  .dark {
    /* same tokens, dark values — must come after the @theme block */
    --color-canvas: ...;
    ...
  }
}
```

Do **not** sprinkle `dark:` utilities across components. If a component needs a
theme-specific value, that means a semantic token is missing — add the token.

## 3. Token refactor (the core of this milestone)

Two layers. The cafe palette stays as the fixed raw values; components reference only
the semantic layer. Existing names (`cream`, `paper`, `espresso`, `roast`, `mocha`,
`caramel`, `caramel-deep`, `latte`) are removed from component code entirely.

| semantic token | light | dark | replaces |
|---|---|---|---|
| `canvas` (page bg) | `#faf6f0` | `#16110d` | cream |
| `card` (elevated surface) | `#fffdf9` | `#211a14` | paper |
| `ink` (primary text) | `#2b2118` | `#f0e7db` | espresso |
| `muted` (secondary text) | `#7a6a5a` | `#a89a89` | mocha |
| `accent` (actions, focus) | `#c07a3e` | `#d08d4e` | caramel |
| `accent-strong` (accent hover/active) | `#9c5f2c` | `#e0a266` | caramel-deep |
| `line` (borders, dividers) | `#e5d9c8` | `#3a2e23` | latte |
| `band` (footer block) | `#4a382a` | `#100c09` | roast |
| `band-ink` (text on band) | `#faf6f0` | `#f0e7db` | cream-on-roast |

Fonts unchanged (`font-display`, `font-body`).

Dark palette rationale: warm browns, never gray/blue — the cafe identity holds at night.
Accent gets brighter in dark mode, not darker; hover goes lighter. Contrast targets:
`ink` on `canvas` ≥ 12:1, `muted` on `canvas` ≥ 4.5:1, `accent` on `card` ≥ 4.5:1 —
verify with a contrast checker, adjust the hex values if a pair misses.

Rename sweep (mechanical, ~12 files): `cream→canvas`, `paper→card`, `espresso→ink`,
`mocha→muted`, `caramel→accent`, `caramel-deep→accent-strong`, `latte→line`,
`roast→band` (footer) or `ink` (headings — headings now use `ink`).

## 4. Theme toggle UI

`features/switch-language` gets a sibling: `features/switch-theme`.

- Core logic in `shared/lib/theme.ts`: `getThemeMode()`, `setThemeMode(mode)`,
  `applyResolvedTheme()`, `subscribeSystemTheme()`. Pure-ish, unit-testable with a
  mocked `matchMedia`.
- `ThemeToggle`: one button cycling system → light → dark, with inline SVG icons
  (monitor / sun / moon) + `aria-label` from i18n. Sits in `SiteHeader` next to the
  language toggle.
- Copy keys: `theme.label` = 切换主题 / Switch theme; `theme.system` = 跟随系统 /
  System; `theme.light` = 浅色 / Light; `theme.dark` = 深色 / Dark. The button's
  accessible name should include the current mode (e.g. "Switch theme, currently system").

## 5. Responsive consolidation

The layout is already mobile-first; this milestone removes duplication and locks the rules:

- Add `shared/ui/Container.tsx`: `mx-auto w-full max-w-6xl px-4 sm:px-6`. SiteHeader,
  NotebookCatalog, SiteFooter all use it (that exact class string appears 3+ times today).
- Hero and grid stay as-is (already: search full-width on mobile, 1/2/3-column grid).
- No new breakpoints, no `useMediaQuery` hooks — Tailwind defaults are the law.

## 6. Tests

- `shared/lib/theme.test.ts`: mode cycling order; persistence; absent key = system;
  dark class applied/removed correctly; system listener reacts to a mocked
  `matchMedia` change only in system mode.
- `features/switch-theme/ThemeToggle.test.tsx`: renders current-mode icon, click cycles,
  aria-label reflects mode.
- Existing tests must pass unchanged (the token rename must not break class assertions).

## 7. Acceptance criteria

1. Gates green: `pnpm lint && pnpm typecheck && pnpm test && pnpm build`.
2. Light mode is pixel-equivalent to before (same hex values on every surface).
3. Dark mode: every surface flips; no component uses `dark:` classes; no raw cafe
   palette token appears in component code (grep check).
4. Toggle cycles system → light → dark; choice survives reload; no flash of wrong
   theme on reload (inline script); OS theme change while in system mode flips the UI live.
5. Manual responsive pass at 320 / 375 / 768 / 1280 px in both themes: no horizontal
   scroll, no overlapping header, grid columns 1/1/2/3.

## 8. Out of scope

Auth, new pages, per-user theme persistence on the server, animations/transitions
between themes (a simple `transition-colors` on body is fine), light-dark() CSS
function (Baseline support is not universal enough; the `.dark` class approach is).
