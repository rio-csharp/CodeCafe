import { useMutation, useQueryClient } from '@tanstack/react-query'
import { login } from '../api/authApi'
import type { LoginRequest } from '@/entities/user'
import { AUTH_ME_KEY } from '@/entities/user'
import { notesKeys } from '@/entities/notebook'
import {
  AI_EDIT_THREAD_STORAGE_PREFIX,
  AI_THREAD_STORAGE_PREFIX,
  clearLocalStorageByPrefix,
} from '@/shared/lib/storageKeys'

export function useLogin() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: LoginRequest) => login(data),
    onSuccess: (data) => {
      queryClient.setQueryData(AUTH_ME_KEY, data)
      queryClient.invalidateQueries({ queryKey: notesKeys.all })
      // A fresh login may be a different person on a shared device (the
      // previous session can expire without an explicit logout) — don't
      // expose the previous user's AI threads, they contain pasted content.
      clearLocalStorageByPrefix(AI_THREAD_STORAGE_PREFIX)
      clearLocalStorageByPrefix(AI_EDIT_THREAD_STORAGE_PREFIX)
    },
  })
}
