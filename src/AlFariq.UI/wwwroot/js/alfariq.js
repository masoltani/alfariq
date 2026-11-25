// Passerelle JavaScript de l'interface : connexion, thème et mises à jour de la PWA.
// Chargée comme module ES par ShellInterop. Aucune donnée métier ici.

const THEME_KEY = "alfariq.theme";
const THEMES = ["auto", "light", "dark"];

// ---------- Connexion ----------

export function initConnectivity(dotnetRef) {
  const push = () => dotnetRef.invokeMethodAsync("OnConnectivityChanged", navigator.onLine);
  window.addEventListener("online", push);
  window.addEventListener("offline", push);
  return navigator.onLine;
}

// ---------- Thème ----------

export function getTheme() {
  try {
    const stored = localStorage.getItem(THEME_KEY);
    return THEMES.includes(stored) ? stored : "auto";
  } catch {
    return "auto";
  }
}

export function setTheme(theme) {
  const value = THEMES.includes(theme) ? theme : "auto";
  try {
    if (value === "auto") {
      localStorage.removeItem(THEME_KEY);
    } else {
      localStorage.setItem(THEME_KEY, value);
    }
  } catch {
    // Stockage indisponible (navigation privée, données bloquées) : le thème reste valable pour la session.
  }
  applyTheme(value);
}

export function applyTheme(theme) {
  const root = document.documentElement;
  if (theme === "light" || theme === "dark") {
    root.setAttribute("data-theme", theme);
  } else {
    root.removeAttribute("data-theme");
  }
}

// ---------- Nouveautés ----------

const SEEN_VERSION_KEY = "alfariq.lastSeenVersion";

/** Dernier numéro de version dont l'utilisateur a vu les nouveautés (null au premier lancement). */
export function getLastSeenVersion() {
  try {
    return localStorage.getItem(SEEN_VERSION_KEY);
  } catch {
    return null;
  }
}

export function setLastSeenVersion(version) {
  try {
    localStorage.setItem(SEEN_VERSION_KEY, version);
  } catch {
    // Stockage indisponible : l'annonce ne se répétera simplement pas à la prochaine visite.
  }
}

// ---------- Mises à jour de l'application ----------

export async function initUpdates(dotnetRef) {
  if (!("serviceWorker" in navigator)) {
    return;
  }

  let registration;
  try {
    registration = await (window.alfariqSwReady ?? navigator.serviceWorker.getRegistration());
  } catch {
    return;
  }
  if (!registration) {
    return;
  }

  const notify = () => dotnetRef.invokeMethodAsync("OnUpdateAvailable");

  // Une version attend déjà (la page a été rouverte sans la recharger).
  if (registration.waiting && navigator.serviceWorker.controller) {
    notify();
  }

  registration.addEventListener("updatefound", () => {
    const worker = registration.installing;
    if (!worker) {
      return;
    }
    worker.addEventListener("statechange", () => {
      // « installed » avec un contrôleur actif = mise à jour prête, pas première installation.
      if (worker.state === "installed" && navigator.serviceWorker.controller) {
        notify();
      }
    });
  });

  // Vérification périodique tant que l'appli reste ouverte (toutes les heures).
  setInterval(() => registration.update().catch(() => {}), 60 * 60 * 1000);
}

export async function applyUpdate() {
  if (!("serviceWorker" in navigator)) {
    location.reload();
    return;
  }

  const registration = await navigator.serviceWorker.getRegistration();
  if (!registration || !registration.waiting) {
    location.reload();
    return;
  }

  navigator.serviceWorker.addEventListener("controllerchange", () => location.reload(), { once: true });
  registration.waiting.postMessage({ type: "SKIP_WAITING" });
}
