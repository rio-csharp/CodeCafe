import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useCreateNotebook } from './useCreateNotebook'

const schema = z.object({
  title: z.string().min(1, 'Title is required'),
  description: z.string(),
  visibility: z.enum(['private', 'unlisted', 'public']),
})

export type CreateNotebookFormData = z.infer<typeof schema>

export function useCreateNotebookForm(
  onSuccess: (slug: string) => void,
  onError: (message: string) => void,
) {
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
        onError: (err: unknown) => onError(err instanceof Error ? err.message : 'Failed to create notebook'),
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
