# 🚪 Menu Quitter

Le menu **Quitter** ([`QuitPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/QuitPage.xaml)) permet de fermer proprement l'application.

---

## 🔒 Processus de Fermeture Sécurisée

Lorsque vous appuyez sur l'onglet **Quitter (🚪)** dans la barre de navigation :

1. **Sauvegarde de l'état de session** :
   - L'application enregistre l'ensemble de vos paramètres en cours, la dernière partition consultée, vos filtres actifs et l'état de la base de données SQLite.
2. **Libération de la mémoire vive** :
   - Les moteurs de rendu PDF et les flux audio sont déchargés proprement pour préserver l'autonomie de la batterie de votre tablette.
3. **Fermeture de l'application** :
   - Un indicateur de progression visuel discret confirme la finalisation des écritures sur le disque, puis l'application se ferme sans risque de perte de données ou de corruption de partition.
