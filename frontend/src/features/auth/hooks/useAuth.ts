import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { login, register, logout, getMe } from '../api/authApi'
import type { AuthResponse, LoginRequest, RegisterRequest } from '../types'

export const AUTH_ME_KEY = ['auth', 'me'] as const

export function useMe() {
  return useQuery<AuthResponse | null>({
    queryKey: AUTH_ME_KEY,
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
      queryClient.setQueryData(AUTH_ME_KEY, data)
    },
  })
}

export function useRegister() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: RegisterRequest) => register(data),
    onSuccess: (data) => {
      queryClient.setQueryData(AUTH_ME_KEY, data)
    },
  })
}

export function useLogout() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: logout,
    onSuccess: async () => {
      await queryClient.cancelQueries({ queryKey: AUTH_ME_KEY })
      queryClient.setQueryData(AUTH_ME_KEY, null)
    },
  })
}
