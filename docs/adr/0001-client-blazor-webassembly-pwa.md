# ADR 0001 — Client Blazor WebAssembly en PWA

Statut : proposé — 22/11/2026

## Contexte

- Un seul développeur .NET.
- Livraison initiale en PWA, utilisable sans réseau (MVP hors ligne, R0).
- Évolution prévue vers des applications mobile et desktop.

## Décision

- Client en **Blazor WebAssembly autonome** (sans hôte serveur), avec service worker et manifeste.
- Toute l'interface dans une **bibliothèque de classes Razor** (`AlFariq.UI`) ; l'hôte `AlFariq.Web` ne contient que la composition et les fichiers PWA. Une future application MAUI Blazor Hybrid pourra réutiliser `AlFariq.UI`.
- Dernière version **LTS** de .NET (10, à confirmer au premier build), épinglée dans `global.json`.
- Textes de l'interface dans des **ressources de traduction** (`Strings.resx`), français par défaut.
- Mise à jour du service worker **à la demande de l'utilisateur** (pas de rechargement automatique pendant une saisie).

## Alternatives écartées

- Blazor Server : pas de fonctionnement hors ligne.
- React ou Angular : deuxième langage et deuxième chaîne d'outils pour un développeur seul.
- MAUI seul : pas de PWA, donc pas d'accès rapide à un large public.

## Conséquences

- Premier chargement plus lourd qu'avec un framework JavaScript (cache ensuite). Un budget de taille sera fixé et mesuré en CI.
- Pas de rendu serveur : les aperçus de liens partagés (R1) seront servis par l'API.
- L'hébergement statique reste à choisir ; le chemin de base (`<base href>` et `base` du service worker) devra être adapté en cas d'hébergement dans un sous-dossier.
