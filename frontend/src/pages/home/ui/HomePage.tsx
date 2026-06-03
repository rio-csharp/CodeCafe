import HeroSection from '@/widgets/hero-section'
import FeaturesSection from '@/widgets/features-section'
import CTASection from '@/widgets/cta-section'
import Footer from '@/widgets/footer'

function HomePage() {
  return (
    <div className="min-h-screen bg-surface relative">
      {/* Subtle background decorations */}
      <div className="fixed inset-0 pointer-events-none z-0">
        <div className="absolute top-[-10%] right-[-5%] w-[300px] h-[300px] sm:w-[500px] sm:h-[500px] rounded-full bg-blue-100/50 dark:bg-blue-900/20 blur-[100px]" />
        <div className="absolute bottom-[-5%] left-[-10%] w-[250px] h-[250px] sm:w-[400px] sm:h-[400px] rounded-full bg-purple-100/50 dark:bg-purple-900/20 blur-[100px]" />
      </div>

      <div className="relative z-10">
        <main>
          <HeroSection />
          <FeaturesSection />
          <CTASection />
        </main>
        <Footer />
      </div>
    </div>
  )
}

export default HomePage
