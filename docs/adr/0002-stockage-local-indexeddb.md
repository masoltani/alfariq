# ADR 0002 — Stockage local IndexedDB derrière une interface de dépôt

Statut : proposé — 25/11/2026 (US-002)

## Contexte

- L'appli est « hors ligne d'abord » : l'interface lit et écrit toujours en local ; la synchronisation (US-022) viendra s'ajouter.
- Les identifiants doivent exister avant tout échange avec un serveur ; les suppressions doivent pouvoir être propagées.
- Le navigateur peut effacer les données d'un site peu utilisé (surtout sur iOS).

## Décision

- **Interface `IRepository<T>`** (`AlFariq.Application`) : ajout, lecture, liste, modification, suppression logique. Le reste de l'appli ne connaît qu'elle.
- **`Entity`** (`AlFariq.Domain`) : `Id` (GUID version 7 créé côté client), `CreatedAt`, `UpdatedAt` (UTC, gérés par le dépôt), `DeletedAt` (suppression logique). Les dates passées par l'appelant sont ignorées.
- **Deux couches côté client** (`AlFariq.Client.Core`) :
  - `IDocumentStore` : stockage clé-valeur de documents JSON par magasin. Implémentation du navigateur : `IndexedDbDocumentStore` + module `local-db.js` ; implémentation en mémoire pour les tests.
  - `LocalRepository<T>` : règles communes (dates, suppression logique, unicité) au-dessus de `IDocumentStore`. Aucune règle métier dans le JavaScript.
- **Migrations de schéma** : `LocalSchema.Migrations` liste des étapes numérotées 1, 2, 3… ; la version de la base IndexedDB est celle de la dernière. À l'ouverture, seules les étapes plus récentes que la base de l'appareil sont appliquées, dans l'ordre. Une migration livrée n'est jamais modifiée ; on en ajoute une.
- **Un magasin d'objets par type d'entité**, déclaré dans une migration ; `AddLocalRepository<T>(nom)` refuse un magasin non déclaré dès le démarrage.
- **Stockage persistant** : `navigator.storage.persist()` est demandé au premier affichage (`MainLayout`). Un refus ou une absence de support n'empêche jamais l'appli de démarrer.

## Alternatives écartées

- `localStorage` : trop limité, synchrone, pas d'index.
- SQLite WASM : plus lourd au chargement ; à réévaluer si les requêtes deviennent complexes (derrière la même interface).
- Bibliothèque IndexedDB tierce : une dépendance de plus pour un besoin simple (get/put/liste) ; l'interface `IDocumentStore` permettrait d'en adopter une plus tard.

## Conséquences

- Lire une liste charge tout le magasin en mémoire puis filtre en C# : suffisant pour des centaines d'éléments (US-005). Si besoin, des index IndexedDB sont déjà prévus dans `StoreDefinition`.
- La suppression logique garde les données : il faudra une purge si le volume le justifie.
- Les écritures d'un dépôt sont sérialisées (lecture puis réécriture). Deux onglets ouverts en même temps peuvent encore s'écraser : à traiter avec la synchronisation.
- L'identifiant d'appareil, `OwnerId` et `Version` de la spec technique (§ 5.2) ne sont pas encore portés : ils arriveront avec US-022 et US-023, par migration.
