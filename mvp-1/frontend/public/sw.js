/* Service worker ContractorHub — prosta obsługa offline dla powłoki aplikacji (SPA).
   Strategia:
   - nawigacje (HTML): network-first z fallbackiem do zcache'owanego index.html (offline),
   - zasoby statyczne (/assets, ikony): cache-first z dopisaniem do cache,
   - żądania /api oraz nie-GET: pomijane (zawsze z sieci). */
const CACHE = 'contractorhub-v2'
const APP_SHELL = ['/', '/index.html', '/manifest.webmanifest', '/icon.svg']

self.addEventListener('install', event => {
  event.waitUntil(
    caches.open(CACHE)
      .then(cache => cache.addAll(APP_SHELL))
      .then(() => self.skipWaiting())
  )
})

self.addEventListener('activate', event => {
  event.waitUntil(
    caches.keys()
      .then(keys => Promise.all(keys.filter(k => k !== CACHE).map(k => caches.delete(k))))
      .then(() => self.clients.claim())
  )
})

self.addEventListener('fetch', event => {
  const { request } = event
  if (request.method !== 'GET') return

  const url = new URL(request.url)
  if (url.origin !== self.location.origin) return
  if (url.pathname.startsWith('/api')) return

  // Nawigacje SPA — sieć z fallbackiem do cache (offline → ostatni index.html).
  if (request.mode === 'navigate') {
    event.respondWith(
      fetch(request)
        .then(response => {
          const copy = response.clone()
          caches.open(CACHE).then(cache => cache.put('/index.html', copy))
          return response
        })
        .catch(() => caches.match('/index.html'))
    )
    return
  }

  // Zasoby statyczne — cache-first.
  const isStatic = url.pathname.startsWith('/assets') || /\.(svg|png|ico|woff2?|css|js)$/.test(url.pathname)
  if (!isStatic) return

  event.respondWith(
    caches.match(request).then(cached =>
      cached ||
      fetch(request).then(response => {
        if (response.ok) {
          const copy = response.clone()
          caches.open(CACHE).then(cache => cache.put(request, copy))
        }
        return response
      })
    )
  )
})
