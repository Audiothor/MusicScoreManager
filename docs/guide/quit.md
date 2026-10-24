<h1 style="text-align: center;">Quitter</h1>

Le menu **Quitter** permet de fermer proprement l'application.

---

## Processus de Fermeture Sécurisée

Lorsque vous appuyez sur l'onglet **Quitter** dans la barre de navigation :

1. **Demande de confirmation (Optionnelle)** :
   - Si l'option est activée dans les paramètres, une boîte de dialogue vous demande confirmation avant de fermer l'application.
2. **Sauvegarde de l'état de session** :
   - L'application enregistre l'ensemble de vos paramètres en cours, la dernière partition consultée, vos filtres actifs et l'état de la base de données SQLite.
3. **Libération de la mémoire vive** :
   - Les moteurs de rendu PDF et les flux audio sont déchargés proprement pour préserver l'autonomie de la batterie de votre tablette.
4. **Fermeture de l'application** :
   - Un indicateur de progression visuel discret confirme la finalisation des écritures sur le disque, puis l'application se ferme sans risque de perte de données ou de corruption de partition.

---

## ⚙️ Personnalisation dans les Paramètres

Dans **Paramètres > Paramètres Application (Général)**, la section **Comportement de l'application** propose deux options dédiées :
- **Afficher le menu « Quitter » dans la barre de navigation** : permet de masquer cet onglet si vous préférez naviguer via les gestes système Android ou le bouton retour physique de votre appareil.
- **Demander confirmation avant de quitter** : sécurise la sortie de l'application contre les appuis involontaires.
