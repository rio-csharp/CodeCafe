import logoIcon from '@/shared/assets/codecafe-icon.png'
import { useLayout } from '@/shared/model/layoutContext'
import { DashboardCards } from '@/widgets/dashboard-cards'

export default function DashboardPage() {
  const { user } = useLayout()
  const displayName = user?.displayName || 'there'

  return (
    <div className="p-8 lg:p-12 max-w-6xl">
      <p className="text-text-secondary text-base">Welcome back, {displayName} 👋</p>
      <h1 className="mt-2 text-4xl font-bold text-text-primary tracking-tight">Your Workspace</h1>
      <p className="mt-3 text-text-secondary">Choose where you want to focus today.</p>

      <DashboardCards />

      <div className="mt-8 flex items-center gap-4 rounded-2xl border border-border-default bg-surface-elevated p-6">
        <img src={logoIcon} alt="CodeCafe" className="h-10 w-10 shrink-0" />
        <div>
          <p className="text-sm font-semibold text-text-primary">CodeCafe is just getting started.</p>
          <p className="text-sm text-text-secondary">More features are brewing. Stay tuned! ☕</p>
        </div>
      </div>
    </div>
  )
}
