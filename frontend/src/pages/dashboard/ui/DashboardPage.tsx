import logoIcon from '@/shared/assets/codecafe-icon.png'
import { useLayout } from '@/shared/model/layoutContext'
import { DashboardCards } from '@/widgets/dashboard-cards'

export default function DashboardPage() {
  const { user } = useLayout()
  const displayName = user?.displayName || 'there'

  return (
    <div className="p-8 lg:p-12 max-w-6xl">
      <p className="text-text-secondary text-base">Welcome back, {displayName}.</p>
      <h1 className="mt-2 text-4xl font-bold text-text-primary tracking-tight">Your knowledge workspace</h1>
      <p className="mt-3 text-text-secondary">Pick up a notebook, or check what is shipping next.</p>

      <DashboardCards />

      <div className="mt-8 flex items-center gap-4 rounded-2xl border border-border-default bg-surface-elevated p-6">
        <img src={logoIcon} alt="CodeCafe" className="h-10 w-10 shrink-0" />
        <div>
          <p className="text-sm font-semibold text-text-primary">Notes are live today.</p>
          <p className="text-sm text-text-secondary">MCP workflows are available now, and the Codes workspace is still taking shape.</p>
        </div>
      </div>
    </div>
  )
}
