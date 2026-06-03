import { useState, useRef, useEffect } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { Home, FileText, Code, ChevronDown, LogOut, PanelLeftClose, PanelLeftOpen, X, Menu } from 'lucide-react'
import logoIcon from '@/shared/assets/codecafe-icon.png'
import { useLogout } from '@/features/authenticate'
import { useLayout } from '@/shared/model/layoutContext'
import { useSidebarStore } from '../model/sidebarStore'
import { ThemeToggle } from '@/shared/ui/ThemeToggle'
import { LanguageSwitcher } from '@/shared/ui/LanguageSwitcher'
import { useTranslation } from 'react-i18next'

export default function Sidebar() {
  const location = useLocation()
  const navigate = useNavigate()
  const { user } = useLayout()
  const logout = useLogout()
  const [showUserMenu, setShowUserMenu] = useState(false)
  const [mobileOpen, setMobileOpen] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)
  const isCollapsed = useSidebarStore((s) => s.isCollapsed)
  const toggle = useSidebarStore((s) => s.toggle)
  const { t } = useTranslation()

  const isActive = (path: string) => {
    if (path === '/') return location.pathname === '/'
    return location.pathname.startsWith(path)
  }

  const navItems = [
    { to: '/dashboard', label: t('nav.workspace'), icon: Home },
    { to: '/notes', label: t('nav.notes'), icon: FileText },
    { to: '/codes', label: t('nav.codes'), icon: Code },
  ]

  const handleLogout = () => {
    logout.mutate(undefined, {
      onSuccess: () => navigate('/'),
    })
  }

  const displayName = user?.displayName || 'User'
  const email = user?.email || ''
  const initial = displayName.charAt(0).toUpperCase()

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setShowUserMenu(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  // Close mobile sidebar on route change
  useEffect(() => {
    setMobileOpen(false)
  }, [location.pathname])

  const sidebarContent = (
    <>
      {/* Toggle + Logo */}
      <div className={`pt-6 pb-4 flex flex-col items-center gap-3 ${isCollapsed ? 'px-2' : 'px-6'}`}>
        <button
          onClick={toggle}
          className="hidden md:flex items-center justify-center h-8 w-8 rounded-lg hover:bg-surface-hover transition-colors"
          aria-label={isCollapsed ? 'Expand sidebar' : 'Collapse sidebar'}
          title={isCollapsed ? 'Expand sidebar' : 'Collapse sidebar'}
        >
          {isCollapsed ? <PanelLeftOpen className="h-4 w-4 text-text-secondary" /> : <PanelLeftClose className="h-4 w-4 text-text-secondary" />}
        </button>
        <Link to="/dashboard" className="flex flex-col items-center gap-1">
          <img src={logoIcon} alt="CodeCafe" className="h-8 w-8" />
          {!isCollapsed && (
            <>
              <span className="text-lg font-bold text-text-primary tracking-tight">CodeCafe</span>
              <span className="text-xs text-text-tertiary">codes.cafe</span>
            </>
          )}
        </Link>
      </div>

      {/* Navigation */}
      <nav className={`flex-1 space-y-0.5 ${isCollapsed ? 'px-1.5' : 'px-3'}`}>
        {navItems.map(({ to, label, icon: Icon }) => (
          <Link
            key={to}
            to={to}
            className={`flex items-center rounded-lg text-sm transition-colors ${
              isCollapsed ? 'justify-center px-2 py-2.5' : 'gap-3 px-4 py-2.5'
            } ${
              isActive(to)
                ? 'bg-surface-active text-text-primary font-medium'
                : 'text-text-secondary hover:bg-surface-hover hover:text-text-primary'
            }`}
            title={isCollapsed ? label : undefined}
          >
            <Icon className="h-4 w-4 shrink-0" />
            {!isCollapsed && label}
          </Link>
        ))}
      </nav>

      {/* Tools */}
      <div className={`px-3 pb-2 ${isCollapsed ? 'px-1.5' : ''}`}>
        <div className={`flex items-center gap-1 ${isCollapsed ? 'justify-center' : ''}`}>
          <ThemeToggle />
          <LanguageSwitcher />
        </div>
      </div>

      {/* User */}
      <div className={`p-3 border-t border-border-subtle relative ${isCollapsed ? 'px-1.5' : ''}`} ref={menuRef}>
        <button
          onClick={() => setShowUserMenu(!showUserMenu)}
          aria-expanded={showUserMenu}
          className={`flex items-center w-full text-left hover:bg-surface-hover rounded-lg p-2 transition-colors ${
            isCollapsed ? 'justify-center' : 'gap-3'
          }`}
        >
          <div className="h-8 w-8 rounded-full bg-brand-brown flex items-center justify-center text-text-inverse text-sm font-medium shrink-0">
            {initial}
          </div>
          {!isCollapsed && (
            <>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-text-primary truncate">{displayName}</p>
                <p className="text-xs text-text-tertiary truncate">{email}</p>
              </div>
              <ChevronDown
                className={`h-4 w-4 text-text-tertiary transition-transform shrink-0 ${
                  showUserMenu ? 'rotate-180' : ''
                }`}
              />
            </>
          )}
        </button>

        {showUserMenu && (
          <div className={`absolute bottom-full mb-1 bg-surface border border-border-subtle rounded-lg shadow-lg py-1 ${isCollapsed ? 'left-1.5 w-40' : 'left-3 right-3'}`}>
            <button
              onClick={handleLogout}
              disabled={logout.isPending}
              className="flex items-center gap-2 w-full px-3 py-2 text-sm text-status-error hover:bg-surface-hover transition-colors"
            >
              <LogOut className="h-4 w-4" />
              {logout.isPending ? t('nav.loggingOut') : t('nav.logout')}
            </button>
          </div>
        )}
      </div>
    </>
  )

  return (
    <>
      {/* Mobile toggle */}
      <button
        onClick={() => setMobileOpen(true)}
        className="md:hidden fixed top-3 left-3 z-50 p-2 rounded-lg bg-surface border border-border-default shadow-sm"
        aria-label="Open sidebar"
      >
        <Menu className="h-5 w-5 text-text-primary" />
      </button>

      {/* Mobile overlay */}
      {mobileOpen && (
        <div
          className="md:hidden fixed inset-0 z-40 bg-black/40 backdrop-blur-sm"
          onClick={() => setMobileOpen(false)}
        />
      )}

      {/* Sidebar: mobile drawer + desktop fixed */}
      <aside
        className={`fixed left-0 top-0 h-screen bg-surface border-r border-border-subtle flex flex-col z-50 transition-all duration-200 ${
          isCollapsed ? 'md:w-16' : 'md:w-[var(--sidebar-width)]'
        } ${
          mobileOpen ? 'translate-x-0 w-[var(--sidebar-width)]' : '-translate-x-full md:translate-x-0'
        }`}
      >
        {/* Mobile close button inside sidebar */}
        <div className="md:hidden absolute top-2 right-2">
          <button
            onClick={() => setMobileOpen(false)}
            className="p-2 rounded-lg hover:bg-surface-hover transition-colors"
            aria-label="Close sidebar"
          >
            <X className="h-5 w-5 text-text-secondary" />
          </button>
        </div>
        {sidebarContent}
      </aside>
    </>
  )
}
