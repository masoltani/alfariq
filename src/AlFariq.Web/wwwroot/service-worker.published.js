// Service worker publié : met l'application en cache après la première visite pour qu'elle s'ouvre hors ligne.
// Basé sur le modèle PWA de Blazor, avec une mise à jour « à la demande » : la nouvelle version attend
// que l'utilisateur accepte de recharger (message SKIP_WAITING), au lieu de s'activer pendant une saisie.

self.importScripts("./service-worker-assets.js");

self.addEventListener("install", event => event.waitUntil(onInstall()));
self.addEventListener("activate", event => event.waitUntil(onActivate()));
self.addEventListener("message", event => {
  if (event.data && event.data.type === "SKIP_WAITING") {
    self.skipWaiting();
  }
});
self.addEventListener("fetch", event => event.respondWith(onFetch(event)));

const cacheNamePrefix = "offline-cache-";
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;

const offlineAssetsInclude = [
  /\.dll$/, /\.pdb$/, /\.wasm$/, /\.html$/, /\.js$/, /\.json$/, /\.css$/,
  /\.woff2?$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.svg$/,
  /\.webmanifest$/, /\.blat$/, /\.dat$/, /\.webcil$/,
];
const offlineAssetsExclude = [/^service-worker\.js$/];

// Hébergement dans un sous-dossier : remplacer par ce dossier, avec une barre oblique finale (ex. "/alfariq/").
const base = "/";
const baseUrl = new URL(base, self.origin);
const manifestUrlList = self.assetsManifest.assets.map(asset => new URL(asset.url, baseUrl).href);

async function onInstall() {
  const assetsRequests = self.assetsManifest.assets
    .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
    .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
    .map(asset => new Request(asset.url, { cache: "no-cache", integrity: asset.hash }));

  const cache = await caches.open(cacheName);
  await cache.addAll(assetsRequests);
}

async function onActivate() {
  const cacheKeys = await caches.keys();
  await Promise.all(
    cacheKeys
      .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
      .map(key => caches.delete(key)));
}

async function onFetch(event) {
  let cachedResponse = null;

  // Seuls les GET de même origine sont servis depuis le cache ; les appels d'API (R1) passent toujours par le réseau.
  if (event.request.method === "GET" && new URL(event.request.url).origin === self.origin) {
    // Navigation vers une route de l'appli (ex. /matchs) : on sert index.html, Blazor affiche la bonne page.
    const shouldServeIndexHtml = event.request.mode === "navigate"
      && !manifestUrlList.some(url => url === event.request.url);

    const request = shouldServeIndexHtml ? "index.html" : event.request;
    const cache = await caches.open(cacheName);
    cachedResponse = await cache.match(request);
  }

  return cachedResponse || fetch(event.request);
}
