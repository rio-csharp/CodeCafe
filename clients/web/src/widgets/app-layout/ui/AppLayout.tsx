import { useEffect, useMemo } from 'react'
import { useLocation } from 'react-router-dom'
import { useUser } from '@/entities/user'
import { useTranslation } from 'react-i18next'
import Navbar from '@/widgets/navbar'
import Sidebar from '@/widgets/sidebar'
import RouteGuardSpinner from '@/shared/ui/RouteGuardSpinner'
import { LayoutContext } from '@/shared/model/layoutContext'
import { useSidebarStore } from '@/widgets/sidebar'

interface AppLayoutProps {
  children: React.ReactNode
}

export default function AppLayout({ children }: AppLayoutProps) {
  const location = useLocation()
  const { data: authData, isPending } = useUser()
  const isCollapsed = useSidebarStore((s) => s.isCollapsed)
  const { t } = useTranslation()

  const user = authData?.user ?? null
  // Memoized so LayoutContext consumers aren't re-rendered by a fresh value
  // object on every AppLayout render.
  const navbarValue = useMemo(() => ({ layout: 'navbar' as const, user }), [user])
  const sidebarValue = useMemo(() => ({ layout: 'sidebar' as const, user }), [user])

  // SPA route changes don't reset scroll or focus by themselves.
  const pathname = location.pathname
  useEffect(() => {
    window.scrollTo(0, 0)
    document.getElementById('main-content')?.focus({ preventScroll: true })
  }, [pathname])

  if (isPending) {
    return <RouteGuardSpinner />
  }

  const isAuthenticated = !!user

  const skipLink = (
    <a
      href="#main-content"
      className="sr-only focus:not-sr-only focus:absolute focus:top-2 focus:left-2 focus:z-[200] focus:rounded-lg focus:bg-surface focus:px-4 focus:py-2 focus:text-sm focus:font-medium focus:text-text-primary focus:shadow-lg focus:ring-2 focus:ring-brand-brown"
    >
      {t('common.skipToContent')}
    </a>
  )

  // Notebook reader routes render their own full-screen chrome
  const isNotebookRoute = /^\/notes\/[^/]+/.test(location.pathname)
  if (isNotebookRoute) {
    return (
      <LayoutContext.Provider value={navbarValue}>
        {children}
      </LayoutContext.Provider>
    )
  }

  if (isAuthenticated) {
    return (
      <LayoutContext.Provider value={sidebarValue}>
        <div className="min-h-screen bg-surface flex">
          {skipLink}
          <Sidebar />
          <main
            id="main-content"
            tabIndex={-1}
            className={`flex-1 transition-all duration-200 outline-none ${isCollapsed ? 'md:ml-16' : 'md:ml-[var(--sidebar-width)]'}`}
          >
            {children}
          </main>
        </div>
      </LayoutContext.Provider>
    )
  }

  return (
    <LayoutContext.Provider value={navbarValue}>
      <div className="min-h-screen bg-surface">
        {skipLink}
        <Navbar />
        <main id="main-content" tabIndex={-1} className="outline-none">
          {children}
        </main>
      </div>
    </LayoutContext.Provider>
  )
}
