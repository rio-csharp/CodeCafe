import { useEffect, useState } from 'react'

export interface ConfirmButtonProps {
  label: string
  confirmLabel: string
  onConfirm: () => void
  busy: boolean
}

/**
 * Destructive actions ask twice: the first click arms the button for a few
 * seconds, the second executes. Lighter than a modal, safer than one click.
 */
export function ConfirmButton({ label, confirmLabel, onConfirm, busy }: ConfirmButtonProps) {
  const [armed, setArmed] = useState(false)

  useEffect(() => {
    if (!armed) {
      return
    }
    const timer = setTimeout(() => {
      setArmed(false)
    }, 3000)
    return () => {
      clearTimeout(timer)
    }
  }, [armed])

  return (
    <button
      type="button"
      disabled={busy}
      onClick={() => {
        if (armed) {
          setArmed(false)
          onConfirm()
        } else {
          setArmed(true)
        }
      }}
      className={[
        'rounded-full px-4 py-1.5 text-xs font-medium transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent disabled:opacity-50',
        armed
          ? 'bg-danger text-card hover:opacity-90'
          : 'border border-line text-danger hover:bg-danger-soft',
      ].join(' ')}
    >
      {armed ? confirmLabel : label}
    </button>
  )
}
