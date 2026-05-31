import { useNavigate } from 'react-router-dom'
import { ArrowLeft, Loader2 } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useCreateNotebook } from '../hooks/useNotesQueries'
import { useToast } from '@/components/ui/useToast'
import VisibilityField from '../components/notebook/VisibilityField'
import { getErrorMessage } from '@/lib/errorUtils'

const schema = z.object({
  title: z.string().min(1, 'Title is required'),
  description: z.string(),
  visibility: z.enum(['private', 'unlisted', 'public']),
})

type FormData = z.infer<typeof schema>

export default function CreateNotebookPage() {
  const navigate = useNavigate()
  const create = useCreateNotebook()
  const { showToast } = useToast()

  const {
    register,
    handleSubmit,
    control,
    formState: { errors },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      title: '',
      description: '',
      visibility: 'private',
    },
  })

  const onSubmit = (data: FormData) => {
    create.mutate(
      {
        title: data.title.trim(),
        description: data.description.trim(),
        visibility: data.visibility,
      },
      {
        onSuccess: (responseData) => {
          showToast('Notebook created')
          navigate(`/notes/${responseData.slug}`)
        },
        onError: (err: unknown) => {
          showToast(getErrorMessage(err, 'Failed to create notebook'), 'error')
        },
      },
    )
  }

  return (
    <div className="pt-24 pb-20 lg:pt-32 lg:pb-24">
      <div className="mx-auto max-w-xl px-6 lg:px-8">
        <button
          type="button"
          onClick={() => navigate('/notes')}
          className="inline-flex items-center gap-1 text-sm text-gray-500 hover:text-black transition-colors mb-6"
        >
          <ArrowLeft className="h-4 w-4" />
          Back to Notes
        </button>

        <h1 className="text-2xl font-bold text-black">Create Notebook</h1>
        <p className="mt-2 text-sm text-gray-500">
          Start a new knowledge base. You can add folders and pages inside.
        </p>

        <form onSubmit={handleSubmit(onSubmit)} className="mt-8 space-y-5">
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

          <VisibilityField control={control} />

          <div className="flex items-center gap-3 pt-2">
            <button
              type="submit"
              disabled={create.isPending}
              className="inline-flex items-center gap-2 rounded-lg bg-brand-brown px-6 py-2.5 text-sm font-medium text-white hover:opacity-90 transition-opacity disabled:opacity-50"
            >
              {create.isPending && <Loader2 className="h-4 w-4 animate-spin" />}
              Create Notebook
            </button>
            <button
              type="button"
              onClick={() => navigate('/notes')}
              className="rounded-lg border border-gray-200 px-6 py-2.5 text-sm font-medium text-gray-700 hover:bg-gray-50 transition-colors"
            >
              Cancel
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
