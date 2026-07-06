import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useCreateNotebook } from './useCreateNotebook'

export type CreateNotebookFormData = z.infer<ReturnType<typeof useCreateNotebookSchema>>

function useCreateNotebookSchema() {
  const { t } = useTranslation()
  return z.object({
    title: z.string().min(1, t('notebook.titleRequired')),
    description: z.string(),
    visibility: z.enum(['private', 'unlisted', 'public']),
  })
}

export function useCreateNotebookForm(
  onSuccess: (slug: string) => void,
  onError: (message: string) => void,
) {
  const { t } = useTranslation()
  const schema = useCreateNotebookSchema()
  const create = useCreateNotebook()

  const {
    register,
    handleSubmit,
    control,
    formState: { errors },
  } = useForm<CreateNotebookFormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      title: '',
      description: '',
      visibility: 'private',
    },
  })

  const onSubmit = (data: CreateNotebookFormData) => {
    create.mutate(
      {
        title: data.title.trim(),
        description: data.description.trim(),
        visibility: data.visibility,
      },
      {
        onSuccess: (responseData) => onSuccess(responseData.slug),
        onError: (err: unknown) => onError(err instanceof Error ? err.message : t('notebook.createFailed')),
      },
    )
  }

  return {
    register,
    handleSubmit: handleSubmit(onSubmit),
    control,
    errors,
    isPending: create.isPending,
  }
}
