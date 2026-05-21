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
            var results = await ImportScoresAsync();
            return results.FirstOrDefault();
        }

        public async Task<List<Score>> ImportScoresAsync()
        {
            var importedScores = new List<Score>();
            try
            {
                var status = await CheckAndRequestStoragePermissionAsync();
                if (status != PermissionStatus.Granted)
                {
                    return importedScores;
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
                    PickerTitle = "Sélectionnez une ou plusieurs partitions (PDF ou Image)",
                    FileTypes = customFileType,
                };

                var results = await FilePicker.Default.PickMultipleAsync(options);
                if (results != null && results.Any())
                {
                    var rootDir = _settingsService.ScoresRootDirectory;
                    if (!Directory.Exists(rootDir)) Directory.CreateDirectory(rootDir);

                    // Séparer les fichiers déjà dans la racine de ceux qui nécessitent une décision d'importation
                    var filesToProcess = new List<(FileResult File, bool AlreadyInRoot)>();
                    foreach (var result in results)
                    {
                        if (result == null) continue;
                        var fullPath = result.FullPath;
                        if (string.IsNullOrEmpty(fullPath)) continue;
                        
                        bool isAlreadyInRoot = !string.IsNullOrEmpty(rootDir) && fullPath.StartsWith(rootDir, StringComparison.OrdinalIgnoreCase);
                        
                        if (!isAlreadyInRoot)
                        {
                            try 
                            {
                                var fileInfoSource = new FileInfo(fullPath);
                                long sourceLength = fileInfoSource.Length;
                                string targetFileName = result.FileName ?? Path.GetFileName(fullPath);

                                // 1. Test direct dans la racine (très rapide et évite le scan)
                                string directPath = Path.Combine(rootDir, targetFileName);
                                if (File.Exists(directPath) && new FileInfo(directPath).Length == sourceLength)
                                {
                                    isAlreadyInRoot = true;
                                    filesToProcess.Add((new FileResult(directPath), true));
                                    continue;
                                }
                                
                                // 2. Si pas trouvé en direct, scan récursif robuste
                                string? foundPath = FindFileRecursively(rootDir, targetFileName, sourceLength);
                                if (foundPath != null)
                                {
                                    isAlreadyInRoot = true;
                                    filesToProcess.Add((new FileResult(foundPath), true));
                                    continue;
                                }
                            }
                            catch { /* Ignore */ }
                        }
                        
                        filesToProcess.Add((result, isAlreadyInRoot));
                    }

                    // Déterminer s'il y a des fichiers externes
                    var externalFiles = filesToProcess.Where(f => !f.AlreadyInRoot).ToList();
                    string? globalAction = null;

                    if (externalFiles.Count > 0)
                    {
                        if (externalFiles.Count == 1)
                        {
                            globalAction = await Shell.Current.DisplayActionSheetAsync(
                                "Organisation de la bibliothèque", 
                                "Annuler", 
                                null, 
                                "Copier vers la bibliothèque (Conseillé)", 
                                "Lier le fichier original (Externe)");
                        }
                        else
                        {
                            globalAction = await Shell.Current.DisplayActionSheetAsync(
                                $"Organisation de la bibliothèque ({externalFiles.Count} fichiers)", 
                                "Annuler", 
                                null, 
                                "Copier tous les fichiers vers la bibliothèque (Conseillé)", 
                                "Lier tous les fichiers originaux (Externe)",
                                "Choisir au cas par cas");
                        }

                        if (globalAction == "Annuler" || globalAction == null)
                        {
                            return importedScores;
                        }
                    }

                    for (int i = 0; i < filesToProcess.Count; i++)
                    {
                        var (fileResult, isAlreadyInRoot) = filesToProcess[i];
                        string finalStoredPath;

                        if (isAlreadyInRoot)
                        {
                            finalStoredPath = _settingsService.GetRelativePath(fileResult.FullPath);
                        }
                        else
                        {
                            string? fileAction = globalAction;
                            
                            if (globalAction == "Choisir au cas par cas")
                            {
                                fileAction = await Shell.Current.DisplayActionSheetAsync(
                                    $"Fichier : {fileResult.FileName}", 
                                    "Passer ce fichier", 
                                    null, 
                                    "Copier vers la bibliothèque (Conseillé)", 
                                    "Lier le fichier original (Externe)");
                            }

                            if (fileAction == "Copier vers la bibliothèque (Conseillé)" || fileAction == "Copier tous les fichiers vers la bibliothèque (Conseillé)")
                            {
                                var sanitizedFileName = fileResult.FileName.Replace(" ", "_");
                                var localFilePath = Path.Combine(rootDir, sanitizedFileName);
                                
                                int counter = 1;
                                while (File.Exists(localFilePath))
                                {
                                    var fileNameOnly = Path.GetFileNameWithoutExtension(sanitizedFileName);
                                    var extension = Path.GetExtension(sanitizedFileName);
                                    localFilePath = Path.Combine(rootDir, $"{fileNameOnly}_{counter}{extension}");
                                    counter++;
                                }

                                using var stream = await fileResult.OpenReadAsync();
                                using var fileStream = File.Create(localFilePath);
                                await stream.CopyToAsync(fileStream);
                                
                                finalStoredPath = _settingsService.GetRelativePath(localFilePath);
                            }
                            else if (fileAction == "Lier le fichier original (Externe)" || fileAction == "Lier tous les fichiers originaux (Externe)")
                            {
                                finalStoredPath = fileResult.FullPath;
                            }
                            else
                            {
                                continue; // Passer ce fichier
                            }
                        }

                        var ext = Path.GetExtension(fileResult.FullPath)?.ToLowerInvariant() ?? "";
                        var type = ext == ".pdf" ? ScoreType.PDF : ScoreType.Image;

                        var score = new Score
                        {
                            Title = Path.GetFileNameWithoutExtension(fileResult.FileName ?? fileResult.FullPath),
                            FilePath = finalStoredPath,
                            Type = type,
                            DateAdded = DateTime.Now
                        };

                        await _databaseService.SaveScoreAsync(score);
                        importedScores.Add(score);
                    }
                }
            }
            catch (Exception)
            {
                // Erreur système ou annulation
            }

            return importedScores;
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

        private string? FindFileRecursively(string dir, string fileName, long size)
        {
            try
            {
                // Vérifier les fichiers du dossier actuel
                foreach (var file in Directory.EnumerateFiles(dir))
                {
                    if (Path.GetFileName(file).Equals(fileName, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            if (new FileInfo(file).Length == size) return file;
                        }
                        catch { }
                    }
                }

                // Explorer les sous-dossiers
                foreach (var subDir in Directory.EnumerateDirectories(dir))
                {
                    var found = FindFileRecursively(subDir, fileName, size);
                    if (found != null) return found;
                }
            }
            catch { /* Dossier inaccessible, on passe au suivant */ }
            return null;
        }
    }
}
