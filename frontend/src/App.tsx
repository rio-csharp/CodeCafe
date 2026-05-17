import { Routes, Route, Link } from 'react-router-dom'

function Home() {
  return (
    <div className="p-8">
      <h1 className="text-2xl font-bold text-blue-600">CodeCafe Home</h1>
      <p className="mt-2 text-gray-600">Tailwind + React Router + TanStack Query are working.</p>
      <Link to="/about" className="mt-4 inline-block text-blue-500 underline">
        Go to About
      </Link>
    </div>
  )
}

function About() {
  return (
    <div className="p-8">
      <h1 className="text-2xl font-bold text-green-600">About</h1>
      <Link to="/" className="mt-4 inline-block text-blue-500 underline">
        Back to Home
      </Link>
    </div>
  )
}

function App() {
  return (
    <Routes>
      <Route path="/" element={<Home />} />
      <Route path="/about" element={<About />} />
    </Routes>
  )
}

export default App
