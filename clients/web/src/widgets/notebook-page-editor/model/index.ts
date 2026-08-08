// Segment-level public API. The editor store is deliberately reachable without going through the
// widget root barrel: that barrel also exports the (heavy) editor component, so importing the store
// from it statically would pull the editor into the main chunk and defeat its lazy import.
export { useEditorStore } from './editorStore'
