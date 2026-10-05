import { lazy } from 'react'

/** Code-split pages: `lazy()` per route, resolved behind `Suspense`. */
export const HomePage = lazy(async () => ({ default: (await import('@/pages/home')).HomePage }))
