import { motion } from 'framer-motion'
import { Code2 } from 'lucide-react'

function CTASection() {
  return (
    <section className="py-6">
      <div className="mx-auto max-w-7xl px-6 lg:px-8">
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
          transition={{ duration: 0.5 }}
          className="rounded-2xl bg-surface-hover py-14 px-8 text-center"
        >
          <Code2 className="mx-auto h-6 w-6 text-text-tertiary mb-4" strokeWidth={1.5} />
          <h2 className="text-xl font-bold text-text-primary">
            Write once. Browse it anywhere. Automate it when needed.
          </h2>
          <p className="mt-2 text-text-secondary">
            CodeCafe notebooks are available in the web app today, with MCP access built into the backend.
          </p>
        </motion.div>
      </div>
    </section>
  )
}

export default CTASection
