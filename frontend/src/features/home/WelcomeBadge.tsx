import HealthDot from './HealthDot'

function WelcomeBadge() {
  return (
    <span className="inline-flex items-center gap-2 rounded-full border border-gray-200 bg-white px-4 py-1.5 text-xs font-medium text-gray-500">
      <HealthDot />
      Welcome to CodeCafe
    </span>
  )
}

export default WelcomeBadge
