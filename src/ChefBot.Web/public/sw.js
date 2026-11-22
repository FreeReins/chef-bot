// ChefBot's service worker: keeps the page itself on the device, so the Shopping and Pantry tabs open with no
// connection (the list and pantry they show are saved by src/lib/offline.svelte.js, not here).
//   /assets/*   named after their content by Vite, so a stored copy is good for ever: stored first, network second.
//   pages       network first, so an upgrade shows straight away; the stored index.html when ChefBot can't be reached.
//   /api/*      never stored here: private data stays out of the cache, and requests go straight to ChefBot.
//   /help/*     the documentation, a separate site: left to the browser, so it never gets the app's index.html.
// Registered by src/main.js, only in a build served over HTTPS (or localhost).
const CACHE = 'chefbot-page-v1';
const SHELL = ['/', '/theme.js', '/manifest.webmanifest', '/icon.svg'];

self.addEventListener('install', (event) => {
  event.waitUntil(caches.open(CACHE).then((cache) => cache.addAll(SHELL)).then(() => self.skipWaiting()));
});

self.addEventListener('activate', (event) => {
  event.waitUntil(caches.keys()
    .then((names) => Promise.all(names.filter((name) => name !== CACHE).map((name) => caches.delete(name))))
    .then(() => self.clients.claim()));
});

self.addEventListener('fetch', (event) => {
  const request = event.request;
  const url = new URL(request.url);
  if (request.method !== 'GET' || url.origin !== location.origin) return;
  if (/^\/(api|openapi|docs|help|health|mcp)(\/|$)/.test(url.pathname)) return;

  if (url.pathname.startsWith('/assets/')) {
    event.respondWith(caches.match(request).then((stored) => stored ?? fetchAndStore(request)));
  } else if (request.mode === 'navigate') {
    event.respondWith(page(request));
  } else if (SHELL.includes(url.pathname)) {
    // Stored copy at once, refreshed in the background for next time.
    event.respondWith(caches.match(request).then((stored) => {
      const fresh = fetchAndStore(request);
      return stored ?? fresh;
    }));
  }
});

/** Every address is the same page (index.html), so one stored copy answers them all offline. */
async function page(request) {
  try {
    const response = await fetch(request);
    if (response.ok && response.headers.get('content-type')?.includes('text/html')) {
      const cache = await caches.open(CACHE);
      await cache.put('/', response.clone());
      pruneAssets(cache, await response.clone().text());
    }
    return response;
  } catch {
    return (await caches.match('/')) ?? Response.error();
  }
}

async function fetchAndStore(request) {
  const response = await fetch(request);
  if (response.ok) (await caches.open(CACHE)).put(request, response.clone());
  return response;
}

/**
 * Drops stored files an upgrade no longer uses, so the cache doesn't grow with every version. The entry files are
 * named in index.html; anything they load (such as a lazily loaded chunk) is kept while one of them is.
 */
async function pruneAssets(cache, html) {
  const named = new Set([...html.matchAll(/\/assets\/[^"'\s>]+/g)].map((match) => match[0]));
  if (named.size === 0) return;
  for (const request of await cache.keys()) {
    const path = new URL(request.url).pathname;
    if (path.startsWith('/assets/') && !named.has(path) && !(await usedBy(cache, named, path))) cache.delete(request);
  }
}

async function usedBy(cache, entries, path) {
  const name = path.slice('/assets/'.length);
  for (const entry of entries) {
    const stored = await cache.match(entry);
    if (stored && (await stored.text()).includes(name)) return true;
  }
  return false;
}
