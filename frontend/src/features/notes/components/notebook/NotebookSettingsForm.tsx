import { useEffect, useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Loader2, Trash2, AlertTriangle } from 'lucide-react'
import { useUpdateNotebook, useDeleteNotebook } from '../../hooks/useNotesQueries'
import { useToast } from '../../../../components/ui/Toast'
import type { Notebook, NotebookVisibility } from '../../types'

const schema = z.object({
  title: z.string().min(1, 'Title is required'),
  description: z.string(),
  visibility: z.enum(['private', 'unlisted', 'public']),
})

type FormData = z.infer<typeof schema>

const VISIBILITY_OPTIONS: { value: NotebookVisibility; label: string }[] = [
  { value: 'private', label: 'Private' },
  { value: 'unlisted', label: 'Unlisted' },
  { value: 'public', label: 'Public' },
]

const visibilityHelp: Record<NotebookVisibility, string> = {
  public: 'This notebook will be published and visible to everyone.',
  unlisted: 'Only people with the link can access this notebook.',
  private: 'Only you can access this notebook.',
}

interface NotebookSettingsFormProps {
  notebook: Notebook
  onSlugChange?: (newSlug: string) => void
  onDeleteSuccess?: () => void
  onCancel?: () => void
}

export default function NotebookSettingsForm({
  notebook,
  onSlugChange,
  onDeleteSuccess,
  onCancel,
}: NotebookSettingsFormProps) {
  const update = useUpdateNotebook(notebook.id)
  const deleteNotebook = useDeleteNotebook()
  const { showToast } = useToast()
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
    control,
  } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      title: notebook.title,
      description: notebook.description,
      visibility: notebook.visibility,
    },
  })

  useEffect(() => {
    reset({
      title: notebook.title,
      description: notebook.description,
      visibility: notebook.visibility,
    })
  }, [notebook, reset])

  const visibility = useWatch({ control, name: 'visibility' })

  const onSubmit = (data: FormData) => {
    const isPublished = data.visibility === 'public'
    update.mutate(
      {
        title: data.title.trim(),
        description: data.description.trim(),
        visibility: data.visibility,
        isPublished,
      },
      {
        onSuccess: (responseData) => {
          showToast('Notebook updated')
          if (responseData.slug !== notebook.slug && onSlugChange) {
            onSlugChange(responseData.slug)
          }
        },
        onError: (err: unknown) => {
          const msg = err instanceof Error ? err.message : 'Failed to update notebook'
          showToast(msg, 'error')
        },
      },
    )
  }

  const handleDelete = () => {
    deleteNotebook.mutate(notebook.id, {
      onSuccess: () => {
        showToast('Notebook deleted')
        onDeleteSuccess?.()
      },
      onError: (err: unknown) => {
        const msg = err instanceof Error ? err.message : 'Failed to delete notebook'
        showToast(msg, 'error')
      },
    })
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="mt-8 space-y-5">
      {/* Title */}
      <div>
        <label htmlFor="notebook-title" className="block text-sm font-medium text-black mb-1">
          Title
        </label>
        <input
          id="notebook-title"
          type="text"
          {...register('title')}
          placeholder="e.g., System Design Notes"
          className="w-full rounded-lg border border-gray-200 px-4 py-2.5 text-sm outline-none focus:border-gray-300"
          autoFocus
        />
        {errors.title && <p className="text-sm text-red-600 mt-1">{errors.title.message}</p>}
      </div>

      {/* Description */}
      <div>
        <label htmlFor="notebook-description" className="block text-sm font-medium text-black mb-1">
          Description
        </label>
        <textarea
          id="notebook-description"
          {...register('description')}
          placeholder="What is this notebook about?"
          rows={3}
          className="w-full rounded-lg border border-gray-200 px-4 py-2.5 text-sm outline-none focus:border-gray-300 resize-none"
        />
      </div>

      {/* Visibility */}
      <div>
        <span className="block text-sm font-medium text-black mb-1">Visibility</span>
        <div className="flex items-center gap-3 flex-wrap">
          {VISIBILITY_OPTIONS.map((opt) => (
            <label
              key={opt.value}
              htmlFor={`visibility-${opt.value}`}
              className={`flex items-center gap-2 rounded-lg border px-4 py-2 text-sm cursor-pointer transition-colors ${
                visibility === opt.value
                  ? 'border-brand-brown bg-stone-50 text-black'
                  : 'border-gray-200 text-gray-600 hover:bg-gray-50'
              }`}
            >
              <input
                id={`visibility-${opt.value}`}
                type="radio"
                {...register('visibility')}
                value={opt.value}
                className="accent-brand-brown"
              />
              {opt.label}
            </label>
          ))}
        </div>
        <p className="mt-2 text-xs text-gray-400">{visibilityHelp[visibility]}</p>
      </div>

      {/* Actions */}
      <div className="flex items-center justify-between pt-2">
        <div className="flex items-center gap-3">
          <button
            type="submit"
            disabled={update.isPending}
            className="inline-flex items-center gap-2 rounded-lg bg-brand-brown px-6 py-2.5 text-sm font-medium text-white hover:opacity-90 transition-opacity disabled:opacity-50"
          >
            {update.isPending && <Loader2 className="h-4 w-4 animate-spin" />}
            Save Changes
          </button>
          {onCancel && (
            <button
              type="button"
              onClick={onCancel}
              className="rounded-lg border border-gray-200 px-6 py-2.5 text-sm font-medium text-gray-700 hover:bg-gray-50 transition-colors"
            >
              Cancel
            </button>
          )}
        </div>

        {!showDeleteConfirm ? (
          <button
            type="button"
            onClick={() => setShowDeleteConfirm(true)}
            className="inline-flex items-center gap-1.5 text-sm text-red-600 hover:text-red-700 transition-colors"
          >
            <Trash2 className="h-4 w-4" />
            Delete
          </button>
        ) : (
          <div className="flex items-center gap-2">
            <span className="text-xs text-red-600 flex items-center gap-1">
              <AlertTriangle className="h-3.5 w-3.5" />
              Sure?
            </span>
            <button
              type="button"
              onClick={handleDelete}
              disabled={deleteNotebook.isPending}
              className="rounded-lg bg-red-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-red-700 transition-colors disabled:opacity-50"
            >
              {deleteNotebook.isPending ? 'Deleting...' : 'Yes, delete'}
            </button>
            <button
              type="button"
              onClick={() => setShowDeleteConfirm(false)}
              className="rounded-lg border border-gray-200 px-3 py-1.5 text-xs font-medium text-gray-700 hover:bg-gray-50 transition-colors"
            >
              Cancel
            </button>
          </div>
        )}
      </div>
    </form>
  )
}
