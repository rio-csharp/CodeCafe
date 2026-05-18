import { useState, useEffect } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import logoIcon from '../assets/codecafe-icon.png'
import { useLogout } from '../features/auth/hooks/useAuth'
import { useLayout } from '../app/LayoutContext'

function Navbar() {
  const [scrolled, setScrolled] = useState(false)
  const location = useLocation()
  const navigate = useNavigate()
  const { user } = useLayout()
  const logout = useLogout()

  useEffect(() => {
    const handleScroll = () => setScrolled(window.scrollY > 10)
    window.addEventListener('scroll', handleScroll, { passive: true })
    return () => window.removeEventListener('scroll', handleScroll)
  }, [])

  const isActive = (path: string) => {
    if (path === '/') return location.pathname === '/'
    return location.pathname.startsWith(path)
  }

  const handleLogout = () => {
    logout.mutate(undefined, {
      onSuccess: () => navigate('/'),
    })
  }

  return (
    <nav
      className={`fixed top-0 left-0 right-0 z-50 transition-all duration-300 ${
        scrolled
          ? 'bg-white/70 backdrop-blur-xl border-b border-gray-200/50 shadow-sm shadow-gray-200/20'
          : 'bg-transparent'
      }`}
    >
      <div className="mx-auto max-w-7xl px-6 lg:px-8">
        <div className="flex h-16 items-center justify-between">
          <Link
            to="/"
            className="flex items-center gap-2 text-lg font-bold text-black tracking-tight hover:opacity-70 transition-opacity"
          >
            <img src={logoIcon} alt="CodeCafe" className="h-7 w-7" />
            CodeCafe
          </Link>

          <div className="hidden md:flex items-center gap-8">
            {[
              { to: '/notes', label: 'Notes' },
              { to: '/codes', label: 'Codes' },
              { to: '/about', label: 'About' },
            ].map(({ to, label }) => (
              <Link
                key={to}
                to={to}
                className={`relative text-sm transition-colors group ${
                  isActive(to) ? 'text-black font-medium' : 'text-gray-500 hover:text-black'
                }`}
              >
                {label}
                <span
                  className={`absolute -bottom-1 left-0 h-0.5 bg-black transition-all duration-300 ${
                    isActive(to) ? 'w-full' : 'w-0 group-hover:w-full'
                  }`}
                />
              </Link>
            ))}
            <a
              href="https://github.com/rio-csharp/CodeCafe"
              target="_blank"
              rel="noopener noreferrer"
              className="text-sm text-gray-500 hover:text-black transition-colors"
            >
              Github
            </a>
          </div>

          <div className="flex items-center gap-3">
            {user ? (
              <>
                <Link
                  to="/dashboard"
                  className={`relative text-sm transition-colors group hidden md:block ${
                    isActive('/dashboard')
                      ? 'text-black font-medium'
                      : 'text-gray-500 hover:text-black'
                  }`}
                >
                  Dashboard
                  <span
                    className={`absolute -bottom-1 left-0 h-0.5 bg-black transition-all duration-300 ${
                      isActive('/dashboard') ? 'w-full' : 'w-0 group-hover:w-full'
                    }`}
                  />
                </Link>
                <button
                  onClick={handleLogout}
                  disabled={logout.isPending}
                  className="inline-flex items-center gap-1 rounded-lg bg-black px-5 py-2 text-sm font-medium text-white hover:bg-gray-800 transition-colors duration-200 disabled:opacity-50"
                >
                  {logout.isPending ? '...' : 'Logout'}
                </button>
              </>
            ) : (
              <Link
                to="/login"
                className="inline-flex items-center gap-1 rounded-lg bg-black px-5 py-2 text-sm font-medium text-white hover:bg-gray-800 transition-colors duration-200"
              >
                Login
              </Link>
            )}
          </div>
        </div>
      </div>
    </nav>
  )
}

export default Navbar
