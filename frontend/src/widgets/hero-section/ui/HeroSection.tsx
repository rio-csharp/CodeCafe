import { Link } from 'react-router-dom'
import { motion } from 'framer-motion'
import { Globe, ArrowRight } from 'lucide-react'
import WelcomeBadge from '@/widgets/welcome-badge'
import { useTranslation } from 'react-i18next'
import { useLayout } from '@/shared/model/layoutContext'

function BrandCard() {
  const { t } = useTranslation()
  return (
    <motion.div
      whileHover={{ scale: 1.02 }}
      transition={{ type: 'spring', stiffness: 300, damping: 20 }}
      className="rounded-2xl border border-border-subtle bg-surface p-6 shadow-lg shadow-surface-active w-full max-w-sm aspect-square flex flex-col items-center justify-center"
    >
      <img
        src="/images/codecafe-brand-card.png"
        alt={t('home.brandIllustration')}
        className="w-full h-full object-contain"
        loading="eager"
      />
    </motion.div>
  )
}

function HeroSection() {
  const { t } = useTranslation()
  const { user } = useLayout()
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
              {t('home.tagline').split(t('home.highlight'))[0]}
              <span className="text-brand-brown">{t('home.highlight')}</span>
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

          {/* Right: Brand card */}
          <motion.div
            initial={{ opacity: 0, scale: 0.95 }}
            animate={{ opacity: 1, scale: 1 }}
            transition={{ duration: 0.5, delay: 0.2 }}
            className="flex justify-center lg:justify-end"
          >
            <BrandCard />
          </motion.div>
        </div>
      </div>
    </section>
  )
}

export default HeroSection
