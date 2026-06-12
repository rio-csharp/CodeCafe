import { useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { FileText, Upload, X } from 'lucide-react'
import type { TreeNode } from '@/entities/notebook'
import { Modal } from '@/shared/ui/Modal'
import { Button } from '@/shared/ui/Button'
import { useToast } from '@/shared/ui/Toast'
import { mapImportError } from '../model/errorMapping'
import { useImportMarkdown } from '../model/useImportMarkdown'

export interface ImportMarkdownModalProps {
  isOpen: boolean
  onClose: () => void
  notebookSlug: string
  notebookId: string
  tree: TreeNode[]
  onSuccess?: (pagePath: string) => void
}

interface FolderOption {
  path: string
  title: string
  depth: number
}

const MAX_TITLE = 200
const MAX_BYTES = 4 * 1024 * 1024 // 4 MB — matches backend default MaxUploadBytes
const ACCEPT = '.md,.markdown,text/markdown'

export function ImportMarkdownModal({
  isOpen,
  onClose,
  notebookSlug,
  notebookId,
  tree,
  onSuccess,
}: ImportMarkdownModalProps) {
  const { t } = useTranslation()
  const { showToast } = useToast()

  const [file, setFile] = useState<File | null>(null)
  const [title, setTitle] = useState('')
  const [parentPath, setParentPath] = useState<string | null>(null)
  const [localError, setLocalError] = useState<string | null>(null)
  const [stage, setStage] = useState<'uploading' | 'converting' | 'saving' | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const folders: FolderOption[] = flattenFolders(tree)

  const resetForm = () => {
    setFile(null)
    setTitle('')
    setParentPath(null)
    setLocalError(null)
    setStage(null)
    mutation.reset()
    if (fileInputRef.current) fileInputRef.current.value = ''
  }

  const handleClose = () => {
    if (mutation.isPending) return
    resetForm()
    onClose()
  }

  const mutation = useImportMarkdown(notebookSlug, notebookId, {
    onStage: ({ stage: next }) => setStage(next),
    onSuccess: (data) => {
      showToast(t('notebook.importMarkdownSuccess', { title: data.title }), 'success')
      onSuccess?.(data.path)
      handleClose()
    },
    onError: (err) => {
      showToast(mapImportError(err, t), 'error')
    },
  })

  const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const next = event.target.files?.[0] ?? null
    // Reset the input so re-picking the same file fires `change` again.
    event.target.value = ''
    setLocalError(null)

    if (!next) {
      setFile(null)
      return
    }

    const isMarkdown =
      /\.(md|markdown)$/i.test(next.name) || next.type === 'text/markdown'
    if (!isMarkdown) {
      setFile(null)
      showToast(t('notebook.importMarkdownFileUnsupported'), 'error')
      return
    }
    if (next.size > MAX_BYTES) {
      setFile(null)
      showToast(t('notebook.importMarkdownErrorTooLarge'), 'error')
      return
    }

    setFile(next)
    // Auto-fill the title from the file name the first time, so subsequent
    // edits to the title field aren't clobbered if the user re-picks.
    if (!title.trim()) {
      setTitle(stripMarkdownExtension(next.name))
    }
  }

  const handleRemoveFile = () => {
    setFile(null)
    if (fileInputRef.current) fileInputRef.current.value = ''
  }

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault()
    if (mutation.isPending) return
    if (!file) {
      setLocalError(t('notebook.importMarkdownFileRequired'))
      return
    }
    if (!title.trim()) {
      setLocalError(t('notebook.importMarkdownFileRequired'))
      return
    }
    if (title.length > MAX_TITLE) {
      setLocalError(t('notebook.importMarkdownErrorGeneric'))
      return
    }
    setLocalError(null)
    setStage('uploading')
    mutation.mutate({
      file,
      title: title.trim(),
      parentPath,
    })
  }

  const stageLabel = stage
    ? t(
        stage === 'uploading'
          ? 'notebook.importMarkdownUploading'
          : stage === 'converting'
            ? 'notebook.importMarkdownConverting'
            : 'notebook.importMarkdownSaving',
      )
    : null

  return (
    <Modal
      isOpen={isOpen}
      onClose={mutation.isPending ? () => {} : handleClose}
      title={t('notebook.importMarkdownTitle')}
      ariaLabel={t('notebook.importMarkdownTitle')}
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        <p className="text-xs text-text-tertiary">{t('notebook.importMarkdownDescription')}</p>

        <div>
          <label className="text-xs font-medium text-text-secondary block mb-1">
            {t('notebook.importMarkdownFileLabel')}
          </label>
          <div className="flex items-start gap-2">
            <label className="inline-flex items-center gap-2 px-3 py-2 rounded-lg border border-border-default bg-surface text-xs font-medium text-text-secondary hover:bg-surface-hover cursor-pointer transition-colors">
              <Upload className="h-3.5 w-3.5" />
              {t('notebook.importMarkdownFileChoose')}
              <input
                ref={fileInputRef}
                type="file"
                accept={ACCEPT}
                onChange={handleFileChange}
                className="sr-only"
                aria-label={t('notebook.importMarkdownFileLabel')}
              />
            </label>
            {file && (
              <div className="flex-1 min-w-0 flex items-center gap-2 px-3 py-2 rounded-lg border border-border-subtle bg-surface-hover">
                <FileText className="h-3.5 w-3.5 shrink-0 text-text-tertiary" />
                <div className="flex-1 min-w-0">
                  <p className="text-xs text-text-primary truncate">{file.name}</p>
                  <p className="text-[11px] text-text-tertiary">{formatBytes(file.size, t)}</p>
                </div>
                <button
                  type="button"
                  onClick={handleRemoveFile}
                  disabled={mutation.isPending}
                  className="p-1 rounded-md text-text-tertiary hover:text-text-primary hover:bg-surface disabled:opacity-50"
                  aria-label={t('notebook.cancel')}
                >
                  <X className="h-3.5 w-3.5" />
                </button>
              </div>
            )}
          </div>
        </div>

        <div>
          <label htmlFor="import-markdown-title" className="text-xs font-medium text-text-secondary block mb-1">
            {t('notebook.importMarkdownTitleLabel')}
          </label>
          <input
            id="import-markdown-title"
            type="text"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            maxLength={MAX_TITLE}
            disabled={mutation.isPending}
            className="w-full px-3 py-2 rounded-lg border border-border-default bg-surface text-sm text-text-primary outline-none focus:border-border-hover disabled:opacity-50"
          />
        </div>

        <div>
          <label htmlFor="import-markdown-parent" className="text-xs font-medium text-text-secondary block mb-1">
            {t('notebook.importMarkdownParentLabel')}
          </label>
          <select
            id="import-markdown-parent"
            value={parentPath ?? ''}
            onChange={(e) => setParentPath(e.target.value === '' ? null : e.target.value)}
            disabled={mutation.isPending}
            className="w-full px-3 py-2 rounded-lg border border-border-default bg-surface text-sm text-text-primary outline-none focus:border-border-hover disabled:opacity-50"
          >
            <option value="">{t('notebook.importMarkdownParentRoot')}</option>
            {folders.map((folder) => (
              <option key={folder.path} value={folder.path}>
                {`${'— '.repeat(folder.depth)}${folder.title}`}
              </option>
            ))}
          </select>
        </div>

        {localError && <p className="text-xs text-status-error">{localError}</p>}
        {stageLabel && mutation.isPending && (
          <p className="text-xs text-text-tertiary">{stageLabel}</p>
        )}

        <div className="flex items-center justify-end gap-2 pt-2">
          <Button
            type="button"
            variant="secondary"
            size="sm"
            onClick={handleClose}
            disabled={mutation.isPending}
          >
            {t('notebook.cancel')}
          </Button>
          <Button
            type="submit"
            variant="primary"
            size="sm"
            isLoading={mutation.isPending}
            disabled={!file || !title.trim()}
          >
            {t('notebook.importMarkdownSubmit')}
          </Button>
        </div>
      </form>
    </Modal>
  )
}

function flattenFolders(nodes: TreeNode[], depth = 0, out: FolderOption[] = []): FolderOption[] {
  for (const node of nodes) {
    if (node.item.type === 'folder') {
      out.push({ path: node.item.path, title: node.item.title, depth })
      flattenFolders(node.children, depth + 1, out)
    }
  }
  return out
}

function stripMarkdownExtension(name: string): string {
  return name.replace(/\.(md|markdown)$/i, '')
}

function formatBytes(bytes: number, t: (key: string) => string): string {
  if (bytes < 1024) return `${bytes} ${t('common.bytes')}`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} ${t('common.kilobytes')}`
  return `${(bytes / (1024 * 1024)).toFixed(2)} ${t('common.megabytes')}`
}
