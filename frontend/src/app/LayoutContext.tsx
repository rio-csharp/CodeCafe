import { createContext, useContext } from 'react'
import type { User } from '../features/auth/types'

export type LayoutType = 'navbar' | 'sidebar'

export interface LayoutContextValue {
  layout: LayoutType
  user: User | null
}

export const LayoutContext = createContext<LayoutContextValue>({
  layout: 'navbar',
  user: null,
})

export function useLayout() {
  return useContext(LayoutContext)
}
