import type { PaletteColor } from '../model/types'

/** Text colour per palette entry. Shared by the reader and editor span renderers. */
export const SPAN_COLOR_CLASS: Record<PaletteColor, string> = {
  primary: 'text-accent-strong',
  success: 'text-success',
  danger: 'text-danger',
  warning: 'text-warning',
  info: 'text-info',
  muted: 'text-muted',
}

/** Highlight background per palette entry; the ink colour stays inherited. */
export const SPAN_HIGHLIGHT_CLASS: Record<PaletteColor, string> = {
  primary: 'bg-accent-soft',
  success: 'bg-success-soft',
  danger: 'bg-danger-soft',
  warning: 'bg-warning-soft',
  info: 'bg-info-soft',
  muted: 'bg-muted-soft',
}
