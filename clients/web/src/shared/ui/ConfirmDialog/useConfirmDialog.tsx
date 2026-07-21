import { useCallback, useRef, useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Modal } from '@/shared/ui/Modal'

interface ConfirmOptions {
  title: string
  description?: string
  confirmLabel?: string
  danger?: boolean
}

/**
 * Promise-based replacement for window.confirm().
 * const { requestConfirm, confirmDialog } = useConfirmDialog()
 * if (!(await requestConfirm({ title, danger: true }))) return
 * Render {confirmDialog} once in the component tree.
 */
export function useConfirmDialog() {
  const { t } = useTranslation()
  const [options, setOptions] = useState<ConfirmOptions | null>(null)
  const resolverRef = useRef<((value: boolean) => void) | null>(null)

  const requestConfirm = useCallback((next: ConfirmOptions) => {
    setOptions(next)
    return new Promise<boolean>((resolve) => {
      resolverRef.current = resolve
    })
  }, [])

  const settle = useCallback((result: boolean) => {
    setOptions(null)
    resolverRef.current?.(result)
    resolverRef.current = null
  }, [])

  const confirmDialog: ReactNode = options ? (
    <Modal isOpen onClose={() => settle(false)} title={options.title}>
      {options.description && (
        <p className="text-sm text-text-secondary leading-relaxed">{options.description}</p>
      )}
      <div className="mt-6 flex justify-end gap-3">
        <button
          type="button"
          onClick={() => settle(false)}
          className="rounded-lg border border-border-default px-4 py-2 text-sm font-medium text-text-secondary hover:bg-surface-hover transition-colors"
        >
          {t('common.cancel')}
        </button>
        <button
          type="button"
          onClick={() => settle(true)}
          className={`rounded-lg px-4 py-2 text-sm font-medium transition-colors ${
            options.danger
              ? 'bg-status-error text-text-inverse hover:bg-status-error-hover'
              : 'bg-text-primary text-text-inverse hover:bg-surface-inverse-hover'
          }`}
        >
          {options.confirmLabel ?? t('common.confirm')}
        </button>
      </div>
    </Modal>
  ) : null

  return { requestConfirm, confirmDialog }
}
