import HealthDot from '@/widgets/health-status'

function WelcomeBadge() {
  return (
    <span className="inline-flex items-center gap-2 rounded-full border border-border-default bg-surface px-4 py-1.5 text-xs font-medium text-text-secondary">
      <HealthDot />
      Welcome to CodeCafe
    </span>
  )
}

export default WelcomeBadge
