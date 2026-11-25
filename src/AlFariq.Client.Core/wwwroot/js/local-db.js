// Module IndexedDB : magasin de documents JSON (un magasin d'objets par type d'entité) et demande de persistance.
// Chargé comme module ES par LocalDbModule (C#). Aucune règle métier ici : elle vit dans LocalRepository.
//
// Les documents arrivent et repartent sous forme de texte JSON. Ils sont stockés comme objets pour que des index
// sur leurs champs soient possibles, avec une clé externe (l'identifiant de l'entité).

let dbPromise = null;

// ---------- Ouverture et migrations ----------

/**
 * Ouvre la base. `version` est la version la plus récente du schéma ; `migrationsJson` la liste de toutes les migrations
 * ([{ version, createStores: [{ name, indexes: [{ name, keyPath, unique }] }] }]).
 * Seules les migrations plus récentes que la base de l'appareil sont appliquées, dans l'ordre.
 */
export async function open(name, version, migrationsJson) {
  if (!dbPromise) {
    dbPromise = openDatabase(name, version, JSON.parse(migrationsJson));
    // En cas d'échec, la prochaine opération pourra retenter l'ouverture.
    dbPromise.catch(() => { dbPromise = null; });
  }
  await dbPromise;
}

function openDatabase(name, version, migrations) {
  return new Promise((resolve, reject) => {
    if (typeof indexedDB === "undefined") {
      reject(new Error("IndexedDB n'est pas disponible dans ce navigateur."));
      return;
    }

    const request = indexedDB.open(name, version);

    request.onupgradeneeded = event => {
      const db = request.result;
      const ordered = [...migrations].sort((a, b) => a.version - b.version);
      for (const migration of ordered) {
        if (migration.version <= event.oldVersion || migration.version > version) {
          continue;
        }
        for (const store of migration.createStores ?? []) {
          createStore(db, store);
        }
      }
    };

    request.onsuccess = () => {
      const db = request.result;
      // Une autre fenêtre veut migrer vers une version plus récente : on libère la base pour ne pas la bloquer.
      db.onversionchange = () => {
        db.close();
        dbPromise = null;
      };
      resolve(db);
    };

    request.onerror = () => reject(request.error ?? new Error("Ouverture de la base locale impossible."));
    // onblocked : une ancienne version est encore ouverte ailleurs ; elle se fermera (onversionchange) et l'ouverture reprendra.
  });
}

function createStore(db, definition) {
  if (db.objectStoreNames.contains(definition.name)) {
    return;
  }
  const store = db.createObjectStore(definition.name);
  for (const index of definition.indexes ?? []) {
    store.createIndex(index.name, index.keyPath, { unique: !!index.unique });
  }
}

// ---------- Documents ----------

async function run(storeName, mode, action) {
  if (!dbPromise) {
    throw new Error("La base locale n'est pas ouverte.");
  }
  const db = await dbPromise;

  return new Promise((resolve, reject) => {
    const transaction = db.transaction(storeName, mode);
    const request = action(transaction.objectStore(storeName));
    // On attend la fin de la transaction (et pas seulement de la requête) : l'écriture est alors validée.
    transaction.oncomplete = () => resolve(request.result);
    transaction.onerror = () => reject(transaction.error);
    transaction.onabort = () => reject(transaction.error ?? new Error("Transaction annulée."));
  });
}

export async function get(store, id) {
  const value = await run(store, "readonly", s => s.get(id));
  return value === undefined ? null : JSON.stringify(value);
}

export async function getAll(store) {
  const values = await run(store, "readonly", s => s.getAll());
  return values.map(value => JSON.stringify(value));
}

export async function put(store, id, json) {
  await run(store, "readwrite", s => s.put(JSON.parse(json), id));
}

// ---------- Stockage persistant ----------

/** Renvoie « granted », « denied » ou « unsupported ». */
export async function requestPersistence() {
  const storage = navigator.storage;
  if (!storage || typeof storage.persist !== "function") {
    return "unsupported";
  }

  try {
    if (typeof storage.persisted === "function" && (await storage.persisted())) {
      return "granted";
    }
    return (await storage.persist()) ? "granted" : "denied";
  } catch {
    return "denied";
  }
}
