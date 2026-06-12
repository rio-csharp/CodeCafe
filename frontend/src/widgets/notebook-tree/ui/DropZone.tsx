import { useState } from 'react'
import { useTranslation } from 'react-i18next'

interface DropZoneProps {
  onDrop: () => void
  className?: string
}

export default function DropZone({ onDrop, className = '' }: DropZoneProps) {
  const { t } = useTranslation()
  const [isOver, setIsOver] = useState(false)

  return (
    <div
      role="separator"
      aria-label={t('common.dropHere')}
      aria-orientation="horizontal"
      className={`transition-all ${isOver ? 'h-2 bg-brand-brown/40 rounded-sm' : 'h-0.5'} ${className}`}
      onDragOver={(e) => {
        e.preventDefault()
        e.stopPropagation()
        setIsOver(true)
      }}
      onDragLeave={() => setIsOver(false)}
      onDrop={(e) => {
        e.preventDefault()
        e.stopPropagation()
        setIsOver(false)
        onDrop()
      }}
    />
  )
}
