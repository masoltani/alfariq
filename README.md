# AlFariq

Application de gestion de matchs de sport entre amis (PWA en Blazor WebAssembly).

Documents de référence dans `docs/` : user stories, spécifications fonctionnelles et techniques, décisions d'architecture (`docs/adr/`).

## Prérequis

- SDK .NET **10** (version épinglée dans `global.json`).
- Python 3 + Pillow, uniquement pour régénérer les icônes (`python3 tools/generate-icons.py`).
- Python 3 + Playwright + Chromium, uniquement pour les tests dans un vrai navigateur (service worker hors ligne, IndexedDB, partage).

## Démarrer

```bash
dotnet restore
dotnet run --project src/AlFariq.Web      # http://localhost:5080
dotnet test
```

En développement, le service worker ne met rien en cache. Pour essayer le mode hors ligne, publier puis servir le résultat :

```bash
dotnet publish src/AlFariq.Web -c Release -o artifacts/web
# servir artifacts/web/wwwroot avec n'importe quel serveur statique (le chemin de base doit être "/")
```

## Structure

| Projet | Rôle |
|---|---|
| `AlFariq.Domain` | Entités, règles métier, algorithmes. Aucune dépendance externe. |
| `AlFariq.Application` | Cas d'usage, interfaces de dépôt. |
| `AlFariq.Client.Core` | Services du client : stockage local IndexedDB (US-002), synchronisation (US-022). |
| `AlFariq.UI` | Bibliothèque Razor : toute l'interface, réutilisable par un futur hôte MAUI Blazor Hybrid. |
| `AlFariq.Web` | Hôte de la PWA : composition, manifeste, icônes, service worker. |
| `tests/AlFariq.Domain.Tests` | Tests unitaires du domaine (versions, journal des changements, entités). |
| `tests/AlFariq.Client.Core.Tests` | Dépôt local, suppression logique, schéma et migrations (stockage en mémoire). |
| `tests/AlFariq.Web.Tests` | Contrôles du manifeste, de la page d'accueil, du service worker et des traductions. |
| `tests/pwa-offline` | Test Chromium (Playwright) : ouverture hors ligne, mise à jour à la demande. |
| `tests/local-db` | Test Chromium (Playwright) : IndexedDB réel, migrations de schéma, stockage persistant. |
| `tests/connectivity` | Test Chromium (Playwright) : détection de la coupure et du retour du réseau. |
| `tests/whats-new` | Test Chromium (Playwright) : mémorisation de la dernière version vue, y compris stockage bloqué. |
| `tests/share` | Test Chromium (Playwright) : partage natif (simulé), copie du texte et image de la composition. |

## Conventions

- Aucun texte en dur dans l'interface : tout passe par `src/AlFariq.UI/Resources/Strings.resx` (`L["Cle"]`).
- Écrans pensés pour 360 px de large, cibles tactiles d'au moins 44 px.
- Hébergement dans un sous-dossier : adapter `<base href>` dans `index.html` **et** la constante `base` de `service-worker.published.js`.

## Stockage local

- Les entités héritent de `Entity` (GUID créé côté client, `CreatedAt`, `UpdatedAt`, `DeletedAt`) et s'utilisent via `IRepository<T>`.
- Pour ajouter une entité : l'ajouter dans une **nouvelle migration** de `LocalSchema` (ne jamais modifier une migration déjà livrée), puis `services.AddLocalRepository<MonEntite>("nom-du-magasin")` dans `Program.cs`.
- Détails : `docs/adr/0002-stockage-local-indexeddb.md`.

## Essayer avec beaucoup de joueurs

Pour vérifier la fluidité du répertoire, lancer l'appli, ouvrir l'écran **Joueurs** une fois, puis coller le contenu de `tools/seed-players.js` dans la console du navigateur et recharger la page. À faire sur un poste de développement : le script ajoute 250 joueurs fictifs à la base locale.

## Publier une version

La version suit le versionnement sémantique (avant 1.0 : le numéro mineur monte à chaque lot de fonctionnalités, le dernier chiffre pour les correctifs).

1. Changer `<Version>` dans `Directory.Build.props` (seul endroit où elle est définie).
2. Ajouter **en tête** de `src/AlFariq.Domain/Releases/changelog.json` une entrée : version, date (AAAA-MM-JJ) et changements (`added`, `changed` ou `fixed`) avec le texte `fr` (obligatoire), `en` et `ar`.
3. Lancer `dotnet test` : les tests échouent si la dernière entrée du journal n'est pas la version de l'appli, si le JSON est invalide (version, date, langue inconnue, texte français manquant) ou si l'anglais ou l'arabe manque.
4. Après fusion, poser l'étiquette `vMAJEUR.MINEUR.CORRECTIF`.

Les utilisateurs voient le journal dans **Plus → À propos → Voir les nouveautés** et un bandeau après chaque mise à jour. Détails : `docs/adr/0003-versionnement-et-journal-des-changements.md`.
