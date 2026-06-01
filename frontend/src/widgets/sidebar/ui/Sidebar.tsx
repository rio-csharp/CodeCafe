import { useState, useRef, useEffect } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { Home, FileText, Code, ChevronDown, LogOut, PanelLeftClose, PanelLeftOpen } from 'lucide-react'
import logoIcon from '@/shared/assets/codecafe-icon.png'
import { useLogout } from '@/features/authenticate'
import { useLayout } from '@/shared/model/layoutContext'
import { useSidebarStore } from '../model/sidebarStore'

export default function Sidebar() {
  const location = useLocation()
  const navigate = useNavigate()
  const { user } = useLayout()
  const logout = useLogout()
  const [showUserMenu, setShowUserMenu] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)
  const isCollapsed = useSidebarStore((s) => s.isCollapsed)
  const toggle = useSidebarStore((s) => s.toggle)

  const isActive = (path: string) => {
    if (path === '/') return location.pathname === '/'
    return location.pathname.startsWith(path)
  }

  const navItems = [
    { to: '/dashboard', label: 'Workspace', icon: Home },
    { to: '/notes', label: 'Notes', icon: FileText },
    { to: '/codes', label: 'Codes', icon: Code },
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

  return (
    <aside
      className={`fixed left-0 top-0 h-screen bg-surface border-r border-border-subtle flex flex-col z-50 transition-all duration-200 ${
        isCollapsed ? 'w-16' : 'w-[var(--sidebar-width)]'
      }`}
    >
      {/* Toggle + Logo */}
      <div className={`pt-6 pb-4 flex flex-col items-center gap-3 ${isCollapsed ? 'px-2' : 'px-6'}`}>
        <button
          onClick={toggle}
          className="flex items-center justify-center h-8 w-8 rounded-lg hover:bg-surface-active transition-colors"
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
              {logout.isPending ? 'Logging out...' : 'Logout'}
            </button>
          </div>
        )}
      </div>
    </aside>
  )
}
