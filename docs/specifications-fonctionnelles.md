# AlFariq — Spécifications fonctionnelles

Version 0.1 — 22/11/2026
Statut : brouillon à valider
Documents liés : `AlFariq-user-stories.md`, `AlFariq-specifications-techniques.md`

---

## 1. Contexte et objectifs

AlFariq est une application de gestion de matchs de sport entre amis. Le football sert d'exemple et de premier sport, mais le modèle doit rester **multi-sports**.

**Objectifs produit**
1. Permettre à un organisateur de préparer un match (joueurs, équipes, composition) en quelques minutes.
2. Faire vivre le groupe : invitations, présences, remplaçants, historique, statistiques.
3. Donner de la valeur dans la durée : retours entre joueurs, compétences, compétitions.
4. Atteindre rapidement un public réel avec une application installable (PWA), fiable même sans connexion.

**Public visé** : amis proches, collègues, équipes de football loisir et équipes amateur, avec d'autres sports possibles ensuite.

**Principes directeurs**
- **Simple d'abord** : le cas courant (organiser un match entre potes) doit demander très peu de saisies.
- **Utile sans compte** : le MVP (R0) fonctionne entièrement sans connexion ni compte.
- **Progressif** : chaque fonctionnalité avancée est facultative (joueurs, postes, notes...).
- **Bienveillant** : les évaluations entre joueurs sont encadrées pour éviter les abus.
- **Multi-sports** : aucun terme « football » en dur dans le modèle.

---

## 2. Périmètre

### 2.1 Dans le périmètre par release

| Release | Périmètre fonctionnel |
|---|---|
| **R0** | Profil local, répertoire de joueurs, match, sélection des joueurs, équipes (manuel et automatique), postes et schéma de terrain, partage, résultat, historique, statistiques de base, export/import, paramètres. Tout en local. |
| **R1** | Compte, synchronisation, profil complet, équipes, invitations par lien, réponses de présence, liste d'attente, remplaçants, page de match partageable, notifications dans l'appli, droits sur les données personnelles. |
| **R2** | Stades et terrains, réservations, conflits de créneaux, partage de coût, rappels, matchs privés/publics, découverte et demandes de participation. |
| **R3** | Postes d'un joueur, compétences, notes et commentaires, retours (sur soi, sur les autres, sur le match), synthèses, modération. |
| **R4** | Séances d'entraînement, compétitions (ligue/coupe), calendrier, classement, statistiques avancées. |
| **R5** | Photos, validation de profil, notifications push, langues, administration. |
| **R6** | Applications mobile et desktop. |

### 2.2 Hors périmètre (à ce stade)
- Paiement en ligne et gestion financière réelle.
- Intégration avec des plateformes tierces de réservation de terrains.
- Diffusion vidéo ou suivi en direct des matchs avec chronomètre synchronisé.
- Messagerie instantanée interne (le partage passe par WhatsApp et les messageries existantes).
- Gestion de licences fédérales et d'arbitrage officiel.

---

## 3. Acteurs et rôles

### 3.1 Acteurs

| Acteur | Description |
|---|---|
| **Utilisateur sans compte** | Utilise l'appli en local (R0). Ses données restent sur son appareil. |
| **Utilisateur connecté** | A un compte (R1+). Ses données sont synchronisées. |
| **Visiteur** | Ouvre un lien partagé (invitation ou page de match) sans être connecté. |
| **Administrateur** | Modère les signalements et gère les comptes (R5). |

### 3.2 Rôles contextuels

Les rôles « joueur, organisateur, entraîneur, président d'équipe » **ne sont pas des types de profil**. Ce sont des **rôles attribués dans un contexte** :

- Dans une **équipe** : président, entraîneur, organisateur, joueur (cumulables).
- Dans un **match** : organisateur du match, participant (joueur), remplaçant.
- Dans une **compétition** : organisateur de la compétition, équipe participante.

Une personne peut donc être président d'une équipe, simple joueuse dans une autre et organisatrice d'un match ponctuel.

### 3.3 Matrice de droits (R1 et suivantes)

