import { createContext, useContext } from 'react'

export interface LayoutUser {
  id: string
  email: string
  displayName: string
}

export type LayoutType = 'navbar' | 'sidebar'

export interface LayoutContextValue {
  layout: LayoutType
  user: LayoutUser | null
}

export const LayoutContext = createContext<LayoutContextValue | undefined>(undefined)

export function useLayout() {
  const ctx = useContext(LayoutContext)
  if (!ctx) {
    throw new Error('useLayout must be used within a LayoutContext.Provider')
  }
  return ctx
}
