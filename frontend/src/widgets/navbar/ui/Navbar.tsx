import { useState, useEffect } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import logoIcon from '@/shared/assets/codecafe-icon.png'
import { useLogout } from '@/features/authenticate'
import { useLayout } from '@/shared/model/layoutContext'

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
          ? 'bg-surface/70 backdrop-blur-xl border-b border-border-default/50 shadow-sm shadow-border-default/20'
          : 'bg-transparent'
      }`}
    >
      <div className="mx-auto max-w-7xl px-6 lg:px-8">
        <div className="flex h-16 items-center justify-between">
          <Link
            to="/"
            className="flex items-center gap-2 text-lg font-bold text-text-primary tracking-tight hover:opacity-70 transition-opacity"
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
                aria-current={isActive(to) ? 'page' : undefined}
                className={`relative text-sm transition-colors group ${
                  isActive(to) ? 'text-text-primary font-medium' : 'text-text-secondary hover:text-text-primary'
                }`}
              >
                {label}
                <span
                  className={`absolute -bottom-1 left-0 h-0.5 bg-text-primary transition-all duration-300 ${
                    isActive(to) ? 'w-full' : 'w-0 group-hover:w-full'
                  }`}
                />
              </Link>
            ))}
            <a
              href="https://github.com/rio-csharp/CodeCafe"
              target="_blank"
              rel="noopener noreferrer"
              className="text-sm text-text-secondary hover:text-text-primary transition-colors"
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
                      ? 'text-text-primary font-medium'
                      : 'text-text-secondary hover:text-text-primary'
                  }`}
                >
                  Dashboard
                  <span
                    className={`absolute -bottom-1 left-0 h-0.5 bg-text-primary transition-all duration-300 ${
                      isActive('/dashboard') ? 'w-full' : 'w-0 group-hover:w-full'
                    }`}
                  />
                </Link>
                <button
                  onClick={handleLogout}
                  disabled={logout.isPending}
                  className="inline-flex items-center gap-1 rounded-lg bg-text-primary px-5 py-2 text-sm font-medium text-text-inverse hover:bg-surface-inverse-hover transition-colors duration-200 disabled:opacity-50"
                >
                  {logout.isPending ? '...' : 'Logout'}
                </button>
              </>
            ) : (
              <Link
                to="/login"
                className="inline-flex items-center gap-1 rounded-lg bg-text-primary px-5 py-2 text-sm font-medium text-text-inverse hover:bg-surface-inverse-hover transition-colors duration-200"
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
