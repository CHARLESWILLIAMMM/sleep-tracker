/* Sleep Tracker Service Worker — cache-first for app shell, network-first for CDN */
const CACHE = 'sleep-tracker-v1';
const APP_SHELL = [
  '/sleep-tracker/',
  '/sleep-tracker/index.html',
  '/sleep-tracker/manifest.json',
  '/sleep-tracker/icon.svg',
  '/sleep-tracker/sw.js'
];

self.addEventListener('install', e => {
  e.waitUntil(
    caches.open(CACHE).then(c => c.addAll(APP_SHELL)).then(() => self.skipWaiting())
  );
});

self.addEventListener('activate', e => {
  e.waitUntil(
    caches.keys()
      .then(keys => Promise.all(keys.filter(k => k !== CACHE).map(k => caches.delete(k))))
      .then(() => self.clients.claim())
  );
});

self.addEventListener('fetch', e => {
  const url = new URL(e.request.url);

  /* CDN resources (Chart.js) — network first, fall back to cache */
  if (url.hostname.includes('jsdelivr') || url.hostname.includes('cdnjs')) {
    e.respondWith(
      fetch(e.request)
        .then(res => {
          const clone = res.clone();
          caches.open(CACHE).then(c => c.put(e.request, clone));
          return res;
        })
        .catch(() => caches.match(e.request))
    );
    return;
  }

  /* App shell — cache first */
  e.respondWith(
    caches.match(e.request).then(r => r || fetch(e.request))
  );
});
