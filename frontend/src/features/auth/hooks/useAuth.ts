import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { login, register, logout, getMe } from '../api/authApi'
import type { AuthResponse, LoginRequest, RegisterRequest } from '../types'

export function useMe() {
  return useQuery<AuthResponse | null>({
    queryKey: ['auth', 'me'],
    queryFn: getMe,
    retry: false,
    staleTime: 5 * 60 * 1000,
  })
}

export function useLogin() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: LoginRequest) => login(data),
    onSuccess: (data) => {
      queryClient.setQueryData(['auth', 'me'], data)
    },
  })
}

export function useRegister() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: RegisterRequest) => register(data),
    onSuccess: (data) => {
      queryClient.setQueryData(['auth', 'me'], data)
    },
  })
}

export function useLogout() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: logout,
    onSuccess: () => {
      queryClient.cancelQueries({ queryKey: ['auth', 'me'] })
      queryClient.setQueryData(['auth', 'me'], null)
    },
  })
}