| Action | Président | Entraîneur | Organisateur (équipe ou match) | Joueur | Visiteur |
|---|:-:|:-:|:-:|:-:|:-:|
| Modifier l'équipe, ajouter/retirer des membres | ✔ | ✔ (membres) | — | — | — |
| Changer les rôles | ✔ | — | — | — | — |
| Créer un match pour l'équipe | ✔ | ✔ | ✔ | — | — |
| Modifier la composition du match | ✔ | ✔ | ✔ | — | — |
| Inviter des joueurs | ✔ | ✔ | ✔ | — | — |
| Répondre à une invitation | ✔ | ✔ | ✔ | ✔ | (après inscription) |
| Saisir le résultat | ✔ | ✔ | ✔ | — | — |
| Évaluer un joueur (poste, compétence) | ✔ | ✔ | ✔ | selon règles (§ 7.12) | — |
| Voir la page de match partagée | ✔ | ✔ | ✔ | ✔ | ✔ (si lien actif) |

Le créateur d'un match en est organisateur par défaut ; il peut déléguer ce rôle.

---

## 4. Glossaire

| Terme | Définition |
|---|---|
| **Joueur** | Personne susceptible de participer à un match. Peut être **invité** (sans compte, créé par un organisateur) ou **réel** (lié à un compte). |
| **Répertoire** | Liste personnelle des joueurs d'un utilisateur. |
| **Profil** | Identité d'un utilisateur (nom affiché, sports, poste préféré, visibilité). |
| **Équipe** | Groupe durable de joueurs avec un nom, un sport et des rôles. |
| **Côté (ou camp)** | Chacune des deux formations qui s'affrontent dans un match (A et B). Un côté peut être rattaché à une équipe ou être ponctuel. |
| **Composition** | Répartition des joueurs entre les côtés, avec postes et emplacements. |
| **Match** | Rencontre planifiée à une date, avec un format, un lieu et des participants. |
| **Format** | Nombre de joueurs par côté (par exemple 5, 7, 11). |
| **Participant** | Joueur inscrit à un match, avec un statut (voir § 7.8). |
| **Remplaçant** | Participant prévu en renfort, hors des titulaires. |
| **Liste d'attente** | File ordonnée de joueurs en attente d'une place. |
| **Séance** | Entraînement planifié. |
| **Compétition** | Ensemble de matchs structurés (ligue ou coupe). |
| **Niveau perçu** | Valeur de 1 à 5 attribuée par un organisateur à un joueur pour équilibrer les équipes. |
| **Évaluation** | Note (1 à 5) et commentaire facultatif sur un poste, une compétence, un match ou une compétition. |
| **Hors ligne / en ligne** | État de connexion de l'appareil. L'appli doit fonctionner dans les deux. |

---

## 5. Modes de fonctionnement

### 5.1 Mode local (R0)
- Aucune authentification. Toutes les données sont stockées sur l'appareil.
- Les fonctions nécessitant un serveur (invitation, synchronisation, matchs publics, notifications) sont masquées ou présentées comme « disponibles avec un compte ».
- L'utilisateur est averti que les données peuvent être perdues s'il efface les données de navigation, et il est invité à sauvegarder (export).

### 5.2 Mode compte (R1+)
- Les données sont enregistrées localement **et** synchronisées avec le serveur.
- L'appli reste pleinement utilisable hors ligne pour les données déjà présentes sur l'appareil.
- Certaines actions exigent le réseau, car elles concernent d'autres personnes : envoyer une invitation, rejoindre un match public, répondre à une demande. L'appli le dit clairement et propose de mettre l'action en attente lorsque c'est possible.

