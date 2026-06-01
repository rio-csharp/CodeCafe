import { Link } from 'react-router-dom'
import { motion } from 'framer-motion'
import { FileText, Code2, ArrowRight } from 'lucide-react'

interface FeatureCardProps {
  icon: React.ReactNode
  title: string
  description: string
  delay: number
  href?: string
}

function FeatureCard({ icon, title, description, delay, href }: FeatureCardProps) {
  const inner = (
    <>
      <div className="inline-flex items-center justify-center w-11 h-11 rounded-xl bg-surface-hover border border-border-subtle">
        {icon}
      </div>
      <h3 className="mt-5 text-lg font-semibold text-text-primary">{title}</h3>
      <p className="mt-2 text-sm text-text-secondary leading-relaxed">{description}</p>
      {href ? (
        <span className="mt-4 inline-flex items-center gap-1 rounded-full bg-brand-brown/10 px-3 py-1 text-xs font-medium text-brand-brown">
          Try it
          <ArrowRight className="h-3 w-3" />
        </span>
      ) : (
        <span className="mt-4 inline-block rounded-full bg-surface-active px-3 py-1 text-xs font-medium text-text-secondary">
          Coming Soon
        </span>
      )}
    </>
  )

  const className =
    'rounded-2xl border border-border-subtle bg-surface p-8 hover:shadow-md hover:shadow-surface-active transition-shadow duration-300 block'

  return (
    <motion.div
      initial={{ opacity: 0, y: 24 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: '-50px' }}
      transition={{ duration: 0.4, delay }}
    >
      {href ? (
        <Link to={href} className={className}>
          {inner}
        </Link>
      ) : (
        <div className={className}>{inner}</div>
      )}
    </motion.div>
  )
}

function FeaturesSection() {
  return (
    <section className="py-16 border-t border-border-subtle">
      <div className="mx-auto max-w-7xl px-6 lg:px-8">
        <motion.div
          initial={{ opacity: 0, y: 16 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
          transition={{ duration: 0.4 }}
          className="text-center mb-10"
        >
          <h2 className="text-2xl font-bold text-text-primary">What you can do</h2>
          <p className="mt-2 text-text-secondary">
            Simple tools for your daily workflow.
          </p>
        </motion.div>

        <div className="grid md:grid-cols-2 gap-5 max-w-3xl mx-auto">
          <FeatureCard
            icon={<FileText className="h-5 w-5 text-text-secondary" strokeWidth={1.5} />}
            title="Notes"
            description="Write and organize your notes. Keep your knowledge structured and easy to find."
            delay={0.1}
            href="/notes"
          />
          <FeatureCard
            icon={<Code2 className="h-5 w-5 text-text-secondary" strokeWidth={1.5} />}
            title="Codes"
            description="Explore code with AI assistance. Understand, explain, and learn more effectively."
            delay={0.15}
          />
        </div>
      </div>
    </section>
  )
}

export default FeaturesSection
