// Applies the stored theme before first paint; the module bundle would be too late and cause a
// flash. Kept as a separate same-origin file (not inline) so the frontend CSP can stay
// script-src 'self' without a hash.
;(function () {
  try {
    var stored = localStorage.getItem('codecafe.theme')
    var dark =
      stored === 'dark' ||
      (stored !== 'light' && window.matchMedia('(prefers-color-scheme: dark)').matches)
    document.documentElement.classList.toggle('dark', dark)
    document.documentElement.style.colorScheme = dark ? 'dark' : 'light'
  } catch {
    /* storage or matchMedia unavailable: stay light */
  }
})()
