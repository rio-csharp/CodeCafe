import { useMutation, useQueryClient } from '@tanstack/react-query'
import { register } from '../api/authApi'
import type { RegisterRequest } from '@/entities/user'
import { AUTH_ME_KEY } from '@/entities/user'

export function useRegister() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: RegisterRequest) => register(data),
    onSuccess: (data) => {
      queryClient.setQueryData(AUTH_ME_KEY, data)
    },
  })
}
