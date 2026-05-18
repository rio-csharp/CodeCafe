import { Link } from 'react-router-dom'
import { FileText, Code, ArrowRight } from 'lucide-react'
import { useMe } from '../../auth/hooks/useAuth'

function NotesIllustration() {
  return (
    <div className="relative w-36 h-28 shrink-0">
      <div className="absolute inset-0 bg-white rounded-xl border border-gray-100 shadow-sm p-2.5 flex flex-col gap-1.5">
        <div className="flex items-center gap-1.5">
          <div className="h-2 w-2 rounded-full bg-brand-brown/30" />
          <div className="h-1.5 w-14 bg-gray-100 rounded-full" />
        </div>
        <div className="h-px bg-gray-50" />
        <div className="flex items-center gap-1.5">
          <div className="h-2 w-2 rounded-full bg-gray-100" />
          <div className="h-1.5 w-20 bg-gray-100 rounded-full" />
        </div>
        <div className="flex items-center gap-1.5">
          <div className="h-2 w-2 rounded-full bg-gray-100" />
          <div className="h-1.5 w-16 bg-gray-100 rounded-full" />
        </div>
        <div className="flex items-center gap-1.5">
          <div className="h-2 w-2 rounded-full bg-gray-100" />
          <div className="h-1.5 w-12 bg-gray-100 rounded-full" />
        </div>
      </div>
      {/* Overlapping card effect */}
      <div className="absolute -right-2 -top-1 w-24 h-20 bg-white rounded-lg border border-gray-100 shadow-md p-2 flex flex-col gap-1">
        <div className="h-1 w-8 bg-brand-brown/30 rounded-full" />
        <div className="h-px bg-gray-50" />
        <div className="flex items-center gap-1.5">
          <div className="h-1.5 w-1.5 rounded-full bg-brand-brown/20" />
          <div className="h-1 w-10 bg-gray-100 rounded-full" />
        </div>
        <div className="flex items-center gap-1.5">
          <div className="h-1.5 w-1.5 rounded-full bg-gray-100" />
          <div className="h-1 w-8 bg-gray-100 rounded-full" />
        </div>
      </div>
    </div>
  )
}

function CodesIllustration() {
  return (
    <div className="relative w-36 h-28 shrink-0">
      <div className="absolute inset-0 bg-white rounded-xl border border-gray-100 shadow-sm p-2 flex flex-col gap-1">
        <div className="flex items-center gap-1 text-[10px] text-gray-300 font-mono">
          <span>&lt;/&gt;</span>
        </div>
        {[1, 2, 3, 4].map((i) => (
          <div key={i} className="flex items-center gap-1.5">
            <span className="text-[9px] text-gray-300 w-3 text-right">{i}</span>
            <div
              className={`h-0.5 rounded-full ${
                i === 1 ? 'w-14 bg-brand-brown/20' : i === 3 ? 'w-10 bg-brand-brown/20' : 'w-12 bg-gray-100'
              }`}
            />
          </div>
        ))}
      </div>
      <div className="absolute -right-2 -top-1 w-24 h-20 bg-white rounded-lg border border-gray-100 shadow-md p-2">
        <div className="flex items-center gap-1 text-[10px] text-gray-300 font-mono mb-1">
          <span>&lt;/&gt;</span>
        </div>
        {[1, 2, 3].map((i) => (
          <div key={i} className="flex items-center gap-1.5 mb-1">
            <span className="text-[9px] text-gray-300 w-3 text-right">{i}</span>
            <div
              className={`h-0.5 rounded-full ${
                i === 2 ? 'w-8 bg-brand-brown/30' : 'w-10 bg-gray-100'
              }`}
            />
          </div>
        ))}
      </div>
    </div>
  )
}

export default function DashboardPage() {
  const { data } = useMe()
  const displayName = data?.user?.displayName || 'there'

  const cards = [
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
  ]

  return (
    <div className="p-8 lg:p-12 max-w-6xl">
      <p className="text-gray-600 text-base">Welcome back, {displayName} 👋</p>
      <h1 className="mt-2 text-4xl font-bold text-black tracking-tight">Your Workspace</h1>
      <p className="mt-3 text-gray-500">Choose where you want to focus today.</p>

      <div className="mt-10 grid gap-6 lg:grid-cols-2">
        {cards.map(({ to, label, desc, icon: Icon, illustration: Illustration, btnText }) => (
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
    </div>
  )
}
