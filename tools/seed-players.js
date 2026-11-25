// Ajoute des joueurs fictifs à la base locale, pour essayer le répertoire avec beaucoup de monde (US-005).
// Usage : ouvrir l'écran « Joueurs » de l'appli, coller ce fichier dans la console du navigateur, puis recharger.
// Les joueurs créés s'appellent « Test 001 Élodie », « Test 002 Sami »... : la recherche « Test » les retrouve.
(async () => {
  const count = 250;
  const names = ["Élodie", "Sami", "Chloé", "Yanis", "Inès", "Hugo", "Zoé", "Karim", "Léa", "Noé"];
  const done = request => new Promise((resolve, reject) => {
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error);
  });

  const db = await done(indexedDB.open("alfariq"));
  try {
    if (!db.objectStoreNames.contains("players")) {
      throw new Error("Magasin « players » absent : ouvre d'abord l'écran Joueurs de l'appli.");
    }

    const transaction = db.transaction("players", "readwrite");
    const store = transaction.objectStore("players");
    const now = new Date().toISOString();
    for (let i = 1; i <= count; i++) {
      const id = crypto.randomUUID();
      const name = `Test ${String(i).padStart(3, "0")} ${names[i % names.length]}`;
      store.put({ id, createdAt: now, updatedAt: now, deletedAt: null, name, level: (i % 5) + 1, isArchived: i % 20 === 0 }, id);
    }

    await new Promise((resolve, reject) => {
      transaction.oncomplete = resolve;
      transaction.onerror = () => reject(transaction.error);
    });
    console.log(`${count} joueurs de test ajoutés. Recharge la page.`);
  } finally {
    db.close();
  }
})();