### 5.3 Règles de synchronisation (vue fonctionnelle)
- **RG-SYNC-01** : toute modification faite hors ligne est conservée et envoyée dès le retour du réseau.
- **RG-SYNC-02** : en cas de modification concurrente du même élément, la **dernière modification** l'emporte ; l'utilisateur est informé si une de ses modifications a été remplacée.
- **RG-SYNC-03** : une suppression est logique (l'élément est marqué supprimé et propagé).
- **RG-SYNC-04** : le statut de synchronisation est toujours visible (à jour, en attente, erreur).
- **RG-SYNC-05** : à la première connexion, l'utilisateur choisit de rattacher ses données locales à son compte.

---

## 6. Navigation et écrans principaux

### 6.1 Navigation (mobile)
Barre inférieure à 4 entrées : **Accueil**, **Matchs**, **Joueurs** (R0) puis **Équipes** (R1), et **Profil / Plus**.

### 6.2 Écrans par release

| Écran | Contenu | Release |
|---|---|---|
| Accueil | Prochain match, bouton « Créer un match », raccourcis, état de connexion. | R0 |
| Liste des matchs | Onglets « À venir » et « Passés », recherche. | R0 |
| Création / modification d'un match | Date, heure, lieu, sport, format, durée, notes. | R0 |
| Détail d'un match | Participants, composition, score, partage, statut. | R0 |
| Composition | Sélection des joueurs, répartition en équipes, suggestion, schéma de terrain, postes. | R0 |
| Résultat | Score, buteurs, statut. | R0 |
| Répertoire de joueurs | Liste, recherche, fiche joueur (matchs, statistiques). | R0 |
| Statistiques | Classements et filtres par période. | R0 |
| Sauvegarde / restauration | Export, import, rappel. | R0 |
| Paramètres | Langue, thème, sport par défaut, à propos. | R0 |
| Versions et nouveautés | Version affichée, journal des changements multilingue, bandeau après mise à jour. | R0 |
| Connexion / inscription | E-mail et mot de passe ou lien magique. | R1 |
| Équipes | Liste, détail, membres, rôles. | R1 |
| Invitations | Génération et suivi des liens, réponses. | R1 |
| Centre de notifications | Liste des événements. | R1 |
| Page de match publique (lecture seule) | Composition et score. | R1 |
| Stades et terrains, réservations | Fiche, calendrier. | R2 |
| Découverte de matchs publics | Liste filtrable, demande de participation. | R2 |
| Évaluations | Notes et commentaires, synthèses. | R3 |
| Compétitions | Calendrier, classement. | R4 |

### 6.3 Principes d'ergonomie
- Interface pensée d'abord pour le **téléphone**, utilisable à une main.
- Actions fréquentes accessibles en au plus **3 taps** (créer un match, ajouter un joueur, saisir un score).
- États vides explicatifs et messages d'erreur compréhensibles.
- Contrastes suffisants, tailles tactiles d'au moins 44 px, navigation au clavier, textes alternatifs.

---

## 7. Règles de gestion par module

### 7.1 Profil
- **RG-PRO-01** : en R0, le profil se limite à un pseudo (obligatoire), un sport par défaut et un poste préféré (facultatifs).
- **RG-PRO-02** : en R1, le profil ajoute bio, sports pratiqués, niveau déclaré, visibilité.
- **RG-PRO-03** : visibilité du profil : *privé* (moi seul), *équipes* (membres de mes équipes), *public* (tout utilisateur connecté). Défaut : *équipes*.
- **RG-PRO-04** : le pseudo n'est pas forcément unique ; l'identité reste le compte.
- **RG-PRO-05** : le profil local de R0 est fusionné avec le profil du compte à la première connexion (l'utilisateur confirme les champs conservés).

### 7.2 Joueurs (répertoire)
- **RG-JOU-01** : un joueur a un nom ou pseudo (obligatoire), un poste préféré, un niveau perçu de 1 à 5 (facultatifs), des notes privées (facultatives).
- **RG-JOU-02** : un joueur **invité** appartient au répertoire de la personne qui l'a créé et n'est visible que d'elle (et des organisateurs des matchs concernés en R1).
- **RG-JOU-03** : un joueur archivé disparaît des sélections mais reste affiché dans l'historique.
- **RG-JOU-04** : la suppression d'un joueur est logique ; ses participations passées restent cohérentes.
- **RG-JOU-05** : les doublons probables (même nom) donnent un avertissement.
- **RG-JOU-06** : le niveau perçu n'est visible que de l'organisateur qui l'a saisi, jamais du joueur concerné (en R1+).
- **RG-JOU-07** : la gestion d'un répertoire est **facultative** : on peut créer un match avec des joueurs ajoutés à la volée.

### 7.3 Matchs
- **RG-MAT-01** : un match a un sport, une date et une heure, un format, une durée et un statut. Le lieu, les notes et la visibilité sont facultatifs.
- **RG-MAT-02** : le format détermine le nombre de titulaires par côté ; il est modifiable tant que le match n'est pas terminé.
- **RG-MAT-03** : le créateur est organisateur ; il peut en désigner d'autres (R1).
- **RG-MAT-04** : un match peut être dupliqué (même lieu, mêmes joueurs, date décalée de 7 jours par défaut).
- **RG-MAT-05** : un match terminé reste modifiable (correction du score), avec date de dernière modification.
- **RG-MAT-06** : un match annulé n'entre pas dans les statistiques.
- **RG-MAT-07** : un match ne peut pas être supprimé définitivement s'il appartient à une compétition ; il peut être annulé.

### 7.4 Composition d'équipes
- **RG-COM-01** : un match a deux côtés (A et B) ; chacun a un nom et une couleur. Un côté peut référencer une équipe existante (R1).
- **RG-COM-02** : un participant est dans **au plus un côté** ; il peut être « non assigné ».
- **RG-COM-03** : l'écart d'effectifs entre deux côtés est de 1 joueur maximum après une suggestion automatique ; la saisie manuelle n'est pas contrainte mais avertit.
- **RG-COM-04** : un poste est facultatif ; il n'est unique que par emplacement (un seul joueur par emplacement du schéma).
- **RG-COM-05** : un remplaçant est un participant de statut « remplaçant » rattaché à un côté, sans emplacement.
- **RG-COM-06** : la composition est enregistrée au fur et à mesure et peut être restaurée dans son état précédent (annulation de la dernière action).
- **RG-COM-07** : après le début du match (date passée), la composition est figée pour les joueurs mais modifiable par l'organisateur.

### 7.5 Suggestion d'équipes équilibrées
- **RG-EQU-01** : le calcul utilise le niveau perçu (R0) puis le niveau combiné aux évaluations (R3+).
- **RG-EQU-02** : objectif : minimiser la différence entre la somme des niveaux de chaque côté, avec un écart d'effectifs d'au plus 1.
- **RG-EQU-03** : un joueur sans niveau reçoit la valeur par défaut 3 ; l'écran le signale.
- **RG-EQU-04** : les joueurs « verrouillés » par l'organisateur gardent leur côté.
- **RG-EQU-05** : le bouton « Autre proposition » fournit une répartition différente de qualité équivalente (tolérance paramétrable, par défaut un écart de 1 point).
- **RG-EQU-06** : une suggestion est **une proposition** ; elle ne remplace jamais une composition sans validation.
- **RG-EQU-07** : si le poste est renseigné, l'algorithme évite de placer tous les gardiens ou tous les attaquants dans le même côté (règle d'équilibrage secondaire).

### 7.6 Résultat et événements de match
- **RG-RES-01** : le score est un entier positif ou nul par côté.
- **RG-RES-02** : les événements (but, passe décisive) sont facultatifs ; chaque événement référence un participant et, si connu, la minute.
- **RG-RES-03** : le nombre de buts saisis par joueur est comparé au score ; un écart provoque un avertissement, pas un blocage.
- **RG-RES-04** : le résultat est : victoire d'un côté, ou match nul. Pas de prolongation ni de tirs au but en R0 (ajoutés en R4 pour les coupes).
- **RG-RES-05** : un match passe « Terminé » dès qu'un score est validé.

### 7.7 Statistiques
- **RG-STA-01** : les statistiques sont **calculées** à partir des matchs terminés ; aucune statistique n'est saisie à la main.
- **RG-STA-02** : par joueur : matchs joués, victoires, nuls, défaites, buts, passes (si renseignées), taux de présence.
- **RG-STA-03** : par équipe (R1) : mêmes indicateurs, sur une période.
- **RG-STA-04** : un joueur compte comme « ayant joué » s'il est titulaire ou remplaçant entré (si cette information est saisie) ; par défaut, tout participant assigné à un côté d'un match terminé.
- **RG-STA-05** : un filtre de période (mois, saison, tout) s'applique à tous les calculs.
- **RG-STA-06** : les statistiques d'un joueur sont visibles de lui-même et des membres de ses équipes, sauf si son profil est privé.

### 7.8 Équipes, invitations et présences (R1)
- **RG-EQP-01** : une équipe a un nom, un sport, une couleur et au moins un président.
- **RG-EQP-02** : un membre peut être un joueur invité ou un utilisateur réel ; les deux peuvent apparaître dans les compositions.
- **RG-EQP-03** : le président ne peut pas quitter l'équipe sans désigner un successeur.
- **RG-INV-01** : une **invitation** est un lien unique, associé à une équipe ou à un match, avec une date d'expiration (7 jours par défaut) et un nombre maximum d'utilisations.
- **RG-INV-02** : l'organisateur peut révoquer une invitation.
- **RG-INV-03** : la personne invitée voit un aperçu (titre, date, lieu, nombre de places) avant de s'inscrire.
- **RG-INV-04** : en acceptant, la personne devient membre ou participante selon le cas ; elle peut être associée à un joueur invité existant (RG-JOU-02).
- **RG-PRS-01** : statuts d'un participant : **invité**, **confirmé**, **peut-être**, **refusé**, **en attente (liste d'attente)**, **remplaçant**, **absent** (après le match).
- **RG-PRS-02** : une réponse est modifiable jusqu'à la date limite fixée par l'organisateur ; après, l'organisateur est averti.
- **RG-PRS-03** : un désistement tardif (par exemple moins de 24 h) est signalé à l'organisateur et consigné.

