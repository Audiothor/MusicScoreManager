using MusicScoreManager.Models;
using System.Text.RegularExpressions;

namespace MusicScoreManager.Services
{
    public class ImportService
    {
        private readonly DatabaseService _databaseService;
        private readonly SettingsService _settingsService;
        private readonly PdfService _pdfService;

        public event Action<bool, string?>? ConversionStateChanged;

        public ImportService(DatabaseService databaseService, PdfService? pdfService = null)
        {
            _databaseService = databaseService;
            _settingsService = new SettingsService();
            _pdfService = pdfService ?? new PdfService();
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
                // Sur Android, FilePicker utilise le sélecteur système SAF qui n'exige pas de permission préalable.
                try
                {
                    await CheckAndRequestStoragePermissionAsync();
                }
                catch { /* Ne pas bloquer si les permissions optionnelles échouent */ }

                var customFileType = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.iOS, new[] { "public.image", "com.adobe.pdf" } },
                        { DevicePlatform.Android, new[] { "image/*", "application/pdf" } },
                        { DevicePlatform.WinUI, new[] { ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".pdf" } },
                        { DevicePlatform.MacCatalyst, new[] { "public.image", "com.adobe.pdf" } },
                    });

                var options = new PickOptions
                {
                    PickerTitle = "Sélectionnez une ou plusieurs partitions (PDF ou Image)",
                    FileTypes = customFileType,
                };

                var results = await FilePicker.Default.PickMultipleAsync(options);
                if (results == null || !results.Any())
                {
                    return importedScores;
                }

                var rootDir = _settingsService.ScoresRootDirectory;
                if (!Directory.Exists(rootDir)) Directory.CreateDirectory(rootDir);

                var cacheDir = FileSystem.CacheDirectory;
                if (!Directory.Exists(cacheDir)) Directory.CreateDirectory(cacheDir);

                var validResults = results.Where(r => r != null && (!string.IsNullOrEmpty(r.FullPath) || !string.IsNullOrEmpty(r.FileName))).Cast<FileResult>().ToList();
                if (!validResults.Any()) return importedScores;

                var imageFiles = validResults.Where(r => IsImageFile(r)).ToList();
                var pdfFiles = validResults.Where(r => IsPdfFile(r)).ToList();

                // Information conviviale pour les fichiers de type image
                if (imageFiles.Any())
                {
                    var fileNames = imageFiles
                        .Select(f => Path.GetFileName(f.FileName ?? f.FullPath))
                        .Where(n => !string.IsNullOrEmpty(n))
                        .ToList();

                    string fileListStr = string.Join("\n• ", fileNames.Take(8));
                    if (fileNames.Count > 8)
                    {
                        fileListStr += $"\n... et {fileNames.Count - 8} autre(s)";
                    }

                    string alertTitle = "Importation d'images";
                    string alertMessage = (imageFiles.Count == 1)
                        ? $"Le fichier sélectionné est une image :\n• {fileListStr}\n\nIl va être converti au format PDF pour être intégré à votre bibliothèque.\n\nSouhaitez-vous continuer ?"
                        : $"Les {imageFiles.Count} fichiers sélectionnés sont des images :\n• {fileListStr}\n\nIls vont être convertis au format PDF pour être intégrés à votre bibliothèque.\n\nSouhaitez-vous continuer ?";

                    bool acceptConversion = await Shell.Current.DisplayAlertAsync(
                        alertTitle,
                        alertMessage,
                        "Continuer",
                        "Annuler");

                    if (!acceptConversion)
                    {
                        // L'utilisateur refuse : on ignore les images
                        imageFiles.Clear();
                    }
                }

                // Cas 1 : Plusieurs images acceptées -> Proposer de fusionner en un unique PDF ou partitions individuelles
                if (imageFiles.Count > 1)
                {
                    string action = await Shell.Current.DisplayActionSheetAsync(
                        $"Sélection de {imageFiles.Count} images",
                        "Annuler",
                        null,
                        "📑 Fusionner en 1 seule partition PDF multi-pages (Conseillé)",
                        "📄 Convertir en partitions individuelles (PDF)");

                    if (action == "Annuler" || string.IsNullOrEmpty(action))
                    {
                        // L'utilisateur annule le traitement des images
                        imageFiles.Clear();
                    }
                    else if (action.StartsWith("📑"))
                    {
                        // Fusionner en un unique PDF
                        var firstFileName = Path.GetFileNameWithoutExtension(imageFiles[0].FileName ?? imageFiles[0].FullPath);
                        var suggestedTitle = Regex.Replace(firstFileName, @"[_-]?\d+$", "").Trim();
                        if (string.IsNullOrEmpty(suggestedTitle)) suggestedTitle = firstFileName;
                        if (string.IsNullOrWhiteSpace(suggestedTitle)) suggestedTitle = "Partition";

                        string? inputTitle = await Shell.Current.DisplayPromptAsync(
                            "Titre de la partition",
                            "Entrez le titre de la nouvelle partition PDF :",
                            initialValue: suggestedTitle,
                            accept: "Créer",
                            cancel: "Annuler");

                        if (inputTitle != null) // non annulé
                        {
                            var finalTitle = string.IsNullOrWhiteSpace(inputTitle) ? suggestedTitle : inputTitle.Trim();
                            var sortedImages = imageFiles.OrderBy(f => f.FileName ?? f.FullPath, new NaturalComparer()).ToList();
                            var tempPaths = new List<string>();

                            ConversionStateChanged?.Invoke(true, $"Fusion et conversion de {sortedImages.Count} images en document PDF...");
                            try
                            {
                                foreach (var img in sortedImages)
                                {
                                    var ext = Path.GetExtension(img.FileName ?? img.FullPath);
                                    if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
                                    var cleanExt = new string(ext.Where(c => char.IsLetterOrDigit(c) || c == '.').ToArray());
                                    if (string.IsNullOrWhiteSpace(cleanExt)) cleanExt = ".jpg";

                                    var tempPath = Path.Combine(cacheDir, $"{Guid.NewGuid()}{cleanExt}");
                                    using (var srcStream = await img.OpenReadAsync())
                                    using (var dstStream = File.Create(tempPath))
                                    {
                                        await srcStream.CopyToAsync(dstStream);
                                    }
                                    tempPaths.Add(tempPath);
                                }

                                var sanitizedTitle = SanitizeFileName(finalTitle);
                                var localPdfPath = GetUniqueFilePath(rootDir, sanitizedTitle, ".pdf");

                                await _pdfService.ConvertImagesToPdfAsync(tempPaths, localPdfPath);

                                var score = new Score
                                {
                                    Title = finalTitle,
                                    FilePath = _settingsService.GetRelativePath(localPdfPath),
                                    Type = ScoreType.PDF,
                                    DateAdded = DateTime.Now,
                                    PageCount = tempPaths.Count
                                };

                                await _databaseService.SaveScoreAsync(score);
                                importedScores.Add(score);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[ImportService] Erreur fusion images : {ex.Message}");
                                await Shell.Current.DisplayAlertAsync("Erreur de conversion", $"Impossible de créer la partition PDF : {ex.Message}", "OK");
                            }
                            finally
                            {
                                ConversionStateChanged?.Invoke(false, null);
                                foreach (var t in tempPaths)
                                {
                                    try { if (File.Exists(t)) File.Delete(t); } catch { }
                                }
                            }
                        }
                    }
                    else
                    {
                        // Convertir individuellement chaque image en un PDF
                        var converted = await ConvertAndImportIndividualImagesAsync(imageFiles, rootDir);
                        importedScores.AddRange(converted);
                    }
                }
                else if (imageFiles.Count == 1)
                {
                    // 1 seule image acceptée -> conversion en PDF dans la bibliothèque
                    var converted = await ConvertAndImportIndividualImagesAsync(imageFiles, rootDir);
                    importedScores.AddRange(converted);
                }

                // Traiter les éventuels fichiers PDF (qu'ils soient seuls ou accompagnés d'images)
                if (pdfFiles.Any())
                {
                    var pdfImported = await ProcessPdfFilesAsync(pdfFiles, rootDir);
                    importedScores.AddRange(pdfImported);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ImportService] Erreur lors de l'import : {ex.Message}");
                await Shell.Current.DisplayAlertAsync("Erreur d'import", $"Une erreur est survenue lors de l'import : {ex.Message}", "OK");
            }

            return importedScores;
        }

        private async Task<List<Score>> ConvertAndImportIndividualImagesAsync(List<FileResult> imageFiles, string rootDir)
        {
            var results = new List<Score>();
            var cacheDir = FileSystem.CacheDirectory;
            if (!Directory.Exists(cacheDir)) Directory.CreateDirectory(cacheDir);

            ConversionStateChanged?.Invoke(true, imageFiles.Count > 1 
                ? $"Conversion de {imageFiles.Count} images en cours..." 
                : "Conversion de l'image en PDF...");

            try
            {
                for (int i = 0; i < imageFiles.Count; i++)
                {
                    var img = imageFiles[i];
                    var rawName = Path.GetFileNameWithoutExtension(img.FileName ?? img.FullPath);
                    if (string.IsNullOrWhiteSpace(rawName)) rawName = "Partition";

                    if (imageFiles.Count > 1)
                    {
                        ConversionStateChanged?.Invoke(true, $"Conversion ({i + 1}/{imageFiles.Count}) :\n\"{rawName}\"...");
                    }
                    else
                    {
                        ConversionStateChanged?.Invoke(true, $"Conversion en cours :\n\"{rawName}\"...");
                    }

                    var ext = Path.GetExtension(img.FileName ?? img.FullPath);
                    if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
                    var cleanExt = new string(ext.Where(c => char.IsLetterOrDigit(c) || c == '.').ToArray());
                    if (string.IsNullOrWhiteSpace(cleanExt)) cleanExt = ".jpg";

                    var tempPath = Path.Combine(cacheDir, $"{Guid.NewGuid()}{cleanExt}");
                    try
                    {
                        using (var src = await img.OpenReadAsync())
                        using (var dst = File.Create(tempPath))
                        {
                            await src.CopyToAsync(dst);
                        }

                        var sanitized = SanitizeFileName(rawName);
                        var localPdfPath = GetUniqueFilePath(rootDir, sanitized, ".pdf");

                        await _pdfService.ConvertImagesToPdfAsync(new[] { tempPath }, localPdfPath);

                        var score = new Score
                        {
                            Title = rawName,
                            FilePath = _settingsService.GetRelativePath(localPdfPath),
                            Type = ScoreType.PDF,
                            DateAdded = DateTime.Now,
                            PageCount = 1
                        };

                        await _databaseService.SaveScoreAsync(score);
                        results.Add(score);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ImportService] Erreur conversion image {img.FileName} : {ex.Message}");
                        await Shell.Current.DisplayAlertAsync("Erreur de conversion", $"Impossible de convertir {Path.GetFileName(img.FileName ?? "l'image")} en PDF : {ex.Message}", "OK");
                    }
                    finally
                    {
                        try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
                    }
                }
            }
            finally
            {
                ConversionStateChanged?.Invoke(false, null);
            }

            return results;
        }

        private static bool CanBeLinkedExternally(FileResult fileResult)
        {
            if (string.IsNullOrWhiteSpace(fileResult.FullPath)) return false;
            if (fileResult.FullPath.StartsWith("content:", StringComparison.OrdinalIgnoreCase)) return false;
            if (!File.Exists(fileResult.FullPath)) return false;

            // Fichier situé dans un répertoire temporaire ou de cache de l'application ou d'un fournisseur cloud
            string cacheDir = FileSystem.CacheDirectory;
            if (!string.IsNullOrEmpty(cacheDir) && fileResult.FullPath.StartsWith(cacheDir, StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        private async Task<List<Score>> ProcessPdfFilesAsync(List<FileResult> pdfFiles, string rootDir)
        {
            var imported = new List<Score>();
            var filesToProcess = new List<(FileResult File, bool AlreadyInRoot)>();

            foreach (var result in pdfFiles)
            {
                var fullPath = result.FullPath;
                bool isAlreadyInRoot = !string.IsNullOrEmpty(rootDir) 
                    && !string.IsNullOrEmpty(fullPath) 
                    && fullPath.StartsWith(rootDir, StringComparison.OrdinalIgnoreCase);

                if (!isAlreadyInRoot)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(fullPath) && File.Exists(fullPath))
                        {
                            var fileInfoSource = new FileInfo(fullPath);
                            long sourceLength = fileInfoSource.Length;
                            string targetFileName = result.FileName ?? Path.GetFileName(fullPath);

                            string directPath = Path.Combine(rootDir, targetFileName);
                            if (File.Exists(directPath) && new FileInfo(directPath).Length == sourceLength)
                            {
                                isAlreadyInRoot = true;
                                filesToProcess.Add((new FileResult(directPath), true));
                                continue;
                            }

                            string? foundPath = FindFileRecursively(rootDir, targetFileName, sourceLength);
                            if (foundPath != null)
                            {
                                isAlreadyInRoot = true;
                                filesToProcess.Add((new FileResult(foundPath), true));
                                continue;
                            }
                        }
                    }
                    catch { /* Ignore les fichiers virtuels/cloud non présents directement sur disque */ }
                }

                filesToProcess.Add((result, isAlreadyInRoot));
            }

            // Les fichiers provenant de Google Drive ou d'autres services cloud (content://, cache, ou flux distant)
            // n'ont pas de chemin local permanent : ils doivent obligatoirement être copiés dans la bibliothèque.
            var linkableExternalFiles = filesToProcess
                .Where(f => !f.AlreadyInRoot && CanBeLinkedExternally(f.File))
                .ToList();

            string? globalAction = null;

            if (linkableExternalFiles.Count > 0)
            {
                string savedMode = _settingsService.DefaultScoreImportMode; // "Copy", "Link", "Ask"

                if (savedMode == "Link")
                {
                    // Mode Liaison directe sans message intempestif
                    globalAction = linkableExternalFiles.Count > 1
                        ? "Lier tous les fichiers originaux (Externe)"
                        : "Lier le fichier original (Externe)";
                }
                else if (savedMode == "Ask")
                {
                    // Mode interactif : solliciter le choix de l'utilisateur
                    if (linkableExternalFiles.Count == 1)
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
                            $"Organisation de la bibliothèque ({linkableExternalFiles.Count} fichiers)",
                            "Annuler",
                            null,
                            "Copier tous les fichiers vers la bibliothèque (Conseillé)",
                            "Lier tous les fichiers originaux (Externe)",
                            "Choisir au cas par cas");
                    }

                    if (globalAction == "Annuler" || globalAction == null)
                    {
                        return imported;
                    }
                }
                else
                {
                    // Mode "Copy" (valeur par défaut recommandée) : copie directe automatique
                    globalAction = linkableExternalFiles.Count > 1
                        ? "Copier tous les fichiers vers la bibliothèque (Conseillé)"
                        : "Copier vers la bibliothèque (Conseillé)";
                }
            }

            try
            {
                for (int i = 0; i < filesToProcess.Count; i++)
                {
                    var (fileResult, isAlreadyInRoot) = filesToProcess[i];
                    string finalStoredPath;

                    // Nom de fichier et titre nettoyés et sécurisés pour les flux cloud (Google Drive, etc.)
                    string rawFileName = !string.IsNullOrWhiteSpace(fileResult.FileName)
                        ? fileResult.FileName
                        : Path.GetFileName(fileResult.FullPath ?? "");

                    if (string.IsNullOrWhiteSpace(rawFileName))
                        rawFileName = "Partition.pdf";

                    string rawTitle = Path.GetFileNameWithoutExtension(rawFileName);
                    if (string.IsNullOrWhiteSpace(rawTitle))
                        rawTitle = "Partition";

                    string ext = Path.GetExtension(rawFileName);
                    if (string.IsNullOrWhiteSpace(ext) || !ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        ext = ".pdf";
                    }

                    if (isAlreadyInRoot && !string.IsNullOrEmpty(fileResult.FullPath))
                    {
                        finalStoredPath = _settingsService.GetRelativePath(fileResult.FullPath);
                    }
                    else
                    {
                        bool canLink = CanBeLinkedExternally(fileResult);
                        string? fileAction = globalAction;

                        if (canLink && globalAction == "Choisir au cas par cas")
                        {
                            fileAction = await Shell.Current.DisplayActionSheetAsync(
                                $"Fichier : {rawFileName}",
                                "Passer ce fichier",
                                null,
                                "Copier vers la bibliothèque (Conseillé)",
                                "Lier le fichier original (Externe)");
                        }

                        // Si le fichier vient de Google Drive / Cloud ou que l'action est Copier
                        if (!canLink || fileAction == null || fileAction.StartsWith("Copier"))
                        {
                            ConversionStateChanged?.Invoke(true, filesToProcess.Count > 1
                                ? $"Importation ({i + 1}/{filesToProcess.Count}) :\n\"{rawTitle}\"..."
                                : $"Importation en cours :\n\"{rawTitle}\"...");

                            var sanitizedBase = SanitizeFileName(rawTitle);
                            if (string.IsNullOrWhiteSpace(sanitizedBase)) sanitizedBase = "Partition";
                            var localFilePath = GetUniqueFilePath(rootDir, sanitizedBase, ext);

                            // Téléchargement sécurisé via fichier temporaire pour éviter tout fichier corrompu à 0 octet
                            var tempPath = Path.Combine(FileSystem.CacheDirectory, $"{Guid.NewGuid()}.tmp");
                            try
                            {
                                using (var stream = await fileResult.OpenReadAsync())
                                using (var fileStream = File.Create(tempPath))
                                {
                                    await stream.CopyToAsync(fileStream);
                                }

                                if (File.Exists(tempPath) && new FileInfo(tempPath).Length > 0)
                                {
                                    File.Move(tempPath, localFilePath);
                                    finalStoredPath = _settingsService.GetRelativePath(localFilePath);
                                }
                                else
                                {
                                    throw new IOException($"Le flux téléchargé pour '{rawTitle}' est vide ou inaccessible.");
                                }
                            }
                            finally
                            {
                                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
                            }
                        }
                        else if (canLink && fileAction.StartsWith("Lier"))
                        {
                            finalStoredPath = fileResult.FullPath!;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    int pageCount = await _pdfService.GetPdfPageCountAsync(_settingsService.GetAbsolutePath(finalStoredPath));

                    var score = new Score
                    {
                        Title = rawTitle,
                        FilePath = finalStoredPath,
                        Type = ScoreType.PDF,
                        DateAdded = DateTime.Now,
                        PageCount = pageCount > 0 ? pageCount : 1
                    };

                    await _databaseService.SaveScoreAsync(score);
                    imported.Add(score);
                }
            }
            finally
            {
                ConversionStateChanged?.Invoke(false, null);
            }

            return imported;
        }

        private static bool IsImageFile(FileResult result)
        {
            if (result == null) return false;
            if (!string.IsNullOrEmpty(result.ContentType) && result.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return true;
            var name = result.FileName ?? result.FullPath ?? "";
            var ext = Path.GetExtension(name)?.ToLowerInvariant() ?? "";
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".webp" || ext == ".bmp" || ext == ".gif";
        }

        private static bool IsPdfFile(FileResult result)
        {
            if (result == null) return false;
            if (!string.IsNullOrEmpty(result.ContentType) && result.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                return true;
            var name = result.FileName ?? result.FullPath ?? "";
            var ext = Path.GetExtension(name)?.ToLowerInvariant() ?? "";
            if (ext == ".pdf") return true;

            // Si ce n'est pas une image et que l'extension est absente ou générique (fichiers Google Drive sans extension explicite),
            // on traite comme un PDF sélectionné dans le picker
            return !IsImageFile(result);
        }

        private static string SanitizeFileName(string name)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var clean = new string(name.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
            return clean.Replace(" ", "_");
        }

        private static string GetUniqueFilePath(string directory, string baseName, string extension)
        {
            var localPath = Path.Combine(directory, $"{baseName}{extension}");
            int counter = 1;
            while (File.Exists(localPath))
            {
                localPath = Path.Combine(directory, $"{baseName}_{counter}{extension}");
                counter++;
            }
            return localPath;
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

                foreach (var subDir in Directory.EnumerateDirectories(dir))
                {
                    var found = FindFileRecursively(subDir, fileName, size);
                    if (found != null) return found;
                }
            }
            catch { }
            return null;
        }

        private class NaturalComparer : IComparer<string>
        {
            public int Compare(string? x, string? y) => NaturalStringComparer.Compare(x, y);
        }
    }
}
