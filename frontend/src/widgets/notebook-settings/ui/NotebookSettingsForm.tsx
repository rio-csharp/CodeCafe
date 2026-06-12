import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useUpdateNotebook } from '@/features/edit-notebook'
import { useDeleteNotebook } from '@/features/delete-notebook'
import { useToast } from '@/shared/ui/Toast'
import { getErrorMessage } from '@/shared/lib/errorUtils'
import ErrorBoundary from '@/shared/ui/ErrorBoundary'
import { ErrorFallback } from '@/shared/ui/ErrorBoundary'
import VisibilityField from './VisibilityField'
import SettingsFormActions from './SettingsFormActions'
import type { Notebook } from '@/entities/notebook'

function useSettingsSchema() {
  const { t } = useTranslation()
  return z.object({
    title: z.string().min(1, t('notebook.titleRequired')),
    description: z.string(),
    visibility: z.enum(['private', 'unlisted', 'public']),
  })
}

type FormData = z.infer<ReturnType<typeof useSettingsSchema>>

interface NotebookSettingsFormProps {
  notebook: Notebook
  onSlugChange?: (newSlug: string) => void
  onDeleteSuccess?: () => void
  onCancel?: () => void
}

function NotebookSettingsFormComponent({
  notebook,
  onSlugChange,
  onDeleteSuccess,
  onCancel,
}: NotebookSettingsFormProps) {
  const update = useUpdateNotebook(notebook.id)
  const deleteNotebook = useDeleteNotebook()
  const { showToast } = useToast()
  const { t } = useTranslation()
  const schema = useSettingsSchema()
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
    update.mutate(
      { title: data.title.trim(), description: data.description.trim() || null, visibility: data.visibility },
      {
        onSuccess: (responseData) => {
          showToast(t('notebook.updated'))
          if (responseData.slug !== notebook.slug && onSlugChange) onSlugChange(responseData.slug)
        },
        onError: (err: unknown) => {
          showToast(getErrorMessage(err, t('notebook.updateFailed')), 'error')
        },
      },
    )
  }

  const handleDelete = () => {
    deleteNotebook.mutate(notebook.id, {
      onSuccess: () => {
        showToast(t('notebook.deleted'))
        onDeleteSuccess?.()
      },
      onError: (err: unknown) => {
        showToast(getErrorMessage(err, t('notebook.deleteFailed')), 'error')
      },
    })
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="mt-8 space-y-5">
      <div>
        <label htmlFor="notebook-title" className="block text-sm font-medium text-text-primary mb-1">{t('notebook.title')}</label>
        <input
          id="notebook-title"
          type="text"
          {...register('title')}
          placeholder={t('notebook.titlePlaceholder')}
          className="w-full rounded-lg border border-border-default bg-surface text-text-primary px-4 py-2.5 text-sm outline-none focus:border-border-hover"
          autoFocus
        />
        {errors.title && <p className="text-sm text-status-error mt-1">{errors.title.message}</p>}
      </div>

      <div>
        <label htmlFor="notebook-description" className="block text-sm font-medium text-text-primary mb-1">{t('notebook.description')}</label>
        <textarea
          id="notebook-description"
          {...register('description')}
          placeholder={t('notebook.descriptionPlaceholder')}
          rows={3}
          className="w-full rounded-lg border border-border-default bg-surface text-text-primary px-4 py-2.5 text-sm outline-none focus:border-border-hover resize-none"
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

export default function NotebookSettingsForm(props: NotebookSettingsFormProps) {
  return (
    <ErrorBoundary fallback={<ErrorFallback />}>
      <NotebookSettingsFormComponent {...props} />
    </ErrorBoundary>
  )
}