### 7.9 Liste d'attente et remplaçants (R1)
- **RG-ATT-01** : la capacité d'un match est définie par l'organisateur (par défaut : 2 × format).
- **RG-ATT-02** : au-delà de la capacité, un joueur confirmé passe en liste d'attente ; le rang suit l'ordre d'arrivée.
- **RG-ATT-03** : si une place se libère, le premier en attente est **promu** ; selon le réglage, la promotion est automatique ou soumise à validation de l'organisateur.
- **RG-ATT-04** : le joueur promu est notifié et dispose d'un délai (2 h par défaut, réglable) pour confirmer ; faute de réponse, la place passe au suivant.
- **RG-ATT-05** : l'organisateur peut forcer l'ordre de la liste d'attente.
- **RG-ATT-06** : un remplaçant peut devenir titulaire ; un titulaire peut être rétrogradé en remplaçant.

### 7.10 Terrains et réservations (R2)
- **RG-TER-01** : un **stade** regroupe des **terrains** ; un terrain a un nom, une surface, un format de jeu, un coût horaire et des horaires d'ouverture.
- **RG-TER-02** : un stade peut être privé (visible de son créateur) ou partagé (visible de ses équipes).
- **RG-RSV-01** : une **réservation** relie un match, un terrain et un créneau ; son statut est : à réserver, réservée, annulée.
- **RG-RSV-02** : le coût est saisi manuellement ; le partage par joueur est calculé (coût total / nombre de participants confirmés).
- **RG-RSV-03** : deux réservations d'un même terrain ne peuvent pas se chevaucher sans avertissement.
- **RG-RSV-04** : le suivi de paiement est déclaratif (payé / non payé) ; aucun paiement n'est exécuté par l'appli.

