import { useQuery } from '@tanstack/react-query'
import { getMe } from '../api/userApi'
import type { AuthResponse } from './types'

export const AUTH_ME_KEY = ['auth', 'me'] as const

export function useUser() {
  return useQuery<AuthResponse | null>({
    queryKey: AUTH_ME_KEY,
    queryFn: ({ signal }) => getMe(signal),
    retry: false,
    staleTime: 5 * 60 * 1000,
  })
}
