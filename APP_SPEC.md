Spécifications Techniques : Music Score Manager
1. Vision du Produit

Application multiplateforme (Cible prioritaire : Android 12+ et Windows 11) permettant aux musiciens de stocker, organiser et annoter leurs partitions sous format PDF et Image.
2. Pile Technique (Stack)

    Framework : .NET MAUI (.NET 10.0)

    Langage : C# 14

    Base de données : SQLite (via sqlite-net-pcl)

    Cible Android : API 31 (Minimum), API 36 (Compilation/SDK)

    Cible Windows : WinUI 3

3. Architecture des Données (Modèle)

Le fichier Score.cs doit contenir :

    Id (Primary Key, AutoIncrement)

    Title (string)

    FilePath (string) : Chemin local vers le fichier.

    Type (Enum) : PDF ou Image.

    DateAdded (DateTime).

    Tags (List) : Pour le filtrage.

4. Fonctionnalités Requises (Backlog)
A. Gestion de la Bibliothèque

    Importation : Utiliser FilePicker pour sélectionner des fichiers .pdf, .jpg, .png.

    Stockage : Sauvegarder les métadonnées dans scores.db3 via un DatabaseService asynchrone.

    Affichage : CollectionView en mode Grille (2 colonnes) avec titre et icône de type de fichier.

B. Visionneuse (Viewer)

    Images : Affichage plein écran avec zoom (Pinch-to-zoom).

    PDF : Intégration d'une vue native ou WebView pour la lecture de partitions multipages.

C. Organisation

    Recherche : Barre de recherche filtrant la collection par titre en temps réel.

    Tags : Possibilité d'ajouter des étiquettes (ex: "Piano", "Jazz", "Classique").

5. Contraintes de Développement (Instructions Antigravity)

    Respect des versions : Toujours utiliser net10.0 dans les fichiers .csproj.

    Permissions Android : Gérer les permissions READ_EXTERNAL_STORAGE et READ_MEDIA_IMAGES dynamiquement au moment de l'import.

    Performance : Utiliser le chargement différé (Lazy Loading) pour les images afin de ne pas ralentir la navigation dans la bibliothèque.

    Interface : Design épuré "Dark Mode" par défaut pour une lecture confortable sur pupitre.