import { lazy } from 'react'

/** Code-split pages: `lazy()` per route, resolved behind `Suspense`. */
export const HomePage = lazy(async () => ({ default: (await import('@/pages/home')).HomePage }))
export const LoginPage = lazy(async () => ({ default: (await import('@/pages/login')).LoginPage }))
export const RegisterPage = lazy(async () => ({
  default: (await import('@/pages/register')).RegisterPage,
}))
export const NotebookReaderPage = lazy(async () => ({
  default: (await import('@/pages/notebook')).NotebookReaderPage,
}))
export const TrashPage = lazy(async () => ({ default: (await import('@/pages/trash')).TrashPage }))
export const SearchPage = lazy(async () => ({
  default: (await import('@/pages/search')).SearchPage,
}))
export const AccountPage = lazy(async () => ({
  default: (await import('@/pages/account')).AccountPage,
}))
