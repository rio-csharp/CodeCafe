const localHostnames = new Set(['localhost', '127.0.0.1', '::1'])

export function isLocalEnvironment() {
  return (
    import.meta.env.DEV ||
    (typeof window !== 'undefined' && localHostnames.has(window.location.hostname))
  )
}
