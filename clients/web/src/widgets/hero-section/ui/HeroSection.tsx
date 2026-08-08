import { Link } from 'react-router-dom'
import { motion } from 'framer-motion'
import { Globe, ArrowRight, Sparkles } from 'lucide-react'
import WelcomeBadge from '@/widgets/welcome-badge'
import { useTranslation } from 'react-i18next'
import { useLayout } from '@/shared/model/layoutContext'
import { LogoMark } from '@/shared/ui/icons'

/**
 * Code-built hero visual: a miniature notebook workspace mock.
 * Replaces the old PNG brand card — theme-aware by design (dark-mode safe),
 * zero image weight. Purely decorative.
 */
function HeroNotebookMock() {
  const { t } = useTranslation()
  return (
    <div role="img" aria-label={t('home.brandIllustration')} className="relative w-full max-w-md">
      {/* soft glow behind the card */}
      <div
        aria-hidden="true"
        className="absolute -inset-8 rounded-[2.5rem] bg-gradient-to-br from-glow-primary via-transparent to-glow-secondary blur-2xl"
      />
      <motion.div
        whileHover={{ scale: 1.02 }}
        transition={{ type: 'spring', stiffness: 300, damping: 20 }}
        className="relative overflow-hidden rounded-2xl border border-border-default bg-surface shadow-2xl"
      >
        {/* window chrome */}
        <div className="flex items-center gap-1.5 border-b border-border-subtle px-4 py-3">
          <span className="h-2.5 w-2.5 rounded-full bg-border-hover" />
          <span className="h-2.5 w-2.5 rounded-full bg-border-hover" />
          <span className="h-2.5 w-2.5 rounded-full bg-border-hover" />
          <div className="ml-3 flex items-center gap-1.5 text-xs text-text-tertiary">
            <LogoMark className="h-4 w-4 text-text-secondary" />
            <span>field-notes</span>
          </div>
        </div>

        <div className="flex">
          {/* mini sidebar */}
          <div className="w-28 shrink-0 space-y-2 border-r border-border-subtle bg-surface-elevated p-3">
            <div className="flex items-center gap-1.5">
              <span className="h-1.5 w-1.5 rounded-sm bg-text-tertiary" />
              <span className="h-1.5 w-10 rounded bg-surface-active" />
            </div>
            <div className="ml-3 space-y-1.5">
              <span className="block h-1.5 w-14 rounded bg-brand-alpha-25 ring-1 ring-brand-brown/40" />
              <span className="block h-1.5 w-11 rounded bg-surface-active" />
              <span className="block h-1.5 w-12 rounded bg-surface-active" />
            </div>
            <div className="flex items-center gap-1.5">
              <span className="h-1.5 w-1.5 rounded-sm bg-text-tertiary" />
              <span className="h-1.5 w-12 rounded bg-surface-active" />
            </div>
            <div className="ml-3 space-y-1.5">
              <span className="block h-1.5 w-10 rounded bg-surface-active" />
              <span className="block h-1.5 w-13 rounded bg-surface-active" />
            </div>
          </div>

          {/* page body */}
          <div className="flex-1 space-y-2.5 p-4">
            <div className="h-3 w-2/5 rounded bg-text-primary/80" />
            <div className="h-2 w-full rounded bg-surface-active" />
            <div className="h-2 w-5/6 rounded bg-surface-active" />
            <div className="h-2 w-3/5 rounded bg-surface-active" />
            {/* code block */}
            <div className="space-y-1.5 rounded-lg bg-[#1C1917] p-3">
              <div className="flex gap-1.5">
                <span className="h-1.5 w-8 rounded bg-brand-brown-light/70" />
                <span className="h-1.5 w-12 rounded bg-emerald-400/60" />
              </div>
              <div className="flex gap-1.5 pl-3">
                <span className="h-1.5 w-10 rounded bg-sky-400/60" />
                <span className="h-1.5 w-6 rounded bg-zinc-500/60" />
              </div>
              <div className="h-1.5 w-5 rounded bg-brand-brown-light/70" />
            </div>
            <div className="h-2 w-4/6 rounded bg-surface-active" />
          </div>
        </div>

        {/* AI assistant bar */}
        <div className="flex items-center gap-2 border-t border-border-subtle px-4 py-2.5">
          <Sparkles className="h-3.5 w-3.5 text-brand-brown" />
          <span className="h-2 w-2/5 rounded bg-surface-active" />
          <span className="ml-auto flex items-center gap-1">
            <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-brand-brown" />
            <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-brand-brown [animation-delay:150ms]" />
            <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-brand-brown [animation-delay:300ms]" />
          </span>
        </div>
      </motion.div>
    </div>
  )
}

function HeroSection() {
  const { t } = useTranslation()
  const { user } = useLayout()

  // Highlight must keep the surrounding text: splitting on the highlight and
  // rendering only part [0] silently drops the tail in locales where it isn't
  // at the end (e.g. zh).
  const renderTagline = () => {
    const tagline = t('home.tagline')
    const highlight = t('home.highlight')
    const idx = tagline.indexOf(highlight)
    if (idx < 0) return tagline
    return (
      <>
        {tagline.slice(0, idx)}
        <span className="text-brand-brown-text">{highlight}</span>
        {tagline.slice(idx + highlight.length)}
      </>
    )
  }

  return (
    <section className="pt-28 pb-16 lg:pt-32 lg:pb-20">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="grid lg:grid-cols-2 gap-10 lg:gap-16 items-center">
          {/* Left: Text */}
          <div className="max-w-lg">
            <motion.div
              initial={{ opacity: 0, y: 16 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.4 }}
            >
              <WelcomeBadge />
            </motion.div>

            <motion.h1
              initial={{ opacity: 0, y: 16 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.4, delay: 0.1 }}
              className="mt-6 text-3xl sm:text-4xl lg:text-5xl font-bold text-text-primary tracking-tight leading-[1.15]"
            >
              {renderTagline()}
            </motion.h1>

            <motion.p
              initial={{ opacity: 0, y: 16 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.4, delay: 0.2 }}
              className="mt-4 text-base text-text-secondary leading-relaxed"
            >
              {t('home.description')}
            </motion.p>

            <motion.div
              initial={{ opacity: 0, y: 16 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.4, delay: 0.3 }}
              className="mt-8 flex flex-col sm:flex-row items-stretch sm:items-center gap-3"
            >
              <Link
                to="/notes"
                className="inline-flex items-center justify-center gap-2 rounded-lg bg-text-primary px-6 py-2.5 text-sm font-medium text-text-inverse hover:bg-surface-inverse-hover transition-colors"
              >
                {t('home.explore')}
                <ArrowRight className="h-4 w-4" />
              </Link>
              {!user && (
                <Link
                  to="/login"
                  className="inline-flex items-center justify-center rounded-lg border border-border-default px-6 py-2.5 text-sm font-medium text-text-primary hover:bg-surface-hover transition-colors"
                >
                  {t('home.login')}
                </Link>
              )}
            </motion.div>

            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              transition={{ duration: 0.4, delay: 0.4 }}
              className="mt-6 flex items-center gap-1.5 text-sm text-text-tertiary"
            >
              <Globe className="h-4 w-4" />
              {t('app.domain')}
            </motion.div>
          </div>

          {/* Right: product mock */}
          <motion.div
            initial={{ opacity: 0, scale: 0.95 }}
            animate={{ opacity: 1, scale: 1 }}
            transition={{ duration: 0.5, delay: 0.2 }}
            className="flex justify-center lg:justify-end"
          >
            <HeroNotebookMock />
          </motion.div>
        </div>
      </div>
    </section>
  )
}

export default HeroSection
