import type { NotebookVisibility } from '../model/types'

export const NOTEBOOK_VISIBILITY_LABELS = {
  private: 'Private',
  unlisted: 'Unlisted',
  public: 'Public',
} satisfies Record<NotebookVisibility, string>

export const NOTEBOOK_VISIBILITY_CONTEXT_LABELS = {
  private: 'Private notebook',
  unlisted: 'Shared via link',
  public: 'Public notebook',
} satisfies Record<NotebookVisibility, string>

export const NOTEBOOK_VISIBILITY_COLLECTION_LABELS = {
  private: 'My notebooks',
  unlisted: 'Shared notebooks',
  public: 'Public notebooks',
} satisfies Record<NotebookVisibility, string>

export const NOTEBOOK_VISIBILITY_HELP_TEXT = {
  private: 'Only you can open this notebook.',
  unlisted: 'Anyone with the link can open it, but it stays out of public listings.',
  public: 'Anyone can discover and open this notebook.',
} satisfies Record<NotebookVisibility, string>
