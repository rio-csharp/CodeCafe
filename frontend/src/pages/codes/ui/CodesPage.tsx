import { useLayout } from '@/shared/model/layoutContext'

function CodesPage() {
  const { layout } = useLayout()

  return (
    <div className={layout === 'sidebar' ? 'p-8 lg:p-12' : 'pt-32 pb-20'}>
      <div className="mx-auto max-w-7xl px-6 lg:px-8">
        <h1 className="text-4xl font-bold text-text-primary">Codes</h1>
        <p className="mt-4 text-text-secondary">Coming soon.</p>
      </div>
    </div>
  )
}

export default CodesPage
