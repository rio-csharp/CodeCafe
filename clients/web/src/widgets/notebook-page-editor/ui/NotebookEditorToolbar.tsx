import { useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import type { Editor } from '@tiptap/react'
import {
  Bold,
  Italic,
  Underline as UnderlineIcon,
  Strikethrough,
  Subscript,
  Superscript,
  Heading1,
  Heading2,
  Heading3,
  Heading4,
  AlignLeft,
  AlignCenter,
  AlignRight,
  AlignJustify,
  List,
  ListOrdered,
  ListChecks,
  Link as LinkIcon,
  Image as ImageIcon,
  Video,
  Code,
  Quote,
  Minus,
  Undo,
  Redo,
  Table as TableIcon,
  Trash2,
  Plus,
} from 'lucide-react'
import ToolbarGroup from './ToolbarGroup'
import ToolbarButton from './ToolbarButton'
import ToolbarColorControls from './ToolbarColorControls'
import ToolbarFontSelect from './ToolbarFontSelect'
import ToolbarLanguageSelect from './ToolbarLanguageSelect'
import { usePromptDialog } from '@/shared/ui/PromptDialog'
import {
  normalizeEditorImageUrl,
  normalizeEditorLinkUrl,
  normalizeEditorYoutubeUrl,
} from '@/shared/lib/safeUrls'

interface NotebookEditorToolbarProps {
  editor: Editor
}

export default function NotebookEditorToolbar({ editor }: NotebookEditorToolbarProps) {
  const { t } = useTranslation()
  const { requestPrompt, promptDialog } = usePromptDialog()

  const handleSetLink = useCallback(async () => {
    const previousUrl = editor.getAttributes('link').href as string | undefined
    const url = await requestPrompt({
      title: t('editor.toolbar.link'),
      label: t('editor.prompt.url'),
      defaultValue: previousUrl ?? '',
      placeholder: 'https://',
      // An empty value means "remove the link" — always valid.
      validate: (value) => (value === '' || normalizeEditorLinkUrl(value) ? null : t('editor.prompt.invalidUrl')),
    })
    if (url === null) return
    if (url === '') {
      editor.chain().focus().extendMarkRange('link').unsetLink().run()
    } else {
      const safeUrl = normalizeEditorLinkUrl(url)
      if (safeUrl) {
        editor.chain().focus().extendMarkRange('link').setLink({ href: safeUrl }).run()
      }
    }
  }, [editor, requestPrompt, t])

  const handleInsertImage = useCallback(async () => {
    const url = await requestPrompt({
      title: t('editor.toolbar.insertImage'),
      label: t('editor.prompt.imageUrl'),
      placeholder: 'https://',
      validate: (value) => (normalizeEditorImageUrl(value) ? null : t('editor.prompt.invalidUrl')),
    })
    if (url) {
      const safeUrl = normalizeEditorImageUrl(url)
      if (safeUrl) {
        editor.chain().focus().setImage({ src: safeUrl }).run()
      }
    }
  }, [editor, requestPrompt, t])

  const handleInsertYoutube = useCallback(async () => {
    const url = await requestPrompt({
      title: t('editor.toolbar.insertYoutube'),
      label: t('editor.prompt.youtubeUrl'),
      placeholder: 'https://www.youtube.com/watch?v=…',
      validate: (value) => (normalizeEditorYoutubeUrl(value) ? null : t('editor.prompt.invalidUrl')),
    })
    if (url) {
      const safeUrl = normalizeEditorYoutubeUrl(url)
      if (safeUrl) {
        editor.chain().focus().setYoutubeVideo({ src: safeUrl }).run()
      }
    }
  }, [editor, requestPrompt, t])

  return (
    <div className="flex items-center gap-1 px-3 py-2 flex-wrap">
      <ToolbarGroup showDivider>
        <ToolbarButton active={editor.isActive('heading', { level: 1 })} onClick={() => editor.chain().focus().toggleHeading({ level: 1 }).run()} title={t('editor.toolbar.heading1')}><Heading1 className="h-4 w-4" /></ToolbarButton>
        <ToolbarButton active={editor.isActive('heading', { level: 2 })} onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()} title={t('editor.toolbar.heading2')}><Heading2 className="h-4 w-4" /></ToolbarButton>
        <ToolbarButton active={editor.isActive('heading', { level: 3 })} onClick={() => editor.chain().focus().toggleHeading({ level: 3 }).run()} title={t('editor.toolbar.heading3')}><Heading3 className="h-4 w-4" /></ToolbarButton>
        <ToolbarButton active={editor.isActive('heading', { level: 4 })} onClick={() => editor.chain().focus().toggleHeading({ level: 4 }).run()} title={t('editor.toolbar.heading4')}><Heading4 className="h-4 w-4" /></ToolbarButton>
      </ToolbarGroup>
      <ToolbarGroup showDivider>
        <ToolbarButton active={editor.isActive('bold')} onClick={() => editor.chain().focus().toggleBold().run()} title={t('editor.toolbar.bold')}><Bold className="h-4 w-4" /></ToolbarButton>
        <ToolbarButton active={editor.isActive('italic')} onClick={() => editor.chain().focus().toggleItalic().run()} title={t('editor.toolbar.italic')}><Italic className="h-4 w-4" /></ToolbarButton>
        <ToolbarButton active={editor.isActive('underline')} onClick={() => editor.chain().focus().toggleUnderline().run()} title={t('editor.toolbar.underline')}><UnderlineIcon className="h-4 w-4" /></ToolbarButton>
        <ToolbarButton active={editor.isActive('strike')} onClick={() => editor.chain().focus().toggleStrike().run()} title={t('editor.toolbar.strikethrough')}><Strikethrough className="h-4 w-4" /></ToolbarButton>
        <ToolbarButton active={editor.isActive('subscript')} onClick={() => editor.chain().focus().toggleSubscript().run()} title={t('editor.toolbar.subscript')}><Subscript className="h-4 w-4" /></ToolbarButton>
        <ToolbarButton active={editor.isActive('superscript')} onClick={() => editor.chain().focus().toggleSuperscript().run()} title={t('editor.toolbar.superscript')}><Superscript className="h-4 w-4" /></ToolbarButton>
      </ToolbarGroup>
      <ToolbarGroup showDivider>
        <ToolbarFontSelect editor={editor} />
        <ToolbarColorControls editor={editor} />
      </ToolbarGroup>
      <ToolbarGroup showDivider>
        <ToolbarButton active={editor.isActive({ textAlign: 'left' })} onClick={() => editor.chain().focus().setTextAlign('left').run()} title={t('editor.toolbar.alignLeft')}><AlignLeft className="h-4 w-4" /></ToolbarButton>
        <ToolbarButton active={editor.isActive({ textAlign: 'center' })} onClick={() => editor.chain().focus().setTextAlign('center').run()} title={t('editor.toolbar.alignCenter')}><AlignCenter className="h-4 w-4" /></ToolbarButton>
        <ToolbarButton active={editor.isActive({ textAlign: 'right' })} onClick={() => editor.chain().focus().setTextAlign('right').run()} title={t('editor.toolbar.alignRight')}><AlignRight className="h-4 w-4" /></ToolbarButton>
        <ToolbarButton active={editor.isActive({ textAlign: 'justify' })} onClick={() => editor.chain().focus().setTextAlign('justify').run()} title={t('editor.toolbar.alignJustify')}><AlignJustify className="h-4 w-4" /></ToolbarButton>
      </ToolbarGroup>
      <ToolbarGroup showDivider>
        <ToolbarButton active={editor.isActive('bulletList')} onClick={() => editor.chain().focus().toggleBulletList().run()} title={t('editor.toolbar.bulletList')}><List className="h-4 w-4" /></ToolbarButton>
        <ToolbarButton active={editor.isActive('orderedList')} onClick={() => editor.chain().focus().toggleOrderedList().run()} title={t('editor.toolbar.numberedList')}><ListOrdered className="h-4 w-4" /></ToolbarButton>
        <ToolbarButton active={editor.isActive('taskList')} onClick={() => editor.chain().focus().toggleTaskList().run()} title={t('editor.toolbar.taskList')}><ListChecks className="h-4 w-4" /></ToolbarButton>
      </ToolbarGroup>
      <ToolbarGroup showDivider>
        <ToolbarButton active={editor.isActive('link')} onClick={handleSetLink} title={t('editor.toolbar.link')}><LinkIcon className="h-4 w-4" /></ToolbarButton>
      </ToolbarGroup>
      <ToolbarGroup showDivider>
        <ToolbarButton active={editor.isActive('codeBlock')} onClick={() => editor.chain().toggleCodeBlock().run()} title={t('editor.toolbar.codeBlock')}><Code className="h-4 w-4" /></ToolbarButton>
        {editor.isActive('codeBlock') && (
          <ToolbarLanguageSelect editor={editor} />
        )}
        <ToolbarButton active={editor.isActive('blockquote')} onClick={() => editor.chain().focus().toggleBlockquote().run()} title={t('editor.toolbar.quote')}><Quote className="h-4 w-4" /></ToolbarButton>
        <ToolbarButton onClick={() => editor.chain().focus().setHorizontalRule().run()} title={t('editor.toolbar.horizontalRule')}><Minus className="h-4 w-4" /></ToolbarButton>
        <ToolbarButton onClick={handleInsertImage} title={t('editor.toolbar.insertImage')}><ImageIcon className="h-4 w-4" /></ToolbarButton>
        <ToolbarButton onClick={handleInsertYoutube} title={t('editor.toolbar.insertYoutube')}><Video className="h-4 w-4" /></ToolbarButton>
      </ToolbarGroup>
      <ToolbarGroup showDivider>
        <ToolbarButton onClick={() => editor.chain().focus().insertTable({ rows: 3, cols: 3, withHeaderRow: true }).run()} title={t('editor.toolbar.insertTable')}><TableIcon className="h-4 w-4" /></ToolbarButton>
        {editor.isActive('table') && (
          <>
            <ToolbarButton onClick={() => editor.chain().focus().addColumnBefore().run()} title={t('editor.toolbar.addColumnBefore')}><Plus className="h-4 w-4" /></ToolbarButton>
            <ToolbarButton onClick={() => editor.chain().focus().addColumnAfter().run()} title={t('editor.toolbar.addColumnAfter')}><Plus className="h-4 w-4" /></ToolbarButton>
            <ToolbarButton onClick={() => editor.chain().focus().deleteColumn().run()} title={t('editor.toolbar.deleteColumn')}><Minus className="h-4 w-4" /></ToolbarButton>
            <ToolbarButton onClick={() => editor.chain().focus().addRowBefore().run()} title={t('editor.toolbar.addRowBefore')}><Plus className="h-4 w-4" /></ToolbarButton>
            <ToolbarButton onClick={() => editor.chain().focus().addRowAfter().run()} title={t('editor.toolbar.addRowAfter')}><Plus className="h-4 w-4" /></ToolbarButton>
            <ToolbarButton onClick={() => editor.chain().focus().deleteRow().run()} title={t('editor.toolbar.deleteRow')}><Minus className="h-4 w-4" /></ToolbarButton>
            <ToolbarButton onClick={() => editor.chain().focus().deleteTable().run()} title={t('editor.toolbar.deleteTable')}><Trash2 className="h-4 w-4" /></ToolbarButton>
          </>
        )}
      </ToolbarGroup>
      <ToolbarGroup>
        <ToolbarButton onClick={() => editor.chain().focus().undo().run()} title={t('editor.toolbar.undo')}><Undo className="h-4 w-4" /></ToolbarButton>
        <ToolbarButton onClick={() => editor.chain().focus().redo().run()} title={t('editor.toolbar.redo')}><Redo className="h-4 w-4" /></ToolbarButton>
      </ToolbarGroup>
      {promptDialog}
    </div>
  )
}
