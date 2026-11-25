# ADR 0003 — Versionnement et journal des changements

Statut : proposé — 25/11/2026 (US-017)

## Contexte

- R0 sera testé par de vrais utilisateurs : il faut savoir quelle version ils utilisent et leur dire ce qui change.
- Le journal doit être lisible **hors ligne**, et prévu pour le français, l'anglais et l'arabe (écriture de droite à gauche) alors que l'interface n'est encore qu'en français.
- Un seul développeur : la publication ne doit pas dépendre de la mémoire.

## Décision

- **Version sémantique** `MAJEUR.MINEUR.CORRECTIF`, définie uniquement dans `Directory.Build.props`. `AppInfo.Version` la lit (sans suffixe de build). Avant 1.0 : chaque lot de fonctionnalités incrémente le mineur, les correctifs le dernier chiffre.
- **Journal = un fichier `changelog.json`** (`src/AlFariq.Domain/Releases/`), **embarqué dans l'assembly** du domaine. Pas d'appel réseau, pas de dépendance au service worker, disponible dès le premier démarrage.
- **Forme** : entrées de la plus récente à la plus ancienne ; chaque entrée = version, date, liste de changements ; chaque changement = type (`added`, `changed`, `fixed`) et textes par langue (`fr`, `en`, `ar`).
- **Langues** : `fr` obligatoire (langue de repli), `en` et `ar` facultatives. Langues inconnues refusées (détecte les fautes de frappe). Un texte de repli est balisé avec `lang` et `dir` de sa propre langue.
- **Modèle et analyse dans le domaine** (`AppVersion`, `Changelog`) : logique pure, testée sans navigateur.
- **Cohérence imposée par les tests** : la dernière entrée du journal doit être la version de l'appli ; versions valides, uniques, décroissantes ; dates valides ; texte français présent.
- **Bandeau « nouveautés »** : le dernier numéro de version vu est gardé dans `localStorage` (confort propre à l'appareil, comme le thème). Premier lancement : on enregistre sans rien afficher.

## Alternatives écartées

- `CHANGELOG.md` en plus du JSON : deux sources qui divergent. Pourra être généré depuis le JSON.
- Journal chargé par HTTP (`wwwroot`) : un échec réseau possible, et dépendance au cache du service worker pour rester disponible hors ligne.
- Fichiers de ressources `.resx` par langue pour le journal : les entrées s'ajoutent à chaque version, un fichier par langue éparpille le contenu d'une même ligne de journal.
- Stocker le « dernier numéro vu » dans IndexedDB (US-002) : la dépendance n'apporte rien pour une simple valeur d'appareil.

## Conséquences

- Publier = changer `<Version>` **et** ajouter l'entrée en tête de `changelog.json` ; sinon les tests échouent.
- L'interface affichera les textes dans la langue active dès que US-016 / R5 permettra de la choisir ; rien à refaire côté données.
- Les traductions en anglais et en arabe doivent être relues avant d'être montrées aux utilisateurs de ces langues.
- Le journal grossit avec les versions ; si cela pèse, on pourra n'afficher que les N dernières entrées.
- Pour afficher les dates en arabe, il faudra charger toutes les données de globalisation de Blazor WebAssembly (`BlazorWebAssemblyLoadAllGlobalizationData`) : à traiter avec le choix de langue (US-016).