### 7.11 Matchs privés et publics (R2)
- **RG-VIS-01** : un match est **privé** (sur invitation) ou **public** (visible dans la découverte).
- **RG-VIS-02** : un match public définit des **critères** : sport, niveau minimal et maximal, nombre de places, validation manuelle ou automatique.
- **RG-VIS-03** : un joueur qui ne remplit pas les critères ne peut pas s'inscrire directement, mais peut envoyer une demande à l'organisateur.
- **RG-VIS-04** : la localisation affichée d'un match public se limite à la ville ou au terrain, jamais à une adresse personnelle.
- **RG-VIS-05** : un organisateur peut exclure un joueur de ses matchs publics.

### 7.12 Évaluations et retours (R3)
- **RG-EVA-01** : une **note** est un entier de 1 à 5 ; un **commentaire** est un texte de 500 caractères au plus, facultatif.
- **RG-EVA-02** : une évaluation porte sur une **cible** (joueur, match, compétition) et une **dimension** (poste, compétence, globale).
- **RG-EVA-03** : **qui évalue qui** : un entraîneur ou un organisateur peut évaluer les joueurs de ses équipes ; les joueurs peuvent évaluer leurs coéquipiers **uniquement après un match joué ensemble** ; chacun peut s'auto-évaluer.
- **RG-EVA-04** : une seule évaluation par auteur, cible, dimension et match ; elle reste modifiable pendant 7 jours après le match.
- **RG-EVA-05** : **confidentialité par défaut** : les évaluations de pairs sont **agrégées** (moyennes) et les commentaires sont visibles de l'entraîneur et de l'organisateur ; le joueur évalué voit les moyennes, et les commentaires seulement si l'évaluateur a choisi de les rendre visibles. Cette règle est à valider (voir § 10).
- **RG-EVA-06** : un seuil minimal d'avis (par exemple 3) est requis avant d'afficher une moyenne de pairs, pour éviter l'identification d'un auteur.
- **RG-EVA-07** : tout commentaire peut être **signalé** ; un commentaire signalé reste masqué pour les tiers jusqu'à examen.
- **RG-EVA-08** : un utilisateur peut bloquer un autre utilisateur, ce qui supprime les évaluations croisées entre eux.
- **RG-EVA-09** : le **retour d'un joueur sur son propre poste** (auto-évaluation) est visible de l'entraîneur et peut être comparé aux évaluations reçues.
- **RG-EVA-10** : l'évaluation d'un **match** (ambiance, terrain, organisation) et d'une **compétition** est anonyme et agrégée.
- **RG-EVA-11** : le catalogue de postes et de compétences est défini par sport ; une équipe peut ajouter les siens.

