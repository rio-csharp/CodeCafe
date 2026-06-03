import type { ReactNode } from 'react'
import logoIcon from '@/shared/assets/codecafe-icon.png'

interface AuthLayoutProps {
  title: string
  subtitle: string
  children: ReactNode
  footer: ReactNode
}

export default function AuthLayout({ title, subtitle, children, footer }: AuthLayoutProps) {
  return (
    <div className="min-h-screen bg-surface flex flex-col items-center justify-center px-4 py-12">
      <div className="w-full max-w-sm">
        <div className="flex flex-col items-center mb-10">
          <img src={logoIcon} alt="CodeCafe" className="h-12 w-12 mb-3" />
          <h1 className="text-2xl font-bold text-text-primary tracking-tight">CodeCafe</h1>
          <p className="text-xs text-text-tertiary mt-0.5">codes.cafe</p>
        </div>

        <div className="text-center mb-8">
          <h2 className="text-2xl font-semibold text-text-primary">{title}</h2>
          <p className="text-sm text-text-secondary mt-1">{subtitle}</p>
        </div>

        <div className="space-y-5">{children}</div>

        <div className="mt-8 text-center">{footer}</div>
      </div>
    </div>
  )
}
