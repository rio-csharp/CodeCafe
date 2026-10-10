import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useEffect, useId, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { importNotebook, notebookKeys } from '@/entities/notebook'
import { MARKDOWN_FILE_ACCEPT, readMarkdownFile } from '@/shared/lib'
import { Button } from '@/shared/ui'

// How long the error bubble stays before dismissing itself.
const ERROR_DISMISS_MS = 4000

type ImportError = 'tooLarge' | 'unreadable' | 'failed'

/**
 * Sits next to "New notebook": pick a markdown file, get a whole notebook and
 * land inside it. The picker is a hidden input; the visible button forwards
 * the click so the trigger stays themeable.
 */
export function ImportNotebookButton() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const inputRef = useRef<HTMLInputElement>(null)
  const errorId = useId()
  const [error, setError] = useState<ImportError | null>(null)

  const mutation = useMutation({
    mutationFn: (data: { fileName: string; markdown: string }) => importNotebook(data),
    onSuccess: (notebook) => {
      void queryClient.invalidateQueries({ queryKey: notebookKeys.all })
      void navigate(`/notebooks/${encodeURIComponent(notebook.slug)}`)
    },
    onError: () => {
      setError('failed')
    },
  })

  useEffect(() => {
    if (error === null) {
      return
    }
    const timeout = setTimeout(() => {
      setError(null)
    }, ERROR_DISMISS_MS)
    return () => clearTimeout(timeout)
  }, [error])

  const pick = async (file: File | undefined) => {
    if (file === undefined) {
      return
    }
    setError(null)
    const read = await readMarkdownFile(file)
    if (!read.ok) {
      setError(read.reason)
      return
    }
    mutation.mutate({ fileName: read.fileName, markdown: read.markdown })
  }

  const errorText = error === 'tooLarge' ? t('import.tooLarge') : t('import.failed')

  return (
    <span className="relative inline-flex">
      <input
        ref={inputRef}
        type="file"
        accept={MARKDOWN_FILE_ACCEPT}
        className="hidden"
        aria-hidden="true"
        tabIndex={-1}
        onChange={(event) => {
          void pick(event.target.files?.[0])
          // Re-picking the same file must fire again.
          event.target.value = ''
        }}
      />
      <Button
        variant="ghost"
        size="sm"
        disabled={mutation.isPending}
        aria-describedby={error !== null ? errorId : undefined}
        onClick={() => {
          inputRef.current?.click()
        }}
        className="inline-flex items-center gap-1.5"
      >
        <svg
          viewBox="0 0 16 16"
          className="size-3.5"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.8"
          strokeLinecap="round"
          strokeLinejoin="round"
          aria-hidden="true"
        >
          <path d="M8 10V2.5M5.5 5 8 2.5 10.5 5M3 10.5V13a1 1 0 0 0 1 1h8a1 1 0 0 0 1-1v-2.5" />
        </svg>
        {mutation.isPending ? t('import.importing') : t('home.importNotebook')}
      </Button>
      {error !== null ? (
        <button
          type="button"
          id={errorId}
          role="alert"
          onClick={() => {
            setError(null)
          }}
          className="absolute top-full right-0 z-10 mt-1 w-52 cursor-pointer rounded-md border border-line bg-card p-2 text-left text-xs text-danger shadow-md"
        >
          {errorText}
        </button>
      ) : null}
    </span>
  )
}
