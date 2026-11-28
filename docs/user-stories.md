# AlFariq — User stories

Version 0.1 — 22/11/2026
Statut : brouillon à valider

---

## 1. Comment lire ce document

### 1.1 Principes de découpage

- Les user stories (US) sont **numérotées et classées dans l'ordre de développement prévu**. Une US ne dépend que d'US de numéro inférieur (sauf mention contraire).
- Le développement avance par **releases (R0 à R6)**. Chaque release est utilisable seule et apporte de la valeur.
- **R0 est un MVP 100 % hors ligne** : aucun compte, aucun serveur, les données restent sur l'appareil. Il permet de tester l'utilité réelle du produit auprès de vrais utilisateurs sans dépendre de l'infrastructure.
- **R1 ajoute les comptes, la synchronisation et les invitations de joueurs réels** : c'est le vrai début du côté « social » de l'appli.
- Les US de R0 et R1 sont détaillées avec critères d'acceptation. Les US des releases suivantes sont volontairement plus courtes : elles seront détaillées juste avant leur développement.
- Les US marquées **[Enabler]** sont techniques. Elles ne sont pas visibles par l'utilisateur mais conditionnent les suivantes.

### 1.2 Format

> **US-xxx — Titre**
> En tant que *[rôle]*, je veux *[action]* afin de *[bénéfice]*.
> Priorité : M (Must) / S (Should) / C (Could) — Taille : S / M / L — Dépend de : US-xxx
> Critères d'acceptation : liste de comportements vérifiables.

Tailles indicatives pour un seul développeur : **S** = environ 1 à 2 jours, **M** = 3 à 5 jours, **L** = plus d'une semaine (à redécouper avant de commencer).

### 1.3 Définition de « terminé » (DoD)

Une US est terminée quand :
1. Les critères d'acceptation sont vérifiés (manuellement ou par test).
2. Le comportement **hors ligne** est vérifié lorsque l'US touche des données (R0 : toujours hors ligne ; ensuite : mode avion testé).
3. Les textes sont en français via des ressources de traduction (pas de texte en dur).
4. L'écran est utilisable sur mobile (360 px de large) et au clavier.
5. Les tests automatisés pertinents sont écrits et la CI est verte.
6. Le déploiement sur l'environnement de test fonctionne.

### 1.4 Rôles utilisés

Les rôles ne sont pas des types de compte fixes mais des **rôles contextuels** (voir spécifications fonctionnelles) :

| Rôle | Description |
|---|---|
| **Utilisateur** | Toute personne qui utilise l'appli, avec ou sans compte. |
| **Joueur** | Participe à un match ou à une séance. |
| **Organisateur** | Crée et gère un match ou une séance. |
| **Entraîneur** | Gère la composition, les séances et les retours d'une équipe. |
| **Président d'équipe** | Gère les membres et les rôles d'une équipe. |
| **Visiteur** | Personne sans compte qui consulte un lien partagé. |

---

## 2. Vue d'ensemble des releases

| Release | Thème | Contenu principal | Connexion requise |
|---|---|---|---|
| **R0** | MVP hors ligne « Match entre potes » | Joueurs, match, équipes, score, historique, stats simples, partage WhatsApp, sauvegarde | Non |
| **R1** | Comptes et invitations | Compte, synchronisation, équipes, invitations, RSVP, liste d'attente, remplaçants | Oui (avec repli hors ligne) |
| **R2** | Organisation | Terrains, réservations, matchs privés/publics, rappels | Oui |
| **R3** | Évaluations et retours | Postes, compétences, notes et commentaires, retours entre joueurs | Oui |
| **R4** | Entraînements et compétitions | Séances, ligues/coupes, classements, statistiques avancées | Oui |
| **R5** | Médias, confiance, finition | Photos, validation de profil, notifications push, langues, administration | Oui |
| **R6** | Applications natives | Mobile (notifications, localisation), desktop (organisateurs) | Oui |

---

## 3. Release R0 — MVP hors ligne

**Objectif :** un organisateur peut, depuis son téléphone et sans compte, préparer un match entre amis (joueurs, équipes équilibrées, composition), partager la composition dans un groupe WhatsApp, saisir le score et retrouver l'historique. Tout fonctionne sans réseau après la première visite.

**Critère de réussite de la release :** des utilisateurs réels organisent au moins 3 matchs de suite avec l'appli et la réutilisent sans qu'on le leur demande.

### US-001 — [Enabler] Socle de l'application installable
En tant que développeur, je veux un squelette d'application PWA (structure de solution, navigation, thème, service worker, manifeste) afin de pouvoir livrer des fonctionnalités rapidement et installables.
Priorité : M — Taille : M
Critères d'acceptation :
- L'appli s'ouvre dans le navigateur et peut être installée sur l'écran d'accueil (Android et iOS).
- Après une première visite en ligne, l'appli s'ouvre en mode avion.
- Une nouvelle version déployée est détectée et l'utilisateur est invité à recharger.
- La solution est découpée en projets (voir spécifications techniques) et la CI compile et exécute les tests.

