import { Link } from 'react-router-dom'
import { FileText, Code, ArrowRight } from 'lucide-react'
import logoIcon from '@/assets/codecafe-icon.png'
import { useLayout } from '@/app/LayoutContext'
import NotesIllustration from '../components/NotesIllustration'
import CodesIllustration from '../components/CodesIllustration'

const CARDS = [
  {
    to: '/notes',
    label: 'Notes',
    desc: 'Write, organize, and revisit your knowledge. Capture ideas, create tutorials, and build your personal knowledge base.',
    icon: FileText,
    illustration: NotesIllustration,
    btnText: 'Open Notes',
  },
  {
    to: '/codes',
    label: 'Codes',
    desc: 'Read and understand code faster with AI-assisted explanations. Explore, learn, and ship better code.',
    icon: Code,
    illustration: CodesIllustration,
    btnText: 'Open Codes',
  },
] as const

export default function DashboardPage() {
  const { user } = useLayout()
  const displayName = user?.displayName || 'there'

  return (
    <div className="p-8 lg:p-12 max-w-6xl">
      <p className="text-gray-600 text-base">Welcome back, {displayName} 👋</p>
      <h1 className="mt-2 text-4xl font-bold text-black tracking-tight">Your Workspace</h1>
      <p className="mt-3 text-gray-500">Choose where you want to focus today.</p>

      <div className="mt-10 grid gap-6 lg:grid-cols-2">
        {CARDS.map(({ to, label, desc, icon: Icon, illustration: Illustration, btnText }) => (
          <div
            key={to}
            className="group relative flex flex-col rounded-2xl border border-gray-200 bg-white p-8 transition-all hover:border-gray-300 hover:shadow-sm"
          >
            <div className="flex items-start justify-between gap-6">
              <div className="flex-1 min-w-0">
                <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-gray-100">
                  <Icon className="h-6 w-6 text-gray-700" />
                </div>
                <h2 className="mt-6 text-2xl font-semibold text-black">{label}</h2>
                <p className="mt-2 text-sm text-gray-500 leading-relaxed max-w-xs">{desc}</p>
              </div>
              <Illustration />
            </div>

            <div className="mt-8">
              <Link
                to={to}
                className="inline-flex items-center justify-center gap-2 rounded-lg bg-brand-brown px-6 py-2.5 text-sm font-medium text-white hover:opacity-90 transition-opacity w-40"
              >
                {btnText}
                <ArrowRight className="h-4 w-4" />
              </Link>
            </div>
          </div>
        ))}
      </div>

      <div className="mt-8 flex items-center gap-4 rounded-2xl border border-gray-200 bg-stone-50 p-6">
        <img src={logoIcon} alt="CodeCafe" className="h-10 w-10 shrink-0" />
        <div>
          <p className="text-sm font-semibold text-black">CodeCafe is just getting started.</p>
          <p className="text-sm text-gray-500">More features are brewing. Stay tuned! ☕</p>
        </div>
      </div>
    </div>
  )
}
