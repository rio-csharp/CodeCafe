import { Check, X } from 'lucide-react'

interface TreeRenameFieldProps {
  value: string
  onChange: (value: string) => void
  onConfirm: () => void
  onCancel: (e?: React.MouseEvent) => void
  onKeyDown: (e: React.KeyboardEvent) => void
  ariaLabel: string
}

export default function TreeRenameField({
  value,
  onChange,
  onConfirm,
  onCancel,
  onKeyDown,
  ariaLabel,
}: TreeRenameFieldProps) {
  return (
    <div className="flex items-center gap-1 flex-1 min-w-0">
      <input
        aria-label={ariaLabel}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        onKeyDown={onKeyDown}
        autoFocus
        className="flex-1 min-w-0 bg-white border border-gray-200 rounded px-1.5 py-0.5 text-[13px] outline-none focus:border-brand-brown"
        onClick={(e) => e.stopPropagation()}
      />
      <button type="button" onClick={onConfirm} className="p-0.5 text-green-600 hover:text-green-700">
        <Check className="h-3.5 w-3.5" />
      </button>
      <button type="button" onClick={(e) => onCancel(e)} className="p-0.5 text-gray-400 hover:text-gray-600">
        <X className="h-3.5 w-3.5" />
      </button>
    </div>
  )
}
