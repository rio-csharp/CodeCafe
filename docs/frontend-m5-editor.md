# M5 — Page editor

> Written after the fact: the editor grew commit-by-commit on top of M4's reader.
> This note records the scope as built. Read `docs/frontend-m4-reader.md` first.

## What the editor is

The reader's edit mode (`PageEditor`, under `widgets/page-editor/`). One flat draft
(`lib/draft.ts`), rendered as a tree; every text block is its own contenteditable
(`TextBlockEditor` + `EditableSpans`). Saving diffs the draft against the loaded
page into a batch of block ops (`lib/ops.ts`) — there is no auto-save; Ctrl/Cmd+Enter
or the Save button commits, Escape cancels.

Editing surface: inline spans with marks (floating toolbar + Ctrl/Cmd+B/I/U),
slash-menu conversion, block selection (Escape, Shift+arrows), drag-and-drop via
the six-dot handle, Tab/Shift+Tab indent, Ctrl/Cmd+Shift+arrows reorder, soft line
breaks (Shift+Enter), nested blocks, callouts as containers, code (highlighted;
the language field is a themed combobox — free text plus a hand-rolled listbox,
because a native <datalist> is OS-drawn and ignores the palette), table, image,
audio, bulleted/numbered lists.

## Undo/redo (session-local)

`lib/history.ts` keeps a snapshot stack of pre-change drafts (cloned — block
payloads may mutate in place). Every draft mutation flows through the editor's
`setDraft` wrapper; keystroke bursts in one block coalesce into a single undo
step (`run` key + 800 ms window), structural changes never coalesce.

- Ctrl/Cmd+Z undo, Ctrl/Cmd+Shift+Z and Ctrl+Y redo, handled at the document
  level; the browser's native contenteditable undo is suppressed because it
  would mutate the DOM behind the model's back.
- Native form fields (page title input, code textarea) keep their own undo.
- History dies with the editing session; saved pages rely on revisions instead.

## Smart paste

`lib/paste.ts` parses pasted plain text. Single-line text inserts at the caret,
EXCEPT a single markdown line pasted into an empty block, which converts it.
Multi-line text becomes one block per line, honouring Markdown line syntax:
`#` headings, `- [ ]` to-dos, `-`/`*` bullets, `1.` numbered items,
`>` quotes, `---` dividers, fenced code with language. Inline marks are NOT
parsed — pasted text carries no formatting. The first line joins a non-empty
block at the caret; pasting into an empty block replaces it entirely.

## Input rules (typing)

`lib/inputRules.ts` converts a block while typing, Notion-style: the whole
pre-caret text being a marker plus the trigger key fires the conversion —
`#`–`###` + Space for headings, `-`/`*`/`1.`/`>` + Space for lists and quotes,
`[]` / `[x]` + Space for to-dos, `---` + Space or the third dash for a divider,
and the third backtick for a code block. (`- [ ]` is deliberately absent: the
`- ` rule fires on the first space, before the brackets can be typed.)

Backspace at the start of a list item (bulleted / numbered / to-do) un-lists
it into a paragraph instead of merging upward; the next Backspace merges.

## Page history panel

The reader's right panel has a History tab (`PageHistoryPanel`) backed by the
revisions API (`GET /api/pages/{id}/revisions`, cursor-paginated, batches
newest-first): one line per batch — relative time, per-kind change summary, an
AI badge for `Ai`-sourced batches. Clicking a batch opens the preview dialog:
the historical content rendered read-only (`GET .../revisions/at?atUtc=...`, a
read-only server-side reconstruction) plus a block-level diff against the
current version (`lib/revisionDiff.ts`: added / removed / edited / moved).
Writers get a one-click restore in the dialog (`POST .../revisions/restore`) —
no confirm step: the log is append-only, so a restore is itself undoable by
restoring again, and that is the safety net. The tab hides while editing —
restoring under an open draft would silently discard it.

## Block-level history (in the editor)

Each block's handle menu has a History entry (`BlockHistoryDialog`, backed by
`GET /api/pages/{id}/blocks/{blockId}/revisions`, cursor-paginated): one row
per revision — relative time, change-kind label, an AI badge for `Ai`-sourced
rows — with a per-row restore (`POST .../revisions/restore`, `blockVersion`).
Freshly minted (unsaved) blocks have no entry. Because a restore changes the
server copy under the open draft, a successful restore exits edit mode back to
the reader (which refetches) and the page shows a short notice — the draft
never silently wins over the restore.

## Deliberately out of scope (still)

Auto-save, real-time collaboration, cross-block copy/cut, inline-mark parsing
on paste, undo/redo buttons in the chrome (keyboard-only today), inline
(word-level) diffs in the revision preview (block-level only).
