# AlFariq

Application de gestion de matchs de sport entre amis (PWA en Blazor WebAssembly).

Documents de référence dans `docs/` : user stories, spécifications fonctionnelles et techniques, décisions d'architecture (`docs/adr/`).

## Prérequis

- SDK .NET **10** (version épinglée dans `global.json`).
- Python 3 + Pillow, uniquement pour régénérer les icônes (`python3 tools/generate-icons.py`).
- Python 3 + Playwright + Chromium, uniquement pour le test hors ligne du service worker.

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
| `AlFariq.Client.Core` | Services du client : stockage local (US-002), synchronisation (US-022). |
| `AlFariq.UI` | Bibliothèque Razor : toute l'interface, réutilisable par un futur hôte MAUI Blazor Hybrid. |
| `AlFariq.Web` | Hôte de la PWA : composition, manifeste, icônes, service worker. |
| `tests/AlFariq.Domain.Tests` | Tests unitaires du domaine. |
| `tests/AlFariq.Web.Tests` | Contrôles du manifeste, de la page d'accueil, du service worker et des traductions. |
| `tests/pwa-offline` | Test Chromium (Playwright) : ouverture hors ligne, mise à jour à la demande. |

## Conventions

- Aucun texte en dur dans l'interface : tout passe par `src/AlFariq.UI/Resources/Strings.resx` (`L["Cle"]`).
- Écrans pensés pour 360 px de large, cibles tactiles d'au moins 44 px.
- Hébergement dans un sous-dossier : adapter `<base href>` dans `index.html` **et** la constante `base` de `service-worker.published.js`.
