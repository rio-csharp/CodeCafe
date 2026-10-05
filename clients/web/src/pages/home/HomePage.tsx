import { useState } from 'react'
import { Hero } from '@/widgets/hero'
import { NotebookCatalog } from '@/widgets/notebook-catalog'
import { SiteFooter } from '@/widgets/site-footer'
import { SiteHeader } from '@/widgets/site-header'

export function HomePage() {
  const [search, setSearch] = useState('')

  return (
    <div className="flex min-h-dvh flex-col bg-canvas">
      <SiteHeader />

      <main className="flex-1">
        <Hero value={search} onChange={setSearch} />
        <NotebookCatalog search={search} />
      </main>

      <SiteFooter />
    </div>
  )
}
