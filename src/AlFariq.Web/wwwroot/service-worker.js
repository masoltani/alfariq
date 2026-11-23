// Service worker de développement : ne met rien en cache, pour ne jamais servir de fichiers périmés pendant le travail.
// La version publiée (service-worker.published.js) gère le cache hors ligne.
self.addEventListener("fetch", () => { });
