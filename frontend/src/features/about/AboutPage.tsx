import { useLayout } from '../../app/LayoutContext'
import { FileText, Code2, Sparkles, Coffee } from 'lucide-react'

function FeatureItem({ icon, title, status }: { icon: React.ReactNode; title: string; status: string }) {
  return (
    <div className="flex items-center gap-3">
      <div className="h-9 w-9 rounded-lg bg-gray-50 border border-gray-100 flex items-center justify-center shrink-0">
        {icon}
      </div>
      <div>
        <p className="text-sm font-medium text-black">{title}</p>
        <p className="text-xs text-gray-400">{status}</p>
      </div>
    </div>
  )
}

function AboutPage() {
  const { layout } = useLayout()

  return (
    <div className={layout === 'sidebar' ? 'p-8 lg:p-12' : 'pt-28 pb-20 lg:pt-32 lg:pb-24'}>
      <div className="mx-auto max-w-2xl px-6 lg:px-8">
        <div className="flex items-center gap-3 mb-6">
          <Coffee className="h-8 w-8 text-brand-brown" />
          <h1 className="text-3xl font-bold text-black">About CodeCafe</h1>
        </div>

        <p className="text-gray-500 leading-relaxed">
          CodeCafe is a minimal workspace for capturing ideas, organizing knowledge, and exploring code.
          Built for engineers who want a clean, distraction-free place to write and think.
        </p>

        <div className="mt-10">
          <h2 className="text-sm font-semibold text-gray-400 uppercase tracking-wider mb-4">Features</h2>
          <div className="space-y-4">
            <FeatureItem
              icon={<FileText className="h-4 w-4 text-gray-600" />}
              title="Notes"
              status="Available — Write, organize, and share structured notebooks with folders and pages."
            />
            <FeatureItem
              icon={<Code2 className="h-4 w-4 text-gray-600" />}
              title="Codes"
              status="Coming soon — Explore and understand code with AI assistance."
            />
            <FeatureItem
              icon={<Sparkles className="h-4 w-4 text-gray-600" />}
              title="AI Assistant"
              status="Coming soon — Ask questions, get summaries, and improve your writing."
            />
          </div>
        </div>

        <div className="mt-10">
          <h2 className="text-sm font-semibold text-gray-400 uppercase tracking-wider mb-4">Tech Stack</h2>
          <div className="flex flex-wrap gap-2">
            {['React 19', 'Vite', 'Tailwind CSS', 'TipTap', 'TanStack Query', 'ASP.NET Core', 'PostgreSQL'].map(
              (tech) => (
                <span key={tech} className="rounded-full bg-gray-50 border border-gray-100 px-3 py-1 text-xs text-gray-600">
                  {tech}
                </span>
              ),
            )}
          </div>
        </div>
      </div>
    </div>
  )
}

export default AboutPage
