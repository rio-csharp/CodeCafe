import { createLowlight, common } from 'lowlight'

/**
 * Shared lowlight instance for TipTap's CodeBlockLowlight extension.
 *
 * The TipTap extension stores the configured `lowlight` instance internally
 * and re-uses it across renders; creating a fresh `createLowlight(common)` per
 * consumer meant two parallel highlighters were always loaded. This module
 * exports a single instance that both the editor and viewer widgets import.
 */
export const lowlight = createLowlight(common)
