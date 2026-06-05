import { useState } from 'react'

interface DropZoneProps {
  onDrop: () => void
  className?: string
}

export default function DropZone({ onDrop, className = '' }: DropZoneProps) {
  const [isOver, setIsOver] = useState(false)

  return (
    <div
      role="separator"
      aria-label="Drop here to reorder"
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
