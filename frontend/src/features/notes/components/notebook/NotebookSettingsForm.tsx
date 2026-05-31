import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useUpdateNotebook, useDeleteNotebook } from '../../hooks/useNotesQueries'
import { useToast } from '@/components/ui/useToast'
import { getErrorMessage } from '@/lib/errorUtils'
import VisibilityField from './VisibilityField'
import SettingsFormActions from './SettingsFormActions'
import type { Notebook } from '../../types'

const schema = z.object({
  title: z.string().min(1, 'Title is required'),
  description: z.string(),
  visibility: z.enum(['private', 'unlisted', 'public']),
})

type FormData = z.infer<typeof schema>

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
      description: notebook.description ?? '',
      visibility: notebook.visibility,
    },
  })

  useEffect(() => {
    reset({ title: notebook.title, description: notebook.description ?? '', visibility: notebook.visibility })
  }, [notebook, reset])

  const onSubmit = (data: FormData) => {
    const isPublished = data.visibility === 'public'
    update.mutate(
      { title: data.title.trim(), description: data.description.trim() || null, visibility: data.visibility, isPublished },
      {
        onSuccess: (responseData) => {
          showToast('Notebook updated')
          if (responseData.slug !== notebook.slug && onSlugChange) onSlugChange(responseData.slug)
        },
        onError: (err: unknown) => {
          showToast(getErrorMessage(err, 'Failed to update notebook'), 'error')
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
        showToast(getErrorMessage(err, 'Failed to delete notebook'), 'error')
      },
    })
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="mt-8 space-y-5">
      <div>
        <label htmlFor="notebook-title" className="block text-sm font-medium text-black mb-1">Title</label>
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

      <div>
        <label htmlFor="notebook-description" className="block text-sm font-medium text-black mb-1">Description</label>
        <textarea
          id="notebook-description"
          {...register('description')}
          placeholder="What is this notebook about?"
          rows={3}
          className="w-full rounded-lg border border-gray-200 px-4 py-2.5 text-sm outline-none focus:border-gray-300 resize-none"
        />
      </div>

      <VisibilityField control={control} />

      <SettingsFormActions
        isPending={update.isPending}
        onCancel={onCancel}
        showDeleteConfirm={showDeleteConfirm}
        onShowDeleteConfirm={() => setShowDeleteConfirm(true)}
        onDelete={handleDelete}
        onCancelDelete={() => setShowDeleteConfirm(false)}
        isDeleting={deleteNotebook.isPending}
      />
    </form>
  )
}
