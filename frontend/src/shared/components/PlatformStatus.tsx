type PlatformStatusProps = {
  label: string
  value: string
  tone?: 'ready' | 'planned' | 'checking' | 'offline'
}

export function PlatformStatus({
  label,
  value,
  tone = 'planned',
}: PlatformStatusProps) {
  return (
    <div className="status-item">
      <span className={`status-dot ${tone}`} aria-hidden="true" />
      <div>
        <span>{label}</span>
        <strong>{value}</strong>
      </div>
    </div>
  )
}
