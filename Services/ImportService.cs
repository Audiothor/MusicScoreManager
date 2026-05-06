using MusicScoreManager.Models;

namespace MusicScoreManager.Services
{
    public class ImportService
    {
        private readonly DatabaseService _databaseService;
        private readonly SettingsService _settingsService;

        public ImportService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _settingsService = new SettingsService();
        }

        public async Task<Score?> ImportScoreAsync()
        {
            try
            {
                var status = await CheckAndRequestStoragePermissionAsync();
                if (status != PermissionStatus.Granted)
                {
                    // Permission refusée
                    return null;
                }

                var customFileType = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.iOS, new[] { "public.image", "com.adobe.pdf" } },
                        { DevicePlatform.Android, new[] { "image/*", "application/pdf" } },
                        { DevicePlatform.WinUI, new[] { ".jpg", ".jpeg", ".png", ".pdf" } },
                        { DevicePlatform.MacCatalyst, new[] { "public.image", "com.adobe.pdf" } },
                    });

                var options = new PickOptions
                {
                    PickerTitle = "Sélectionnez une partition (PDF ou Image)",
                    FileTypes = customFileType,
                };

                var result = await FilePicker.Default.PickAsync(options);
                if (result != null)
                {
                    var rootDir = _settingsService.ScoresRootDirectory;
                    if (!Directory.Exists(rootDir)) Directory.CreateDirectory(rootDir);

                    string finalStoredPath;
                    
                    // Vérification intelligente : soit le chemin commence par le root (Windows), 
                    // soit un fichier de même nom et même taille existe déjà dans le root (Android workaround)
                    bool isAlreadyInRoot = result.FullPath.StartsWith(rootDir, StringComparison.OrdinalIgnoreCase);
                    
                    if (!isAlreadyInRoot)
                    {
                        try 
                        {
                            // On énumère tous les fichiers pour faire une comparaison de nom insensible à la casse
                            // C'est plus lent mais beaucoup plus fiable sur Android/SD Card
                            var fileInfoSource = new FileInfo(result.FullPath);
                            long sourceLength = fileInfoSource.Length;
                            string targetFileName = result.FileName;

                            var allFiles = Directory.EnumerateFiles(rootDir, "*", SearchOption.AllDirectories);
                            foreach (var potentialPath in allFiles)
                            {
                                string currentFileName = Path.GetFileName(potentialPath);
                                if (currentFileName.Equals(targetFileName, StringComparison.OrdinalIgnoreCase))
                                {
                                    var fileInfoDest = new FileInfo(potentialPath);
                                    if (sourceLength == fileInfoDest.Length)
                                    {
                                        isAlreadyInRoot = true;
                                        result = new FileResult(potentialPath);
                                        break;
                                    }
                                }
                            }
                        }
                        catch { /* Ignore les erreurs d'accès */ }
                    }

                    if (isAlreadyInRoot)
                    {
                        finalStoredPath = _settingsService.GetRelativePath(result.FullPath);
                    }
                    else
                    {
                        string? action = await Shell.Current.DisplayActionSheetAsync(
                            "Organisation de la bibliothèque", 
                            "Annuler", 
                            null, 
                            "Copier vers la bibliothèque (Conseillé)", 
                            "Lier le fichier original (Externe)");

                        if (action == "Copier vers la bibliothèque (Conseillé)")
                        {
                            var sanitizedFileName = result.FileName.Replace(" ", "_");
                            var localFilePath = Path.Combine(rootDir, sanitizedFileName);
                            
                            if (File.Exists(localFilePath))
                            {
                                var fileNameOnly = Path.GetFileNameWithoutExtension(sanitizedFileName);
                                var extension = Path.GetExtension(sanitizedFileName);
                                localFilePath = Path.Combine(rootDir, $"{fileNameOnly}_{DateTime.Now:yyyyMMddHHmmss}{extension}");
                            }

                            using var stream = await result.OpenReadAsync();
                            using var fileStream = File.Create(localFilePath);
                            await stream.CopyToAsync(fileStream);
                            
                            finalStoredPath = _settingsService.GetRelativePath(localFilePath);
                        }
                        else if (action == "Lier le fichier original (Externe)")
                        {
                            finalStoredPath = result.FullPath;
                        }
                        else
                        {
                            return null; // Annulation
                        }
                    }

                    var ext = Path.GetExtension(result.FileName).ToLowerInvariant();
                    var type = ext == ".pdf" ? ScoreType.PDF : ScoreType.Image;

                    var score = new Score
                    {
                        Title = Path.GetFileNameWithoutExtension(result.FileName),
                        FilePath = finalStoredPath,
                        Type = type,
                        DateAdded = DateTime.Now
                    };

                    await _databaseService.SaveScoreAsync(score);
                    return score;
                }
            }
            catch (Exception)
            {
                // Utilisateur a annulé ou erreur système
            }

            return null;
        }

        private async Task<PermissionStatus> CheckAndRequestStoragePermissionAsync()
        {
            if (DeviceInfo.Platform != DevicePlatform.Android)
                return PermissionStatus.Granted;

            if (DeviceInfo.Version.Major >= 13)
            {
                var status = await Permissions.CheckStatusAsync<Permissions.Photos>();
                if (status == PermissionStatus.Granted)
                    return status;
                return await Permissions.RequestAsync<Permissions.Photos>();
            }
            else
            {
                var status = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
                if (status == PermissionStatus.Granted)
                    return status;
                return await Permissions.RequestAsync<Permissions.StorageRead>();
            }
        }
    }
}
