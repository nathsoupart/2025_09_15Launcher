
## Rapport Technique – MyLauncher LeClick Unity 6 Meta Quest 3

### 1. Objectif du Launcher

Le **launcher LeClick** est une application Unity destinée aux casques Meta Quest.
Il détecte automatiquement les autres applications XR développées par *LeClick* installées sur le casque (ou présentes sous forme d’APK dans le stockage interne), et permet de :

- **Lister dynamiquement** ces applications dans une interface 3D interactive (VR launcher).
- **Afficher les informations** et **prévisualisations** des applications (description, icône, image preview).
- **Lancer directement** une des applications détectées par clic sur "Play".

***

### 2. Architecture générale

Le système repose sur trois composantes principales :


| Élément                                                     | Rôle                                                                                      |
|:------------------------------------------------------------|:------------------------------------------------------------------------------------------|
| **DetectApkLauncher.cs**                                    | Gère la détection des APKs, la lecture du JSON et l’affichage UI.                         |
| **Launcher.cs**                                             | à ajouter avec le prefab "quit "aux Apks crées  pour revenir au launcher                  |
| **appdata.json**                                            | Fournit les métadonnées (nom, package, image, description) de chaque application LeClick. |
| **AndroidManifest.xml**                                     | Définit les permissions nécessaires et la configuration VR/Quest.                         |
| **Prefabs UI (Panellauncher, play UI, bouton quit, icons)** | Déterminent l’interface visuelle et interactive XR.                                       |

***

## Charte Graphique du Launcher LeClick

Le launcher respecte la **charte graphique officielle de LeClick**, qui garantit une identité visuelle cohérente et professionnelle. Cette charte comprend les éléments suivants :

- **Logo LeClick** : Le logo doit être utilisé selon la version officielle définie dans les ressources graphiques du projet. Toute modification ou remplacement du logo doit être validé pour préserver l’image de marque.
- **Palette de Couleurs** :
    - Trois couleurs principales sont utilisées de manière harmonieuse dans l’interface pour renforcer la reconnaissance visuelle et assurer une bonne lisibilité.
    - Ces couleurs sont paramétrées dans les styles des boutons, fonds, textes et éléments décoratifs.
    - Si une nouvelle charte graphique est adoptée, ces trois couleurs doivent être mises à jour dans le code  pour refléter la nouvelle identité.
- **Typographie** :
    - La police utilisée suit la typographie officielle LeClick.
    - Toute modification de police doit être effectuée dans les styles UI et testée en VR pour s’assurer qu’elle reste ergonomique.


### Flexibilité de la Charte

Le launcher est conçu pour être facilement **adaptable** à une autre charte graphique si besoin, par exemple en cas de changement d’identité visuelle de la marque ou pour une déclinaison personnalisée.

Cela implique  manuellement:

- La possibilité de remplacer le logo dans les assets dossier Material du projet
  > Dans le "panellauncher" remplace "Toplogo"par celui de l'organisme.
  > Si partenaire collaborateur, remplace dans le "bottom" du panellauncher.
  
- La mise à jour facile des couleurs dans les Objets Ui des panels comme
  > settings (roue), Le bouton prefab "Play", le fond du panel "InfoApk" dans "fondinformation"
- Le remplacement de la police dans les composants UI TextMeshPro comme
  > "TextInfo" dans le panel "infoApk" ou les préfabs "Boutonapk" et "play" ainsi que dans les "Bottoms"



***

### 3. Fonctionnement Chronologique (Cycle d’exécution)

#### Étape 1 – Chargement initial (`Start()` – DetectApkLauncher)

1. **Copie des ressources** depuis *StreamingAssets* vers *persistentDataPath*
(JSON + images preview).
2. **Chargement du JSON** `appdata.json` via `LoadAppDataJson()`.
3. **Vérification des permissions** Android (`READ_EXTERNAL_STORAGE`).
4. **Lancement de `RefreshApps()`** pour détecter les applications.

#### Étape 2 – Détection des applications

Deux méthodes de détection :

- **GetInstalledApps()** : récupère les apps installées via le *PackageManager* Android.
→ Seules celles contenant « leclick » dans leur nom sont listées. (ici pour ajouter ou changer le filtre des apks à trouver)
- **GetApkFilesFromPrivateFolder()** : recherche des fichiers `.apk` dans le dossier interne.

