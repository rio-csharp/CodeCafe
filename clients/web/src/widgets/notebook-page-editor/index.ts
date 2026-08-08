// Only the lazily loaded editor component. useEditorStore is exported from ./model instead: a
// static import of the store through this barrel would also pull in the editor component, so the
// lazy import in NotebookReaderPage would stop producing a separate chunk.
export { default } from './ui/NotebookPageEditor'
