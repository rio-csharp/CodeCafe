import StarterKit from '@tiptap/starter-kit'
import CodeBlockLowlight from '@tiptap/extension-code-block-lowlight'
import Color from '@tiptap/extension-color'
import { TextStyle } from '@tiptap/extension-text-style'
import Highlight from '@tiptap/extension-highlight'
import TaskList from '@tiptap/extension-task-list'
import TaskItem from '@tiptap/extension-task-item'
import { Table } from '@tiptap/extension-table'
import TableRow from '@tiptap/extension-table-row'
import TableHeader from '@tiptap/extension-table-header'
import TableCell from '@tiptap/extension-table-cell'
import Underline from '@tiptap/extension-underline'
import Link from '@tiptap/extension-link'
import Image from '@tiptap/extension-image'
import TextAlign from '@tiptap/extension-text-align'
import Subscript from '@tiptap/extension-subscript'
import Superscript from '@tiptap/extension-superscript'
import Placeholder from '@tiptap/extension-placeholder'
import CharacterCount from '@tiptap/extension-character-count'
import Youtube from '@tiptap/extension-youtube'
import FontFamily from '@tiptap/extension-font-family'
import { lowlight } from './lowlight'

export interface TipTapExtensionOptions {
  editable?: boolean
}

export function createTipTapExtensions(options: TipTapExtensionOptions = {}) {
  const { editable = true } = options

  return [
    StarterKit.configure({ codeBlock: false, link: false, underline: false }),
    CodeBlockLowlight.configure({ lowlight, defaultLanguage: 'plaintext' }),
    Underline,
    Link.configure({ openOnClick: !editable }),
    FontFamily,
    Color,
    TextStyle,
    Highlight.configure({ multicolor: true }),
    TextAlign.configure({ types: ['heading', 'paragraph'] }),
    Subscript,
    Superscript,
    Image,
    Youtube.configure({ nocookie: true }),
    TaskList,
    TaskItem.configure({ nested: true }),
    Table.configure({ resizable: true }),
    TableRow,
    TableHeader,
    TableCell,
    ...(editable
      ? [Placeholder.configure({ placeholder: 'Start writing something …' }), CharacterCount]
      : [CharacterCount]),
  ]
}