Les données trouvées sont associées aux métadonnées du JSON (description, previews…).

#### Étape 3 – Génération de l’interface

- Instanciation dynamique des boutons depuis `buttonPrefab` (Unity UI).
- Chaque bouton contient :
    - Nom de l’app (TextMeshPro)
    - Icône (`appIcon` ou `defaultSprite`)
    - Listener `OnAppSelected()`


#### Étape 4 – Sélection et affichage d’infos

Lorsqu’un bouton est cliqué :

- La fiche descriptive s’affiche (texte description, image preview, icône éventuelle, logo du projet , liste des partenaires, logo des soutiens financiers).
- L’utilisateur peut ensuite cliquer sur le bouton **Play** pour lancer l’application.


#### Étape 5 – Lancement de l’application (`OnPlayButtonClicked`)

Bouton cliquable "play" sur le panel de droite "InfoApk" permet de lancer l'application sélectionnée.

***

### 4. Permissions importantes (AndroidManifest.xml)

Vérifie les permissions dans le fichier Manifest qui se trouve dans les Assets -> Plugins -> android 

***

### 5. JSON de configuration (`appdata.json`)

A trouver dans les Assets -> streamingAssets

Il permet de compléter le panel "InfoApk" sur l'application sélectionnée via son packageName.

Chaque entrée décrit une app détectable :

```json
[
  {
    "appName": "Demoa",
    "packageName": "com.leclick.demoa",
    "apkFilePath": null,
    "version": "1.0.0",
    "description": "Une démo interactive développée par LeClick.",
    "previewImage": "demoa_preview.png"
  },
  {
    "appName": "Demob",
    "packageName": "com.leclick.demob",
    "apkFilePath": null,
    "version": "1.0.0",
    "description": "Une démo bonhomme lunette",
    "previewImage": "demob_preview.png"
  },
  {
    "appName": "Mylaunch",
    "packageName": "be.leclick.mylauncher",
    "apkFilePath": null,
    "version": "1.0.0",
    "description": "Une application qui détecte les apks installées par LeClick...",
    "previewImage": "mylauncher_preview.png"
  }
]
```
Ce fichier grâce au script "StreamingAssetcopier.cs" est installé avec les images  .png dans le "persistentDataPath" du casque.
! ne se fait pas automatiquement, il faut supprimer le fichier et les images sur le casque  (quand il est modifier). Ensuite, rebuildé pour le replacer automatiquement avec modifications.

**À modifier :**

- Ajouter ou retirer des données selon les projets présents dans ton le casque meta quest 3 comme
   > Nom/ version / Description / Image demo / liste partenaires / logos des soutiens financiers
- Adapter `packageName` pour correspondre à celui défini dans chaque `AndroidManifest` d’app.
- Les images `.png` et logos doivent se trouver dans `StreamingAssets/`.

***
### 6. Bouton "Quit"

sur les Apks "démo", il faut avoir placer le préfab "Quit" et son script "Launcher.cs" pour permettre le retour à l'application Launcher.

Il est à copier dans le dossier Assets -> Prefab


***
### 7. Fichiers du projet nécessaires

- **Scripts :**
    - `DetectApkLauncher.cs`
    - `Launcher.cs` (à placer sur apk crée avec le prefab quit)
    - `StreamingAssetsCopier.cs`
  
- **UI Prefabs :**
    - `ButtonPrefab` (avec TextMeshProUGUI + Image "Icon")
    - `PreviewImage` et `InfoPanel` UI
    - `Play`
    - `Info`
    - `quit`
- **Assets Streaming :**
    - `demoa_preview.png`(exemple)
    - `demob_preview.png`(exemple)
    - `mylauncher_preview.png`
    **JSON :**
    - `appdata.json`

***

### 7. Résumé d’utilisation

1. Définir si la charte graphique est celle du "Click" ou à adapter.
2. Modifier les filtres pour limiter les apks à montrer.
3. Remplir le fichier Json des infos sur les Apks à montrer et les images, logos à ajouter.
4. Ajouter le bouton Quit et script "launcher.cs" dans les apks à monter.
5. Installer le launcher sur le casque 
6. Lancer le Launcher.
7. L’utilisateur peut ensuite lancer les apps en un clic depuis le casque.

***


