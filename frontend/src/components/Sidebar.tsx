import { useState, useRef, useEffect } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { Home, FileText, Code, ChevronDown, LogOut } from 'lucide-react'
import logoIcon from '../assets/codecafe-icon.png'
import { useLogout } from '../features/auth/hooks/useAuth'
import { useLayout } from '../app/LayoutContext'

export default function Sidebar() {
  const location = useLocation()
  const navigate = useNavigate()
  const { user } = useLayout()
  const logout = useLogout()
  const [showUserMenu, setShowUserMenu] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)

  const isActive = (path: string) => location.pathname === path

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
      className="fixed left-0 top-0 h-screen bg-white border-r border-gray-100 flex flex-col z-50"
      style={{ width: 'var(--sidebar-width)' }}
    >
      {/* Logo */}
      <div className="pt-8 pb-6 px-6">
        <Link to="/" className="flex flex-col items-center gap-1">
          <img src={logoIcon} alt="CodeCafe" className="h-10 w-10" />
          <span className="text-xl font-bold text-black tracking-tight">CodeCafe</span>
          <span className="text-xs text-gray-400">codes.cafe</span>
        </Link>
      </div>

      {/* Navigation */}
      <nav className="flex-1 px-3 space-y-0.5">
        {navItems.map(({ to, label, icon: Icon }) => (
          <Link
            key={to}
            to={to}
            className={`flex items-center gap-3 px-4 py-2.5 rounded-lg text-sm transition-colors ${
              isActive(to)
                ? 'bg-stone-100 text-stone-800 font-medium'
                : 'text-gray-600 hover:bg-gray-50 hover:text-black'
            }`}
          >
            <Icon className="h-4 w-4" />
            {label}
          </Link>
        ))}
      </nav>

      {/* User */}
      <div className="p-3 border-t border-gray-100 relative" ref={menuRef}>
        <button
          onClick={() => setShowUserMenu(!showUserMenu)}
          className="flex items-center gap-3 w-full text-left hover:bg-gray-50 rounded-lg p-2 transition-colors"
        >
          <div className="h-8 w-8 rounded-full bg-brand-brown flex items-center justify-center text-white text-sm font-medium shrink-0">
            {initial}
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium text-black truncate">{displayName}</p>
            <p className="text-xs text-gray-400 truncate">{email}</p>
          </div>
          <ChevronDown
            className={`h-4 w-4 text-gray-400 transition-transform shrink-0 ${
              showUserMenu ? 'rotate-180' : ''
            }`}
          />
        </button>

        {showUserMenu && (
          <div className="absolute bottom-full left-3 right-3 mb-1 bg-white border border-gray-100 rounded-lg shadow-lg py-1">
            <button
              onClick={handleLogout}
              disabled={logout.isPending}
              className="flex items-center gap-2 w-full px-3 py-2 text-sm text-red-600 hover:bg-gray-50 transition-colors"
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
