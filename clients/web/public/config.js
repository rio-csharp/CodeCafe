// Default runtime configuration, baked into the image so /config.js always
// resolves. A deployment shadows this file with a ConfigMap carrying the
// browser-facing API origin; leaving it empty keeps every request same-origin.
window.__CODECAFE_CONFIG__ = {}
