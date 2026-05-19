import {
  FolderOpen,
  Code2,
  Database,
  BookOpen,
  FileText,
  Layers,
} from 'lucide-react'

const icons = [FolderOpen, Code2, Database, BookOpen, FileText, Layers]

export default function pickIcon(title: string) {
  let hash = 0
  for (let i = 0; i < title.length; i++) hash = title.charCodeAt(i) + ((hash << 5) - hash)
  return icons[Math.abs(hash) % icons.length]
}
