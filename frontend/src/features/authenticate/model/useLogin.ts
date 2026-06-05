import { useMutation, useQueryClient } from '@tanstack/react-query'
import { login } from '../api/authApi'
import type { LoginRequest } from '@/entities/user'
import { AUTH_ME_KEY } from '@/entities/user'
import { notesKeys } from '@/entities/notebook'

export function useLogin() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: LoginRequest) => login(data),
    onSuccess: (data) => {
      queryClient.setQueryData(AUTH_ME_KEY, data)
      queryClient.invalidateQueries({ queryKey: notesKeys.all })
    },
  })
}
