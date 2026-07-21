interface LogoMarkProps {
  className?: string
}

/**
 * CodeCafe brand mark: coffee cup with code brackets and steam.
 * Vector replacement for the old 737KB codecafe-icon.png.
 * Cup/brackets use currentColor so the mark follows the theme;
 * steam stays on the brand brown.
 */
function LogoMark({ className }: LogoMarkProps) {
  return (
    <svg className={className} viewBox="0 0 64 64" fill="none" aria-hidden="true">
      <g
        stroke="var(--color-brand-brown)"
        strokeWidth="3"
        strokeLinecap="round"
      >
        <path d="M23 19c-2.8-2.5-2.8-5 0-7.5s2.8-5 0-7.5" />
        <path d="M35 19c-2.8-2.5-2.8-5 0-7.5s2.8-5 0-7.5" />
      </g>
      <path
        d="M14 25h32a4 4 0 0 1 4 4v14a12 12 0 0 1-12 12H22a12 12 0 0 1-12-12V29a4 4 0 0 1 4-4z"
        stroke="currentColor"
        strokeWidth="4"
      />
      <path
        d="M50 31h1.5a7.5 7.5 0 0 1 0 15H50"
        stroke="currentColor"
        strokeWidth="4"
        strokeLinecap="round"
      />
      <g
        stroke="currentColor"
        strokeWidth="3"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <path d="M25.5 36l-5 5.5 5 5.5" />
        <path d="M34.5 36l5 5.5-5 5.5" />
        <path d="M31.5 35 28.5 48" />
      </g>
    </svg>
  )
}

export default LogoMark
