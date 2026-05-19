import type { Editor } from '@tiptap/react'

const LANGUAGES = [
  { value: 'plaintext', label: 'Plain text' },
  { value: 'javascript', label: 'JavaScript' },
  { value: 'typescript', label: 'TypeScript' },
  { value: 'python', label: 'Python' },
  { value: 'bash', label: 'Bash' },
  { value: 'json', label: 'JSON' },
  { value: 'html', label: 'HTML' },
  { value: 'css', label: 'CSS' },
  { value: 'csharp', label: 'C#' },
  { value: 'sql', label: 'SQL' },
  { value: 'markdown', label: 'Markdown' },
  { value: 'java', label: 'Java' },
  { value: 'go', label: 'Go' },
  { value: 'rust', label: 'Rust' },
  { value: 'yaml', label: 'YAML' },
  { value: 'xml', label: 'XML' },
]

interface ToolbarLanguageSelectProps {
  editor: Editor
}

export default function ToolbarLanguageSelect({ editor }: ToolbarLanguageSelectProps) {
  const currentLang = (editor.getAttributes('codeBlock').language as string | undefined) || 'plaintext'

  return (
    <select
      value={currentLang}
      onChange={(e) => editor.chain().focus().setCodeBlock({ language: e.target.value }).run()}
      className="text-xs border border-gray-200 rounded px-1.5 py-0.5 bg-white text-gray-700 outline-none focus:border-gray-300 cursor-pointer"
      title="Code language"
    >
      {LANGUAGES.map((lang) => (
        <option key={lang.value} value={lang.value}>
          {lang.label}
        </option>
      ))}
    </select>
  )
}
