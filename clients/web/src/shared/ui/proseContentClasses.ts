/**
 * Shared Tailwind prose classes for notebook page content rendering.
 * Used by both the read-only viewer (NotebookPageContent) and the editor
 * (NotebookPageEditor) to keep styling consistent.
 */
export const PROSE_CONTENT_CLASSES =
  'prose prose-sm max-w-none ' +
  'prose-headings:font-semibold prose-headings:text-text-primary prose-headings:leading-snug ' +
  'prose-p:text-text-primary prose-p:leading-normal ' +
  'prose-strong:text-text-primary ' +
  'prose-li:text-text-primary prose-li:leading-normal ' +
  'prose-a:text-brand-brown ' +
  'prose-blockquote:text-text-secondary prose-blockquote:border-border-hover ' +
  'prose-hr:border-border-default ' +
  'prose-pre:bg-surface-active prose-pre:text-text-primary prose-pre:border prose-pre:border-border-default prose-pre:rounded-lg prose-pre:px-5 prose-pre:py-4 prose-pre:font-mono prose-pre:text-sm prose-pre:leading-relaxed prose-pre:overflow-x-auto prose-pre:relative ' +
  '[&_pre_code]:bg-transparent [&_pre_code]:text-inherit [&_pre_code]:p-0 [&_pre_code]:rounded-none ' +
  'prose-code:font-mono prose-code:text-sm prose-code:bg-surface-active prose-code:text-text-primary prose-code:px-1.5 prose-code:py-0.5 prose-code:rounded ' +
  "[&_ul[data-type='taskList']]:list-none [&_ul[data-type='taskList']]:pl-0 " +
  "[&_ul[data-type='taskList']_li]:flex [&_ul[data-type='taskList']_li]:items-start [&_ul[data-type='taskList']_li]:gap-2 " +
  "[&_ul[data-type='taskList']_li>label]:flex [&_ul[data-type='taskList']_li>label]:items-center [&_ul[data-type='taskList']_li>label]:mt-0.5 " +
  "[&_ul[data-type='taskList']_li>div]:flex-1 [&_ul[data-type='taskList']_p]:my-0"
