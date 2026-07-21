import { useMutation, useQueryClient } from '@tanstack/react-query'
import { logout } from '../api/authApi'
import { AUTH_ME_KEY } from '@/entities/user'
import { notesKeys } from '@/entities/notebook'
import {
  AI_EDIT_THREAD_STORAGE_PREFIX,
  AI_THREAD_STORAGE_PREFIX,
  clearLocalStorageByPrefix,
} from '@/shared/lib/storageKeys'

export function useLogout() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: logout,
    onSuccess: async () => {
      await queryClient.cancelQueries({ queryKey: AUTH_ME_KEY })
      queryClient.setQueryData(AUTH_ME_KEY, null)
      queryClient.removeQueries({ queryKey: notesKeys.all })
      // AI threads contain notebook content the user pasted — don't leave
      // them behind for the next person on a shared device.
      clearLocalStorageByPrefix(AI_THREAD_STORAGE_PREFIX)
      clearLocalStorageByPrefix(AI_EDIT_THREAD_STORAGE_PREFIX)
    },
  })
}
