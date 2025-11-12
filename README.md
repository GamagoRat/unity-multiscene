<div align="center">

# Multi-scene Controller
[![Made with Unity](https://img.shields.io/badge/Made%20with-Unity-57b9d3.svg?style=for-the-badge&logo=unity)](https://unity3d.com) [![Unity Version](https://img.shields.io/badge/Unity-6000.0.58f-57b9d3.svg?style=for-the-badge&logo=unity)](https://unity3d.com) [![License](https://img.shields.io/badge/License-MIT-4caf50.svg?style=for-the-badge&logo=github)](https://opensource.org/licenses/MIT) [![Release](https://img.shields.io/github/v/release/Gamagorat/unity-multiscene?style=for-the-badge&logo=github)](https://github.com/Gamagorat/unity-multiscene/releases)

</div>

Un contrôleur simple pour gérer le chargement/déchargement multi-scène (Additive) dans Unity, avec une Fluent API pour construire des plans de transition.

## Obtenir le package

Le package du projet se trouve dans les releases GitHub :
[Releases · GamagoRat/unity-multiscene](https://github.com/GamagoRat/unity-multiscene/releases)

## Origine & concept

L'implémentation s'inspire de cette vidéo : https://www.youtube.com/watch?v=oejg12YyYyI
# Multi-scene Controller

Ce document décrit l'implémentation et l'utilisation du SceneController et de sa Fluent API (SceneTransitionPlan).

## Obtenir le package

Le package du projet se trouve dans les releases GitHub :
[https://github.com/GamagoRat/unity-multiscene/releases](https://github.com/GamagoRat/unity-multiscene/releases)

## Origine et exemple (source)

- implémentation s'inspire largement de cette vidéo : https://www.youtube.com/watch?v=oejg12YyYyI
- Ce projet Unity utilise la fonctionnalité Multi-Scene pour gérer plusieurs scènes (1 scène principale qui load et unload d'autres scènes secondaires)
- Le script "SceneController" est un singleton accessible depuis toutes les scènes et permet de charger et décharger des scènes secondaires
- Utilise une fluent API pour charger et décharger les scènes secondaires
- Pour faire des effets de transition, création d'une classe abstraite `TransitionEffect` (nommée parfois `SceneTransition` dans la documentation) qui sera héritée par des classes concrètes (exemple fourni utilisant un Animator).

Exemples de structure de scènes :

- Une scène "Core"
  - possède le SceneController
  - possède "ProjectManager" qui charge la scène de menu principal au démarrage
  - un Canvas avec un Panel ; le Canvas possède un `AnimatorTransitionEffect` qui hérite de `TransitionEffect` et permet de faire des transitions avec des animations. Attention : le Canvas de transition doit avoir une priorité haute pour passer au-dessus des autres canvases des autres scènes.

- Une scène "MainMenu"
  - possède un Canvas avec des boutons, dont un "Play" qui charge la scène "Game" en utilisant la fluent API.
  - possède `MainMenuManager` qui gère les interactions du menu principal (fonction qui transitionne vers la scène game).

- Une scène "Game"
  - possède un Canvas avec un bouton "Stop" qui décharge la scène "Game" et recharge la scène "MainMenu" en utilisant la fluent API (permet de décharger les ressources 3D).
  - possède `GameManager` qui gère les interactions du jeu (fonction qui transitionne vers la scène main menu).

`SceneDatabase` définit des constantes pour les noms des scènes secondaires (MainMenu, Game) ainsi que des slots qui définissent les endroits dans la hiérarchie où les scènes secondaires seront chargées (MainMenuSlot, GameSlot).

- Pour tester : ouvrir la scène "Core" et lancer le jeu. Le menu principal devrait apparaître avec une transition. Cliquer sur "Play" pour charger la scène de jeu avec une transition, puis cliquer sur "Stop" pour revenir au menu principal avec une transition.

## Vue d'ensemble

- SceneController est un singleton destiné à être placé dans la scène "Core" (ou scène centrale) et gère le chargement/déchargement de scènes secondaires en mode Additive.
- Il expose une API fluide pour construire un plan de transition (SceneTransitionPlan) puis l'exécuter.
- Le contrôleur garde en mémoire les scènes chargées par "slot" (clé logique → nom de scène) afin de pouvoir remplacer ou décharger facilement des scènes.
- Le système supporte des effets de transition (TransitionEffect) et le nettoyage des assets non utilisés.

## Principes clés

- Chargement en Additive : les scènes secondaires sont chargées via `SceneManager.LoadSceneAsync(..., LoadSceneMode.Additive)`.
- Active scene : on peut choisir quelle scène deviendra la scène active après le chargement.
- Slots : un `slotKey` est une clé logique (ex: `MainMenuSlot`, `Session`) qui identifie l'emplacement de la scène dans la hiérarchie ou la logique du projet.
- isBusy : empêche l'exécution simultanée de plusieurs transitions. Si un plan est déjà exécuté, un nouvel appel loggue un warning et est ignoré.

## Fluent API — SceneTransitionPlan

Utilisation typique :

https://github.com/GamagoRat/unity-multiscene/blob/a75e0d3cbd1d68603a212a4c3ccc0b32cf43eb7d/Assets/example/Scripts/GameManager.cs#L7-L12

Méthodes disponibles :

- NewTransition()
  - Crée et renvoie un nouvel objet `SceneTransitionPlan`.

- Load(string slotKey, string sceneName, bool setActive = false)
  - slotKey : identifiant logique du slot.
  - sceneName : nom de la scène Unity (doit être présent dans les Build Settings).
  - setActive : si `true`, la scène sera définie comme active après le chargement.
  - Comportement : remplace la valeur du dictionnaire `ScenesToLoad` pour la clé `slotKey` (écrase la valeur précédente si existante).

- Unload(string slotKey)
  - Ajoute `slotKey` à la liste `ScenesToUnload`. Lors de l'exécution, la scène enregistrée pour ce slot (si présente) sera déchargée.

- WithOverlay()
  - Active l'option `Overlay` pour le plan. Si un `TransitionEffect` est configuré sur le `SceneController`, `FadeIn()` est appelé avant les opérations et `FadeOut()` après, permettant un effet visuel (ex: écran de transition).

- WithClearUnusedAssets()
  - Active `ClearUnusedAssets` : après les déchargements, `Resources.UnloadUnusedAssets()` sera exécuté pour libérer la mémoire des assets non référencés.

- Perform()
  - Envoie le plan au `SceneController` via `ExecutePlan(this)` et retourne la Coroutine d'exécution. Ne bloque pas le thread principal — la gestion est asynchrone via Coroutine.

## Flux d'exécution (ChangeSceneRoutine)

1. Si `Overlay == true` et un `TransitionEffect` est assigné :
   - `FadeIn()` (attente de la coroutine), puis courte pause (0.5s).
2. Déchargement : parcourt `ScenesToUnload` et appelle `UnloadSceneAsync` pour chaque slot (résolu en nom de scène depuis `loadedScenesBySlot`).
3. Si `ClearUnusedAssets` : appelle `Resources.UnloadUnusedAssets()` et attend la fin.
4. Chargement : parcourt `ScenesToLoad` (slotKey → sceneName) :
   - Si le slot contient déjà une scène, elle est déchargée (`UnloadSceneRoutine`).
   - Charge la nouvelle scène en Additive via `LoadSceneAsync`.
   - Gère `allowSceneActivation` pour contrôler le moment d'activation.
   - Si la scène est marquée active (`ActiveSceneName == sceneName`), appelle `SceneManager.SetActiveScene`.
   - Met à jour `loadedScenesBySlot[slotKey] = sceneName`.
5. Si `Overlay == true` et `TransitionEffect` présent : `FadeOut()`.
6. Réinitialise `isBusy = false`.

## TransitionEffect

- Type abstrait :

https://github.com/GamagoRat/unity-multiscene/blob/a75e0d3cbd1d68603a212a4c3ccc0b32cf43eb7d/Assets/Scripts/TransitionEffect.cs#L4-L8

- Exemple concret : un `AnimatorTransitionEffect` qui utilise un Animator pour animer un canvas de transition.
- Important : si l'effet utilise un Canvas, assurer que sa priorité (Sorting Layer / Order in Layer) est supérieure afin qu'il apparaisse au-dessus des autres UI des scènes chargées.

## Exemples (issus du dossier `example`)

- Charger le menu au démarrage (ProjectManager) :

https://github.com/GamagoRat/unity-multiscene/blob/a75e0d3cbd1d68603a212a4c3ccc0b32cf43eb7d/Assets/example/Scripts/ProjectManager.cs#L7-L9

- Depuis le menu, démarrer le jeu (MainMenuManager) :

https://github.com/GamagoRat/unity-multiscene/blob/a75e0d3cbd1d68603a212a4c3ccc0b32cf43eb7d/Assets/example/Scripts/MainMenuManager.cs#L7-L12

- Depuis le jeu, retourner au menu (GameManager) :

https://github.com/GamagoRat/unity-multiscene/blob/a75e0d3cbd1d68603a212a4c3ccc0b32cf43eb7d/Assets/example/Scripts/GameManager.cs#L7-L12

## Conseils / Best Practices

- Toujours s'assurer que les `sceneName` utilisés sont listés dans les Build Settings de Unity.
- Utiliser des constantes (ex: `SceneDatabase`) pour éviter les erreurs de frappe dans les noms et slots.
- Placer le `SceneController` dans une scène persistante (par ex. "Core") et marquer l'objet `DontDestroyOnLoad` si nécessaire (selon l'architecture du projet).
- Testez les effets de transition avec un canvas dédié ayant une priorité d'affichage élevée.
- Évitez de lancer plusieurs `.Perform()` simultanés — `isBusy` protège mais un contrôle applicatif peut améliorer l'UX.

## Dépannage

- Warning "SceneController is busy executing another plan." : attendre la fin de la coroutine ou vérifier la logique appelante.
- Scène non trouvée / LoadSceneAsync retourne null : vérifier le nom exact et qu'il est inclus dans les Build Settings.
- Problèmes d'UI masquée pendant la transition : vérifier l'ordre des canvases et l'alpha/visibilité de l'effet de transition.