### 7.13 Séances d'entraînement (R4)
- **RG-ENT-01** : une séance appartient à une équipe, avec une date, un lieu, un objectif et une liste de convoqués.
- **RG-ENT-02** : les présences se gèrent comme celles d'un match (RG-PRS-01).
- **RG-ENT-03** : l'entraîneur peut consigner des notes de séance, privées ou partagées avec l'équipe.

### 7.14 Compétitions (R4)
- **RG-CMP-01** : une compétition a un format : **championnat** (aller simple ou aller-retour) ou **coupe** (élimination directe), avec un nombre d'équipes.
- **RG-CMP-02** : points par défaut : victoire 3, nul 1, défaite 0 ; réglables.
- **RG-CMP-03** : le calendrier est généré puis ajustable ; chaque rencontre est un match de la compétition.
- **RG-CMP-04** : le classement est calculé automatiquement ; départage : différence de buts, buts marqués, confrontation directe (ordre paramétrable).
- **RG-CMP-05** : en coupe, un match nul n'est pas autorisé : une prolongation ou des tirs au but sont saisis.
- **RG-CMP-06** : une compétition clôturée n'est plus modifiable, sauf par son organisateur avec traçabilité.

### 7.15 Notifications (R1 à R5)
- **RG-NOT-01** : événements notifiables : invitation reçue, réponse reçue, promotion depuis la liste d'attente, match modifié ou annulé, rappel avant match, demande de participation, évaluation reçue (si autorisé).
- **RG-NOT-02** : R1 : notifications **dans l'appli** ; R5 : notifications push web ; R6 : notifications natives.
- **RG-NOT-03** : l'utilisateur règle ses préférences par type d'événement.
- **RG-NOT-04** : une notification contient un lien direct vers l'écran concerné.
- **RG-NOT-05** : pas de notification pendant la nuit (par défaut 22 h à 8 h), sauf pour une annulation de dernière minute.

