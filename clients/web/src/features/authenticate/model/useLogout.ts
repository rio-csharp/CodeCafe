import { useMutation, useQueryClient } from '@tanstack/react-query'
import { logout } from '../api/authApi'
import { AUTH_ME_KEY } from '@/entities/user'
import { notesKeys } from '@/entities/notebook'

export function useLogout() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: logout,
    onSuccess: async () => {
      await queryClient.cancelQueries({ queryKey: AUTH_ME_KEY })
      queryClient.setQueryData(AUTH_ME_KEY, null)
      queryClient.removeQueries({ queryKey: notesKeys.all })
    },
  })
}
