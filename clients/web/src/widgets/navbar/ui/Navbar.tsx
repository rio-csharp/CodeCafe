import { useState, useEffect } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { LogoMark } from '@/shared/ui/icons'
import { useLogout } from '@/features/authenticate'
import { useLayout } from '@/shared/model/layoutContext'
import { ThemeToggle } from '@/shared/ui/ThemeToggle'
import { LanguageSwitcher } from '@/shared/ui/LanguageSwitcher'
import { useTranslation } from 'react-i18next'
import { Menu, X } from 'lucide-react'

function Navbar() {
  const [scrolled, setScrolled] = useState(false)
  const [mobileOpenPathname, setMobileOpenPathname] = useState<string | null>(null)
  const location = useLocation()
  const navigate = useNavigate()
  const { user } = useLayout()
  const logout = useLogout()
  const { t } = useTranslation()
  const mobileOpen = mobileOpenPathname === location.pathname

  useEffect(() => {
    const handleScroll = () => setScrolled(window.scrollY > 10)
    window.addEventListener('scroll', handleScroll, { passive: true })
    return () => window.removeEventListener('scroll', handleScroll)
  }, [])

  const isActive = (path: string) => {
    if (path === '/') return location.pathname === '/'
    return location.pathname.startsWith(path)
  }

  const toggleMobileMenu = () => {
    setMobileOpenPathname((pathname) => (pathname === location.pathname ? null : location.pathname))
  }

  const handleLogout = () => {
    logout.mutate(undefined, {
      onSuccess: () => navigate('/'),
    })
  }

  const navLinks = [
    { to: '/notes', label: t('nav.notes') },
    { to: '/codes', label: t('nav.codes') },
    { to: '/about', label: t('nav.about') },
  ]

  return (
    <nav
      className={`fixed top-0 left-0 right-0 z-50 transition-all duration-300 ${
        scrolled
          ? 'bg-surface/70 backdrop-blur-xl border-b border-border-default/50 shadow-sm shadow-border-default/20'
          : 'bg-transparent'
      }`}
    >
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="flex h-16 items-center justify-between">
          <Link
            to="/"
            className="flex items-center gap-2 text-lg font-bold text-text-primary tracking-tight hover:opacity-70 transition-opacity"
          >
            <LogoMark className="h-7 w-7 text-text-primary" />
            CodeCafe
          </Link>

          {/* Desktop nav */}
          <div className="hidden md:flex items-center gap-8">
            {navLinks.map(({ to, label }) => (
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
              {t('nav.github')}
            </a>
          </div>

          <div className="flex items-center gap-2">
            <div className="hidden sm:flex items-center gap-1">
              <ThemeToggle />
              <LanguageSwitcher />
            </div>

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
                  {t('nav.dashboard')}
                  <span
                    className={`absolute -bottom-1 left-0 h-0.5 bg-text-primary transition-all duration-300 ${
                      isActive('/dashboard') ? 'w-full' : 'w-0 group-hover:w-full'
                    }`}
                  />
                </Link>
                <button
                  onClick={handleLogout}
                  disabled={logout.isPending}
                  className="inline-flex items-center gap-1 rounded-lg bg-text-primary px-4 py-2 text-sm font-medium text-text-inverse hover:bg-surface-inverse-hover transition-colors duration-200 disabled:opacity-50"
                >
                  {logout.isPending ? '...' : t('nav.logout')}
                </button>
              </>
            ) : (
              <Link
                to="/login"
                className="inline-flex items-center gap-1 rounded-lg bg-text-primary px-4 py-2 text-sm font-medium text-text-inverse hover:bg-surface-inverse-hover transition-colors duration-200"
              >
                {t('nav.login')}
              </Link>
            )}

            {/* Mobile menu button */}
            <button
              className="md:hidden p-2 rounded-lg hover:bg-surface-hover transition-colors"
              onClick={toggleMobileMenu}
              aria-label={t('nav.toggleMenu')}
            >
              {mobileOpen ? <X className="h-5 w-5 text-text-primary" /> : <Menu className="h-5 w-5 text-text-primary" />}
            </button>
          </div>
        </div>
      </div>

      {/* Mobile menu */}
      {mobileOpen && (
        <div className="md:hidden border-t border-border-default bg-surface/95 backdrop-blur-xl">
          <div className="px-4 py-4 space-y-3">
            {navLinks.map(({ to, label }) => (
              <Link
                key={to}
                to={to}
                className={`block text-sm font-medium ${
                  isActive(to) ? 'text-text-primary' : 'text-text-secondary'
                }`}
              >
                {label}
              </Link>
            ))}
            <a
              href="https://github.com/rio-csharp/CodeCafe"
              target="_blank"
              rel="noopener noreferrer"
              className="block text-sm text-text-secondary"
            >
              {t('nav.github')}
            </a>
            {user && (
              <>
                <Link
                  to="/dashboard"
                  className={`block text-sm font-medium ${
                    isActive('/dashboard') ? 'text-text-primary' : 'text-text-secondary'
                  }`}
                >
                  {t('nav.dashboard')}
                </Link>
                <button
                  type="button"
                  onClick={handleLogout}
                  disabled={logout.isPending}
                  className="block text-sm font-medium text-status-error"
                >
                  {logout.isPending ? t('nav.loggingOut') : t('nav.logout')}
                </button>
              </>
            )}
            <div className="flex items-center gap-2 pt-2 border-t border-border-subtle">
              <ThemeToggle />
              <LanguageSwitcher />
            </div>
          </div>
        </div>
      )}
    </nav>
  )
}

export default Navbar