### 7.16 Photos (R5)
- **RG-PHO-01** : types de photos : avatar, photo d'équipe, photo de match.
- **RG-PHO-02** : taille et format limités (compression à l'envoi) ; contenu inapproprié signalable.
- **RG-PHO-03** : une photo de match est visible des participants ; l'identification d'une personne sur une photo exige son accord.
- **RG-PHO-04** : l'utilisateur peut supprimer ses photos et les photos où il est identifié peuvent être retirées à sa demande.

### 7.17 Validation de profil (R5)
- **RG-VAL-01** : niveaux de confiance : *non vérifié*, *e-mail vérifié*, *téléphone vérifié*, *validé par un organisateur ou un club*.
- **RG-VAL-02** : un organisateur de match public peut exiger un niveau minimal de validation.
- **RG-VAL-03** : le badge est affiché sur le profil et sur la liste des participants.
- **RG-VAL-04** : les pièces éventuellement fournies pour une validation ne sont jamais visibles des autres utilisateurs et sont supprimées après validation.

---

## 8. Cycles de vie (états)

### 8.1 Match
```
Brouillon → Planifié → (Ouvert aux inscriptions) → Complet → En cours → Terminé
                  ↘───────────────────────────────→ Annulé
```
- **Brouillon** : en cours de création (facultatif en R0 : un match est créé directement « Planifié »).
- **Planifié** : date et format connus.
- **Ouvert aux inscriptions / Complet** : à partir de R1, selon la capacité.
- **En cours** : à partir de l'heure de début (calculé, non saisi).
- **Terminé** : un score est validé.
- **Annulé** : motif facultatif ; notification des participants en R1.

### 8.2 Participant
```
Invité → Confirmé / Peut-être / Refusé
Confirmé → En attente (si capacité réduite) → Promu → Confirmé
Confirmé → Remplaçant ↔ Titulaire
Confirmé → Absent (après le match)
```

### 8.3 Invitation
```
Créée → Utilisée / Expirée / Révoquée
```

### 8.4 Réservation
```
À réserver → Réservée → Annulée
```

---

## 9. Exigences non fonctionnelles (vue fonctionnelle)

| Domaine | Exigence |
|---|---|
| **Disponibilité hors ligne** | R0 : 100 % des fonctions hors ligne après la première visite. R1+ : consultation et saisie des données locales hors ligne ; actions sociales avec réseau. |
| **Performance perçue** | Ouverture de l'appli installée en moins de 3 secondes sur un téléphone d'entrée de gamme ; actions courantes en moins d'une seconde. |
| **Fiabilité des données** | Aucune perte silencieuse : sauvegarde automatique, export/import, journal des conflits de synchronisation. |
| **Sécurité et vie privée** | Voir spécifications techniques. Minimisation des données, droits RGPD (accès, export, suppression), visibilité réglable. |
| **Accessibilité** | Cible WCAG 2.1 niveau AA pour les parcours principaux. |
| **Langues** | Français au départ ; ressources prévues pour l'anglais et l'arabe (avec écriture de droite à gauche). |
| **Compatibilité** | Navigateurs mobiles récents (Chrome, Safari, Firefox, Edge) ; installation PWA sur Android et iOS. |
| **Évolutivité** | Le modèle prévoit plusieurs sports, plusieurs langues et des versions natives sans refonte. |

---

## 10. Points à trancher

| # | Question | Impact |
|---|---|---|
| 1 | Échelle de notation : 1 à 5 ou 1 à 10 ? | Modèle d'évaluation, algorithme d'équilibrage. |
| 2 | Visibilité des retours entre joueurs : agrégée, nominative ou au choix ? | Confiance, risque de conflits. |
| 3 | Joueurs mineurs : concernés ? | Consentement parental, visibilité, photos. |
| 4 | Un joueur invité doit-il pouvoir voir ses propres statistiques sans compte (lien personnel) ? | Valeur pour les non-inscrits, complexité des liens. |
| 5 | Les matchs publics reposent-ils sur la ville seule ou sur une géolocalisation précise ? | Vie privée, R6. |
| 6 | Faut-il un mode « club » (plusieurs équipes, plusieurs entraîneurs) dès R1 ? | Modèle d'équipe et de droits. |
| 7 | Doit-on gérer plusieurs sports par équipe ? | Catalogues de postes et de compétences. |
| 8 | Quel mode d'authentification en priorité (lien magique, mot de passe, Google) ? | Friction à l'inscription. |
| 9 | La promotion depuis la liste d'attente est-elle automatique par défaut ? | Expérience des organisateurs. |
| 10 | Autorise-t-on un match sans date précise (« à fixer ») ? | Modèle de match, sondage de dates (idée future). |

## 11. Idées futures (non planifiées)
- Sondage de disponibilité pour choisir une date.
- Suivi de la forme et de la fatigue des joueurs.
- Tableau d'honneur saisonnier (meilleur buteur, joueur le plus assidu).
- Partage d'un récapitulatif de saison.
- Gestion de plusieurs terrains et de créneaux récurrents (« tous les jeudis »).
- Intégration calendrier (export iCal).