### US-002 — [Enabler] Stockage local et accès aux données
En tant que développeur, je veux une couche de stockage local (IndexedDB) derrière une interface de dépôt (repository) afin de pouvoir remplacer ou compléter le stockage plus tard (synchronisation).
Priorité : M — Taille : M — Dépend de : US-001
Critères d'acceptation :
- Les entités sont identifiées par des GUID générés côté client et portent `CreatedAt`, `UpdatedAt`, `DeletedAt` (suppression logique).
- Un mécanisme de migration de schéma local existe (version de base locale).
- L'appli demande le stockage persistant au navigateur lorsque c'est possible.
- Des tests couvrent la création, la lecture, la modification et la suppression logique.

### US-003 — Indicateur d'état et écran d'accueil
En tant qu'utilisateur, je veux voir en un coup d'œil si l'appli est hors ligne et retrouver mes prochains matchs sur l'accueil afin de savoir où j'en suis.
Priorité : S — Taille : S — Dépend de : US-001
Critères d'acceptation :
- Un bandeau discret indique l'état hors ligne et disparaît au retour du réseau.
- L'accueil affiche le prochain match (s'il existe), un bouton « Créer un match » et l'accès aux joueurs.
- Un état vide explicite guide le premier lancement.

**Note (US-003)** : tant que les matchs n'existent pas, l'accueil lit `IUpcomingMatchQuery` (implémentation vide à l'époque) et « Créer un match » ouvrait une page d'attente (`/matchs/nouveau`). US-006 a fourni la lecture réelle (`UpcomingMatchQuery`) et le formulaire.

### US-004 — Créer mon profil local
En tant qu'utilisateur, je veux saisir mon nom ou pseudo, mon sport par défaut et mon poste préféré afin de personnaliser l'appli sans créer de compte.
Priorité : M — Taille : S — Dépend de : US-002
Critères d'acceptation :
- Le profil est créé en moins de 30 secondes (seul le pseudo est obligatoire).
- Le sport par défaut est « Football » ; la liste de sports est extensible.
- Le profil local est modifiable et persiste après fermeture de l'appli.
- Je suis ajouté automatiquement à mon répertoire de joueurs.

**Note (US-004)** : le catalogue des sports et des postes est dans `SportCatalog` (domaine). Le joueur « moi » est lié au profil par `Player.ProfileId` ; son nom et son poste sont réécrits à chaque enregistrement du profil. US-005 décidera s'il peut être modifié, archivé ou supprimé depuis le répertoire.

### US-005 — Gérer mon répertoire de joueurs
En tant qu'organisateur, je veux ajouter, modifier, archiver et retrouver des joueurs (nom, poste préféré, niveau perçu optionnel) afin de composer rapidement mes matchs.
Priorité : M — Taille : M — Dépend de : US-002
Critères d'acceptation :
- On peut ajouter un joueur avec seulement un prénom ou un pseudo.
- Le niveau perçu est une valeur de 1 à 5, facultative.
- Un joueur archivé n'apparaît plus dans les sélections mais reste visible dans l'historique des matchs passés.
- La recherche et le tri par nom fonctionnent sur au moins 200 joueurs sans ralentissement visible.
- Les doublons de nom déclenchent un avertissement, pas un blocage.

**Note (US-005)** : le joueur « moi » tient son nom et son poste du profil ; seul son niveau se modifie dans le répertoire, et il ne s'archive pas. Les futures sélections (US-007) utilisent `PlayerService.ListSelectableAsync` ; l'historique retrouve un joueur avec `FindAsync`, même archivé. La suppression d'un joueur (RG-JOU-04) et les notes privées (RG-JOU-01) ne sont pas dans cette US.

### US-006 — Créer un match
En tant qu'organisateur, je veux créer un match avec date, heure, lieu (texte libre), sport, format (par exemple 5 contre 5, 7 contre 7, 11 contre 11) et durée afin de préparer la rencontre.
Priorité : M — Taille : M — Dépend de : US-005
Critères d'acceptation :
- Les champs date, heure et format sont obligatoires ; le lieu et les notes sont facultatifs.
- Le format fixe le nombre de joueurs par équipe et reste modifiable.
- Un match est créé à l'état « Planifié ».
- On peut modifier, dupliquer et supprimer (suppression logique) un match.
- « Dupliquer » préremplit un nouveau match (même lieu, même format, mêmes joueurs, date + 7 jours).

**Note (US-006)** : les joueurs d'un match sont des entités `MatchParticipant` ; US-007 ajoutera leur sélection. La duplication reprend le sport, le format, la durée, l'heure et le lieu à J+7, ainsi que les joueurs non archivés (inscrits à l'enregistrement de la copie) ; les notes ne sont pas reprises. Un match reste « à venir » tant qu'il n'est pas terminé (début + durée).

### US-007 — Choisir les joueurs d'un match
En tant qu'organisateur, je veux sélectionner les joueurs présents depuis mon répertoire (et en ajouter un à la volée) afin de savoir qui joue.
Priorité : M — Taille : S — Dépend de : US-006
Critères d'acceptation :
- La sélection se fait en quelques taps (cases à cocher, recherche).
- Un compteur indique « X joueurs sur Y attendus ».
- L'ajout rapide d'un joueur depuis cet écran l'ajoute aussi au répertoire.
- Un avertissement apparaît si le nombre de joueurs est inférieur ou supérieur au format, sans bloquer.

**Note (US-007)** : le nombre de joueurs attendus est le double du format (`Match.ExpectedPlayers`). La sélection (`/matchs/{id}/joueurs`, `SquadService`) est enregistrée à chaque case cochée ; retirer un joueur supprime logiquement son inscription. Un joueur archivé reste sur les matchs où il est inscrit, mais ne s'ajoute pas à un autre. Les statuts de participant (RG-PRS-01) ne sont pas dans cette US.

### US-008 — Composer les équipes à la main
En tant qu'organisateur, je veux répartir les joueurs entre deux équipes (A et B) afin de définir la composition du match.
Priorité : M — Taille : M — Dépend de : US-007
Critères d'acceptation :
- Chaque joueur est assigné à l'équipe A, à l'équipe B, ou reste « non assigné ».
- On peut nommer et colorer chaque équipe.
- On peut déplacer un joueur d'une équipe à l'autre en un geste (glisser-déposer ou bouton).
- Un joueur ne peut pas être dans deux équipes en même temps.
- La composition est enregistrée automatiquement.

**Note (US-008)** : l'équipe d'un joueur est `MatchParticipant.Side` (un seul champ, donc jamais deux équipes) ; le nom et la couleur sont dans `MatchSide`, créé à la première personnalisation (`LineupService`, `/matchs/{id}/equipes`). Le déplacement se fait par boutons A / B ; le glisser-déposer n'est pas fourni. Un joueur retiré du match perd son équipe. La duplication d'un match ne reprend pas la composition. L'annulation de la dernière action (RG-COM-06) n'est pas dans cette US.

### US-009 — Proposer des équipes équilibrées
En tant qu'organisateur, je veux qu'une suggestion de répartition équilibrée soit calculée à partir du niveau perçu afin de gagner du temps et d'éviter les débats.
Priorité : S — Taille : M — Dépend de : US-008
Critères d'acceptation :
- Le calcul se fait localement, sans réseau.
- L'écart total de niveau entre les deux équipes est minimal et les effectifs diffèrent d'au plus un joueur.
- Les joueurs sans niveau reçoivent un niveau par défaut (3) et un avertissement l'indique.
- L'organisateur peut verrouiller certains joueurs dans une équipe avant de relancer le calcul.
- Un bouton « Autre proposition » donne une répartition différente d'un score équivalent.
- La suggestion n'est jamais appliquée sans validation.

**Note (US-009)** : le calcul est dans le domaine (`TeamBalancer`), exact et local ; l'écran `/matchs/{id}/equipes/proposition` n'enregistre rien avant « Appliquer ». Il cherche d'abord le plus petit écart de niveau, puis, à écart égal, sépare les joueurs d'un même poste (RG-EQU-07, étendue à tous les postes du sport du match, gardiens d'abord). « Autre proposition » garde la tolérance de 1 point (RG-EQU-05) : le niveau total étant fixe, les écarts possibles vont de 2 en 2, l'écart reste donc identique. Échanger les deux équipes ne compte pas comme une autre proposition. Les verrous valent pour la visite de l'écran et ne sont pas enregistrés. Le niveau combiné aux évaluations (RG-EQU-01, à partir de R3) n'est pas dans cette US.

### US-010 — Attribuer des postes et visualiser la composition
En tant qu'organisateur, je veux affecter un poste à chaque joueur et voir la composition sur un terrain schématique afin d'avoir une vue claire de l'équipe.
Priorité : S — Taille : L — Dépend de : US-008
Critères d'acceptation :
- Le catalogue de postes du football est fourni (gardien, défenseur, milieu, attaquant, avec variantes).
- Quelques formations prédéfinies existent selon le format (par exemple 4-4-2 en 11 contre 11, 2-2-1 en 5 contre 5).
- Un joueur peut être placé sur un emplacement du terrain et remplacé par un autre.
- Le visuel reste lisible sur un écran de téléphone en mode portrait.
- Les postes sont optionnels : la composition sans postes reste valide.

**Note (US-010)** : le catalogue ajoute des variantes aux quatre postes du football (`SportCatalog`) ; l'équilibrage (US-009) les compte comme leur poste général. Les formations sont dans `FormationCatalog` : 2-1-1, 1-2-1 et 2-2-1 (sans gardien attitré, 2-2-1 faisant cinq joueurs) en 5 contre 5 ; 2-3-1, 3-2-1 et 2-2-2 en 7 contre 7 ; 4-4-2, 4-3-3, 4-2-3-1 et 3-5-2 en 11 contre 11. Sur le terrain (`/matchs/{id}/equipes/terrain`, une équipe à la fois), le poste vient de l'emplacement (`MatchParticipant.Slot`, `MatchSide.FormationCode`) ; placer un joueur déjà sur le terrain échange les deux places. Sans formation (autre sport, format libre ou par choix), un poste se donne joueur par joueur. Changer un joueur d'équipe libère son emplacement. Si le format du match change, la formation enregistrée n'est plus appliquée. Le glisser-déposer et l'annulation (RG-COM-06) ne sont pas dans cette US.

### US-011 — Partager la composition
En tant qu'organisateur, je veux partager la composition (image ou texte) vers WhatsApp ou une autre appli afin d'informer tout le monde sans qu'ils aient l'appli.
Priorité : M — Taille : M — Dépend de : US-008
Critères d'acceptation :
- Un bouton « Partager » utilise le partage natif de l'appareil lorsqu'il est disponible.
- Un repli « Copier le texte » existe lorsque le partage natif est indisponible.
- Le texte contient date, heure, lieu, équipes et joueurs, lisible dans WhatsApp.
- Une image de la composition est générée localement.

**Note (US-011)** : l'écran `/matchs/{id}/partage` montre le message et l'image avant l'envoi. La même carte (`LineupCardBuilder`) donne les deux ; elle ne contient ni les niveaux (RG-JOU-06) ni les notes du match. Le partage passe par `IShareService` (navigateur : `share.js`) ; l'image est dessinée sur un canevas (`lineup-image.js`), avec le terrain pour une équipe qui a une formation et des joueurs placés. « Copier le texte » reste proposé même quand le partage natif existe ; sans partage de fichiers, l'image se télécharge. Texte et image se partagent séparément. La page de match en ligne est l'US-030.

### US-012 — Saisir le résultat d'un match
En tant qu'organisateur, je veux saisir le score et, si je le souhaite, les buteurs afin de garder une trace des résultats.
Priorité : M — Taille : M — Dépend de : US-008
Critères d'acceptation :
- Le score de chaque équipe se saisit en deux taps (boutons + et −).
- Les buteurs et passeurs sont facultatifs.
- Le statut du match passe à « Terminé » et le résultat reste modifiable.
- Un match peut être « Annulé » avec un motif facultatif.
- Les buts saisis par joueur sont cohérents avec le score (avertissement en cas d'écart).

**Note (US-012)** : la saisie est sur `/matchs/{id}/resultat` (`ResultService`). Le match passe « Terminé » à la validation, pas au premier tap (RG-RES-05) ; le score est porté par `MatchSide.Score`. Les buts et passes sont des `MatchEvent` (magasin `match_events`, migration 5) désignés par match et joueur, sans minute pour l'instant ; seuls les joueurs d'une équipe comptent. Buteurs et passeurs ne sont proposés que pour le football, le futsal et le handball. L'écart est signalé par équipe dès qu'au moins un but y est attribué. Seul un match « Planifié » s'annule ; il peut être rétabli, et un résultat saisi par erreur peut être effacé. Le sport et le format d'un match terminé sont figés (RG-MAT-02).

### US-013 — Historique des matchs
En tant qu'utilisateur, je veux consulter la liste de mes matchs passés et à venir, filtrer par statut et ouvrir le détail afin de retrouver une rencontre.
Priorité : M — Taille : S — Dépend de : US-012
Critères d'acceptation :
- Deux onglets : « À venir » et « Passés ».
- Le détail d'un match affiche équipes, composition, score et buteurs.
- Un match peut être retrouvé par date ou par nom de joueur.

**Note (US-013)** : la liste `/matchs` filtre les deux onglets à la fois par nom de joueur (partie du nom, sans tenir compte de la casse ni des accents) et par jour affiché (`MatchSearch`, `MatchHistoryService`) ; les joueurs archivés ou supprimés restent trouvables sur leurs matchs (RG-JOU-03 et 04). Le filtre par statut ne propose que les statuts présents dans l'onglet et n'apparaît que s'il y en a au moins deux ; un match passé sans résultat reste « Planifié ». L'onglet et la recherche sont gardés tant que l'application est ouverte (`MatchListState`). La fiche présente la composition comme le partage : formation dès qu'un joueur est placé, titulaires depuis le gardien, puis remplaçants ; le terrain dessiné reste sur son écran. Le filtrage se fait en mémoire, sans index IndexedDB à ce stade.

### US-014 — Statistiques de base
En tant qu'utilisateur, je veux voir mes statistiques et celles de mes joueurs (matchs joués, victoires, nuls, défaites, buts) afin de mesurer l'évolution.
Priorité : S — Taille : M — Dépend de : US-012
Critères d'acceptation :
- Les statistiques sont calculées à partir de l'historique, sans donnée dupliquée.
- Un classement des joueurs par victoires ou par buts est disponible.
- On peut filtrer sur une période.
- Les statistiques ne prennent en compte que les matchs « Terminés ».

### US-015 — Exporter et importer mes données
En tant qu'utilisateur, je veux sauvegarder toutes mes données dans un fichier et les restaurer afin de ne rien perdre si mon navigateur efface le stockage ou si je change d'appareil.
Priorité : M — Taille : M — Dépend de : US-002
Critères d'acceptation :
- L'export génère un fichier JSON versionné contenant toutes les entités.
- L'import vérifie la version, signale les conflits et propose « Fusionner » ou « Remplacer ».
- Un rappel invite à sauvegarder après un certain nombre de matchs saisis ou de jours sans export.
- Un fichier invalide est refusé avec un message clair, sans modifier les données existantes.

### US-016 — Paramètres de base
En tant qu'utilisateur, je veux régler la langue (français pour commencer), le thème clair/sombre et mon sport par défaut afin d'adapter l'appli.
Priorité : C — Taille : S — Dépend de : US-004
Critères d'acceptation :
- Le thème suit par défaut celui de l'appareil et peut être forcé.
- Tous les textes passent par des ressources de traduction.
- Un lien « À propos » indique la version et le mode de stockage des données.

### US-017 — Versionnement et journal des changements
En tant qu'utilisateur, je veux voir la version que j'utilise et lire ce qui a changé depuis ma dernière visite afin de comprendre les nouveautés et de savoir si mon appli est à jour ; en tant que développeur, je veux une source unique pour la version et pour le journal afin de publier sans oubli.
Priorité : S — Taille : S — Dépend de : US-001 (la sélection de la langue d'affichage viendra avec US-016 puis R5)
Critères d'acceptation :
- La version suit le **versionnement sémantique** (`MAJEUR.MINEUR.CORRECTIF`, avant 1.0 : chaque lot de fonctionnalités incrémente le numéro mineur) et est définie **en un seul endroit** (`Directory.Build.props`). L'écran « À propos » l'affiche, sans suffixe de build.
- Le journal des changements est un **fichier unique et versionné** (`changelog.json`), embarqué dans l'appli : il s'affiche **sans réseau**. Chaque entrée porte une version, une date (AAAA-MM-JJ) et des changements classés en *Nouveautés*, *Améliorations* ou *Corrections*.
- Chaque changement est rédigé en **français (obligatoire), anglais et arabe (facultatifs pour l'instant)**. Un texte absent dans la langue d'affichage est remplacé par le français, balisé avec sa langue et son sens d'écriture (`lang`, `dir`) pour s'afficher correctement, y compris en arabe (de droite à gauche).
- Un écran « Nouveautés » liste les versions de la plus récente à la plus ancienne et repère la version installée. Il est accessible depuis « À propos ».
- Après une mise à jour, un bandeau discret propose de voir les nouveautés (une seule fois par version, fermable). Il n'apparaît **jamais au premier lancement**, ni quand le journal ne contient rien de nouveau depuis la dernière version vue.
- Des contrôles automatiques échouent si : une version est invalide ou en double, les entrées ne sont pas classées, une date est invalide, un texte français manque, une langue inconnue apparaît, ou si la **dernière entrée du journal ne correspond pas à la version de l'appli**.

**Analyse (US-017)**
- *Pourquoi maintenant :* R0 sera testé par de vrais utilisateurs ; leur dire ce qui change (et savoir quelle version ils ont quand ils font un retour) vaut mieux que de le découvrir par hasard. Le mécanisme de mise à jour (US-001) existe déjà ; cette US lui donne un sens lisible.
- *Source unique :* un seul fichier de journal, lu par l'appli et vérifié en test. Un `CHANGELOG.md` séparé divergerait tôt ou tard ; il pourra être généré à partir du JSON si besoin (notes de version GitHub).
- *Langues :* l'interface reste en français jusqu'à US-016 / R5, mais les données du journal sont prêtes pour l'anglais et l'arabe. Les traductions arabes et anglaises sont à faire relire avant publication (hors R0, le français fait foi).
- *Hors périmètre :* notes de version envoyées par notification (R5), journal côté serveur (R1), numéro de build ou identifiant de commit affiché (utile au support, à ajouter si besoin).
- *Risques :* oublier de mettre à jour le journal (couvert par le contrôle de cohérence) ; bandeau jugé intrusif (limité à une fois par version, fermable) ; stockage du « dernier numéro vu » effacé par le navigateur (le bandeau ne s'affiche alors pas : sans gravité).

**Fin de R0 — point de décision :** avant de lancer R1, analyser l'usage réel (matchs créés, retours des testeurs). Les US de R1 peuvent être réordonnées.

---

## 4. Release R1 — Comptes, synchronisation et invitations

**Objectif :** un utilisateur crée un compte, retrouve ses données sur plusieurs appareils, invite de vrais joueurs qui répondent depuis leur téléphone. L'appli continue de fonctionner hors ligne.

### US-020 — [Enabler] Backend, base et déploiement
En tant que développeur, je veux une API .NET connectée à Supabase et déployée automatiquement afin de disposer d'un environnement de test et de production.
Priorité : M — Taille : L
Critères d'acceptation :
- L'API répond sur un point de santé (`/health`) en environnement de test.
- La base est créée par migrations versionnées.
- Le déploiement est déclenché par la CI à chaque fusion sur la branche principale.
- Les secrets ne sont jamais dans le dépôt.
- Les journaux et les erreurs sont consultables.

### US-021 — Créer un compte et se connecter
En tant qu'utilisateur, je veux créer un compte et me connecter (e-mail avec lien magique ou mot de passe) afin de retrouver mes données ailleurs.
Priorité : M — Taille : M — Dépend de : US-020
Critères d'acceptation :
- L'inscription et la connexion fonctionnent depuis le téléphone en moins d'une minute.
- La session persiste et se renouvelle sans action de l'utilisateur.
- Le mot de passe oublié est géré.
- L'appli reste utilisable hors ligne pour un utilisateur déjà connecté.
- Un utilisateur non connecté continue d'utiliser R0 sans restriction.

### US-022 — Synchronisation hors ligne / en ligne
En tant qu'utilisateur connecté, je veux que mes modifications faites hors ligne soient envoyées au retour du réseau et que les changements d'autres appareils m'arrivent afin de travailler sans me soucier de la connexion.
Priorité : M — Taille : L — Dépend de : US-020, US-021, US-002
Critères d'acceptation :
- Les modifications hors ligne sont mises en file d'attente puis envoyées dans l'ordre.
- La synchronisation est idempotente : rejouer une requête ne crée pas de doublon.
- Les conflits sont résolus par « dernière modification gagnante » avec journalisation.
- Un indicateur montre l'état (synchronisé, en attente, erreur) et permet de relancer.
- Les suppressions logiques se propagent.

### US-023 — Migrer mes données locales vers mon compte
En tant qu'utilisateur, je veux que mes joueurs et mes matchs créés avant l'inscription soient rattachés à mon compte afin de ne rien perdre.
Priorité : M — Taille : M — Dépend de : US-021, US-022
Critères d'acceptation :
- Au premier login, l'appli propose de rattacher les données locales au compte.
- Aucun doublon n'est créé si l'opération est relancée.
- Les identifiants GUID existants sont conservés.
- En cas d'échec partiel, les données locales restent intactes.

### US-024 — Profil complet et rôles contextuels
En tant qu'utilisateur, je veux compléter mon profil (photo plus tard, bio, sports, poste préféré, visibilité) afin de me présenter aux autres joueurs.
Priorité : S — Taille : M — Dépend de : US-021
Critères d'acceptation :
- Les rôles (joueur, organisateur, entraîneur, président) dépendent du contexte (équipe, match) et non du compte.
- La visibilité du profil est réglable : privé, visible par mes équipes, public.
- Mon profil local de R0 est fusionné avec mon profil de compte.

### US-025 — Créer et gérer une équipe
En tant que président d'équipe, je veux créer une équipe, définir ses membres et leurs rôles afin d'organiser plusieurs matchs avec le même groupe.
Priorité : M — Taille : M — Dépend de : US-021
Critères d'acceptation :
- Une équipe a un nom, un sport, une couleur et un créateur qui en est président.
- Les rôles possibles sont président, entraîneur, organisateur, joueur.
- Un membre peut être un joueur invité (sans compte) ou un utilisateur réel.
- Seuls le président et l'entraîneur modifient la composition de l'équipe.
- Un président ne peut pas quitter l'équipe sans transmettre ce rôle.

### US-026 — Inviter des joueurs réels
En tant qu'organisateur, je veux inviter des personnes à une équipe ou à un match par lien partageable afin qu'elles rejoignent sans friction.
Priorité : M — Taille : L — Dépend de : US-025
Critères d'acceptation :
- Un lien d'invitation est généré et partageable par WhatsApp, SMS ou e-mail.
- Le lien expire (durée réglable) et peut être révoqué.
- Une personne sans compte voit une page d'accueil claire et peut répondre après une inscription courte.
- Le lien affiche un aperçu lisible dans les messageries (titre, date, lieu).
- Une invitation ne peut pas être utilisée au-delà du nombre de places.

### US-027 — Répondre à une invitation (RSVP)
En tant que joueur invité, je veux répondre « Je viens », « Je ne viens pas » ou « Peut-être » afin que l'organisateur sache sur qui compter.
Priorité : M — Taille : M — Dépend de : US-026
Critères d'acceptation :
- La réponse peut être modifiée jusqu'à une date limite définie par l'organisateur.
- L'organisateur voit en temps réel qui a répondu quoi.
- Une réponse tardive (après la date limite) est signalée à l'organisateur.
- Les réponses sont visibles pour tous les participants si l'organisateur l'a décidé.

### US-028 — Lier un joueur local à un compte réel
En tant qu'organisateur, je veux relier un joueur de mon répertoire (créé sans compte) au compte de la personne quand elle s'inscrit afin de conserver son historique.
Priorité : S — Taille : M — Dépend de : US-026
Critères d'acceptation :
- Une personne qui rejoint par invitation peut être associée à un joueur existant.
- L'historique du joueur (matchs, statistiques) suit le compte après liaison.
- La liaison est refusée si le joueur est déjà lié à un autre compte.
- Une liaison peut être annulée par l'organisateur.

### US-029 — Liste d'attente et remplaçants
En tant qu'organisateur, je veux fixer un nombre maximum de joueurs, gérer une liste d'attente et désigner des remplaçants afin de gérer les désistements.
Priorité : M — Taille : M — Dépend de : US-027
Critères d'acceptation :
- Au-delà de la capacité, les nouveaux inscrits entrent en liste d'attente avec un rang.
- Quand une place se libère, le premier en attente est promu automatiquement (réglable : automatique ou à confirmer par l'organisateur) et prévenu.
- Un joueur peut être marqué « remplaçant » dans la composition.
- Le rang d'attente est visible par le joueur concerné.

### US-030 — Page de match partageable en lecture seule
En tant qu'organisateur, je veux partager un lien vers une page de match consultable sans compte afin d'afficher la composition et le score aux visiteurs.
Priorité : S — Taille : M — Dépend de : US-026
Critères d'acceptation :
- La page affiche équipes, composition et score, mais jamais de données privées (e-mails, notes).
- L'aperçu du lien dans les messageries est correct.
- Le lien peut être désactivé par l'organisateur.

### US-031 — Centre de notifications dans l'appli
En tant qu'utilisateur, je veux voir dans l'appli les événements qui me concernent (invitation, promotion depuis la liste d'attente, match modifié ou annulé) afin de ne rien manquer.
Priorité : S — Taille : M — Dépend de : US-027
Critères d'acceptation :
- Les notifications sont conservées et marquées comme lues.
- Un badge indique le nombre de non-lues.
- Une notification renvoie vers l'écran concerné.
- Les notifications reçues hors ligne apparaissent à la prochaine synchronisation.

### US-032 — Données personnelles : export et suppression du compte
En tant qu'utilisateur, je veux exporter mes données et supprimer mon compte afin de garder le contrôle de ma vie privée.
Priorité : M — Taille : M — Dépend de : US-021
Critères d'acceptation :
- L'export fournit un fichier lisible regroupant mes données.
- La suppression est confirmée par une étape explicite et demande une reconnexion récente.
- Mes données personnelles sont supprimées ou anonymisées selon des règles documentées ; les matchs partagés restent cohérents pour les autres participants.
- Un texte de politique de confidentialité est accessible depuis l'appli.

**Fin de R1 — point de décision :** à ce stade, l'appli peut être testée avec un public large. Décider de l'ordre de R2 et R3 selon les retours.

---

## 5. Release R2 — Terrains, réservations et visibilité des matchs

Objectif : gérer le lieu et la réservation, et ouvrir certains matchs à des joueurs hors de son cercle.

- **US-040 — Gérer les stades et terrains.** En tant qu'organisateur, je veux enregistrer des stades et leurs terrains (nom, adresse, surface, format, coût, contact) afin de les réutiliser. Priorité : M — Taille : M — Dépend de : US-025.
- **US-041 — Associer un match à un terrain.** Le lieu en texte libre de R0 est remplacé par un terrain choisi dans la liste, sans perdre les anciens matchs. Priorité : M — Taille : S — Dépend de : US-040.
- **US-042 — Suivre une réservation.** En tant qu'organisateur, je veux noter l'état d'une réservation (à réserver, réservée, annulée), son créneau, son coût et son responsable. Priorité : M — Taille : M — Dépend de : US-041.
- **US-043 — Détecter les conflits de créneaux.** Alerte lorsque deux réservations se chevauchent sur le même terrain ou lorsqu'un joueur est convoqué à deux matchs simultanés. Priorité : S — Taille : M — Dépend de : US-042.
- **US-044 — Partager le coût d'un match.** Calcul du coût par joueur et suivi du paiement (déclaratif, sans paiement en ligne). Priorité : C — Taille : M — Dépend de : US-042.
- **US-045 — Rappels avant match.** Notifications dans l'appli (puis push en R5) à J-1 et le jour même. Priorité : S — Taille : M — Dépend de : US-031.
- **US-046 — Match privé ou public.** En tant qu'organisateur, je veux choisir la visibilité d'un match et des critères d'accès (niveau, sport, nombre de places, validation manuelle). Priorité : M — Taille : M — Dépend de : US-029.
- **US-047 — Découvrir les matchs publics et demander à rejoindre.** Liste filtrable (sport, date, ville, niveau), demande de participation, acceptation ou refus par l'organisateur. Priorité : S — Taille : L — Dépend de : US-046.

---

## 6. Release R3 — Évaluations et retours

Objectif : donner de la valeur à long terme via les notes, les commentaires et les retours, dans un cadre bienveillant et maîtrisé.

- **US-050 — Catalogue de postes par sport et postes d'un joueur.** Un joueur a un ou plusieurs postes, avec un poste principal. Priorité : M — Taille : M — Dépend de : US-010.
- **US-051 — Note et commentaire par poste et par joueur.** Un entraîneur ou un organisateur évalue un joueur sur un poste donné. Priorité : M — Taille : M — Dépend de : US-050.
- **US-052 — Catalogue de compétences par sport.** Liste de compétences (par exemple passe, tir, placement, endurance) personnalisable par équipe. Priorité : M — Taille : M.
- **US-053 — Note et commentaire par compétence et par joueur.** Priorité : M — Taille : M — Dépend de : US-052.
- **US-054 — Retour d'un joueur sur son propre poste.** Auto-évaluation après un match et comparaison avec les évaluations reçues. Priorité : S — Taille : M — Dépend de : US-051.
- **US-055 — Retour d'un joueur sur les autres joueurs.** Après un match, les participants peuvent noter et commenter leurs coéquipiers selon des règles de visibilité (voir spécifications fonctionnelles). Priorité : S — Taille : L — Dépend de : US-053.
- **US-056 — Note et commentaire de match.** Évaluation du match (ambiance, terrain, organisation) et vote du joueur du match. Priorité : S — Taille : M — Dépend de : US-012.
- **US-057 — Synthèse et évolution d'un joueur.** Moyennes par compétence et poste, courbes d'évolution. Priorité : S — Taille : M — Dépend de : US-053.
- **US-058 — Modération et signalement.** Signaler un commentaire, le masquer, bloquer un utilisateur. Priorité : M — Taille : M — Dépend de : US-055. À livrer **avant** d'ouvrir les retours entre joueurs à un large public.
- **US-059 — Équilibrage amélioré par les notes.** L'algorithme de US-009 utilise les évaluations reçues en plus du niveau perçu. Priorité : C — Taille : S — Dépend de : US-057.

---

## 7. Release R4 — Entraînements et compétitions

- **US-060 — Gérer une séance d'entraînement.** Date, lieu, objectifs, convocation, présences, notes. Priorité : S — Taille : M — Dépend de : US-025.
- **US-061 — Créer une compétition (ligue ou coupe).** Nom, format, règles de points, équipes participantes. Priorité : S — Taille : L.
- **US-062 — Générer un calendrier.** Championnat aller simple/aller-retour et élimination directe, avec réglage des dates. Priorité : S — Taille : L — Dépend de : US-061.
- **US-063 — Saisir les résultats et obtenir le classement.** Classement automatique avec règles de départage paramétrables. Priorité : S — Taille : M — Dépend de : US-062.
- **US-064 — Note et commentaire par compétition.** Priorité : C — Taille : S — Dépend de : US-063.
- **US-065 — Statistiques avancées.** Tendances par joueur, par équipe, par compétition, comparaisons. Priorité : C — Taille : L — Dépend de : US-063.

---

## 8. Release R5 — Médias, confiance et finition

- **US-070 — Photos.** Avatar, photos de match et d'équipe, compression côté appareil, stockage sécurisé, suppression. Priorité : S — Taille : M.
- **US-071 — Validation de profil.** Niveaux de confiance (e-mail vérifié, téléphone vérifié, validé par un organisateur ou un club) avec badge. Priorité : S — Taille : M.
- **US-072 — Notifications push.** Abonnement aux notifications web (installation de la PWA requise sur iOS), préférences par type. Priorité : S — Taille : L — Dépend de : US-031.
- **US-073 — Langues supplémentaires.** Anglais et arabe, avec prise en charge de l'écriture de droite à gauche. Priorité : C — Taille : L.
- **US-074 — Administration et modération.** Outil minimal pour traiter les signalements et suspendre un compte. Priorité : S — Taille : M — Dépend de : US-058.
- **US-075 — Accessibilité et performances.** Audit (lecteur d'écran, contrastes, budget de performance) et correctifs. Priorité : S — Taille : M.

---

## 9. Release R6 — Applications natives

- **US-080 — Application mobile.** Réutilisation de l'interface web dans une application native pour les notifications natives et la géolocalisation (matchs proches, aide à la localisation du terrain). Priorité : C — Taille : L.
- **US-081 — Application desktop pour organisateurs et gestionnaires.** Vues denses (calendrier, plusieurs équipes, exports). Priorité : C — Taille : L.

---

## 10. Suggestions ajoutées par rapport à la description initiale

| Ajout | Pourquoi |
|---|---|
| Export / import des données (US-015) | Les données locales peuvent être effacées par le navigateur : indispensable pour un mode hors ligne fiable. |
| Partage de la composition (US-011) | Les groupes de joueurs vivent dans WhatsApp : c'est le moyen le plus rapide de démontrer la valeur dès R0. |
| Équilibrage automatique (US-009) | Fonctionnalité visible et amusante dès le MVP, qui incite à renseigner le niveau des joueurs. |
| Joueur invité lié ensuite à un compte (US-028) | Permet de démarrer sans exiger que chaque joueur crée un compte. |
| Rôles contextuels (US-024) | Une même personne est organisatrice d'un match et simple joueuse d'un autre. |
| Suppression de compte et export de données (US-032) | Obligation RGPD pour des utilisateurs dans l'Union européenne, et condition des boutiques d'applications en R6. |
| Modération avant les retours entre joueurs (US-058) | Les commentaires sur autrui sont le principal risque de conflit et d'abus. |
| Page de match en lecture seule (US-030) | Donne de la visibilité sans obliger les spectateurs à s'inscrire. |

## 11. Questions ouvertes

1. **Échelle de notation** : 1 à 5 étoiles ou 1 à 10 ? (Cette version suppose 1 à 5.)
2. **Visibilité des retours entre joueurs** : anonymes agrégés, nominatifs, ou au choix de l'équipe ?
3. **Mineurs** : des joueurs de moins de 18 ans seront-ils concernés (équipes amateur, jeunes) ? Cela impose des règles de consentement et de visibilité.
4. **Réservation** : suivi manuel seulement, ou intégration à terme avec des plateformes de réservation de terrains ?
5. **Paiement** : partage de coûts déclaratif suffisant, ou paiement en ligne à terme ?
6. **Modèle économique** : gratuit au démarrage, puis offre pour clubs et organisateurs ? Cela peut influencer les US de R4 à R6.
7. **Langue de départ** : français seul pour R0 et R1 ?
