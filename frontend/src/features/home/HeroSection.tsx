import { Link } from 'react-router-dom'
import { motion } from 'framer-motion'
import { Globe, ArrowRight } from 'lucide-react'
import WelcomeBadge from './WelcomeBadge'

function BrandCard() {
  return (
    <motion.div
      whileHover={{ scale: 1.02 }}
      transition={{ type: 'spring', stiffness: 300, damping: 20 }}
      className="rounded-2xl border border-gray-100 bg-white p-6 shadow-lg shadow-gray-100 w-full max-w-sm aspect-square flex flex-col items-center justify-center"
    >
      <img
        src="/images/codecafe-brand-card.png"
        alt="CodeCafe brand illustration"
        className="w-full h-full object-contain"
        loading="eager"
      />
    </motion.div>
  )
}

function HeroSection() {
  return (
    <section className="pt-28 pb-16 lg:pt-32 lg:pb-20">
      <div className="mx-auto max-w-7xl px-6 lg:px-8">
        <div className="grid lg:grid-cols-2 gap-12 lg:gap-16 items-center">
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
              className="mt-6 text-4xl lg:text-5xl font-bold text-black tracking-tight leading-[1.15]"
            >
              Your space for notes, code, and{' '}
              <span className="text-brand-brown">engineering thoughts.</span>
            </motion.h1>

            <motion.p
              initial={{ opacity: 0, y: 16 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.4, delay: 0.2 }}
              className="mt-4 text-base text-gray-500 leading-relaxed"
            >
              CodeCafe is a minimal workspace where you can capture ideas,
              organize knowledge, and explore code with clarity.
            </motion.p>

            <motion.div
              initial={{ opacity: 0, y: 16 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.4, delay: 0.3 }}
              className="mt-8 flex items-center gap-3"
            >
              <Link
                to="/notes"
                className="inline-flex items-center gap-2 rounded-lg bg-black px-6 py-2.5 text-sm font-medium text-white hover:bg-gray-800 transition-colors"
              >
                Explore Notes
                <ArrowRight className="h-4 w-4" />
              </Link>
              <Link
                to="/login"
                className="inline-flex items-center rounded-lg border border-gray-200 px-6 py-2.5 text-sm font-medium text-black hover:bg-gray-50 transition-colors"
              >
                Login
              </Link>
            </motion.div>

            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              transition={{ duration: 0.4, delay: 0.4 }}
              className="mt-6 flex items-center gap-1.5 text-sm text-gray-400"
            >
              <Globe className="h-4 w-4" />
              codes.cafe
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
