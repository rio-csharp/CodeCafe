import { Link } from 'react-router-dom'
import { motion } from 'framer-motion'
import { ArrowRight } from 'lucide-react'
import { useTranslation } from 'react-i18next'

function CTASection() {
  const { t } = useTranslation()
  return (
    <section className="py-6">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
          transition={{ duration: 0.5 }}
          className="rounded-2xl border border-border-default bg-gradient-to-br from-brand-brown/10 via-surface-elevated to-surface-elevated dark:from-brand-brown/15 dark:via-surface-elevated py-14 px-6 sm:px-8 text-center"
        >
          <h2 className="text-xl sm:text-2xl font-bold text-text-primary">
            {t('cta.title')}
          </h2>
          <p className="mt-3 text-text-secondary max-w-xl mx-auto">
            {t('cta.subtitle')}
          </p>
          <div className="mt-6">
            <Link
              to="/notes"
              className="inline-flex items-center gap-2 rounded-lg bg-brand-brown-dark dark:bg-brand-brown px-6 py-2.5 text-sm font-medium text-text-inverse hover:opacity-90 transition-opacity"
            >
              {t('cta.getStarted')}
              <ArrowRight className="h-4 w-4" />
            </Link>
          </div>
        </motion.div>
      </div>
    </section>
  )
}

export default CTASection
