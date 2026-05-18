import { useLayout } from '../../app/LayoutContext'

function NotesPage() {
  const { layout } = useLayout()

  return (
    <div className={layout === 'sidebar' ? 'p-8 lg:p-12' : 'pt-32 pb-20'}>
      <div className="mx-auto max-w-7xl px-6 lg:px-8">
        <h1 className="text-4xl font-bold text-black">Notes</h1>
        <p className="mt-4 text-gray-500">Coming soon.</p>
      </div>
    </div>
  )
}

export default NotesPage
