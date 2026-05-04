self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open('codecafe-shell').then((cache) => cache.addAll(['/', '/index.html', '/manifest.webmanifest'])).then(() => self.skipWaiting()),
  )
})

self.addEventListener('activate', (event) => {
  event.waitUntil(self.clients.claim())
})

self.addEventListener('fetch', (event) => {
  if (event.request.method !== 'GET') {
    return
  }

  event.respondWith(
    fetch(event.request).catch(async () => {
      if (event.request.mode === 'navigate') {
        const cache = await caches.open('codecafe-shell')
        const cachedIndex = await cache.match('/index.html')

        if (cachedIndex) {
          return cachedIndex
        }
      }

      throw new TypeError('Network request failed')
    }),
  )
})
