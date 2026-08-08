import { useMutation, useQueryClient } from '@tanstack/react-query'
import { register } from '../api/authApi'
import type { RegisterRequest } from '@/entities/user'
import { AUTH_ME_KEY } from '@/entities/user'
import { notesKeys } from '@/entities/notebook'
import {
  AI_EDIT_THREAD_STORAGE_PREFIX,
  AI_THREAD_STORAGE_PREFIX,
  clearLocalStorageByPrefix,
} from '@/shared/lib/storageKeys'

export function useRegister() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: RegisterRequest) => register(data),
    onSuccess: (data) => {
      queryClient.setQueryData(AUTH_ME_KEY, data)
      queryClient.invalidateQueries({ queryKey: notesKeys.all })
      // Same shared-device concern as useLogin: a new account must not see
      // AI threads the previous browser user left in localStorage.
      clearLocalStorageByPrefix(AI_THREAD_STORAGE_PREFIX)
      clearLocalStorageByPrefix(AI_EDIT_THREAD_STORAGE_PREFIX)
    },
  })
}
