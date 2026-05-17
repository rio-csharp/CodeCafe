import Navbar from '../../components/Navbar'

function AboutPage() {
  return (
    <div className="min-h-screen bg-white">
      <Navbar />
      <main className="pt-32 pb-20">
        <div className="mx-auto max-w-7xl px-6 lg:px-8">
          <h1 className="text-4xl font-bold text-black">About</h1>
          <p className="mt-4 text-gray-500">CodeCafe is a minimal workspace for notes, code, and engineering thoughts.</p>
        </div>
      </main>
    </div>
  )
}

export default AboutPage
