interface PageTransitionProps {
  children: React.ReactNode
}

// CSS-only page transition (see .page-transition in app/styles/index.css).
// Replaced framer-motion so the motion bundle stays out of the entry chunk.
export default function PageTransition({ children }: PageTransitionProps) {
  return <div className="page-transition">{children}</div>
}
