/* ==========================================================
   MoneyManager - Service Worker
   - Offline-first cache for static shell assets
   - Web Push notification handler
   ========================================================== */

const CACHE_NAME = 'moneymanager-v3';

// Assets to pre-cache on install (shell assets only - not API data)
const PRECACHE_URLS = [
  '/',
  '/index.html',
  '/manifest.json',
  '/favicon.ico',
  '/favicon.svg',
  '/css/app.css',
  '/css/modern-theme.css',
  '/css/navbar.css'
];

// Install: skip waiting to activate immediately
self.addEventListener('install', function(event) {
  self.skipWaiting();
});

// Activate: take control of all clients immediately
self.addEventListener('activate', function(event) {
  event.waitUntil(clients.claim());
});

// Fetch: network-first for i18n, cache-first for static, skip API
self.addEventListener('fetch', event => {
  const url = new URL(event.request.url);

  // Skip non-GET and cross-origin requests
  if (event.request.method !== 'GET') return;
  if (url.origin !== self.location.origin) return;

  // Never cache API calls
  if (url.pathname.startsWith('/api/')) return;

  // Network-first for localization files: always fetch the latest translations,
  // fall back to cache when offline.
  if (url.pathname.startsWith('/i18n/')) {
    event.respondWith(
      fetch(event.request)
        .then(response => {
          if (response && response.status === 200) {
            const cloned = response.clone();
            caches.open(CACHE_NAME).then(cache => cache.put(event.request, cloned));
          }
          return response;
        })
        .catch(() => caches.match(event.request))
    );
    return;
  }

  // Cache-first for all other same-origin static assets.
  event.respondWith(
    caches.match(event.request)
      .then(cached => {
        if (cached) return cached;

        return fetch(event.request).then(response => {
          // Only cache successful same-origin responses
          if (!response || response.status !== 200 || response.type !== 'basic') {
            return response;
          }

          const cloned = response.clone();
          caches.open(CACHE_NAME).then(cache => cache.put(event.request, cloned));
          return response;
        });
      })
      .catch(() => {
        // Fallback to index.html for navigation requests (SPA)
        if (event.request.mode === 'navigate') {
          return caches.match('/index.html');
        }
      })
  );
});

// Push: receive and display notification
self.addEventListener('push', function(event) {
  let title = 'MoneyManager';
  let body = 'Você tem uma nova notificação.';

  if (event.data) {
    try {
      const data = event.data.json();
      title = data.title || title;
      body = data.body || body;
    } catch (e) {
      body = event.data.text();
    }
  }

  event.waitUntil(
    self.registration.showNotification(title, {
      body: body,
      icon: '/favicon.svg',
      badge: '/favicon.svg'
    })
  );
});

// Notification click: open the app
self.addEventListener('notificationclick', function(event) {
  event.notification.close();
  event.waitUntil(
    clients.openWindow('/')
  );
});
